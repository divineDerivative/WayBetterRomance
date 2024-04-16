using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Verse;

namespace BetterRomance.HarmonyPatches
{
    [HarmonyPatch(typeof(Pawn_IdeoTracker), "CertaintyChangeFactor", MethodType.Getter)]
    public static class Pawn_IdeoTracker_CertaintyChangeFactor
    {
        public static bool Prefix(ref SimpleCurve ___pawnAgeCertaintyCurve, Pawn ___pawn)
        {
            if (___pawn.def.defName != "PRFDrone")
            {
                ___pawnAgeCertaintyCurve = null;
            }
            return true;
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            FieldInfo HumanlikeChild = AccessTools.Field(typeof(LifeStageDefOf), nameof(LifeStageDefOf.HumanlikeChild));
            FieldInfo HumanlikeAdult = AccessTools.Field(typeof(LifeStageDefOf), nameof(LifeStageDefOf.HumanlikeAdult));
            MethodInfo LifeStageMinAge = AccessTools.Method(typeof(Pawn_AgeTracker), nameof(Pawn_AgeTracker.LifeStageMinAge));

            foreach (CodeInstruction code in instructions)
            {
                if (code.LoadsField(HumanlikeChild))
                {
                    //get pawn out of the age tracker
                    yield return CodeInstruction.LoadField(typeof(Pawn_AgeTracker), "pawn");
                    yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.ChildAge));
                }
                else if (code.LoadsField(HumanlikeAdult))
                {
                    yield return CodeInstruction.LoadField(typeof(Pawn_AgeTracker), "pawn");
                    yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.AdultAgeForLearning));
                }
                else if (code.Calls(LifeStageMinAge))
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
