using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace BetterRomance.HarmonyPatches
{
    //Let HAR go first so my curve can be passed to his helper
    [HarmonyPatch(typeof(JobDriver_Lovin), "GenerateRandomMinTicksToNextLovin")]
    [HarmonyAfter(["rimworld.erdelf.alien_race.main"])]
    public static class JobDriver_Lovin_GenerateRandomMinTicksToNextLovin
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {

            foreach (CodeInstruction code in instructions)
            {
                //If HAR is active, this will put my curve in place of humanDefault, which is only used if lovinIntervalHoursFromAge is not set
                if (code.LoadsField(AccessTools.Field(typeof(JobDriver_Lovin), "LovinIntervalHoursFromAgeCurve")))
                {
                    //Because the instruction I'm replacing is used as a jump to point, the new instruction needs to have the same label as the old one
                    yield return new CodeInstruction(OpCodes.Ldarg_1) { labels = code.ExtractLabels() };
                    yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.GetLovinCurve));
                }
                else
                {
                    yield return code;
                }
            }
        }
    }
}