using RimWorld;
using Verse;
using HarmonyLib;
using Verse.AI;
using UnityEngine;
using System.Text;
using System.Collections.Generic;

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
                chance = HookupSuccessChance(hookupTarget, initiator);
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
        public static AcceptanceReport HookupEligiblePair(Pawn initiator, Pawn target, bool forOpinionExplanation)
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
            //Don't allow if fertility requirement is not met
            if (!target.MeetsHookupFertilityRequirement(true))
            {
                return "WBR.CantHookupTargetInfertile".Translate();
            }
            //Don't allow if trait requirement is not met
            if (!target.MeetsHookupTraitRequirment(out TraitDef trait, true))
            {
                return "WBR.CantHookupTargetTrait".Translate(trait);
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
            if (!forOpinionExplanation && HookupSuccessChance(target, initiator) <= 0f)
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
        /// Checks if <paramref name="pawn"/> is allowed to participate in an ordered hookup
        /// </summary>
        /// <param name="pawn">The <see cref="Pawn"/> being checked</param>
        /// <param name="initiator">If <paramref name="pawn"/> is initiating the hookup</param>
        /// <param name="forOpinionExplanation">Need to find out where vanilla uses this</param>
        /// <returns></returns>
        public static AcceptanceReport HookupEligible(Pawn pawn, bool initiator, bool forOpinionExplanation)
        {
            //Check the basic requirements first
            AcceptanceReport ar = HookupUtility.WillPawnTryHookup(pawn, initiator, true);
            if (!ar.Accepted)
            {
                return ar;
            }
            if (pawn.IsPrisoner)
            {
                return initiator ? "WBR.CantHookupInitiateMessagePrisoner".Translate(pawn).CapitalizeFirst() : "CantRomanceTargetPrisoner".Translate();
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

        public static bool CanDrawTryHookup(Pawn pawn)
        {
            if (pawn.ageTracker.AgeBiologicalYearsFloat >= pawn.MinAgeForSex() && pawn.Spawned)
            {
                return pawn.IsFreeColonist;
            }
            return false;
        }

        /// <summary>
        /// Determines if <paramref name="target"/> agrees to a hookup with <paramref name="asker"/>. Takes cheating into account.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="asker"></param>
        /// <returns>True or False</returns>
        public static float HookupSuccessChance(Pawn target, Pawn asker)
        {
            if (target.relations.OpinionOf(asker) < target.MinOpinionForHookup())
            {
                return 0f;
            }
            //Asexual pawns below a certain rating will only agree to sex with existing partners
            if (target.IsAsexual() && target.AsexualRating() < 0.5f)
            {
                if (!LovePartnerRelationUtility.LovePartnerRelationExists(target, asker))
                {
                    return 0f;
                }
                //Otherwise their rating is already factored in via secondary romance chance factor
            }
            if (RomanceUtilities.WillPawnContinue(target, asker, out _))
            {
                //It's either not cheating or they have decided to cheat
                float romanceFactor = target.relations.SecondaryRomanceChanceFactor(asker);
                if (!LovePartnerRelationUtility.LovePartnerRelationExists(target, asker))
                {
                    romanceFactor /= 1.5f;
                }

                return romanceFactor * OpinionFactor(target, asker);
            }
            return 0f;
        }

        public static float OpinionFactor(Pawn target, Pawn asker)
        {
            float opinionFactor = 1f;
            //Decrease if opinion is negative
            opinionFactor *= Mathf.InverseLerp(-100f, 0f, target.relations.OpinionOf(asker));
            //Increase if opinion is positive, but on a lesser scale to above
            opinionFactor *= GenMath.LerpDouble(0, 100f, 1f, 1.5f, target.relations.OpinionOf(asker));
            return opinionFactor;
        }

        /// <summary>
        /// Will <paramref name="pawn"/> participate in a hookup. Checks settings and asexuality rating.
        /// </summary>
        /// <param name="pawn">The pawn in question</param>
        /// <returns></returns>
        public static AcceptanceReport WillPawnTryHookup(Pawn pawn, bool initiator = false, bool ordered = false)
        {
            //Check age
            if (pawn.ageTracker.AgeBiologicalYearsFloat < pawn.MinAgeForSex())
            {
                return false;
            }
            //Sex repulsed asexual pawns will never agree to sex
            if (pawn.IsAsexual() && pawn.AsexualRating() < 0.2f)
            {
                return initiator ? "WBR.CantHookupInitiateMessageAsexual".Translate(pawn).CapitalizeFirst() : "WBR.CantHookupTargetAsexual".Translate();
            }
            //Is the race/pawnkind allowed to have hookups?
            if (!pawn.HookupAllowed())
            {
                //Decide if this should be reported to user and how to word it
                return false;
            }
            if (!pawn.MeetsHookupFertilityRequirement(ordered))
            {
                return initiator ? "WBR.CantHookupInitiateMessageFertility".Translate(pawn) : "WBR.CantHookupTargetInfertile".Translate();
            }
            if (!pawn.MeetsHookupTraitRequirment(out TraitDef trait, ordered))
            {
                return initiator ? "WBR.CantHookupInitiateMessageTrait".Translate(pawn, trait) : "WBR.CantHookupTargetTrait".Translate(trait);
            }
            //If their ideo prohibits all lovin', do not allow
            if (!new HistoryEvent(HistoryEventDefOf.SharedBed, pawn.Named(HistoryEventArgsNames.Doer)).DoerWillingToDo())
            {
                return initiator ? "WBR.CantHookupInitiateMessageIdeo".Translate(pawn) : "WBR.CantHookupTargetIdeo".Translate(pawn.ideo.Ideo);
            }
            //Check against canLovinTick, except for drawing the ordered hookup menu
            if (Find.TickManager.TicksGame < pawn.mindState.canLovinTick && !ordered)
            {
                return false;
            }
            return true;
        }

        public static string HookupFactors(Pawn initiator, Pawn target)
        {
            StringBuilder text = new StringBuilder();
            text.AppendLine(HookupFactorLine("RomanceChanceOpinionFactor".Translate(), OpinionFactor(initiator, target)));
            text.AppendLine(HookupFactorLine("RomanceChanceAgeFactor".Translate(), target.relations.LovinAgeFactor(initiator)));
            if (RomanceUtilities.IsThisCheating(initiator, target, out List<Pawn> partnerList))
            {
                float partnerFactor = RomanceUtilities.PartnerFactor(target, partnerList, out _);
                if (partnerFactor != 1f)
                {
                    text.AppendLine(HookupFactorLine("RomanceChancePartnerFactor".Translate(), partnerFactor));
                }
            }
            float prettyFactor = target.relations.PrettinessFactor(initiator);
            if (prettyFactor != 1f)
            {
                text.AppendLine(HookupFactorLine("RomanceChanceBeautyFactor".Translate(), prettyFactor));
            }
            if (ModsConfig.BiotechActive)
            {
                if (initiator.genes != null)
                {
                    foreach (Gene gene in initiator.genes.GenesListForReading)
                    {
                        if (gene.Active && gene.def.missingGeneRomanceChanceFactor != 1f && (target.genes == null || !target.genes.HasGene(gene.def)))
                        {
                            float value = gene.def.missingGeneRomanceChanceFactor;
                            string geneText1 = string.Empty;
                            if (target.story?.traits != null && target.story.traits.HasTrait(TraitDefOf.Kind))
                            {
                                value = 1f;
                                geneText1 = " (" + TraitDefOf.Kind.DataAtDegree(0).label + ")";
                            }
                            text.AppendLine(HookupFactorLine(gene.def.LabelCap + " (" + initiator.NameShortColored.Resolve() + ")", value) + geneText1);
                        }
                    }
                }
                if (target.genes != null)
                {
                    foreach (Gene gene in target.genes.GenesListForReading)
                    {
                        if (gene.Active && gene.def.missingGeneRomanceChanceFactor != 1f && (initiator.genes == null || !initiator.genes.HasGene(gene.def)))
                        {
                            float value2 = gene.def.missingGeneRomanceChanceFactor;
                            string geneText2 = string.Empty;
                            if (target.story?.traits != null && initiator.story.traits.HasTrait(TraitDefOf.Kind))
                            {
                                value2 = 1f;
                                geneText2 = " (" + TraitDefOf.Kind.DataAtDegree(0).label + ")";
                            }
                            text.AppendLine(HookupFactorLine(gene.def.LabelCap + " (" + target.NameShortColored + ")", value2) + geneText2);
                        }
                    }
                }
            }
            return text.ToString();
        }

        private static string HookupFactorLine(string label, float value)
        {
            return " - " + label + ": x" + value.ToStringPercent();
        }
    }
}
