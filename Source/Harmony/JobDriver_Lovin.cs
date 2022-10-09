using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace BetterRomance
{
    //Uses age settings to generate the lovin curve, then calculates it as normal
    [HarmonyPatch(typeof(JobDriver_Lovin), "GenerateRandomMinTicksToNextLovin")]
    public static class JobDriver_Lovin_GenerateRandomMinTicksToNextLovin
    {
        public static bool Prefix(Pawn pawn, ref int __result)
        {
            SimpleCurve LovinIntervalHoursFromAgeCurve = new SimpleCurve(GetCurve(pawn));
            if (DebugSettings.alwaysDoLovin)
            {
                __result = 100;
                return false;
            }
            float centerX = LovinIntervalHoursFromAgeCurve.Evaluate(pawn.ageTracker.AgeBiologicalYearsFloat);
            centerX = Rand.Gaussian(centerX, 0.3f);
            if (centerX < 0.5f)
            {
                centerX = 0.5f;
            }
            __result = (int)(centerX * 2500f);
            return false;
        }

        //Generates points for the curve based on age settings
        private static List<CurvePoint> GetCurve(Pawn pawn)
        {
            float minAge = pawn.MinAgeForSex();
            float maxAge = pawn.MaxAgeForSex();
            float declineAge = pawn.DeclineAtAge();
            List<CurvePoint> curves = new List<CurvePoint>
            {
                new CurvePoint(minAge, 1.5f),
                new CurvePoint((declineAge / 5) + minAge, 1.5f),
                new CurvePoint(declineAge, 4f),
                new CurvePoint((maxAge / 4) + declineAge, 12f),
                new CurvePoint(maxAge, 36f)
            };
            return curves;
        }
    }
}