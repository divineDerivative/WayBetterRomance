using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection.Emit;
using Verse;

namespace BetterRomance.HarmonyPatches
{
    [HarmonyPatch(typeof(Recipe_ImplantIUD), nameof(Recipe_ImplantIUD.AvailableOnNow))]
    public static class Recipe_ImplantIUD_AvailableOnNow
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) => instructions.AgeToHaveChildrenInt(OpCodes.Ldloc_0);

        public static bool Prefix(Thing thing, ref bool __result)
        {
            if (thing is Pawn pawn && pawn.gender is Gender.Female or Gender.Male)
            {
                return true;
            }
            __result = false;
            return false;
        }
    }
}
