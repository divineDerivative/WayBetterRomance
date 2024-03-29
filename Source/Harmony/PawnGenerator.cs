﻿using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using Verse;

namespace BetterRomance.HarmonyPatches
{
    //Orientation traits are now added with a new method, don't allow that method to run in order to use user settings
    //Still ending up with occasional duplicate traits
    [HarmonyPatch(typeof(PawnGenerator), nameof(PawnGenerator.TryGenerateSexualityTraitFor))]
    public static class PawnGenerator_TryGenerateSexualityTraitFor
    {
        public static bool Prefix(Pawn pawn, bool allowGay)
        {
            //Just use my method instead
            pawn.EnsureTraits();
            //Do anything with the allowGay bool?
            return false;
        }
    }

    [HarmonyPatch(typeof(PawnGenerator), "GenerateSkills")]
    public static class PawnGenerator_GenerateSkills
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            bool done = false;
            foreach (CodeInstruction code in instructions)
            {
                if (code.Is(OpCodes.Ldc_I4_S, 13))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldc_I4, 2);
                    yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.GetGrowthMoment));
                }
                else if (!done && code.LoadsConstant(3))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.ChildAge));
                    done = true;
                }
                else
                {
                    yield return code;
                }
            }
        }
    }

    [HarmonyPatch(typeof(PawnGenerator), "GenerateTraits")]
    public static class PawnGenerator_GenerateTraits
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction code in instructions)
            {
                if (code.LoadsConstant(13))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldc_I4, 2);
                    yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.GetGrowthMoment));
                }
                else if (code.LoadsConstant(3))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0).MoveLabelsFrom(code);
                    yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.ChildAge));
                }
                else
                {
                    yield return code;
                }
            }
        }
    }

    [HarmonyPatch(typeof(PawnGenerator), "GenerateInitialHediffs")]
    public static class PawnGenerator_GenerateInitialHediffs
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction code in instructions)
            {
                if (code.LoadsConstant(16))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.MinAgeForSex));
                    yield return new CodeInstruction(OpCodes.Conv_I4);
                }
                else if (code.LoadsConstant(20))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.GetMinAgeForAdulthood));
                    yield return new CodeInstruction(OpCodes.Conv_I4);
                }
                else
                {
                    yield return code;
                }
            }
        }
    }

    [HarmonyPatch(typeof(PawnGenerator), "FinalLevelOfSkill")]
    public static class PawnGenerator_FinalLevelOfSkill
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction code in instructions)
            {
                if (code.LoadsField(AccessTools.Field(typeof(PawnGenerator), "AgeSkillMaxFactorCurve")))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.AgeSkillMaxFactorCurve));
                }
                else if (code.LoadsField(AccessTools.Field(typeof(PawnGenerator), "AgeSkillFactor")))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.AgeSkillFactor));
                }
                else
                {
                    yield return code;
                }
            }
        }
    }
}