using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace BetterRomance.HarmonyPatches
{

    [HarmonyPatch(typeof(RitualOutcomeComp_PawnAge), nameof(RitualOutcomeComp_PawnAge.QualityOffset))]
    public static class RitualOutcomeComp_PawnAge_QualityOffset
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) => instructions.ChildBirthAgeCurveTranspiler(OpCodes.Ldarg_1, true);
    }

    [HarmonyPatch]
    public static class RitualOutcomeComp_PawnAge_Desc
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(RitualOutcomeComp_PawnAge), nameof(RitualOutcomeComp_PawnAge.GetDesc));
#if v1_4
            yield return AccessTools.Method(typeof(RitualOutcomeComp_PawnAge), nameof(RitualOutcomeComp_PawnAge.GetExpectedOutcomeDesc));
#else
            yield return AccessTools.Method(typeof(RitualOutcomeComp_PawnAge), nameof(RitualOutcomeComp_PawnAge.GetQualityFactor));
#endif
        }
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) => instructions.ChildBirthAgeCurveTranspiler(OpCodes.Ldloc_0);
    }
}
