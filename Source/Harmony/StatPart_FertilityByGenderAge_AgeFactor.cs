using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BetterRomance
{
    [HarmonyPatch(typeof(StatPart_FertilityByGenderAge), "AgeFactor")]
    public static class StatPart_FertilityByGenderAge_AgeFactor
    {
        public static bool Prefix(Pawn pawn, ref float __result)
        {
            SimpleCurve curve = new SimpleCurve(pawn.GetFertilityAgeCurve());
            if (curve != null)
            {
                __result = curve.Evaluate(pawn.ageTracker.AgeBiologicalYearsFloat);
                return false;
            }
            return true;
        }
    }
}
