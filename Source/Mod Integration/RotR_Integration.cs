using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using HarmonyLib;
using RomanceOnTheRim;
using UnityEngine;

namespace BetterRomance
{
    public class RotR_Integration
    {
        //This is the cheating section of the original postfix patches, separated out so it can be applied to hookups and dates
        public static float RotRCheatChanceModifier(Pawn pawn)
        {
            if (pawn.HasLoverFastFromRomanceNeed())
            {
                if (pawn.Ideo.HasPrecept(CustomPreceptDefOf.RomanceOnTheRim_Cheat_Forbidden))
                {
                    return 0f;
                }
                if (pawn.Ideo.HasPrecept(CustomPreceptDefOf.RomanceOnTheRim_Cheat_Discouraged))
                {
                    return 0.5f;
                }
                if (pawn.Ideo.HasPrecept(CustomPreceptDefOf.RomanceOnTheRim_Cheat_Encouraged))
                {
                    return 1.5f;
                }
            }
            return 1f;
        }

        public static string RotRCheatingPreceptExplanation(Pawn pawn)
        {
            string result = string.Empty;
            if (pawn.Ideo.HasPrecept(CustomPreceptDefOf.RomanceOnTheRim_Cheat_Forbidden))
            {
                result = PreceptExplanation(CustomPreceptDefOf.RomanceOnTheRim_Cheat_Forbidden, 0f);
            }
            else if (pawn.Ideo.HasPrecept(CustomPreceptDefOf.RomanceOnTheRim_Cheat_Discouraged))
            {
                result = PreceptExplanation(CustomPreceptDefOf.RomanceOnTheRim_Cheat_Discouraged, 0.5f);
            }
            else if (pawn.Ideo.HasPrecept(CustomPreceptDefOf.RomanceOnTheRim_Cheat_Encouraged))
            {
                result = PreceptExplanation(CustomPreceptDefOf.RomanceOnTheRim_Cheat_Encouraged, 1.5f);
            }
            return result;
        }

        private static string PreceptExplanation(PreceptDef preceptDef, float value)
        {
            return (string)HelperClasses.RotRPreceptExplanation.Invoke(null, new object[] { preceptDef, value });
        }
    }

    namespace HarmonyPatches
    {
        public static class RotRPatches
        {
            public static void PatchRotR(this Harmony harmony)
            {
                if (ModsConfig.IdeologyActive)
                {
                    //Remove the original patch
                    harmony.Unpatch(typeof(InteractionWorker_RomanceAttempt).GetMethod("RandomSelectionWeight"), typeof(HarmonyPatch_InteractionWorker_RomanceAttempt).GetMethod("RandomSelectionWeightPostfix"));
                    //Add my version of the patch
                    harmony.Patch(typeof(InteractionWorker_RomanceAttempt).GetMethod("RandomSelectionWeight"), postfix: new HarmonyMethod(typeof(RotRPatches).GetMethod("RomanceAttempt_RandomSelectionWeight_Patch")));

                    harmony.Unpatch(typeof(InteractionWorker_RomanceAttempt).GetMethod("SuccessChance"), typeof(HarmonyPatch_InteractionWorker_RomanceAttempt).GetMethod(nameof(HarmonyPatch_InteractionWorker_RomanceAttempt.SuccessChancePostfix)));
                    harmony.Patch(typeof(InteractionWorker_RomanceAttempt).GetMethod("SuccessChance"), postfix: new HarmonyMethod(typeof(RotRPatches).GetMethod(nameof(SuccessChancePostfix))));

                    harmony.Patch(typeof(HarmonyPatch_SocialCardUtility_RomanceExplanation).GetMethod("AddPreceptExplanation"), transpiler: new HarmonyMethod(typeof(RotRPatches).GetMethod(nameof(AddPreceptExplanationTranspiler))));
                }
            }

            //This is the same as the original patch with the cheating section removed
            public static void RomanceAttempt_RandomSelectionWeight_Patch(ref float __result, Pawn initiator, Pawn recipient)
            {
                if (__result == 0f || initiator.Ideo == null)
                {
                    return;
                }
                if (initiator.Ideo.HasPrecept(CustomPreceptDefOf.RomanceOnTheRim_RomanceAttempt_Arranged))
                {
                    __result = 0f;
                }
                else if (initiator.Ideo.HasPrecept(CustomPreceptDefOf.RomanceOnTheRim_RomanceAttempt_Forbidden))
                {
                    __result = 0f;
                }
                else if (initiator.Ideo.HasPrecept(CustomPreceptDefOf.RomanceOnTheRim_RomanceAttempt_Discouraged))
                {
                    __result = Mathf.Clamp01(__result * 0.5f);
                }
                else if (initiator.Ideo.HasPrecept(CustomPreceptDefOf.RomanceOnTheRim_RomanceAttempt_Encouraged))
                {
                    __result = Mathf.Clamp01(__result * 1.5f);
                }
            }

            //This is the same as the original patch with the cheating section removed
            public static void SuccessChancePostfix(ref float __result, Pawn initiator, Pawn recipient)
            {
                if (recipient.Ideo == null)
                {
                    return;
                }
                if (recipient.HasLoverFastFromRomanceNeed())
                {
                    if (initiator.Ideo.HasPrecept(CustomPreceptDefOf.RomanceOnTheRim_RomanceAttempt_Arranged) && recipient.Ideo.HasPrecept(CustomPreceptDefOf.RomanceOnTheRim_RomanceAttempt_Arranged))
                    {
                        return;
                    }
                }
                if (recipient.Ideo.HasPrecept(CustomPreceptDefOf.RomanceOnTheRim_RomanceAttempt_Forbidden))
                {
                    __result = 0f;
                }
                else if (recipient.Ideo.HasPrecept(CustomPreceptDefOf.RomanceOnTheRim_RomanceAttempt_Discouraged))
                {
                    __result = Mathf.Clamp01(__result * 0.5f);
                }
                else if (recipient.Ideo.HasPrecept(CustomPreceptDefOf.RomanceOnTheRim_RomanceAttempt_Encouraged))
                {
                    __result = Mathf.Clamp01(__result * 1.5f);
                }
            }

            //Removes the check against the romancer's romance need
            public static IEnumerable<CodeInstruction> AddPreceptExplanationTranspiler(IEnumerable<CodeInstruction> instructions)
            {
                bool skip = false;
                foreach (CodeInstruction code in instructions)
                {
                    if (code.IsLdarg(1))
                    {
                        skip = true;
                    }

                    if (!skip)
                    {
                        yield return code;
                    }

                    if (skip && code.Branches(out _))
                    {
                        skip = false;
                    }
                }
            }
        }
    }
}
