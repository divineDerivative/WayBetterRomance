using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace BetterRomance
{
    //Uses age settings to generate the lovin curve, then calculates it as normal
    [HarmonyPatch(typeof(JobDriver_Lovin), "GenerateRandomMinTicksToNextLovin")]
    public static class JobDriver_Lovin_GenerateRandomMinTicksToNextLovin
    {
        public static bool Prefix(Pawn pawn, ref int __result)
        {

            SimpleCurve LovinIntervalHoursFromAgeCurve = new SimpleCurve(RomanceUtilities.GetLovinCurve(pawn));
            if (DebugSettings.alwaysDoLovin)
            {
                __result = 100;
                return false;
            }
            float num = LovinIntervalHoursFromAgeCurve.Evaluate(pawn.ageTracker.AgeBiologicalYearsFloat);

            //Added from vanilla for 1.4
            if (ModsConfig.BiotechActive && pawn.genes != null)
            {
                foreach (Gene item in pawn.genes.GenesListForReading)
                {
                    num *= item.def.lovinMTBFactor;
                }
            }
            num = Rand.Gaussian(num, 0.3f);
            if (num < 0.5f)
            {
                num = 0.5f;
            }
            __result = (int)(num * 2500f);
            pawn.mindState.canLovinTick = Find.TickManager.TicksGame + 1;
            return false;
        }
    }
}