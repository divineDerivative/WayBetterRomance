using HarmonyLib;
using RimWorld;
using System.Reflection;

namespace BetterRomance.HarmonyPatches
{
    public static class TweaksGalore_Patches
    {
        public static void PatchTweaksGalore(this Harmony harmony)
        {
            MethodInfo tgIsSexualityTrait = AccessTools.Method("TweaksGalore.Patch_PawnGenerator_GenerateTraits:IsSexualityTrait");
            harmony.Patch(tgIsSexualityTrait, prefix: new HarmonyMethod(typeof(TweaksGalore_Patches), nameof(TweaksGalore_Patches.IsSexualityTraitPrefix)));
            LogUtil.Message("TweaksGalore patches applied.");
        }

        // Override TweaksGalore's IsSexualityTrait method that hard-codes the base-game traits only (Gay, Bisexual, Asexual)
        public static bool IsSexualityTraitPrefix(Trait trait, ref bool __result)
        {
            bool ret = RomanceUtilities.OrientationTraits.Contains(trait.def);
            __result = ret;
            return false;
        }
    }
}
