using HarmonyLib;
using RimWorld;

namespace BetterRomance.HarmonyPatches
{
    [HarmonyPatch(typeof(ScenPart_ForcedTrait), "PawnHasTraitForcedByBackstory")]
    public static class ScenPart_ForcedTrait_PawnHasTraitForcedByBackstory
    {
        public static void Postfix(TraitDef trait, ref bool __result)
        {
            if (!__result) // If it's already true, then it's a Backstory trait and can't be removed anyway
            {
                __result = SexualityUtility.OrientationTraits.Contains(trait);
            }
        }
    }
}
