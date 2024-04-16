using HarmonyLib;
using RimWorld;
using Verse;

namespace BetterRomance.HarmonyPatches
{
    //Gets the new lover only event if love relations are over the spouse count
    [HarmonyPatch(typeof(LovePartnerRelationUtility), nameof(LovePartnerRelationUtility.GetHistoryEventLoveRelationCount))]
    public static class LovePartnerRelationUtility_GetHistoryEventLoveRelationCount
    {
        public static bool Prefix(Pawn pawn, ref HistoryEventDef __result)
        {
            if (pawn.CompareSpouseAndLoverCount() == SpouseCountComparison.Over)
            {
                __result = pawn.GetHistoryEventLoverCount();
                return false;
            }
            return true;
        }
    }

    //Gets the new lover only event if love relations are at or over the spouse count
    [HarmonyPatch(typeof(LovePartnerRelationUtility), nameof(LovePartnerRelationUtility.GetHistoryEventForLoveRelationCountPlusOne))]
    public static class LovePartnerRelationUtility_GetHistoryEventForLoveRelationCountPlusOne
    {
        public static bool Prefix(Pawn pawn, ref HistoryEventDef __result)
        {
            if (pawn.CompareSpouseAndLoverCount() != SpouseCountComparison.Below)
            {
                __result = pawn.GetHistoryEventLoverCountPlusOne();
                return false;
            }
            return true;
        }
    }
}
