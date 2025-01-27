﻿using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;

namespace BetterRomance
{
    public static class HookupUtility
    {
        /// <summary>
        /// Creates an individual <see cref="FloatMenuOption">FloatMenuOption</see> for an ordered hookup between <paramref name="initiator"/> and <paramref name="hookupTarget"/>
        /// </summary>
        /// <param name="initiator"></param>
        /// <param name="hookupTarget"></param>
        /// <param name="option"></param>
        /// <param name="chance"></param>
        /// <returns><see langword="True"/> if an accepted <paramref name="option"/> was created, <see langword="False"/> if rejected <paramref name="option"/> is created or <paramref name="option"/> is null</returns>
        public static bool HookupOption(Pawn initiator, Pawn hookupTarget, out FloatMenuOption option, out float chance)
        {
            //Do not allow if target's gender does not match initiator's orientation
            if (!RelationsUtility.AttractedToGender(initiator, hookupTarget.gender))
            {
                option = null;
                chance = 0f;
                return false;
            }
            //Check eligibility
            AcceptanceReport acceptanceReport = HookupEligiblePair(initiator, hookupTarget, forOpinionExplanation: false);
            //If rejected but no reason was given, no option is created
            if (!acceptanceReport.Accepted && acceptanceReport.Reason.NullOrEmpty())
            {
                option = null;
                chance = 0f;
                return false;
            }
            //If accepted, calculate success chance and create option
            if (acceptanceReport.Accepted)
            {
                chance = HookupSuccessChance(hookupTarget, initiator, true);
                string label = string.Format("{0} ({1} {2})", hookupTarget.LabelShort, chance.ToStringPercent(), "chance".Translate());
                option = new FloatMenuOption(label, delegate
                {
                    GiveHookupJobWithWarning(initiator, hookupTarget);
                }, MenuOptionPriority.Low);
                return true;
            }
            //If rejected, create disabled option with rejection reason listed
            chance = 0f;
            option = new FloatMenuOption(hookupTarget.LabelShort + " (" + acceptanceReport.Reason + ")", null);
            return false;
        }

        /// <summary>
        /// Checks for potential relationship conflicts, then gives the ordered hookup job, with a warning first if needed
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
            if (!romanceTarget.IsFree(RomanticActivityType.OrderedHookup, out string reason))
            {
                Messages.Message("WBR.HookupTargetNotFreeReason".Translate() + reason, MessageTypeDefOf.RejectInput, false);
                return;
            }
            string text = null;
            //Check if pawns are in a relationship with each other
            DirectPawnRelation existingRelation = LovePartnerRelationUtility.ExistingLoveRealtionshipBetween(romancer, romanceTarget, false);
            if (existingRelation == null)
            //Create string for warning if either pawn is in a relationship
            {
                text = GetRelationshipWarning(romancer) + GetRelationshipWarning(romanceTarget);
            }
            //If neither is in a relationship or they're in a relationship with each other, give the job
            if (existingRelation != null || text.NullOrEmpty())
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

        /// <summary>
        /// Creates the ordered hookup job and force <paramref name="romancer"/> to start it
        /// </summary>
        /// <param name="romancer"></param>
        /// <param name="romanceTarget"></param>
        /// <param name="bed"></param>
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
                maxPartners = !IdeoUtility.DoerWillingToDo(pawn.GetHistoryEventForLoveRelationCountPlusOne(), pawn);
                //LogUtil.Message($"{pawn.LabelShort} is {(maxPartners ? "not " : "")}allowed to take a new lover");
            }
            if (!maxPartners || !pawn.CaresAboutCheating())
            {
                return "";
            }
            return count <= 1 ? (" - " + "WBR.HookupWarningMonogamous".Translate(pawn) + "\n") : (" - " + "WBR.HookupWarningPolygamous".Translate(pawn, count) + "\n");
        }

        /// <summary>
        /// Checks if an ordered hookup between <paramref name="initiator"/> and <paramref name="target"/> is possible
        /// </summary>
        /// <param name="initiator"></param>
        /// <param name="target"></param>
        /// <param name="forOpinionExplanation">If the report is only for the opinion tooltip</param>
        /// <returns>An <see cref="AcceptanceReport"/> with a rejection reason if applicable</returns>
        public static AcceptanceReport HookupEligiblePair(Pawn initiator, Pawn target, bool forOpinionExplanation)
        {
            if (initiator == target)
            {
                return false;
            }
            //Don't allow if initiator can't do hookups at all
            if (!HookupEligible(initiator, initiator: true))
            {
                return false;
            }
            //Don't even try with drones
            if (target.DroneCheck())
            {
                return false;
            }
            //Start with reasons that are not subject to change
            //Don't allow if target is too young
            if (forOpinionExplanation && target.ageTracker.AgeBiologicalYearsFloat < target.MinAgeForSex())
            {
                return "WBR.CantHookupTargetYoung".Translate();
            }
            //Don't allow if it would be incestuous
            if ((bool)AccessTools.Method(typeof(RelationsUtility), "Incestuous").Invoke(null, [initiator, target]))
            {
                return "WBR.CantHookupTargetIncest".Translate();
            }
            //Don't allow if prisoner status doesn't match
            if (target.IsPrisoner != initiator.IsPrisoner)
            {
                return "WBR.CantHookupTargetPrisoner".Translate();
            }
            //Don't allow if slave status doesn't match
            if (target.IsSlave != initiator.IsSlave)
            {
                return "WBR.CantHookupTargetSlave".Translate();
            }
            //Don't allow if target's gender does not match initiator's orientation
            if (!RelationsUtility.AttractedToGender(initiator, target.gender))
            {
                return "WBR.CantHookupTargetGender".Translate();
            }
            //Don't allow if fertility requirements are not met
            if (!SettingsUtilities.MeetsHookupBreedingRequirement(initiator, target))
            {
                return "WBR.CantHookupTargetPregnancy".Translate();
            }
            //Next check if target is eligible for hookups
            AcceptanceReport acceptanceReport = HookupEligible(target, initiator: false);
            if (!acceptanceReport)
            {
                return acceptanceReport;
            }
            //Don't allow if they're not on the same map
            if (!target.Spawned || target.Map != initiator.Map)
            {
                return "WBR.CantHookupTargetUnreachable".Translate();
            }
            //Don't allow if target is busy
            if (!target.IsFree(RomanticActivityType.OrderedHookup, out string reason) && !forOpinionExplanation)
            {
                return reason;
            }
            //Don't allow if opinion is too low
            if (initiator.relations.OpinionOf(target) < initiator.MinOpinionForHookup(true))
            {
                return "WBR.CantHookupTargetOpinion".Translate();
            }
            //Don't allow if acceptance chance is 0
            if (!forOpinionExplanation && HookupSuccessChance(target, initiator, true) <= 0f)
            {
                return "WBR.CantHookupTargetZeroChance".Translate();
            }
            //Don't allow if they can't reach the target
            if ((!forOpinionExplanation && !initiator.CanReach(target, PathEndMode.Touch, Danger.Deadly)) || target.IsForbidden(initiator))
            {
                return "WBR.CantHookupTargetUnreachable".Translate();
            }
            return true;
        }

        /// <summary>
        /// Checks if <paramref name="pawn"/> is allowed to participate in an ordered hookup
        /// </summary>
        /// <param name="pawn">The <see cref="Pawn"/> being checked</param>
        /// <param name="initiator">If <paramref name="pawn"/> is initiating the hookup</param>
        /// <param name="forOpinionExplanation">If the report is only for the opinion tooltip</param>
        /// <returns></returns>
        public static AcceptanceReport HookupEligible(Pawn pawn, bool initiator)
        {
            //Check the basic requirements first
            AcceptanceReport ar = WillPawnTryHookup(pawn, initiator, true);
            if (!ar.Accepted)
            {
                return ar;
            }
            //Check fertility requirements
            if (pawn.HookupForBreedingOnly() && (!pawn.ageTracker.CurLifeStage.reproductive || !pawn.IsFertile()))
            {
                return initiator ? "WBR.CantHookupInitiateMessageFertility".Translate(pawn) : "WBR.CantHookupTargetInfertile".Translate();
            }
            //Prisoners can't participate in ordered hookups
            if (pawn.IsPrisoner)
            {
                return initiator ? "WBR.CantHookupInitiateMessagePrisoner".Translate(pawn).CapitalizeFirst() : "WBR.CantHookupTargetPrisoner".Translate();
            }
            return true;
        }

        /// <summary>
        /// Finds a bed for two pawns to have a hookup in
        /// </summary>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <returns>A bed with at least two sleeping spots</returns>
        public static Building_Bed FindHookupBed(Pawn first, Pawn second)
        {
            //If first owns a suitable bed that no one is currently using, use that
            if (first.ownership.OwnedBed is Building_Bed bed1 && bed1.SleepingSlotsCount > 1 && CanBothReach(bed1, first, second))
            {
                //If they have the same bed just use that one, even if it's occupied
                if (second.ownership.OwnedBed == bed1 || (!bed1.AnyOccupants && RestUtility.CanUseBedEver(second, bed1.def)))
                {
                    return bed1;
                }
            }
            //If second owns a suitable bed that no one is currently using, use that
            if (second.ownership.OwnedBed is Building_Bed bed2 && bed2.SleepingSlotsCount > 1 && !bed2.AnyOccupants && CanBothReach(bed2, first, second))
            {
                if (RestUtility.CanUseBedEver(first, bed2.def))
                {
                    return bed2;
                }
            }
            //Otherwise, look through all beds to see if one is usable
            //Make a list of all beds that meet basic requirements
            List<Building_Bed> bedList = (from b in first.Map.listerBuildings.AllBuildingsColonistOfClass<Building_Bed>()
                                              //Has no owners, is for humans, has at least two sleeping spots
                                          where !b.OwnersForReading.Any() && b.def.building.bed_humanlike && b.SleepingSlotsCount > 1
                                          //So they'll pick nice beds if available
                                          orderby b.GetStatValue(StatDefOf.BedRestEffectiveness) descending
                                          select b).ToList();
            foreach (Building_Bed bed in bedList)
            {
                //Is it unoccupied and they both can use it
                if (CanBothUse(bed, first, second) && !bed.AnyOccupants && CanBothReach(bed, first, second))
                {
                    return bed;
                }
            }
            return null;
        }

        /// <summary>
        /// Checks that bed is reachable by both pawns and not forbidden to either of them
        /// </summary>
        /// <param name="bed"></param>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <returns></returns>
        private static bool CanBothReach(Building_Bed bed, Pawn first, Pawn second)
        {
            //Reachability = is there a valid path to the bed; forbidden = it's not in an allowed area or has been forbidden for colonist use
            //Nothing is forbidden to pawns of non-player factions
            return first.Map.reachability.CanReach(first.Position, bed.Position, PathEndMode.OnCell, TraverseParms.For(first)) && second.Map.reachability.CanReach(second.Position, bed.Position, PathEndMode.OnCell, TraverseParms.For(second)) && !bed.IsForbidden(first) && !bed.IsForbidden(second);
        }

        /// <summary>
        /// Just a simplified way of calling CanUseBedEver for both pawns at once
        /// </summary>
        /// <param name="bed"></param>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <returns></returns>
        private static bool CanBothUse(Building_Bed bed, Pawn first, Pawn second)
        {
            return RestUtility.CanUseBedEver(first, bed.def) && RestUtility.CanUseBedEver(second, bed.def);
        }

        /// <summary>
        /// Determines if the "Hook Up" button should be drawn on <paramref name="pawn"/>'s social card
        /// </summary>
        /// <param name="pawn"></param>
        /// <returns></returns>
        public static bool CanDrawTryHookup(Pawn pawn)
        {
            return pawn.ageTracker.AgeBiologicalYearsFloat >= pawn.MinAgeForSex() && pawn.Spawned && pawn.IsFreeColonist && !pawn.DroneCheck();
        }

        /// <summary>
        /// Determines if <paramref name="target"/> agrees to a hookup with <paramref name="asker"/>. Takes cheating into account.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="asker"></param>
        /// <returns>Chance of success</returns>
        public static float HookupSuccessChance(Pawn target, Pawn asker, bool ordered)
        {
            //Check minimum opinion settings
            if (target.relations.OpinionOf(asker) < target.MinOpinionForHookup(ordered))
            {
                return 0f;
            }
            //Asexual pawns below a certain rating will only agree to sex with existing partners
            //They'll also refuse partners if rating is low enough
            if (target.SexRepulsed() || target.SexAverse(asker))
            {
                return 0f;
                //Otherwise their rating is already factored in via secondary romance chance factor
            }
            //Check non spouse lovin' precepts
            float preceptFactor = 1f;
            if (!target.relations.DirectRelationExists(PawnRelationDefOf.Spouse, asker))
            {
                preceptFactor = PreceptUtility.NonSpouseLovinWillDoChance(target.ideo?.Ideo);
            }
            RomanceUtilities.WillPawnContinue(target, asker, out float chance, false, false);
            float romanceFactor = target.relations.SecondaryRomanceChanceFactor(asker);
            //Lower chances if they're not in a relationship
            if (!LovePartnerRelationUtility.LovePartnerRelationExists(target, asker))
            {
                //Only if they're not free approved
                if (target.ideo?.Ideo.GetLovinPreceptDef() != RomanceDefOf.Lovin_FreeApproved)
                {
                    romanceFactor /= 1.5f;
                }
            }
            float opinionFactor = OpinionFactor(target, asker, ordered);
            //Adjust based on opinion and increase chance for forced job
            return chance * romanceFactor * opinionFactor * preceptFactor * (ordered ? 1f : 0.8f);
        }

        /// <summary>
        /// Factor for adjusting hookup success chance based on <paramref name="target"/>'s opinion of <paramref name="asker"/>
        /// </summary>
        /// <param name="target"></param>
        /// <param name="asker"></param>
        /// <returns></returns>
        public static float OpinionFactor(Pawn target, Pawn asker, bool ordered = false)
        {
            //Keeping this just in case I change my mind
            //float opinionFactor = 1f;
            ////Decrease if opinion is negative
            //opinionFactor *= Mathf.InverseLerp(-100f, 0f, target.relations.OpinionOf(asker));
            ////Increase if opinion is positive, but on a lesser scale to above
            //opinionFactor *= GenMath.LerpDouble(0, 100f, 1f, 1.5f, target.relations.OpinionOf(asker));

            //This will bottom out at 0.2f at min opinion and max out at 1.5f at 50 opinion or min opinion + 50, whichever is higher
            //May need to adjust in case min opinion is set higher than 50
            float minOpinion = target.MinOpinionForHookup(ordered);
            return (float)GenMath.LerpDoubleClamped(minOpinion, (float)Math.Max(50f, minOpinion + 50f), 0.2f, 1.5f, target.relations.OpinionOf(asker));
        }

        /// <summary>
        /// Will <paramref name="pawn"/> participate in a hookup. Checks settings and asexuality.
        /// </summary>
        /// <param name="pawn">The pawn in question</param>
        /// <param name="initiator">If <paramref name="pawn"/> is initiating the hookup</param>
        /// <param name="ordered">If this is an ordered hookup</param>
        /// <returns>An <see cref="AcceptanceReport"/> with rejection reason if applicable</returns>
        public static AcceptanceReport WillPawnTryHookup(Pawn pawn, bool initiator, bool ordered)
        {
            //We return false if no reason needs to be given, otherwise return the string for the rejection reason
            //Check for drones
            if (pawn.DroneCheck())
            {
                return false;
            }
            //Check age
            if (pawn.ageTracker.AgeBiologicalYearsFloat < pawn.MinAgeForSex())
            {
                return false;
            }
            //Asexual pawns will never initiate a hookup
            if (initiator && pawn.IsAsexual())
            {
                return "WBR.CantHookupInitiateMessageAsexual".Translate(pawn).CapitalizeFirst();
            }
            //Is the race/pawnkind allowed to have hookups?
            if (!pawn.HookupAllowed(ordered))
            {
                //Decide if this should be reported to user and how to word it
                return false;
            }
            //Check fertility for ordered hookups
            if (ordered && pawn.HookupForBreedingOnly() && !pawn.IsFertile())
            {
                return initiator ? (AcceptanceReport)"WBR.CantHookupInitiateMessageFertility".Translate(pawn) : (AcceptanceReport)"WBR.CantHookupTargetInfertile".Translate();
            }
            //Check trait requirements
            if (ordered && !pawn.MeetsHookupTraitRequirment(out List<string> traitList))
            {
                if (!traitList.NullOrEmpty())
                {
                    string text = traitList.ToCommaListOr();
                    return initiator ? "WBR.CantHookupInitiateMessageTrait".Translate(pawn, text) : "WBR.CantHookupTargetTrait".Translate();
                }
                //Make a list of traits pawn needs and report that somehow
                return initiator ? "WBR.CantHookupInitiateMessageTrait".Translate(pawn) : "WBR.CantHookupTargetTrait".Translate();
            }
            //If their ideo prohibits all lovin', do not allow
            if (!IdeoUtility.DoerWillingToDo(HistoryEventDefOf.SharedBed, pawn))
            {
                return initiator ? "WBR.CantHookupInitiateMessageIdeo".Translate(pawn) : "WBR.CantHookupTargetIdeo".Translate(pawn.ideo.Ideo);
            }
            //Check against canLovinTick, except for drawing the ordered hookup menu
            return Find.TickManager.TicksGame >= pawn.mindState?.canLovinTick || ordered;
        }

        /// <summary>
        /// Creates string for opinion tooltip on social card
        /// </summary>
        /// <param name="initiator"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static string HookupFactors(Pawn initiator, Pawn target)
        {
            StringBuilder text = new();
            //Opinion factor
            float opinionFactor = target.relations.OpinionOf(initiator) < target.MinOpinionForHookup(true) ? 0f : OpinionFactor(target, initiator);
            text.AppendLine(HookupFactorLine("WBR.HookupChanceOpinionFactor".Translate(), opinionFactor));
            //Relative age
            text.AppendLine(HookupFactorLine("WBR.HookupChanceAgeFactor".Translate(), target.relations.LovinAgeFactor(initiator)));
            //Adjustment for not being in a relationship
            if (!LovePartnerRelationUtility.LovePartnerRelationExists(target, initiator))
            {
                //Only if they're not free approved
                if (target.ideo?.Ideo.GetLovinPreceptDef() != RomanceDefOf.Lovin_FreeApproved)
                {
                    text.AppendLine(HookupFactorLine("WBR.HookupChanceNotPartner".Translate(), 2f / 3f));
                }
            }
            //Adjustment for non-spouse lovin precept
            if (!target.relations.DirectRelationExists(PawnRelationDefOf.Spouse, initiator))
            {
                float precept = PreceptUtility.NonSpouseLovinWillDoChance(target.ideo?.Ideo);
                if (precept != 1f)
                {
                    text.AppendLine(HookupFactorLine("WBR.CantHookupTargetIdeo".Translate(target.ideo.Ideo), precept));
                }
            }
            //Adjustment for opinion of existing partner
            if (RomanceUtilities.IsThisCheating(target, initiator, out List<Pawn> partnerList) && !partnerList.NullOrEmpty())
            {
                float cheating = RomanceUtilities.PartnerFactor(target, partnerList, out _, false) * RomanceUtilities.CheatingChance(target, true);
                text.AppendLine(HookupFactorLine("WBR.HookupChanceCheatingChance".Translate(), cheating));
                if (Settings.RotRActive && target.Ideo != null)
                {
                    string precept = RotR_Integration.RotRCheatingPreceptExplanation(target);
                    if (!precept.NullOrEmpty())
                    {
                        text.AppendLine(precept);
                    }
                }
            }
            //Effect of target's beauty stat
            float prettyFactor = target.relations.PrettinessFactor(initiator);
            if (prettyFactor != 1f)
            {
                text.AppendLine(HookupFactorLine("WBR.HookupChanceBeautyFactor".Translate(), prettyFactor));
            }
            //Adjustment for any sexuality incompatibilities
            float sexualityFactor = target.SexAverse() ? 0f : RomanceUtilities.SexualityFactor(target, initiator);
            if (sexualityFactor != 1f)
            {
                text.AppendLine(HookupFactorLine("WBR.HookupChanceSexuality".Translate(), sexualityFactor));
            }
            //Adjustment for any genes that make people less attractive to others that don't also have the gene
            if (ModsConfig.BiotechActive)
            {
                if (initiator.genes != null)
                {
                    foreach (Gene gene in initiator.genes.GenesListForReading)
                    {
#if v1_4
                        if (gene.Active && gene.def.missingGeneRomanceChanceFactor != 1f && (target.genes == null || !target.genes.HasGene(gene.def)))
#else
                        if (gene.Active && gene.def.missingGeneRomanceChanceFactor != 1f && (target.genes == null || !target.genes.HasActiveGene(gene.def)))
#endif
                        {
                            float value = gene.def.missingGeneRomanceChanceFactor;
                            string kind = string.Empty;
                            //Nullify with kind trait
                            if (target.story?.traits != null && target.story.traits.HasTrait(TraitDefOf.Kind))
                            {
                                value = 1f;
                                kind = " (" + TraitDefOf.Kind.DataAtDegree(0).label + ")";
                            }
                            text.AppendLine(HookupFactorLine(gene.def.LabelCap + " (" + initiator.NameShortColored.Resolve() + ")", value) + kind);
                        }
                    }
                }
                if (target.genes != null)
                {
                    foreach (Gene gene in target.genes.GenesListForReading)
                    {
#if v1_4
                        if (gene.Active && gene.def.missingGeneRomanceChanceFactor != 1f && (initiator.genes == null || !initiator.genes.HasGene(gene.def)))
#else
                        if (gene.Active && gene.def.missingGeneRomanceChanceFactor != 1f && (initiator.genes == null || !initiator.genes.HasActiveGene(gene.def)))
#endif
                        {
                            float value = gene.def.missingGeneRomanceChanceFactor;
                            string kind = string.Empty;
                            //Nullify with kind trait
                            if (target.story?.traits != null && target.story.traits.HasTrait(TraitDefOf.Kind))
                            {
                                value = 1f;
                                kind = " (" + TraitDefOf.Kind.DataAtDegree(0).label + ")";
                            }
                            text.AppendLine(HookupFactorLine(gene.def.LabelCap + " (" + target.NameShortColored + ")", value) + kind);
                        }
                    }
                }
            }
            return text.ToString();
        }

        /// <summary>
        /// Formats information into a consistent string
        /// </summary>
        /// <param name="label"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private static string HookupFactorLine(string label, float value)
        {
            return " - " + label + ": x" + value.ToStringPercent();
        }

        /// <summary>
        /// Is pregnancy possible between <paramref name="first"/> and <paramref name="second"/>. To be used only if both Biotech and HAR are not active.
        /// </summary>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <returns>An <see cref="AcceptanceReport"/> with rejection reason if applicable</returns>
        public static AcceptanceReport CanEverProduceChild(Pawn first, Pawn second)
        {
            if (first.Dead)
            {
                return "WBR.PawnIsDead".Translate(first.Named("PAWN"));
            }
            if (second.Dead)
            {
                return "WBR.PawnIsDead".Translate(second.Named("PAWN"));
            }
            if (first.gender == second.gender)
            {
                return "WBR.PawnsHaveSameGender".Translate(first.Named("PAWN1"), second.Named("PAWN2")).Resolve();
            }
            //At this point they have to be different genders.
            //This will get messed up if either pawn has Gender.None, but that should only happen with HAR, in which case this method won't be called
            Pawn man = (first.gender == Gender.Male) ? first : second;
            Pawn woman = (first.gender == Gender.Female) ? first : second;
            //Check if life stage is reproductive
            bool manYoung = !man.ageTracker.CurLifeStage.reproductive;
            bool womanYoung = !woman.ageTracker.CurLifeStage.reproductive;
            if (manYoung && womanYoung)
            {
                return "WBR.PawnsAreTooYoung".Translate(man.Named("PAWN1"), woman.Named("PAWN2")).Resolve();
            }
            if (manYoung != womanYoung)
            {
                return "WBR.PawnIsTooYoung".Translate((manYoung ? man : woman).Named("PAWN")).Resolve();
            }
            //Check fertility of both pawns
            bool manInfertile = man.IsFertile();
            bool womanInfertile = woman.IsFertile();
            if (manInfertile && womanInfertile)
            {
                return "WBR.PawnsAreInfertile".Translate(man.Named("PAWN1"), woman.Named("PAWN2")).Resolve();
            }
            if (manInfertile != womanInfertile)
            {
                return "WBR.PawnIsInfertile".Translate((manInfertile ? man : woman).Named("PAWN")).Resolve();
            }
            //Check for sterility
            //This normally returns false if Biotech is not active, going to have to assume that any pregnancy mod would patch that somehow
            //Interestingly it also checks the life stage and fertility, so the only instance in which it would return true here is if they have a hediff that prevents pregnancy
            //Those are PregnantHuman (which is excluded in the second part below), PregnancyLabor, PregnancyLaborPushing, Pregnant (the version for animals, and with some pregnancy mods, humans), Sterilized (usually used for animals), and derivations thereof, Vasectomy, ImplantedIUD, and TubalLigation
            //So going to need to review each pregnancy mod to make sure I don't need to add any other checks
            bool womanSterile = woman.Sterile() && PregnancyUtility.GetPregnancyHediff(woman) == null;
            bool manSterile = man.Sterile();
            if (manSterile && womanSterile)
            {
                return "WBR.PawnsAreSterile".Translate(man.Named("PAWN1"), woman.Named("PAWN2")).Resolve();
            }
            if (manSterile != womanSterile)
            {
                return "WBR.PawnIsSterile".Translate((manSterile ? man : woman).Named("PAWN")).Resolve();
            }
            return true;
        }
    }
}
