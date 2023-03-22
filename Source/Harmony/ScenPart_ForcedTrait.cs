using HarmonyLib;
using RimWorld;

namespace BetterRomance.HarmonyPatches
{
    [HarmonyPatch]
    public static class ScenPart_ForcedTrait_Patches
    {
        // ScenPart_ForcedTrait picks a trait to remove when it adds it's chosen trait.
        // We can patch here and make sure that Orientation traits are not removed by
        // this ScenPart (forcing it to pick a different trait instead)
        [HarmonyPatch(typeof(ScenPart_ForcedTrait), "PawnHasTraitForcedByBackstory")]
        [HarmonyPostfix]
        public static void ScenPart_ForcedTrait_PawnHasTraitForcedByBackstory_Postfix(TraitDef trait, ref bool __result)
        {
            if(!__result) // If it's already true, then it's a Backstory trait and can't be removed anyway
            {
                var ret = RomanceUtilities.OrientationTraits.Contains(trait);
                if (ret)
                {
                    __result = true;
                }
            }
        }
    }
}
