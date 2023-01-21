using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using HarmonyLib;
using System.Reflection.Emit;
using System.Reflection;
using UnityEngine;
using System.Text;
using System;

namespace BetterRomance.HarmonyPatches
{
    [HarmonyPatch(typeof(SocialCardUtility), "CanDrawTryRomance")]
    public static class SocialCardUtility_CanDrawTryRomance
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.Is(OpCodes.Ldc_R4, 16f))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.MinAgeForSex));
                }
                else
                {
                    yield return instruction;
                }
            }
        }
    }

    //Adds a button for ordered hookups to the social card
    [HarmonyPatch(typeof(SocialCardUtility), nameof(SocialCardUtility.DrawRelationsAndOpinions))]
    public static class SocialCardUtility_DrawRelationsAndOpinions
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo EndGroup = AccessTools.Method(typeof(Widgets), nameof(Widgets.EndGroup));
            foreach (CodeInstruction code in instructions)
            {
                if (code.Calls(EndGroup))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return CodeInstruction.Call(typeof(SocialCardUtility_DrawRelationsAndOpinions), "SocialCardHelper");
                }
                yield return code;
            }
        }

        private static void SocialCardHelper(Pawn pawn, Rect rect)
        {
            Vector2 ButtonSize = (Vector2)AccessTools.Field(typeof(SocialCardUtility), "RoleChangeButtonSize").GetValue(null);
            if (HookupUtility.CanDrawTryHookup(pawn))
            {
                DrawTryHookup(new Rect(rect.width - 150f - ButtonSize.x - 5f, rect.height - ButtonSize.y, ButtonSize.x, ButtonSize.y), pawn);
            }
        }

        private static void DrawTryHookup(Rect buttonRect, Pawn pawn)
        {
            Color color = GUI.color;
            bool isTryHookupOnCooldown = pawn.CheckForPartnerComp().IsOrderedHookupOnCooldown;
            AcceptanceReport canDoHookup = HookupUtility.HookupEligible(pawn, initiator: true, forOpinionExplanation: false);
            List<FloatMenuOption> list = canDoHookup.Accepted ? HookupOptions(pawn) : null;
            GUI.color = (!canDoHookup.Accepted || list.NullOrEmpty() || isTryHookupOnCooldown) ? ColoredText.SubtleGrayColor : Color.white;
            if (Widgets.ButtonText(buttonRect, "WBR.TryHookupButtonLabel".Translate() + "..."))
            {
                if (isTryHookupOnCooldown)
                {
                    int numTicks = pawn.CheckForPartnerComp().orderedHookupTick - Find.TickManager.TicksGame;
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

        private static List<FloatMenuOption> HookupOptions(Pawn romancer)
        {
            List<(float, FloatMenuOption)> eligibleList = new List<(float, FloatMenuOption)>();
            List<FloatMenuOption> ineligibleList = new List<FloatMenuOption>();

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

    //Adds an explanation of hookup chance to the social card tooltip
    [HarmonyPatch(typeof(SocialCardUtility), "GetPawnRowTooltip")]
    public static class SocialCardUtility_GetPawnRowTooltip
    {
        
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
        {
            MethodInfo RomanceExplanation = AccessTools.Method(typeof(SocialCardUtility), "RomanceExplanation");
            FieldInfo otherPawn = AccessTools.Field(AccessTools.TypeByName("RimWorld.SocialCardUtility.CachedSocialTabEntry"), "otherPawn");
            Label newLabel = ilg.DefineLabel();
            Label oldLabel = ilg.DefineLabel();
            LocalBuilder text = ilg.DeclareLocal(typeof(string));
            bool startFound = false;

            foreach (CodeInstruction code in instructions)
            {
                if (startFound && code.opcode == OpCodes.Brtrue_S)
                {
                    oldLabel = (Label)code.operand;
                    code.operand = newLabel;
                }

                yield return code;

                if (startFound && code.opcode == OpCodes.Pop)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_1) { labels = new List<Label> { newLabel } };
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return CodeInstruction.LoadField(AccessTools.Inner(typeof(SocialCardUtility), "CachedSocialTabEntry"), "otherPawn");
                    yield return CodeInstruction.Call(typeof(SocialCardUtility_GetPawnRowTooltip), nameof(SocialCardUtility_GetPawnRowTooltip.HookupExplanation));
                    yield return new CodeInstruction(OpCodes.Stloc, text);
                    yield return new CodeInstruction(OpCodes.Ldloc, text);
                    yield return CodeInstruction.Call(typeof(GenText), nameof(GenText.NullOrEmpty));
                    yield return new CodeInstruction(OpCodes.Brtrue, oldLabel);
                    yield return new CodeInstruction(OpCodes.Ldloc_0);
                    yield return new CodeInstruction(OpCodes.Ldloc, text);
                    yield return CodeInstruction.Call(typeof(StringBuilder), nameof(StringBuilder.AppendLine), parameters: new Type[] { typeof(string) });
                    yield return new CodeInstruction(OpCodes.Pop);

                    startFound = false;
                }
                
                if (code.Calls(RomanceExplanation))
                {
                    startFound = true;
                }
            }
        }

        public static string HookupExplanation(Pawn initiator, Pawn target)
        {
            if (!HookupUtility.CanDrawTryHookup(initiator))
            {
                return null;
            }
            var ar = HookupUtility.HookupEligiblePair(initiator, target, true);
            if (!ar.Accepted && ar.Reason.NullOrEmpty())
            {
                return null;
            }
            if (!ar.Accepted)
            {
                return "WBR.HookupChanceCant".Translate() + (" (" + ar.Reason + ")\n");
            }
            var text = new StringBuilder();
            float chance = HookupUtility.HookupSuccessChance(target, initiator);
            text.AppendLine(("WBR.HookupChance".Translate() + (": " + chance.ToStringPercent())).Colorize(ColoredText.TipSectionTitleColor));
            text.Append(HookupUtility.HookupFactors(initiator, target));
            return text.ToString();
        }
    }
}
