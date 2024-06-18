using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace BetterRomance.HarmonyPatches
{
    [HarmonyPatch(typeof(HumanEmbryo), "CanImplantReport")]
    public static class HumanEmbryo_CanImplantReport
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) => instructions.AgeToHaveChildrenInt(OpCodes.Ldarg_1);
    }
}
