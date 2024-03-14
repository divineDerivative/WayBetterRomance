using System.Collections.Generic;
using RimWorld;
using HarmonyLib;
using System.Reflection.Emit;

namespace BetterRomance.HarmonyPatches
{
    [HarmonyPatch(typeof(CompAbilityEffect_WordOfLove), nameof(CompAbilityEffect_WordOfLove.ValidateTarget))]
    public static class CompAbilityEffect_WordOfLove_ValidateTarget
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            bool firstFound = false;
            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.Is(OpCodes.Ldc_R4, 16f) && !firstFound)
                {
                    yield return new CodeInstruction(OpCodes.Ldloc_0);
                    yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.MinAgeForSex));
                    firstFound = true;
                }
                else if (instruction.Is(OpCodes.Ldc_R4, 16f))
                {
                    yield return new CodeInstruction(OpCodes.Ldloc_1);
                    yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.MinAgeForSex));
                }
                else
                {
                    yield return instruction;
                }
            }
        }
    }
}
