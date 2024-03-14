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
            //This needs to be reworked to only use the curve from the comp, because the pawnkind might have a different setting than the race
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