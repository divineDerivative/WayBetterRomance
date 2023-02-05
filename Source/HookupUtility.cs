using RimWorld;
using Verse;
using HarmonyLib;
using Verse.AI;
using System.Text;
using System.Collections.Generic;
using System.Linq;

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
        /// <returns><see langword="True"/> if an <paramref name="option"/> was created, <see langword="False"/> if <paramref name="option"/> is null</returns>
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
                maxPartners = !new HistoryEvent(pawn.GetHistoryEventForLoveRelationCountPlusOne(), pawn.Named(HistoryEventArgsNames.Doer)).DoerWillingToDo();
            }
            if (!maxPartners || !pawn.CaresAboutCheating())
            {
                return "";
            }
            if (count <= 1)
            {
                return " - " + "WBR.HookupWarningMonogamous".Translate(pawn) + "\n";
            }
            return " - " + "WBR.HookupWarningPolygamous".Translate(pawn, count) + "\n";
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
            if (!HookupEligible(initiator, initiator: true, forOpinionExplanation))
            {
                return false;
            }
            //Don't allow if target is too young
            if (forOpinionExplanation && target.ageTracker.AgeBiologicalYearsFloat < target.MinAgeForSex())
            {
                return "WBR.CantHookupTargetYoung".Translate();
            }
            //Don't allow if it would be incestuous
            if ((bool)AccessTools.Method(typeof(RelationsUtility), "Incestuous").Invoke(null, new object[] { initiator, target }))
            {
                return "WBR.CantHookupTargetIncest".Translate();
            }
            //Don't allow if prisoner status doesn't match
            if (forOpinionExplanation && target.IsPrisoner != initiator.IsPrisoner)
            {
                return "WBR.CantHookupTargetPrisoner".Translate();
            }
            //Don't allow if slave status doesn't match
            if (forOpinionExplanation && target.IsSlave != initiator.IsSlave)
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
                return "WBR.CantHookupTargetInfertile".Translate();
            }
            //Next check if target is eligible for hookups
            AcceptanceReport acceptanceReport = HookupEligible(target, initiator: false, forOpinionExplanation);
            if (!acceptanceReport)
            {
                return acceptanceReport;
            }
            //Don't allow if opinion is too low
            if (initiator.relations.OpinionOf(target) <= initiator.MinOpinionForHookup(true))
            {
                return "WBR.CantHookupTargetOpinion".Translate();
            }
            //Don't allow if acceptance chance is 0
            if (!forOpinionExplanation && HookupSuccessChance(target, initiator) <= 0f)
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
        public static AcceptanceReport HookupEligible(Pawn pawn, bool initiator, bool forOpinionExplanation)
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
            if (pawn.Downed && !forOpinionExplanation)
            {
                return initiator ? "WBR.CantHookupInitiateMessageDowned".Translate(pawn).CapitalizeFirst() : "WBR.CantHookupTargetDowned".Translate();
            }
            if (pawn.Drafted && !forOpinionExplanation)
            {
                return initiator ? "WBR.CantHookupInitiateMessageDrafted".Translate(pawn).CapitalizeFirst() : "WBR.CantHookupTargetDrafted".Translate();
            }
            if (pawn.MentalState != null)
            {
                return (initiator && !forOpinionExplanation) ? "WBR.CantHookupInitiateMessageMentalState".Translate(pawn).CapitalizeFirst() : "WBR.CantHookupTargetMentalState".Translate();
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
            Building_Bed result;
            //If first owns a suitable bed that no one is currently using, use that
            if (first.ownership.OwnedBed != null && first.ownership.OwnedBed.SleepingSlotsCount > 1 && !first.ownership.OwnedBed.AnyOccupants)
            {
                result = first.ownership.OwnedBed;
                return result;
            }
            //If second owns a suitable bed that no one is currently using, use that
            if (second.ownership.OwnedBed != null && second.ownership.OwnedBed.SleepingSlotsCount > 1 && !second.ownership.OwnedBed.AnyOccupants)
            {
                result = second.ownership.OwnedBed;
                return result;
            }
            //Otherwise, look through all beds to see if one is usable
            foreach (ThingDef current in RestUtility.AllBedDefBestToWorst)
            {
                //This checks if it's a human or animal bed
                if (!RestUtility.CanUseBedEver(first, current))
                {
                    continue;
                }
                //This checks if the bed is too far away
                Building_Bed building_Bed = (Building_Bed)GenClosest.ClosestThingReachable(first.Position, first.Map,
                    ThingRequest.ForDef(current), PathEndMode.OnCell, TraverseParms.For(first), 9999f, x => true);
                if (building_Bed == null)
                {
                    continue;
                }
                //Does it have at least two sleeping spots
                if (building_Bed.SleepingSlotsCount <= 1)
                {
                    continue;
                }
                //Is anyone currently using it
                if (building_Bed.AnyOccupants)
                {
                    continue;
                }
                //Use that bed
                result = building_Bed;
                return result;
            }
            return null;
        }

        /// <summary>
        /// Determines if the "Hook Up" button should be drawn on <paramref name="pawn"/>'s social card
        /// </summary>
        /// <param name="pawn"></param>
        /// <returns></returns>
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
        /// <returns>Chance of success</returns>
        public static float HookupSuccessChance(Pawn target, Pawn asker, bool ordered = false)
        {
            //Check minimum opinion settings
            if (target.relations.OpinionOf(asker) < target.MinOpinionForHookup(ordered))
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
                //Lower chances if they're not in a relationship
                if (!LovePartnerRelationUtility.LovePartnerRelationExists(target, asker))
                {
                    romanceFactor /= 1.5f;
                }
                //Adjust based on opinion and increase chance for forced job
                return romanceFactor * OpinionFactor(target, asker) * (ordered ? 1.2f : 1f);
            }
            return 0f;
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
            //This will bottom out at 0.2f at min opinion and max out at 1.5f at 50 opinion
            //May need to adjust in case min opinion is set higher than 50
            return GenMath.LerpDoubleClamped(target.MinOpinionForHookup(ordered), 50f, 0.2f, 1.5f, target.relations.OpinionOf(asker));
        }

        /// <summary>
        /// Will <paramref name="pawn"/> participate in a hookup. Checks settings and asexuality rating.
        /// </summary>
        /// <param name="pawn">The pawn in question</param>
        /// <param name="initiator">If <paramref name="pawn"/> is initiating the hookup</param>
        /// <param name="ordered">If this is an ordered hookup</param>
        /// <returns>An <see cref="AcceptanceReport"/> with rejection reason if applicable</returns>
        public static AcceptanceReport WillPawnTryHookup(Pawn pawn, bool initiator = false, bool ordered = false)
        {
            //We return false if no reason needs to be given, otherwise return the string for the rejection reason
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

        /// <summary>
        /// Creates string for opinion tooltip on social card
        /// </summary>
        /// <param name="initiator"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static string HookupFactors(Pawn initiator, Pawn target)
        {
            StringBuilder text = new StringBuilder();
            //Opinion factor
            float opinionFactor;
            if (target.relations.OpinionOf(initiator) < target.MinOpinionForHookup(true))
            {
                opinionFactor = 0f;
            }
            else
            {
                opinionFactor = OpinionFactor(target, initiator);
            }
            text.AppendLine(HookupFactorLine("WBR.HookupChanceOpinionFactor".Translate(), opinionFactor));
            //Relative age
            text.AppendLine(HookupFactorLine("WBR.HookupChanceAgeFactor".Translate(), target.relations.LovinAgeFactor(initiator)));
            //Adjustment for not being in a relationship
            if (!LovePartnerRelationUtility.LovePartnerRelationExists(target, initiator))
            {
                text.AppendLine(HookupFactorLine("WBR.HookupChanceNotPartner".Translate(), 2f / 3f));
            }
            //Adjustment for opinion of existing partner
            if (RomanceUtilities.IsThisCheating(initiator, target, out List<Pawn> partnerList))
            {
                float partnerFactor = RomanceUtilities.PartnerFactor(target, partnerList, out _);
                if (partnerFactor != 1f)
                {
                    text.AppendLine(HookupFactorLine("WBR.HookupChancePartnerFactor".Translate(), partnerFactor));
                }
            }
            //Effect of target's beauty stat
            float prettyFactor = target.relations.PrettinessFactor(initiator);
            if (prettyFactor != 1f)
            {
                text.AppendLine(HookupFactorLine("WBR.HookupChanceBeautyFactor".Translate(), prettyFactor));
            }
            //Adjustment for any sexuality incompatibilities
            float sexualityFactor;
            if (target.IsAsexual() && target.AsexualRating() < 0.5f)
            {
                sexualityFactor = 0f;
            }
            else
            {
                sexualityFactor = RomanceUtilities.SexualityFactor(target, initiator);
            }
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
                        if (gene.Active && gene.def.missingGeneRomanceChanceFactor != 1f && (target.genes == null || !target.genes.HasGene(gene.def)))
                        {
                            float value = gene.def.missingGeneRomanceChanceFactor;
                            string geneText1 = string.Empty;
                            //Nullify with kind trait
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
                            //Nullify with kind trait
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
            bool manInfertile = man.GetFertilityLevel() <= 0f;
            bool womanInfertile = woman.GetFertilityLevel() <= 0f;
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
