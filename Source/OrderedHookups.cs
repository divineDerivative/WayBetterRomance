using RimWorld;
using Verse;
using HarmonyLib;
using Verse.AI;

namespace BetterRomance
{
    public static class HookupUtility
    {
        /// <summary>
        /// Creates an individual <see cref="FloatMenuOption">FloatMenuOption</see> for a hookup between <paramref name="initiator"/> and <paramref name="hookupTarget"/>
        /// </summary>
        /// <param name="initiator"></param>
        /// <param name="hookupTarget"></param>
        /// <param name="option"></param>
        /// <param name="chance"></param>
        /// <returns><see langword="True"/> if an <paramref name="option"/> was created, <see langword="False"/> if <paramref name="option"/> is null</returns>
        public static bool HookupOption(Pawn initiator, Pawn hookupTarget, out FloatMenuOption option, out float chance)
        {
            if (!RelationsUtility.AttractedToGender(initiator, hookupTarget.gender))
            {
                option = null;
                chance = 0f;
                return false;
            }
            AcceptanceReport acceptanceReport = HookupEligiblePair(initiator, hookupTarget, forOpinionExplanation: false);
            if (!acceptanceReport.Accepted && acceptanceReport.Reason.NullOrEmpty())
            {
                option = null;
                chance = 0f;
                return false;
            }
            if (acceptanceReport.Accepted)
            {
                chance = RomanceUtilities.HookupSuccessChance(hookupTarget, initiator);
                string label = string.Format("{0} ({1} {2})", hookupTarget.LabelShort, chance.ToStringPercent(), "chance".Translate());
                option = new FloatMenuOption(label, delegate
                {
                    GiveHookupJobWithWarning(initiator, hookupTarget);
                }, MenuOptionPriority.Low);
                return true;
            }
            chance = 0f;
            option = new FloatMenuOption(hookupTarget.LabelShort + " (" + acceptanceReport.Reason + ")", null);
            return false;
        }

        /// <summary>
        /// Checks for potential relationship conflicts then gives the ordered hookup job, with a warning first if needed
        /// </summary>
        /// <param name="romancer"></param>
        /// <param name="romanceTarget"></param>
        private static void GiveHookupJobWithWarning(Pawn romancer, Pawn romanceTarget)
        {
            //First try to find a bed
            Building_Bed bed = FindHookupBed(romancer, romanceTarget);
            //If no bed is found, send warning message and stop
            if (bed == null)
            {
                Messages.Message("WBR.NoHookupBedFound".Translate(romancer, romanceTarget), MessageTypeDefOf.RejectInput, false);
                return;
            }
            //If target is not free, send warning message and stop
            if (!RomanceUtilities.IsPawnFree(romanceTarget, true))
            {
                Messages.Message("WBR.HookupTargetNotFree".Translate(romanceTarget), MessageTypeDefOf.RejectInput, false);
                return;
            }
            //Check if pawns are in a relationship with each other
            DirectPawnRelation existingRelation = LovePartnerRelationUtility.ExistingLoveRealtionshipBetween(romancer, romanceTarget, false);
            //Create string for warning if either pawn is in a relationship
            string text = GetRelationshipWarning(romancer) + GetRelationshipWarning(romanceTarget);
            //If neither is in a relationship or they're in a relationship with each other, give the job
            if (text.NullOrEmpty() || existingRelation != null)
            {
                GiveHookupJob(romancer, romanceTarget, bed);
            }
            //Otherwise, create warning message and allow player to decide whether to continue
            else
            {
                Dialog_MessageBox window = Dialog_MessageBox.CreateConfirmation("WBR.HookupExistingRelationshipWarning".Translate(romancer.Named("INITIATOR"), romanceTarget.Named("TARGET")) + "\n\n" + text + "\n" + "StillWishContinue".Translate(), delegate
                {
                    GiveHookupJob(romancer, romanceTarget, bed);
                }, destructive: true);
                Find.WindowStack.Add(window);
            }
        }

        private static void GiveHookupJob(Pawn romancer, Pawn romanceTarget, Building_Bed bed)
        {
            Job job = JobMaker.MakeJob(RomanceDefOf.OrderedHookup, romanceTarget, bed);
            job.interaction = RomanceDefOf.TriedHookupWith;
            romancer.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        }

        /// <summary>
        /// Generates a warning for an ordered hookup job if the participating <paramref name="pawn"/> is in a relationship
        /// </summary>
        /// <param name="pawn"></param>
        /// <returns></returns>
        private static string GetRelationshipWarning(Pawn pawn)
        {
            int count = pawn.GetLoveRelations(includeDead: false).Count;
            bool maxPartners = count >= 1;
            if (maxPartners && ModsConfig.IdeologyActive)
            {
                maxPartners = !new HistoryEvent(pawn.GetHistoryEventForLoveRelationCountPlusOne(), pawn.Named(HistoryEventArgsNames.Doer)).DoerWillingToDo();
            }
            if (!maxPartners || !pawn.CaresAboutCheating())
            {
                return "";
            }
            if (count <= 1)
            {
                return " - " + "RomanceWarningMonogamous".Translate(pawn) + "\n";
            }
            return " - " + "RomanceWarningPolygamous".Translate(pawn, count) + "\n";
        }

        /// <summary>
        /// Checks if a hookup between <paramref name="initiator"/> and <paramref name="target"/> is possible
        /// </summary>
        /// <param name="initiator"></param>
        /// <param name="target"></param>
        /// <param name="forOpinionExplanation"></param>
        /// <returns>An <see cref="AcceptanceReport"/> with a rejection reason if applicable</returns>
        private static AcceptanceReport HookupEligiblePair(Pawn initiator, Pawn target, bool forOpinionExplanation)
        {
            if (initiator == target)
            {
                return false;
            }
            //Don't allow if initiator can't do hookups at all
            if (!HookupEligible(initiator, initiator: true, forOpinionExplanation))
            {
                return false;
            }
            //Don't allow if target is too young
            if (forOpinionExplanation && target.ageTracker.AgeBiologicalYearsFloat < target.MinAgeForSex())
            {
                return "CantRomanceTargetYoung".Translate();
            }
            //Don't allow if it would be incestuous
            if ((bool)AccessTools.Method(typeof(RelationsUtility), "Incestuous").Invoke(null, new object[] { initiator, target }))
            {
                return "CantRomanceTargetIncest".Translate();
            }
            //Don't allow if prisoner status doesn't match
            if (forOpinionExplanation && target.IsPrisoner != initiator.IsPrisoner)
            {
                return "CantRomanceTargetPrisoner".Translate();
            }
            //Don't allow if slave status doesn't match
            if (forOpinionExplanation && target.IsSlave != initiator.IsSlave)
            {
                return "WBR.CantHookupTargetSlave".Translate();
            }
            //Don't allow if target's gender does not match initiator's orientation
            if (!RelationsUtility.AttractedToGender(initiator, target.gender))
            {
                if (!forOpinionExplanation)
                {
                    return AcceptanceReport.WasRejected;
                }
                return "WBR.CantHookupTargetGender".Translate();
            }
            AcceptanceReport acceptanceReport = HookupEligible(target, initiator: false, forOpinionExplanation);
            //Don't allow if target is not able to hookup at all
            if (!acceptanceReport)
            {
                return acceptanceReport;
            }
            //Don't allow if opinion is too low
            if (initiator.relations.OpinionOf(target) <= initiator.MinOpinionForHookup())
            {
                return "CantRomanceTargetOpinion".Translate();
            }
            //Don't allow if acceptance chance is 0
            if (!forOpinionExplanation && RomanceUtilities.HookupSuccessChance(target, initiator) <= 0f)
            {
                return "CantRomanceTargetZeroChance".Translate();
            }
            //Don't allow if they can't reach the target
            if ((!forOpinionExplanation && !initiator.CanReach(target, PathEndMode.Touch, Danger.Deadly)) || target.IsForbidden(initiator))
            {
                return "CantRomanceTargetUnreachable".Translate();
            }
            return true;

        }

        /// <summary>
        /// Checks if <paramref name="pawn"/> is allowed to participate in a hookup
        /// </summary>
        /// <param name="pawn">The <see cref="Pawn"/> being checked</param>
        /// <param name="initiator">If <paramref name="pawn"/> is initiating the hookup</param>
        /// <param name="forOpinionExplanation"></param>
        /// <returns></returns>
        public static AcceptanceReport HookupEligible(Pawn pawn, bool initiator, bool forOpinionExplanation)
        {
            //Check the basic requirements first
            AcceptanceReport ar = RomanceUtilities.WillPawnTryHookup(pawn, true);
            if (!ar.Accepted)
            {
                return ar;
            }
            if (pawn.IsPrisoner)
            {
                if (!initiator || forOpinionExplanation)
                {
                    return AcceptanceReport.WasRejected;
                }
                return "WBR.CantHookupInitiateMessagePrisoner".Translate(pawn).CapitalizeFirst();
            }
            if (pawn.Downed && !forOpinionExplanation)
            {
                return initiator ? "WBR.CantHookupInitiateMessageDowned".Translate(pawn).CapitalizeFirst() : "CantRomanceTargetDowned".Translate();
            }
            if (pawn.Drafted && !forOpinionExplanation)
            {
                return initiator ? "WBR.CantHookupInitiateMessageDrafted".Translate(pawn).CapitalizeFirst() : "CantRomanceTargetDrafted".Translate();
            }
            if (pawn.MentalState != null)
            {
                return (initiator && !forOpinionExplanation) ? "WBR.CantHookupInitiateMessageMentalState".Translate(pawn).CapitalizeFirst() : "CantRomanceTargetMentalState".Translate();
            }
            return true;
        }

        /// <summary>
        /// Finds a bed for two pawns to have a hookup in
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns>A bed with at least two sleeping spots</returns>
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
