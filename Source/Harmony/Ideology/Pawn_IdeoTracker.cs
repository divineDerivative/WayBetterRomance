using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection.Emit;
using Verse;

namespace BetterRomance.HarmonyPatches
{
    [HarmonyPatch(typeof(Pawn_IdeoTracker), "CertaintyChangeFactor", MethodType.Getter)]
    public static class Pawn_IdeoTracker_CertaintyChangeFactor
    {
        public static void Prefix(ref SimpleCurve ___pawnAgeCertaintyCurve, Pawn ___pawn)
        {
            if (___pawn.def.race.Humanlike)
            {
                ___pawnAgeCertaintyCurve = null;
            }
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction code in instructions)
            {
                if (code.LoadsField(typeof(LifeStageDefOf), nameof(LifeStageDefOf.HumanlikeChild)))
                {
                    //get pawn out of the age tracker
                    yield return InfoHelper.AgeTrackerPawn.LoadField();
                    yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.ChildAge));
                }
                else if (code.LoadsField(typeof(LifeStageDefOf), nameof(LifeStageDefOf.HumanlikeAdult)))
                {
                    yield return InfoHelper.AgeTrackerPawn.LoadField();
                    yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.AdultAgeForLearning));
                }
                else if (code.Calls(typeof(Pawn_AgeTracker), nameof(Pawn_AgeTracker.LifeStageMinAge)))
                {
                    yield return new CodeInstruction(OpCodes.Conv_R4);
                }
                else
                {
                    yield return code;
                }
            }
        }
    }
}
