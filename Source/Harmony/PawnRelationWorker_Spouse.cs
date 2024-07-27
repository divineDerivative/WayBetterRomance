using HarmonyLib;
using RimWorld;
using Verse;

namespace BetterRomance.HarmonyPatches
{
    //Respect marriage rules
    [HarmonyPatch(typeof(PawnRelationWorker_Spouse), nameof(PawnRelationWorker_Spouse.GenerationChance))]
    public static class PawnRelationWorker_Spouse_GenerationChance
    {
        public static void Postfix(Pawn generated, Pawn other, ref float __result)
        {
            if (__result == 0f)
            {
                return;
            }
            if (!OrientationUtility.CouldWeBeMarried(generated, other))
            {
                __result = 0f;
                return;
            }
        }
    }
}
