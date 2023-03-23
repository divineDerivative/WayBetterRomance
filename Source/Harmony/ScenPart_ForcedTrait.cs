using HarmonyLib;
using RimWorld;

namespace BetterRomance.HarmonyPatches
{
    [HarmonyPatch(typeof(ScenPart_ForcedTrait), "PawnHasTraitForcedByBackstory")]
    public static class ScenPart_ForcedTrait_PawnHasTraitForcedByBackstory
    {
        // ScenPart_ForcedTrait picks a trait to remove when it adds it's chosen trait.
        // We can patch here and make sure that Orientation traits are not removed by
        // this ScenPart (forcing it to pick a different trait instead)

        public static void Postfix(TraitDef trait, ref bool __result)
        {
            // If it's already true, then it's a Backstory trait and can't be removed anyway
            if (!__result)
            {
                __result = RomanceUtilities.OrientationTraits.Contains(trait);
            }
        }
    }
}
