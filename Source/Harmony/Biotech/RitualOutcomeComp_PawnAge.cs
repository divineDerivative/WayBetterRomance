using System.Collections.Generic;
using RimWorld;
using HarmonyLib;
using Verse;
using System.Reflection.Emit;
using System.Reflection;

namespace BetterRomance.HarmonyPatches
{

    [HarmonyPatch(typeof(RitualOutcomeComp_PawnAge), nameof(RitualOutcomeComp_PawnAge.QualityOffset))]
    public static class RitualOutcomeComp_PawnAge_QualityOffset
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            FieldInfo curve = AccessTools.Field(typeof(RitualOutcomeComp_Quality), nameof(RitualOutcomeComp_PawnAge.curve));
            foreach (CodeInstruction code in instructions)
            {
                if (code.LoadsField(curve))
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

    [HarmonyPatch(typeof(RitualOutcomeComp_PawnAge), nameof(RitualOutcomeComp_PawnAge.GetDesc))]
    public static class RitualOutcomeComp_PawnAge_GetDesc
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction code in instructions)
            {
                if (code.LoadsField(AccessTools.Field(typeof(RitualOutcomeComp_Quality), nameof(RitualOutcomeComp_PawnAge.curve))))
                {
                    yield return new CodeInstruction(OpCodes.Pop);
                    yield return new CodeInstruction(OpCodes.Ldloc_0);
                    yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.GetChildBirthAgeCurve));
                }
                else
                {
                    yield return code;
                }
            }
        }
    }

#if v1_4
    [HarmonyPatch(typeof(RitualOutcomeComp_PawnAge), nameof(RitualOutcomeComp_PawnAge.GetExpectedOutcomeDesc))]
#else
    [HarmonyPatch(typeof(RitualOutcomeComp_PawnAge), nameof(RitualOutcomeComp_PawnAge.GetQualityFactor))]
#endif
    public static class RitualOutcomeComp_PawnAge_GetExpectedOutcomeDesc
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction code in instructions)
            {
                if (code.LoadsField(AccessTools.Field(typeof(RitualOutcomeComp_Quality), nameof(RitualOutcomeComp_PawnAge.curve))))
                {
                    yield return new CodeInstruction(OpCodes.Pop);
                    yield return new CodeInstruction(OpCodes.Ldloc_0);
                    yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.GetChildBirthAgeCurve));
                }
                else
                {
                    yield return code;
                }
            }
        }
    }
}
