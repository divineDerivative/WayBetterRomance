using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Verse;

namespace BetterRomance.HarmonyPatches
{

    [HarmonyPatch(typeof(RitualOutcomeComp_PawnAge), nameof(RitualOutcomeComp_PawnAge.QualityOffset))]
    public static class RitualOutcomeComp_PawnAge_QualityOffset
    {
        //Consider whether to use the dynamic transpiler here, might add too much unnecessary complexity, and there's just this one use (so far)
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction code in instructions)
            {
                if (code.LoadsField(InfoHelper.RitualPawnAgeCurve))
                {
                    yield return CodeInstruction.LoadField(typeof(RitualOutcomeComp_PawnAge), nameof(RitualOutcomeComp_PawnAge.roleId));
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return CodeInstruction.Call(typeof(RitualOutcomeComp_PawnAge_QualityOffset), nameof(RolePawn));
                    yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.GetChildBirthAgeCurve));
                }
                else
                {
                    yield return code;
                }
            }
        }

        private static Pawn RolePawn(string roleID, LordJob_Ritual ritual)
        {
            return ritual.PawnWithRole(roleID);
        }
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
