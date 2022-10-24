using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;
using Verse.AI;

namespace BetterRomance
{
    public class JoyGiver_CasualHookup : JoyGiver
    {
        public override Job TryGiveJob(Pawn pawn)
        {
            float percentRate = pawn.HookupRate() /2;

            //Asexual pawns will never initiate sex
            if (pawn.story.traits.HasTrait(TraitDefOf.Asexual))
            {
                return null;
            }
            //Checks on whether pawn should try hookup now
            else if (!InteractionUtility.CanInitiateInteraction(pawn) || !RomanceUtilities.WillPawnTryHookup(pawn) || PawnUtility.WillSoonHaveBasicNeed(pawn))
            {
                return null;
            }
            //Generate random number to check against hookup settings
            else if (100f * Rand.Value > percentRate)
            {
                return null;
            }
            else
            {
                Comp_PartnerList comp = pawn.TryGetComp<Comp_PartnerList>();
                if (comp == null)
                {
                    FieldInfo field = AccessTools.Field(typeof(ThingWithComps), "comps");
                    List<ThingComp> compList = (List<ThingComp>)field.GetValue(pawn);
                    ThingComp newComp = (ThingComp)Activator.CreateInstance(typeof(Comp_PartnerList));
                    newComp.parent = pawn;
                    compList.Add(newComp);
                    newComp.Initialize(new CompProperties_PartnerList());
                    comp = pawn.TryGetComp<Comp_PartnerList>();
                    if (comp == null)
                    {
                        Log.Error("Unable to add Comp_HookupList");
                    }
                }
                Pawn partner = comp.GetPartner(true);
                if (partner == null || !partner.Spawned || !partner.Awake())
                {
                    return null;
                }

                //If this hookup is cheating, will pawn do it anyways?
                if (!RomanceUtilities.WillPawnContinue(pawn, partner, out _))
                {
                    return null;
                }

                //Also check ideo for non spouse lovin' thoughts
                if (!new HistoryEvent(HistoryEventDefOf.GotLovin_NonSpouse, pawn.Named(HistoryEventArgsNames.Doer)).DoerWillingToDo() && !pawn.relations.DirectRelationExists(PawnRelationDefOf.Spouse, partner))
                {
                    return null;
                }
                //If its not cheating, or they decided to cheat, continue with making the job
                Building_Bed bed = FindHookupBed(pawn, partner);
                //If no suitable bed found, do not continue
                if (bed == null)
                {
                    return null;
                }
                //Create the lead hookup job with partner and bed info
                else
                {
                    return new Job(def.jobDef, partner, bed);
                }
            }
        }

        /// <summary>
        /// Finds a bed for two pawns to have a hookup in
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns>A bed with at least two sleeping spots</returns>
        private static Building_Bed FindHookupBed(Pawn p1, Pawn p2)
        {
            Building_Bed result;
            //If p1 owns a suitable bed, use that
            if (p1.ownership.OwnedBed != null)
            {
                if (p1.ownership.OwnedBed.SleepingSlotsCount > 1)
                {
                    result = p1.ownership.OwnedBed;
                    return result;
                }
            }
            //If p2 owns a suitable bed, use that
            if (p2.ownership.OwnedBed != null)
            {
                if (p2.ownership.OwnedBed.SleepingSlotsCount > 1)
                {
                    result = p2.ownership.OwnedBed;
                    return result;
                }
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