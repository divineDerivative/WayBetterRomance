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
            List<CodeInstruction> codesForPawn =
            [
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, pawn)
            ];
            return instructions.AdultMinAgeInt(codesForPawn);
        }
    }

    //Go before HAR so corrected ages get used
    [HarmonyPatch(typeof(ChoiceLetter_GrowthMoment), "CacheLetterText")]
    [HarmonyBefore(["rimworld.erdelf.alien_race.main"])]
    public static class ChoiceLetter_GrowthMoment_CacheLetterText
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            FieldInfo GrowthMomentAges = AccessTools.Field(typeof(GrowthUtility), nameof(GrowthUtility.GrowthMomentAges));

            List<CodeInstruction> codes = instructions.ToList();

            foreach (CodeInstruction code in codes)
            {
                if (code.LoadsField(GrowthMomentAges))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return CodeInstruction.LoadField(typeof(ChoiceLetter_GrowthMoment), nameof(ChoiceLetter_GrowthMoment.pawn));
                    yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.GetBiotechSettings));
                    yield return CodeInstruction.LoadField(typeof(CompSettingsBiotech), nameof(CompSettingsBiotech.growthMoments));
                }
                else
                {
                    yield return code;
                }
            }
        }
    }
}
