using System;
using System.Linq;
using System.Reflection;
using Verse;
using RimWorld;
using HarmonyLib;

namespace BetterRomance
{
    //Only gets called once at pawn generation, then on every growth check for pawns that are not yet adults
    //Tries to find the correct adult life stage and uses age from that
    [HarmonyPatch(typeof(Pawn_AgeTracker), "AdultMinAge", MethodType.Getter)]
    public static class Pawn_AgeTracker_AdultMinAge
    {
        public static bool Prefix(ref float __result, Pawn ___pawn)
        {
            foreach (LifeStageAge stage in ___pawn.RaceProps.lifeStageAges)
            {
                if (stage.def.defName == "HumanlikeAdult")
                {
                    __result = stage.minAge;
                    return false;
                }
            }
            foreach (LifeStageAge stage in ___pawn.RaceProps.lifeStageAges)
            {
                if (stage.def.defName.ToLower().Contains("adult"))
                {
                    __result = stage.minAge;
                    return false;
                }
            }
            return true;
        }
    }
}