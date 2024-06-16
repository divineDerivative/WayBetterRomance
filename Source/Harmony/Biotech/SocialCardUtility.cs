using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using UnityEngine;
using Verse;

namespace BetterRomance.HarmonyPatches
{
    //Use min age setting instead of static 16
    [HarmonyPatch(typeof(SocialCardUtility), "CanDrawTryRomance")]
    public static class SocialCardUtility_CanDrawTryRomance
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) => instructions.MinAgeForSexTranspiler(OpCodes.Ldarg_0);

        public static void Postfix(Pawn pawn, ref bool __result)
        {
            __result = __result && !pawn.DroneCheck();
        }
    }

    //Adds a button for ordered hookups to the social card
    [HarmonyPatch(typeof(SocialCardUtility), nameof(SocialCardUtility.DrawRelationsAndOpinions))]
    [HarmonyBefore(["Dra.RimderRomanceControl"])]
    public static class SocialCardUtility_DrawRelationsAndOpinions
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction code in instructions)
            {
                if (code.Calls(typeof(Widgets), nameof(Widgets.EndGroup)))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return CodeInstruction.Call(typeof(SocialCardUtility_DrawRelationsAndOpinions), nameof(SocialCardHelper));
                }
                yield return code;
                if (code.Calls(typeof(SocialCardUtility), "CanDrawTryRomance"))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return CodeInstruction.Call(typeof(HookupUtility), nameof(HookupUtility.CanDrawTryHookup));
                    yield return new CodeInstruction(OpCodes.Or);
                }
            }
        }

        private static void SocialCardHelper(Pawn pawn, Rect rect)
        {
            Vector2 ButtonSize = (Vector2)AccessTools.Field(typeof(SocialCardUtility), "RoleChangeButtonSize").GetValue(null);
            float padding = (float)AccessTools.Field(typeof(SocialCardUtility), "RowLeftRightPadding").GetValue(null);
            if (HookupUtility.CanDrawTryHookup(pawn))
            {
                bool romanceDrawn = (bool)AccessTools.Method(typeof(SocialCardUtility), "CanDrawTryRomance").Invoke(null, [pawn]);
                //Adjust x position based on if romance button is present
                float width = romanceDrawn ? rect.width - 150f - ButtonSize.x + padding : rect.width - 150f + (padding * 2);
                DrawTryHookup(new Rect(width, rect.height - ButtonSize.y, ButtonSize.x, ButtonSize.y), pawn);
            }
        }

        private static void DrawTryHookup(Rect buttonRect, Pawn pawn)
        {
            Color color = GUI.color;
            bool isTryHookupOnCooldown = pawn.CheckForComp<Comp_PartnerList>().IsOrderedHookupOnCooldown;
            AcceptanceReport canDoHookup = HookupUtility.HookupEligible(pawn, initiator: true);
            bool incapacitated = pawn.IsIncapable(out string reason);
            List<FloatMenuOption> list = canDoHookup.Accepted ? HookupOptions(pawn) : null;
            GUI.color = (!canDoHookup.Accepted || list.NullOrEmpty() || isTryHookupOnCooldown || incapacitated) ? ColoredText.SubtleGrayColor : Color.white;
            if (Widgets.ButtonText(buttonRect, "WBR.TryHookupButtonLabel".Translate() + "..."))
            {
                if (isTryHookupOnCooldown)
                {
                    int numTicks = pawn.CheckForComp<Comp_PartnerList>().orderedHookupTick - Find.TickManager.TicksGame;
                    Messages.Message("WBR.CantHookupInitiateMessageCooldown".Translate(pawn, numTicks.ToStringTicksToPeriod()), MessageTypeDefOf.RejectInput, historical: false);
                    return;
                }
                if (!canDoHookup.Accepted)
                {
                    if (!canDoHookup.Reason.NullOrEmpty())
                    {
                        Messages.Message(canDoHookup.Reason, MessageTypeDefOf.RejectInput, historical: false);
                    }
                    return;
                }
                if (incapacitated)
                {
                    Messages.Message("WBR.CantHookupInitiateBusy".Translate(pawn) + reason, MessageTypeDefOf.RejectInput, false);
                    return;
                }
                if (list.NullOrEmpty())
                {
                    Messages.Message("WBR.TryHookupNoOptsMessage".Translate(pawn), MessageTypeDefOf.RejectInput, historical: false);
                }
                else
                {
                    Find.WindowStack.Add(new FloatMenu(list));
                }
            }
            GUI.color = color;
        }

        /// <summary>
        /// Generates a list of <see cref="Pawn"/>s that <paramref name="romancer"/> can try to hook up with
        /// </summary>
        /// <param name="romancer"></param>
        /// <returns></returns>
        private static List<FloatMenuOption> HookupOptions(Pawn romancer)
        {
            List<(float, FloatMenuOption)> eligibleList = new();
            List<FloatMenuOption> ineligibleList = new();

            foreach (Pawn p in RomanceUtilities.GetAllSpawnedHumanlikesOnMap(romancer.Map))
            {
                if (HookupUtility.HookupOption(romancer, p, out FloatMenuOption option, out float chance))
                {
                    eligibleList.Add((chance, option));
                }
                else if (option != null)
                {
                    ineligibleList.Add(option);
                }
            }
            return (from pair in eligibleList
                    orderby pair.Item1 descending
                    select pair.Item2).Concat(ineligibleList.OrderBy((FloatMenuOption opt) => opt.Label)).ToList();
        }
    }

    //Adds an explanation of ordered hookup acceptance chance to the social card tooltip
    [HarmonyPatch(typeof(SocialCardUtility), "GetPawnRowTooltip")]
    public static class SocialCardUtility_GetPawnRowTooltip
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
        {
            Label newLabel = ilg.DefineLabel();
            bool insert = false;

            foreach (CodeInstruction code in instructions)
            {
                if (insert && code.Branches(out _))
                {
                    code.operand = newLabel;
                }

                yield return code;

                if (insert && code.opcode == OpCodes.Pop)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_1).WithLabels(newLabel);
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return CodeInstruction.LoadField(AccessTools.Inner(typeof(SocialCardUtility), "CachedSocialTabEntry"), "otherPawn");
                    yield return new CodeInstruction(OpCodes.Ldloca, 0);
                    yield return CodeInstruction.Call(typeof(SocialCardUtility_GetPawnRowTooltip), nameof(PawnRowTooltipHelper));
                    insert = false;
                }

                if (code.Calls(typeof(SocialCardUtility), "RomanceExplanation"))
                {
                    insert = true;
                }
            }
        }

        private static void PawnRowTooltipHelper(Pawn initiator, Pawn target, ref StringBuilder stringBuilder)
        {
            string text = HookupExplanation(initiator, target);
            if (!text.NullOrEmpty())
            {
                stringBuilder.AppendLine(text);
            }
        }

        private static string HookupExplanation(Pawn initiator, Pawn target)
        {
            if (!HookupUtility.CanDrawTryHookup(initiator))
            {
                return null;
            }
            AcceptanceReport ar = HookupUtility.HookupEligiblePair(initiator, target, forOpinionExplanation: true);
            if (!ar.Accepted && ar.Reason.NullOrEmpty())
            {
                return null;
            }
            if (!ar.Accepted)
            {
                return "WBR.HookupChanceCant".Translate() + (" (" + ar.Reason + ")\n");
            }
            StringBuilder text = new();
            text.AppendLine(("WBR.HookupChance".Translate() + (": " + HookupUtility.HookupSuccessChance(target, initiator, ordered: true, forTooltip: true).ToStringPercent())).Colorize(ColoredText.TipSectionTitleColor));
            text.Append(HookupUtility.HookupFactors(initiator, target));
            return text.ToString();
        }
    }

    [HarmonyPatch(typeof(SocialCardUtility), "DrawTryRomance")]
    public static class SocialCardUtility_DrawTryRomance
    {
        //This is to prevent drawing the button since I had to co-opt the bool that usually prevents it
        public static bool Prefix(Pawn pawn)
        {
            return (bool)AccessTools.Method(typeof(SocialCardUtility), "CanDrawTryRomance").Invoke(null, [pawn]);
        }
    }

    //Lets the PartnerFactor patch know I don't care about the result of WillPawnContinue
    [HarmonyPatch(typeof(SocialCardUtility), "RomanceExplanation")]
    public static class SocialCardUtility_RomanceExplanation
    {
        public static void Prefix()
        {
            InteractionWorker_RomanceAttempt_PartnerFactor.forTooltip = true;
        }
    }
}