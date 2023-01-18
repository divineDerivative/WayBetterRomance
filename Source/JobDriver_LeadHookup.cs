using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace BetterRomance
{
    public class JobDriver_LeadHookup : JobDriver
    {
        public bool successfulPass = true;
        //This is necessary, see below
        public bool WasSuccessfulPass => successfulPass;
        private Pawn Actor => GetActor();
        private Pawn TargetPawn => TargetThingA as Pawn;
        private Building_Bed TargetBed => TargetThingB as Building_Bed;
        private TargetIndex TargetPawnIndex => TargetIndex.A;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        private bool DoesTargetPawnAcceptAdvance()
        {
            return RomanceUtilities.IsPawnFree(TargetPawn) && RomanceUtilities.WillPawnTryHookup(TargetPawn) && Rand.Range(0.05f, 1f) < RomanceUtilities.HookupSuccessChance(TargetPawn, Actor);
        }

        private bool IsTargetPawnOkay()
        {
            return !TargetPawn.Dead && !TargetPawn.Downed;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            //Fail if partner gets despawned, wasn't assigned, or wanders into an area forbidden to actor
            this.FailOnDespawnedNullOrForbidden(TargetPawnIndex);
            //Stop if the target is not available
            if (!RomanceUtilities.IsPawnFree(TargetPawn))
            {
                yield break;
            }
            //Walk to the target
            Toil walkToTarget = Toils_Interpersonal.GotoInteractablePosition(TargetPawnIndex);
            walkToTarget.socialMode = RandomSocialMode.Off;
            yield return walkToTarget;
            //This is from vanilla, not sure why it's needed
            Toil finalGoto = Toils_Interpersonal.GotoInteractablePosition(TargetPawnIndex);
            yield return Toils_Jump.JumpIf(finalGoto, () => !TargetPawn.Awake());

            //Wait if needed
            Toil wait = Toils_Interpersonal.WaitToBeAbleToInteract(pawn);
            wait.socialMode = RandomSocialMode.Off;
            yield return wait;

            finalGoto.socialMode = RandomSocialMode.Off;
            Toil proposeHookup = new Toil
            {
                defaultCompleteMode = ToilCompleteMode.Delay,
                //Make heart fleck
                initAction = delegate
                {
                    ticksLeftThisToil = 50;
                    FleckMaker.ThrowMetaIcon(Actor.Position, Actor.Map, FleckDefOf.Heart);
                },
            };
            //Fail if target is dead or downed
            proposeHookup.AddFailCondition(() => !IsTargetPawnOkay());
            yield return proposeHookup;

            Toil awaitResponse = new Toil
            {
                defaultCompleteMode = ToilCompleteMode.Instant,
                initAction = delegate
                {
                    List<RulePackDef> list = new List<RulePackDef>();
                    //See if target accepts
                    successfulPass = DoesTargetPawnAcceptAdvance();
                    if (successfulPass)
                    {
                        //Make hearts and add correct string to the log
                        FleckMaker.ThrowMetaIcon(TargetPawn.Position, TargetPawn.Map, FleckDefOf.Heart);
                        Find.HistoryEventsManager.RecordEvent(new HistoryEvent(HistoryEventDefOf.InitiatedLovin, pawn.Named(HistoryEventArgsNames.Doer)));
                        list.Add(RomanceDefOf.HookupSucceeded);
                    }
                    else
                    {
                        //Remove the person from the list here
                        //Make ! mote, add rebuffed memories, and add correct string to the log
                        FleckMaker.ThrowMetaIcon(TargetPawn.Position, TargetPawn.Map, FleckDefOf.IncapIcon);
                        Actor.needs.mood.thoughts.memories.TryGainMemory(RomanceDefOf.RebuffedMyHookupAttempt, TargetPawn);
                        TargetPawn.needs.mood.thoughts.memories.TryGainMemory(RomanceDefOf.FailedHookupAttemptOnMe, Actor);
                        list.Add(RomanceDefOf.HookupFailed);
                        Actor.GetComp<Comp_PartnerList>().Hookup.list.Remove(TargetPawn);
                    }
                    //Add "tried hookup with" to the log, with the result
                    Find.PlayLog.Add(new PlayLogEntry_Interaction(RomanceDefOf.TriedHookupWith, pawn, TargetPawn, list));
                }
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
                yield return new Toil
                {
                    defaultCompleteMode = ToilCompleteMode.Instant,
                    initAction = delegate
                    {
                        //By the time this delegate function runs, the actual success has been determined, so if it wasn't successful, end the toil
                        if (!WasSuccessfulPass)
                        {
                            return;
                        }
                        //If actually successful
                        //Give casual lovin job to actor, with target and bed info
                        Actor.jobs.jobQueue.EnqueueFirst(new Job(RomanceDefOf.DoLovinCasual, TargetPawn, TargetBed, TargetBed.GetSleepingSlotPos(0)));
                        //Give casual lovin job to target, with actor and bed info
                        TargetPawn.jobs.jobQueue.EnqueueFirst(new Job(RomanceDefOf.DoLovinCasual, Actor, TargetBed, TargetBed.GetSleepingSlotPos(1)));
                        //Allow them to be interrupted
                        TargetPawn.jobs.EndCurrentJob(JobCondition.InterruptOptional);
                        Actor.jobs.EndCurrentJob(JobCondition.InterruptOptional);
                    }
                };
            }
        }
    }
}