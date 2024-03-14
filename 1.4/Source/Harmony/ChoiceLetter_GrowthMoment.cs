﻿using System.Collections.Generic;
using System.Linq;
using RimWorld;
using HarmonyLib;
using System.Reflection.Emit;
using System.Reflection;

namespace BetterRomance.HarmonyPatches
{
    [HarmonyPatch(typeof(ChoiceLetter_GrowthMoment), "MakeChoices")]
    public static class ChoiceLetter_GrowthMoment_MakeChoices
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            FieldInfo pawn = AccessTools.Field(typeof(ChoiceLetter_GrowthMoment), "pawn");
            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.Is(OpCodes.Ldc_I4_S, 13))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, pawn);
                    yield return new CodeInstruction(OpCodes.Ldc_I4_2);
                    yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.GetGrowthMoment));
                }
                else
                {
                    yield return instruction;
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
                CodeInstruction instruction = codes[i];
                if (instruction.LoadsField(GrowthMomentAges))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return CodeInstruction.LoadField(typeof(ChoiceLetter_GrowthMoment), nameof(ChoiceLetter_GrowthMoment.pawn));
                    yield return new CodeInstruction(OpCodes.Ldc_I4_0);
                    yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.GetGrowthMoment));
                    i += 2;
                }
                else
                {
                    yield return instruction;
                }
            }
        }
    }
}
