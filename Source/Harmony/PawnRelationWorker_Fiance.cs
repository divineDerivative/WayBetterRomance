using HarmonyLib;
using RimWorld;
using Verse;
using UnityEngine;

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
}