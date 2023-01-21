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
            //Asexual pawns will never initiate sex
            if (pawn.IsAsexual())
            {
                return null;
            }
            //Don't allow for kids
            else if (pawn.ageTracker.AgeBiologicalYearsFloat < pawn.MinAgeForSex())
            {
                return null;
            }
            //Checks on whether pawn should try hookup now
            else if (!InteractionUtility.CanInitiateInteraction(pawn) || !HookupUtility.WillPawnTryHookup(pawn, initiator: true) || PawnUtility.WillSoonHaveBasicNeed(pawn))
            {
                return null;
            }
            //Generate random number to check against hookup settings
            else if (100f * Rand.Value > pawn.HookupRate() / 2)
            {
                return null;
            }
            else
            {
                Comp_PartnerList comp = pawn.CheckForPartnerComp();
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
                Building_Bed bed = HookupUtility.FindHookupBed(pawn, partner);
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
    }
}