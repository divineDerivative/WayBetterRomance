using HarmonyLib;
using RimWorld;

namespace BetterRomance.HarmonyPatches
{
    public static class TweaksGalore_Patches
    {
        public static void PatchTweaksGalore(this Harmony harmony)
        {
            var tgIsSexualityTrait = typeof(TweaksGalore.Patch_PawnGenerator_GenerateTraits).GetMethod(nameof(TweaksGalore.Patch_PawnGenerator_GenerateTraits.IsSexualityTrait));
            var prefixIsSexualityTrait = typeof(TG_PawnGenerator_GenerateTraits_IsSexualityTrait).GetMethod(nameof(TG_PawnGenerator_GenerateTraits_IsSexualityTrait.Prefix));
            harmony.Patch(tgIsSexualityTrait, prefix: new HarmonyMethod(prefixIsSexualityTrait));
            LogUtil.Message("TweaksGalore patches applied.");
        }
    }
    
    public static class TG_PawnGenerator_GenerateTraits_IsSexualityTrait
    {
        // Override TweaksGalore's IsSexualityTrait method that hard-codes the base-game traits only (Gay, Bisexual, Asexual)
        public static bool Prefix(Trait trait, ref bool __result)
        {
            var ret = RomanceUtilities.OrientationTraits.Contains(trait.def);
            __result = ret;
            return false;
        }
    }
}
