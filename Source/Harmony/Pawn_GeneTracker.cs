using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Verse;

namespace BetterRomance.HarmonyPatches
{
    [HarmonyPatch(typeof(Pawn_GeneTracker), "Notify_GenesChanged")]
    public static class Pawn_GeneTracker_Notify_GenesChanged
    {
        public static void Postfix(GeneDef addedOrRemovedGene, Pawn_GeneTracker __instance)
        {
            if (addedOrRemovedGene.defName == "DiseaseFree")
            {
                SettingsUtilities.CachedNonSenescentPawns.Remove(__instance.pawn);
                SettingsUtilities.CachedSenescentPawns.Remove(__instance.pawn);
            }
        }
    }

    [HarmonyPatch(typeof(Pawn_GeneTracker), nameof(Pawn_GeneTracker.BiologicalAgeTickFactor), MethodType.Getter)]
    public static class Pawn_GeneTracker_BiologicalAgeTickFactor
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            FieldInfo biologicalAgeTickFactorFromAgeCurve = AccessTools.Field(typeof(GeneDef), nameof(GeneDef.biologicalAgeTickFactorFromAgeCurve));

            foreach (CodeInstruction code in instructions)
            {
                if (code.LoadsField(biologicalAgeTickFactorFromAgeCurve))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return CodeInstruction.LoadField(typeof(Pawn_GeneTracker), nameof(Pawn_GeneTracker.pawn));
                    yield return CodeInstruction.Call(typeof(Pawn_GeneTracker_BiologicalAgeTickFactor), nameof(CurveHelper));
                }
                else
                {
                    yield return code;
                }
            }
        }

        private static SimpleCurve CurveHelper(GeneDef geneDef, Pawn pawn)
        {
            if (geneDef.defName == "Ageless")
            {
                return
                [
                    new CurvePoint(pawn.ageTracker.AdultMinAge, 1f),
                    new CurvePoint(SettingsUtilities.AdultAgeForLearning(pawn) + 0.5f, 0f)
                ];
            }

            return geneDef.biologicalAgeTickFactorFromAgeCurve;
        }
    }
}
