using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using HarmonyLib;

namespace BetterRomance.HarmonyPatches
{

    [HarmonyPatch(typeof(SocialCardUtility), "CanDrawTryRomance")]
    public static class SocialCardUtility_CanDrawTryRomance
    {
        public static bool Prefix(Pawn pawn, ref bool __result)
        {
            if (ModsConfig.BiotechActive && pawn.ageTracker.AgeBiologicalYearsFloat >= pawn.MinAgeForSex() && pawn.Spawned)
            {
                __result = pawn.IsFreeColonist;
                return false;
            }
            __result = false;
            return false;
        }
    }
}
