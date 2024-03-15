using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace BetterRomance
{
    public class JobDriver_LeadHookup : JobDriver
    {
        public bool successfulPass = true;
        private const int orderedHookupInterval = 120000;
        private int waitCount = 0;
        //This is necessary, see below
        public bool WasSuccessfulPass => successfulPass;
        private Pawn Actor => GetActor();
        private Pawn TargetPawn => TargetThingA as Pawn;
        private Building_Bed TargetBed => TargetThingB as Building_Bed;
        private TargetIndex TargetPawnIndex => TargetIndex.A;
        private bool Ordered => job.def == RomanceDefOf.OrderedHookup;
        private string ActorName => Actor.Name.ToStringShort;
        private string TargetName => TargetPawn.Name.ToStringShort;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        private bool DoesTargetPawnAcceptAdvance(out string rejectReason)
        {
            return TargetPawn.IsFree(Ordered ? RomanticActivityType.OrderedHookup : RomanticActivityType.CasualHookup, out rejectReason) && HookupUtility.WillPawnTryHookup(TargetPawn, initiator: false, ordered: Ordered) && Rand.Range(0.05f, 1f) < HookupUtility.HookupSuccessChance(TargetPawn, Actor, Ordered);
        }

        private bool IsTargetPawnOkay()
        {
            return !TargetPawn.Dead && !TargetPawn.Downed;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            //Fail if partner gets despawned, wasn't assigned, or wanders into an area forbidden to actor
            this.FailOnDespawnedNullOrForbidden(TargetPawnIndex);
            //Walk to the target
            Toil walkToTarget = Toils_Interpersonal.GotoInteractablePosition(TargetPawnIndex);
            walkToTarget.AddPreInitAction(delegate
            {
                ticksLeftThisToil = DateUtility.walkingTicks;
                LogUtil.Message($"{ActorName} is going to ask {TargetName} to hook up", true);
            });
            walkToTarget.AddPreTickAction(delegate
            {
                if (ticksLeftThisToil <= 1)
                {
                    if (!Ordered && DateUtility.DistanceFailure(Actor, TargetPawn, ref waitCount, ref ticksLeftThisToil))
                    {
                        LogUtil.Message($"{ActorName} gave up asking {TargetName} to hook up because it took too long to find them, {debugTicksSpentThisToil} ticks", true);
                        Actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                    }
                }
            });
            walkToTarget.socialMode = RandomSocialMode.Off;
            yield return walkToTarget;

            //Wait if needed
            Toil wait = Toils_Interpersonal.WaitToBeAbleToInteract(pawn);
            wait.socialMode = RandomSocialMode.Off;
            yield return wait;

            //Wake target up if job is forced
            if (Ordered)
            {
                yield return Toils_General.Do(delegate
                {
                    if (!TargetPawn.Awake())
                    {
                        TargetPawn.jobs.SuspendCurrentJob(JobCondition.InterruptForced);
                        if (!pawn.interactions.CanInteractNowWith(TargetPawn, RomanceDefOf.TriedHookupWith))
                        {
                            Messages.Message("HookupFailedUnexpected".Translate(pawn, TargetPawn), MessageTypeDefOf.NegativeEvent, historical: false);
                        }
                    }
                });
            }

            //Ask the target
            Toil proposeHookup = ToilMaker.MakeToil();
            proposeHookup.defaultCompleteMode = ToilCompleteMode.Delay;
            //Make heart fleck
            proposeHookup.initAction = delegate
            {
                ticksLeftThisToil = 50;
                //Make heart fleck
                FleckMaker.ThrowMetaIcon(Actor.Position, Actor.Map, FleckDefOf.Heart);
                if (Ordered)
                {
                    Actor.CheckForComp<Comp_PartnerList>().orderedHookupTick = Find.TickManager.TicksGame + orderedHookupInterval;
                }
                LogUtil.Message($"It took {Find.TickManager.TicksGame - startTick} ticks for {ActorName} to walk to {TargetName}", true);
            };
            //Fail if target is dead or downed
            proposeHookup.AddFailCondition(() => !IsTargetPawnOkay());
            yield return proposeHookup;

            //Wait for target to respond
            Toil awaitResponse = ToilMaker.MakeToil();
            awaitResponse.defaultCompleteMode = ToilCompleteMode.Instant;
            awaitResponse.initAction = delegate
            {
                List<RulePackDef> list = new();
                //See if target accepts
                successfulPass = DoesTargetPawnAcceptAdvance(out string rejectReason);
                if (successfulPass)
                {
                    //Make hearts and add correct string to the log
                    FleckMaker.ThrowMetaIcon(TargetPawn.Position, TargetPawn.Map, FleckDefOf.Heart);
                    Find.HistoryEventsManager.RecordEvent(new HistoryEvent(HistoryEventDefOf.InitiatedLovin, pawn.Named(HistoryEventArgsNames.Doer)));
                    list.Add(RomanceDefOf.HookupSucceeded);
                    LogUtil.Message($"{TargetName} agreed to hook up with {ActorName}", true);
                    //Send message if job was ordered
                    if (Ordered)
                    {
                        Messages.Message("WBR.TryHookupSuccessMessage".Translate(Actor, TargetPawn), Actor, MessageTypeDefOf.NegativeEvent, historical: false);
                    }
                }
                else
                {
                    //Remove the person from the list here
                    //Make ! mote, add rebuffed memories, and add correct string to the log
                    FleckMaker.ThrowMetaIcon(TargetPawn.Position, TargetPawn.Map, FleckDefOf.IncapIcon);
                    Actor.needs.mood.thoughts.memories.TryGainMemory(RomanceDefOf.RebuffedMyHookupAttempt, TargetPawn);
                    TargetPawn.needs.mood.thoughts.memories.TryGainMemory(RomanceDefOf.FailedHookupAttemptOnMe, Actor);
                    list.Add(RomanceDefOf.HookupFailed);
                    Comp_PartnerList comp = Actor.GetComp<Comp_PartnerList>();
                    if (!comp.Hookup.list.NullOrEmpty())
                    {
                        comp.Hookup.list.Remove(TargetPawn);
                    }
                    LogUtil.Message($"{TargetName} did not agree to hook up with {ActorName}: {rejectReason}", true);
                    //Send message if job was ordered
                    if (Ordered)
                    {
                        if (rejectReason == PawnAvailabilityExtensions.AvailabilityReasons[PawnAvailability.Free])
                        {
                            Messages.Message("WBR.TryHookupRejectedMessage".Translate(Actor, TargetPawn), Actor, MessageTypeDefOf.NegativeEvent, historical: false);
                        }
                        else
                        {
                            Messages.Message("WBR.HookupTargetNotFreeReason".Translate() + rejectReason, Actor, MessageTypeDefOf.NegativeEvent, historical: false);
                        }
                    }
                }
                //Add "tried hookup with" to the log, with the result
                Find.PlayLog.Add(new PlayLogEntry_Interaction(RomanceDefOf.TriedHookupWith, pawn, TargetPawn, list));
            };
            //Fail if target said no
            //If successfulPass was used here, the fail condition would be set to false, since at the moment of assignment, successfulPass is always true
            //It only changes to false when the delegate function inside the toil is run
            //Therefore the fail condition has to be a function that changes its value whenever successfulPass does
            awaitResponse.AddFailCondition(() => !WasSuccessfulPass);
            yield return awaitResponse;

            //At this point, successfulPass and WasSuccessfulPass is always true, so this toil always gets added
            if (WasSuccessfulPass)
            {
                Toil makeJobs = ToilMaker.MakeToil();
                makeJobs.defaultCompleteMode = ToilCompleteMode.Instant;
                makeJobs.initAction = delegate
                {
                    //By the time this delegate function runs, the actual success has been determined, so if it wasn't successful, end the toil
                    if (!WasSuccessfulPass)
                    {
                        Actor.jobs.EndCurrentJob(JobCondition.Succeeded);
                        return;
                    }
                    //If actually successful
                    //Give casual lovin job to actor, with target and bed info
                    Actor.jobs.jobQueue.EnqueueFirst(JobMaker.MakeJob(RomanceDefOf.DoLovinCasual, TargetPawn, TargetBed, TargetBed.GetSleepingSlotPos(0)), JobTag.SatisfyingNeeds);
                    //Give casual lovin job to target, with actor and bed info
                    TargetPawn.jobs.jobQueue.EnqueueFirst(JobMaker.MakeJob(RomanceDefOf.DoLovinCasual, Actor, TargetBed, TargetBed.GetSleepingSlotPos(1)), JobTag.SatisfyingNeeds);
                    //Stop the current job
                    TargetPawn.jobs.EndCurrentJob(JobCondition.InterruptOptional);
                    Actor.jobs.EndCurrentJob(JobCondition.InterruptOptional);
                };
                yield return makeJobs;
            }
        }
    }
}