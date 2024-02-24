using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace BetterRomance
{
    public class JobDriver_DoLovinCasual : JobDriver
    {
        private readonly TargetIndex PartnerInd = TargetIndex.A;
        private readonly TargetIndex BedInd = TargetIndex.B;
        private readonly TargetIndex SlotInd = TargetIndex.C;
        private const int TicksBetweenHeartMotes = 100;
        private const float PregnancyChance = 0.05f;
        private const int ticksForEnhancer = 2750;
        private const int ticksOtherwise = 1000;
        private int waitCount = 0;
        private string ActorName => Actor.Name.ToStringShort;
        private string PartnerName => Partner.Name.ToStringShort;

        private Building_Bed Bed => (Building_Bed)(Thing)job.GetTarget(BedInd);
        private Pawn Partner => (Pawn)(Thing)job.GetTarget(PartnerInd);
        private Pawn Actor => GetActor();

        private bool IsInOrByBed(Building_Bed b, Pawn p)
        {
            for (int i = 0; i < b.SleepingSlotsCount; i++)
            {
                if (b.GetSleepingSlotPos(i).InHorDistOf(p.Position, 1f))
                {
                    return true;
                }
            }
            return false;
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Partner, job, 1, -1, null, errorOnFailed) && pawn.Reserve(Bed, job, Bed.SleepingSlotsCount, 0, null, errorOnFailed);
        }

        public override bool CanBeginNowWhileLyingDown()
        {
            return JobInBedUtility.InBedOrRestSpotNow(pawn, job.GetTarget(BedInd));
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            //Fail if bed gets despawned, wasn't assigned, or becomes forbidden
            this.FailOnDespawnedOrNull(BedInd);
            this.FailOnForbidden(BedInd);
            //Reserve the bed, not claiming
            yield return Toils_Reserve.Reserve(BedInd, 2, 0);

            //This is to prevent immediately ending the job because the other guy hasn't started yet
            Toil WaitForPartnerJob = new Toil
            {
                defaultCompleteMode = ToilCompleteMode.Delay,
                initAction = delegate
                {
                    ticksLeftThisToil = 1;
                    if (Partner.CurJobDef == RomanceDefOf.DoLovinCasual)
                    {
                        LogUtil.Message($"Hookup job started for {ActorName} and {PartnerName}", true);
                    }
                }
            };
            yield return WaitForPartnerJob;

            //Go to assigned spot in bed
            Toil walkToBed = Toils_Goto.Goto(SlotInd, PathEndMode.OnCell);
            walkToBed.AddFailCondition(() => DateUtility.FailureCheck(Partner, RomanceDefOf.DoLovinCasual));
            yield return walkToBed;

            //Wait at bed for partner
            Toil wait = new Toil
            {
                initAction = delegate { ticksLeftThisToil = DateUtility.waitingTicks; },
                tickAction = delegate
                {
                    //Once they've arrived, start the next toil
                    if (IsInOrByBed(Bed, Partner))
                    {
                        ReadyForNextToil();
                    }
                    else if (ticksLeftThisToil <= 1)
                    {
                        //But don't wait forever
                        if (DateUtility.DistanceFailure(Actor, Partner, ref waitCount, ref ticksLeftThisToil))
                        {
                            LogUtil.Message($"{ActorName} called off the hook up with {PartnerName} because they took too long to show up.", true);
                            Partner.jobs.EndCurrentJob(JobCondition.Incompletable);
                            Actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                        }
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Delay,
            };
            wait.AddFailCondition(() => DateUtility.FailureCheck(Partner, RomanceDefOf.DoLovinCasual));
            wait.AddFinishAction(() =>
            {
                LogUtil.Message($"{ActorName} waited {debugTicksSpentThisToil} ticks", true);
            });
            yield return wait;

            //Get in the bed
            Toil layDown = new Toil
            {
                initAction = delegate
                {
                    Actor.pather.StopDead();
                    Actor.jobs.curDriver.asleep = false;
                    Actor.jobs.posture = PawnPosture.LayingInBed;
                },
                tickAction = delegate
                {
                    Actor.GainComfortFromCellIfPossible();
                }
            };
            yield return layDown;

            //Actually have sex
            Toil loveToil = new Toil
            {
                //This checks if actor is cheating
                initAction = delegate
                {
                    ticksLeftThisToil = (RomanceUtilities.HasLoveEnhancer(Actor) || RomanceUtilities.HasLoveEnhancer(Partner)) ? ticksForEnhancer : ticksOtherwise;

                    if (Settings.VREHighmateActive)
                    {
                        ticksLeftThisToil = OtherMod_Methods.AdjustLovinTicks(ticksLeftThisToil, Actor, Partner);
                    }

                    if (RomanceUtilities.IsThisCheating(Actor, Partner, out List<Pawn> cheatedOnList))
                    {
                        //This is really just to grab the list, separate if statement since it can return false even if the list is not empty
                    }
                    //If they're in a relationship with target, then this list will be empty
                    if (!cheatedOnList.NullOrEmpty())
                    {
                        foreach (Pawn p in cheatedOnList)
                        {
                            //Ignore if p doesn't care about cheating
                            if (!p.CaresAboutCheating())
                            {
                                continue;
                            }
                            //If p is on the map there's a 25% they notice the cheating
                            if (p.Map == Actor.Map || Rand.Value < 0.25f)
                            {
                                p.needs.mood.thoughts.memories.TryGainMemory(ThoughtDefOf.CheatedOnMe, Actor);
                            }
                        }
                    }
                    LogUtil.Message($"Casual lovin' started for {ActorName}", true);
                },
                tickAction = delegate
                {
                    //Make hearts every 100 ticks
                    if (ticksLeftThisToil % TicksBetweenHeartMotes == 0)
                    {
                        FleckMaker.ThrowMetaIcon(Actor.Position, Actor.Map, FleckDefOf.Heart);
                    }
                    Actor.GainComfortFromCellIfPossible();
                    //Gain joy every tick, but only if they have a joy need
                    if (Actor.needs.joy != null)
                    {
                        JoyUtility.JoyTickCheckEnd(Actor, JoyTickFullJoyAction.None);
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Delay,
                debugName = "HookupLovinToil",
            };
            //Fail if partner dies/goes down, or there's over 100 ticks left and the partner has wandered off
            loveToil.AddFailCondition(() => Partner.Dead || Partner.Downed || (ticksLeftThisToil > 100 && !IsInOrByBed(Bed, Partner)));
            yield return loveToil;

            //If they finish, add the appropriate memory and record events
            //Vanilla has this as a "finish action" on the previous toil, but I think it only makes sense to add this stuff if the lovin' actually... finishes
            yield return new Toil
            {
                initAction = delegate
                {
                    Thought_Memory thought_Memory = (Thought_Memory)ThoughtMaker.MakeThought(ThoughtDefOf.GotSomeLovin);
                    if (Settings.VREHighmateActive)
                    {
                        OtherMod_Methods.DoLovinResult(Actor, Partner);
                    }
                    //Increase mood power if either pawn had a love enhancer
                    if (RomanceUtilities.HasLoveEnhancer(Actor) || RomanceUtilities.HasLoveEnhancer(Partner))
                    {
                        thought_Memory.moodPowerFactor = 1.5f;
                    }
                    if (Actor.needs.mood != null)
                    {
                        Actor.needs.mood.thoughts.memories.TryGainMemory(thought_Memory, Partner);
                        HelperClasses.RotRFillRomanceBar?.Invoke(null, new object[] { Actor, 0.5f });
                    }
                    Find.HistoryEventsManager.RecordEvent(new HistoryEvent(HistoryEventDefOf.GotLovin, Actor.Named(HistoryEventArgsNames.Doer)));
                    //Do I need to account for spouse like custom relations?
                    HistoryEventDef def = Actor.relations.DirectRelationExists(PawnRelationDefOf.Spouse, Partner) ? HistoryEventDefOf.GotLovin_Spouse : HistoryEventDefOf.GotLovin_NonSpouse;
                    Find.HistoryEventsManager.RecordEvent(new HistoryEvent(def, Actor.Named(HistoryEventArgsNames.Doer)));
                    //Attempt to have hookups behave more like normal lovin, use the same cooldown period based on age
                    Actor.mindState.canLovinTick = Find.TickManager.TicksGame + GenerateRandomMinTicksToNextLovin(Actor);
                    HelperClasses.CSLLoved?.Invoke(this, new object[] { Actor, Partner, false });
                    //Biotech addition
                    if (ModsConfig.BiotechActive)
                    {
                        Pawn male = (Actor.gender == Gender.Male) ? Actor : ((Partner.gender == Gender.Male) ? Partner : null);
                        Pawn female = (Actor.gender == Gender.Female) ? Actor : ((Partner.gender == Gender.Female) ? Partner : null);
                        if (male != null && female != null && Rand.Chance(PregnancyChance * PregnancyUtility.PregnancyChanceForPartners(female, male)))
                        {
                            GeneSet inheritedGeneSet = PregnancyUtility.GetInheritedGeneSet(male, female, out bool success);
                            if (success)
                            {
                                Hediff_Pregnant hediff_Pregnant = (Hediff_Pregnant)HediffMaker.MakeHediff(HediffDefOf.PregnantHuman, female);
                                hediff_Pregnant.SetParents(null, male, inheritedGeneSet);
                                female.health.AddHediff(hediff_Pregnant);
                            }
                            else if (PawnUtility.ShouldSendNotificationAbout(male) || PawnUtility.ShouldSendNotificationAbout(female))
                            {
                                Messages.Message("MessagePregnancyFailed".Translate(male.Named("FATHER"), female.Named("MOTHER")) + ": " + "CombinedGenesExceedMetabolismLimits".Translate(), new LookTargets(male, female), MessageTypeDefOf.NegativeEvent);
                            }
                        }
                        LogUtil.Message($"Pregnancy code run successfully", true);
                    }
                    LogUtil.Message($"Hook up between {ActorName} and {PartnerName} finished successfully", true);
                },
                defaultCompleteMode = ToilCompleteMode.Instant,
                socialMode = RandomSocialMode.Off,
                debugName = "HookupFinish",
            };
        }

        private int GenerateRandomMinTicksToNextLovin(Pawn pawn)
        {
            if (DebugSettings.alwaysDoLovin)
            {
                return 100;
            }
            float num = pawn.GetLovinCurve().Evaluate(pawn.ageTracker.AgeBiologicalYearsFloat);
            if (ModsConfig.BiotechActive && pawn.genes != null)
            {
                foreach (Gene item in pawn.genes.GenesListForReading)
                {
                    num *= item.def.lovinMTBFactor;
                }
            }
            num = Rand.Gaussian(num, 0.3f);
            if (num < 0.5f)
            {
                num = 0.5f;
            }
            return (int)(num * 2500f);
        }
    }
}