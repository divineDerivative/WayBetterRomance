using HarmonyLib;
using RimWorld;
using Verse;

namespace BetterRomance.HarmonyPatches
{
    [HarmonyPatch(typeof(StatPart_FertilityByGenderAge), "AgeFactor")]
    public static class StatPart_FertilityByGenderAge_AgeFactor
    {
        public static bool Prefix(Pawn pawn, ref float __result)
        {
            if (Settings.HARActive)
            {
                if (HAR_Integration.FertilityCurveExists(pawn))
                {
                    return true;
                }
            }
            SimpleCurve curve = new SimpleCurve(pawn.GetFertilityAgeCurve());
            __result = curve.Evaluate(pawn.ageTracker.AgeBiologicalYearsFloat);
            return false;

        }
    }
}