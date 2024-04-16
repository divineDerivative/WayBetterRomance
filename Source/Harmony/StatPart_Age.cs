using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace BetterRomance.HarmonyPatches
{
    [HarmonyPatch]
    [HarmonyAfter(["rimworld.erdelf.alien_race.main"])]
    public static class StatPart_Age_AgeEffect
    {
        public static bool Prefix(Pawn pawn, ref float __result, bool ___humanlikeOnly, bool ___useBiologicalYears, SimpleCurve ___curve)
        {
            if (___humanlikeOnly && ___useBiologicalYears)
            {
                SimpleCurve curve = NewCurve(pawn, ___curve);
                __result = curve.Evaluate(pawn.ageTracker.AgeBiologicalYears);
                return false;
            }
            return true;
        }

        private static SimpleCurve NewCurve(Pawn pawn, SimpleCurve oldCurve)
        {
            if (pawn.HasNoGrowth())
            {
                return
                [
                    new CurvePoint(1f, 1f)
                ];
            }
            SimpleCurve newCurve = new();
            foreach (CurvePoint point in oldCurve.Points)
            {
                newCurve.Points.Add(new CurvePoint(SettingsUtilities.ConvertAge(point.x, pawn), point.y));
            }
            return newCurve;
        }

        public static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(StatPart_Age), "AgeMultiplier");
            yield return AccessTools.Method(typeof(StatPart_AgeOffset), "AgeOffset");
        }
    }
}
