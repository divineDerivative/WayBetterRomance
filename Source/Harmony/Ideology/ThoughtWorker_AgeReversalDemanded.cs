using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace BetterRomance.HarmonyPatches
{
    [HarmonyPatch(typeof(ThoughtWorker_AgeReversalDemanded), "ShouldHaveThought")]
    public static class ThoughtWorker_AgeReversalDemanded_ShouldHaveThought
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) => instructions.AgeReversalDemandAgeTranspiler(OpCodes.Ldarg_1);
    }
}
