using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace BetterRomance.HarmonyPatches
{
    [HarmonyPatch(typeof(Recipe_ImplantIUD), nameof(Recipe_ImplantIUD.AvailableOnNow))]
    public static class Recipe_ImplantIUD_AvailableOnNow
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) => instructions.AgeToHaveChildrenInt(OpCodes.Ldloc_0);
    }
}
