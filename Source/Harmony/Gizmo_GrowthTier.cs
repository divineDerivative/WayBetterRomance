using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace BetterRomance.HarmonyPatches
{
    [HarmonyPatch(typeof(Gizmo_GrowthTier), "GrowthTierTooltip")]
    public static class Gizmo_GrowthTier_GrowthTierTooltip
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes =
            [
                new CodeInstruction(OpCodes.Ldarg_0),
                CodeInstruction.LoadField(typeof(Gizmo_GrowthTier), "child")
            ];
            return DynamicTranspilers.AdultMinAgeInt(instructions, codes);
        }
    }
}
