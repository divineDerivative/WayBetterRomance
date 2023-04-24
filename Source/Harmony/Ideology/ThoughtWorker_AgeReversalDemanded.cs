using System.Collections.Generic;
using RimWorld;
using HarmonyLib;
using System.Reflection.Emit;

namespace BetterRomance.HarmonyPatches
{
    [HarmonyDebug]
    [HarmonyPatch(typeof(ThoughtWorker_AgeReversalDemanded), "ShouldHaveThought")]
    public static class ThoughtWorker_AgeReversalDemanded_ShouldHaveThought
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction code in instructions)
            {
                if (code.LoadsConstant(25))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.AgeReversalDemandAge));
                }
                else
                {
                    yield return code;
                }
            }
        }
    }
}
