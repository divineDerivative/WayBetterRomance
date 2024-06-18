using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace BetterRomance.HarmonyPatches
{
    [HarmonyPatch(typeof(HumanOvum), "CanFertilizeReport")]
    public static class HumanOvum_CanFertilizeReport
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) => instructions.AgeToHaveChildren(OpCodes.Ldarg_1, OpCodes.Ldarg_1, false);
    }
}
