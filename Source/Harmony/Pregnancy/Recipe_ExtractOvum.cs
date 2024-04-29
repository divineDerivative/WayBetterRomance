using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace BetterRomance.HarmonyPatches
{
    [HarmonyPatch(typeof(Recipe_ExtractOvum), nameof(Recipe_ExtractOvum.AvailableReport))]
    public static class Recipe_ExtractOvum_AvailableReport
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return instructions.AgeToHaveChildrenInt(OpCodes.Ldloc_0, true);
        }
    }
}
