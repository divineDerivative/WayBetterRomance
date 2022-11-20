using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;

namespace BetterRomance
{
    public class JobDriver_DoLovinCasual : JobDriver
    {
        private readonly TargetIndex PartnerInd = TargetIndex.A;
        private readonly TargetIndex BedInd = TargetIndex.B;
        private readonly TargetIndex SlotInd = TargetIndex.C;
        private int ticksLeft;
        private const int TicksBetweenHeartMotes = 100;
        private static float PregnancyChance = 0.05f;
        private const int ticksForEnhancer = 2750;
        private const int ticksOtherwise = 1000;

        private Building_Bed Bed => (Building_Bed)(Thing)job.GetTarget(BedInd);

        private Pawn Partner => (Pawn)(Thing)job.GetTarget(PartnerInd);
        private Pawn Actor => GetActor();

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref ticksLeft, "ticksLeft");
        }

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
            if (pawn.Reserve(Partner, job, 1, -1, null, errorOnFailed))
            {
                return pawn.Reserve(Bed, job, Bed.SleepingSlotsCount, 0, null, errorOnFailed);
            }
            return false;
        }

        public override bool CanBeginNowWhileLyingDown()
        {
            return JobInBedUtility.InBedOrRestSpotNow(pawn, job.GetTarget(BedInd));
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            //Fail if bed or partner gets despawned or wasn't assigned
            this.FailOnDespawnedOrNull(BedInd);
            this.FailOnDespawnedOrNull(PartnerInd);
            //Fail if partner is unconscious
            this.FailOn(() => !Partner.health.capacities.CanBeAwake);
            //Reserve the bed, not claiming
            yield return Toils_Reserve.Reserve(BedInd, 2, 0);
            //Go to assigned spot in bed
            yield return Toils_Goto.Goto(SlotInd, PathEndMode.OnCell);
            //Wait for partner
            yield return new Toil
            {
                initAction = delegate { ticksLeftThisToil = 300; },
                tickAction = delegate
                {
                    if (IsInOrByBed(Bed, Partner))
                    {
                        ticksLeftThisToil = 0;
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Delay
            };
            //Get in the bed
            Toil layDown = new Toil();
            layDown.initAction = delegate
            {
                layDown.actor.pather.StopDead();
                JobDriver curDriver = layDown.actor.jobs.curDriver;
                curDriver.asleep = false;
                layDown.actor.jobs.posture = PawnPosture.LayingInBed;
            };
            layDown.tickAction = delegate {
                Actor.GainComfortFromCellIfPossible();
            };
            yield return layDown;

            //Actually have sex
            Toil loveToil = new Toil
            {
                //This checks if actor is cheating
                initAction = delegate
                {
                    if ((Actor.health != null && Actor.health.hediffSet != null && Actor.health.hediffSet.hediffs.Any((Hediff h) => h.def == HediffDefOf.LoveEnhancer)) || (Partner.health != null && Partner.health.hediffSet != null && Partner.health.hediffSet.hediffs.Any((Hediff h) => h.def == HediffDefOf.LoveEnhancer)))
                    {
                        ticksLeftThisToil = ticksForEnhancer;
                    }
                    else
                    {
                        ticksLeftThisToil = ticksOtherwise;
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
                            //Ignore if p has the free love precept
                            if (p.ideo.Ideo.PreceptsListForReading.Any((Precept x) => x.def == RomanceDefOf.Lovin_FreeApproved))
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
                },
                tickAction = delegate
                {
                    //Make hearts every 100 ticks
                    if (ticksLeftThisToil % TicksBetweenHeartMotes == 0)
                    {
                        FleckMaker.ThrowMetaIcon(Actor.Position, Actor.Map, FleckDefOf.Heart);
                    }
                    //Gain joy every tick
                    JoyUtility.JoyTickCheckEnd(Actor, JoyTickFullJoyAction.None);
                },
                defaultCompleteMode = ToilCompleteMode.Delay
            };
            //Fail if partner dies, or there's over 100 ticks left and the partner has wandered off
            loveToil.AddFailCondition(() => Partner.Dead || ticksLeftThisToil > 100 && !IsInOrByBed(Bed, Partner));
            yield return loveToil;
            //If they finish, add the appropriate memory and record events
            //Vanilla has this as a "finish action" on the previous toil, but I think it only makes sense to add this stuff if the lovin' actually... finishes
            yield return new Toil
            {
                initAction = delegate
                {
                    Thought_Memory thought_Memory = (Thought_Memory)ThoughtMaker.MakeThought(ThoughtDefOf.GotSomeLovin);
                    //Increase mood power if either pawn had a love enhancer
                    if ((Actor.health != null && Actor.health.hediffSet != null && Actor.health.hediffSet.hediffs.Any((Hediff h) => h.def == HediffDefOf.LoveEnhancer)) || (Partner.health != null && Partner.health.hediffSet != null && Partner.health.hediffSet.hediffs.Any((Hediff h) => h.def == HediffDefOf.LoveEnhancer)))
                    {
                        thought_Memory.moodPowerFactor = 1.5f;
                    }
                    if (Actor.needs.mood != null)
                    {
                        Actor.needs.mood.thoughts.memories.TryGainMemory(thought_Memory, Partner);
                    }
                    Find.HistoryEventsManager.RecordEvent(new HistoryEvent(HistoryEventDefOf.GotLovin, Actor.Named(HistoryEventArgsNames.Doer)));
                    HistoryEventDef def = Actor.relations.DirectRelationExists(PawnRelationDefOf.Spouse, Partner) ? HistoryEventDefOf.GotLovin_Spouse : HistoryEventDefOf.GotLovin_NonSpouse;
                    Find.HistoryEventsManager.RecordEvent(new HistoryEvent(def, Actor.Named(HistoryEventArgsNames.Doer)));
                    //Attempt to have hookups behave more like normal lovin, use the same cooldown period based on age
                    Actor.mindState.canLovinTick = Find.TickManager.TicksGame + GenerateRandomMinTicksToNextLovin(Actor);
                    //Biotech addition
                    if (ModsConfig.BiotechActive)
                    {
                        Pawn male = (Actor.gender == Gender.Male) ? Actor : ((Partner.gender == Gender.Male) ? Partner : null);
                        Pawn female = ((Actor.gender == Gender.Female) ? Actor : ((Partner.gender == Gender.Female) ? Partner : null));
                        if (male != null && female != null && Rand.Chance(PregnancyChance * PregnancyUtility.PregnancyChanceForPartners(female, male)))
                        {
                            Hediff_Pregnant hediff_Pregnant = (Hediff_Pregnant)HediffMaker.MakeHediff(HediffDefOf.PregnantHuman, female);
                            hediff_Pregnant.SetParents(null, male, PregnancyUtility.GetInheritedGeneSet(male, female));
                            female.health.AddHediff(hediff_Pregnant);
                        }
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Instant,
                socialMode = RandomSocialMode.Off,
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