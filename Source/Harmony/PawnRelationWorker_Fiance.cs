using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace BetterRomance.HarmonyPatches
{
    //Adjusted to use age settings
    //Could maybe make this a transpiler but seems like a lot of effort for such a small method
    [HarmonyPatch(typeof(PawnRelationWorker_Fiance), "GetOldAgeFactor")]
    public static class PawnRelationWorker_Fiance_GetOldAgeFactor
    {
        public static bool Prefix(Pawn pawn, ref float __result)
        {
            __result = Mathf.Clamp(GenMath.LerpDouble(pawn.MaxAgeForSex() - pawn.DeclineAtAge(), pawn.MaxAgeForSex(), 1f, 0.01f, pawn.ageTracker.AgeBiologicalYears), 0.01f, 1f);
            return false;
        }
    }

    //Respect sex repulsion rules
    [HarmonyPatch(typeof(PawnRelationWorker_Fiance), nameof(PawnRelationWorker_Fiance.GenerationChance))]
    public static class PawnRelationWorker_Fiance_GenerationChance
    {
        public static void Postfix(Pawn generated, Pawn other, ref float __result)
        {
            if (__result == 0f)
            {
                return;
            }
            if (generated.IsAsexual() && generated.AsexualRating() < 0.2f && !other.IsAsexual())
            {
                __result = 0f;
                return;
            }
            if (other.IsAsexual() && other.AsexualRating() < 0.2f && !generated.IsAsexual())
            {
                __result = 0f;
                return;
            }
        }
    }
}