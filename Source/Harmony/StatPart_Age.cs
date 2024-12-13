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
                SimpleCurve curve = SettingsUtilities.ConvertCurve(___curve, pawn);
                __result = curve.Evaluate(pawn.ageTracker.AgeBiologicalYears);
                return false;
            }
            return true;
        }

        public static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(StatPart_Age), "AgeMultiplier");
            yield return AccessTools.Method(typeof(StatPart_AgeOffset), "AgeOffset");
        }
    }
}
