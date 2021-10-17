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
        internal static FieldInfo _pawn;
        public static bool Prefix(ref float __result, ref Pawn_AgeTracker __instance)
        {
            Pawn pawn = __instance.GetPawn();
            foreach (LifeStageAge stage in pawn.RaceProps.lifeStageAges)
            {
                if (stage.def.defName == "HumanlikeAdult")
                {
                    __result = stage.minAge;
                    return false;
                }
            }
            foreach (LifeStageAge stage in pawn.RaceProps.lifeStageAges)
            {
                if (stage.def.defName.ToLower().Contains("adult"))
                {
                    __result = stage.minAge;
                    return false;
                }
            }
            return true;
        }

        private static Pawn GetPawn(this Pawn_AgeTracker _this)
        {
            bool flag = _pawn == null;
            if (!flag)
            {
                return (Pawn)_pawn.GetValue(_this);
            }

            _pawn = typeof(Pawn_AgeTracker).GetField("pawn", BindingFlags.Instance | BindingFlags.NonPublic);
            bool flag2 = _pawn == null;
            if (flag2)
            {
                Log.Error("Unable to reflect Pawn_RelationsTracker.pawn!");
            }

            return (Pawn)_pawn?.GetValue(_this);
        }
    }
}