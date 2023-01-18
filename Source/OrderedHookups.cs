using RimWorld;
using Verse;
using HarmonyLib;
using Verse.AI;

namespace BetterRomance
{
    public static class HookupUtility
    {
        public static Building_Bed FindHookupBed(Pawn p1, Pawn p2)
        {
            Building_Bed result;
            //If p1 owns a suitable bed, use that
            if (p1.ownership.OwnedBed != null && p1.ownership.OwnedBed.SleepingSlotsCount > 1 && !p1.ownership.OwnedBed.AnyOccupants)
            {
                result = p1.ownership.OwnedBed;
                return result;
            }
            //If p2 owns a suitable bed, use that
            if (p2.ownership.OwnedBed != null && p2.ownership.OwnedBed.SleepingSlotsCount > 1 && !p2.ownership.OwnedBed.AnyOccupants)
            {
                result = p2.ownership.OwnedBed;
                return result;
            }
            //Otherwise, look through all beds to see if one is usable
            foreach (ThingDef current in RestUtility.AllBedDefBestToWorst)
            {
                //This checks if it's a human or animal bed
                if (!RestUtility.CanUseBedEver(p1, current))
                {
                    continue;
                }
                //This checks if the bed is too far away
                Building_Bed building_Bed = (Building_Bed)GenClosest.ClosestThingReachable(p1.Position, p1.Map,
                    ThingRequest.ForDef(current), PathEndMode.OnCell, TraverseParms.For(p1), 9999f, x => true);
                if (building_Bed == null)
                {
                    continue;
                }
                //Does it have at least two sleeping spots
                if (building_Bed.SleepingSlotsCount <= 1)
                {
                    continue;
                }
                //Use that bed
                result = building_Bed;
                return result;
            }
            return null;
        }
    }
}
