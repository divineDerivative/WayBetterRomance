using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using HarmonyLib;
using System.Reflection.Emit;
using System.Reflection;
using UnityEngine;

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
            if (CanDrawTryHookup(pawn))
            {
                DrawTryHookup(new Rect(rect.width - 150f - ButtonSize.x - 5f, rect.height - ButtonSize.y, ButtonSize.x, ButtonSize.y), pawn);
            }
        }

        private static bool CanDrawTryHookup(Pawn pawn)
        {
            if (pawn.ageTracker.AgeBiologicalYearsFloat >= pawn.MinAgeForSex() && pawn.Spawned)
            {
                return pawn.IsFreeColonist;
            }
            return false;
        }

        private static void DrawTryHookup(Rect buttonRect, Pawn pawn)
        {
            Color color = GUI.color;
            bool isTryRomanceOnCooldown = false; //pawn.relations.IsTryRomanceOnCooldown;
            AcceptanceReport canDoHookup = HookupUtility.HookupEligible(pawn, initiator: true, forOpinionExplanation: false);
            List<FloatMenuOption> list = canDoHookup.Accepted ? HookupOptions(pawn) : null;
            GUI.color = (!canDoHookup.Accepted || list.NullOrEmpty() || isTryRomanceOnCooldown) ? ColoredText.SubtleGrayColor : Color.white;
            if (Widgets.ButtonText(buttonRect, "WBR.TryHookupButtonLabel".Translate() + "..."))
            {
                //Need to figure this out
                if (isTryRomanceOnCooldown)
                {
                    int numTicks = pawn.relations.romanceEnableTick - Find.TickManager.TicksGame;
                    Messages.Message("CantRomanceInitiateMessageCooldown".Translate(pawn, numTicks.ToStringTicksToPeriod()), MessageTypeDefOf.RejectInput, historical: false);
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
}
