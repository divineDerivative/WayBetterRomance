using System.Collections.Generic;
using RimWorld;
using Verse;
using HarmonyLib;
using System.Reflection.Emit;

namespace BetterRomance.HarmonyPatches
{
    [HarmonyPatch(typeof(Gizmo_GrowthTier), "GrowthTierTooltip")]
    public static class Gizmo_GrowthTier_GrowthTierTooltip
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.Is(OpCodes.Ldc_I4_S, 13))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return CodeInstruction.LoadField(typeof(Gizmo_GrowthTier), "child");
                    yield return CodeInstruction.LoadField(typeof(Pawn), nameof(Pawn.ageTracker));
                    yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.DeclaredPropertyGetter(typeof(Pawn_AgeTracker), nameof(Pawn_AgeTracker.AdultMinAge)));
                    yield return new CodeInstruction(OpCodes.Conv_I4);
                }
                else
                {
                    yield return instruction;
                }
            }
        }
    }
}
