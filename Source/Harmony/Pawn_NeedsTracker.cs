﻿using HarmonyLib;
using RimWorld;
using Verse;

namespace BetterRomance.HarmonyPatches
{
    //This is the patch to apply the partner comp only to pawns with the joy need, since it's only used for joy activities
    //Also adds the asexual comp to asexual pawns
    [HarmonyPatch(typeof(Pawn_NeedsTracker), nameof(Pawn_NeedsTracker.AddOrRemoveNeedsAsAppropriate))]
    public static class Pawn_NeedsTracker_AddOrRemoveNeedsAsAppropriate
    {
        public static void Postfix(Pawn_NeedsTracker __instance, Pawn ___pawn)
        {
            if (__instance.joy != null)
            {
                ___pawn.CheckForComp<Comp_PartnerList>();
            }
            if (___pawn.IsAsexual())
            {
                ___pawn.CheckForComp<Comp_SexRepulsion>();
            }
        }
    }

    //Fixes joy need being added to guests if the option to add it to prisoners is selected
    //Made it its own setting
    [HarmonyPatch(typeof(Pawn_NeedsTracker), "ShouldHaveNeed")]
    public static class Pawn_NeedsTracker_ShouldHaveNeed
    {
        public static bool Prefix(ref bool __result, NeedDef nd, Pawn ___pawn)
        {
            if (nd.defName == "Joy" && !BetterRomanceMod.settings.joyOnGuests)
            {
                if (nd.colonistAndPrisonersOnly && (___pawn.Faction == null || !___pawn.Faction.IsPlayer) && !___pawn.IsPrisoner)
                {
                    __result = false;
                    return false;
                }
            }
            return true;
        }
    }
}
