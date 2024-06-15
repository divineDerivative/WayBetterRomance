using HarmonyLib;
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
        public static bool Prefix(Pawn pawn)
        {
            //Just use my method instead
            pawn.EnsureTraits();
            return false;
        }
    }

    [HarmonyPatch(typeof(PawnGenerator), "GenerateSkills")]
    public static class PawnGenerator_GenerateSkills
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) => instructions.AdultMinAgeInt(OpCodes.Ldarg_0).ChildAgeTranspiler(new CodeInstruction(OpCodes.Ldarg_0), false);
    }

    [HarmonyPatch(typeof(PawnGenerator), "GenerateTraits")]
    public static class PawnGenerator_GenerateTraits
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) => instructions.AdultMinAgeInt(OpCodes.Ldarg_0).ChildAgeTranspiler(new CodeInstruction(OpCodes.Ldarg_0));
    }

    [HarmonyPatch(typeof(PawnGenerator), "GenerateInitialHediffs")]
    public static class PawnGenerator_GenerateInitialHediffs
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) => instructions.MinAgeForAdulthoodTranspiler(OpCodes.Ldarg_0, true).MinAgeForSexTranspiler(OpCodes.Ldarg_0);
    }

    [HarmonyPatch(typeof(PawnGenerator), "FinalLevelOfSkill")]
    [HarmonyAfter(["rimworld.erdelf.alien_race.main"])]
    //I go second, so my curves will be used if there's not one provided for HAR
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