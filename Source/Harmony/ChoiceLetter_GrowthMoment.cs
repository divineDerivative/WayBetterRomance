using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace BetterRomance.HarmonyPatches
{
    [HarmonyPatch(typeof(ChoiceLetter_GrowthMoment), nameof(ChoiceLetter_GrowthMoment.MakeChoices))]
    public static class ChoiceLetter_GrowthMoment_MakeChoices
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            FieldInfo pawn = AccessTools.Field(typeof(ChoiceLetter_GrowthMoment), "pawn");
            foreach (CodeInstruction code in instructions)
            {
                if (code.LoadsConstant(13))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, pawn);
                    yield return new CodeInstruction(OpCodes.Ldc_I4_2);
                    yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.GetGrowthMoment));
                }
                else
                {
                    yield return code;
                }
            }
        }
    }

    [HarmonyPatch(typeof(ChoiceLetter_GrowthMoment), "CacheLetterText")]
    public static class ChoiceLetter_GrowthMoment_CacheLetterText
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            FieldInfo GrowthMomentAges = AccessTools.Field(typeof(GrowthUtility), nameof(GrowthUtility.GrowthMomentAges));

            List<CodeInstruction> codes = instructions.ToList();

            for (int i = 0; i < codes.Count; i++)
            {
                CodeInstruction code = codes[i];
                if (code.LoadsField(GrowthMomentAges))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return CodeInstruction.LoadField(typeof(ChoiceLetter_GrowthMoment), nameof(ChoiceLetter_GrowthMoment.pawn));
                    yield return new CodeInstruction(OpCodes.Ldc_I4_0);
                    yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.GetGrowthMoment));
                    i += 2;
                }
                else
                {
                    yield return code;
                }
            }
        }
    }
}
