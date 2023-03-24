using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace BetterRomance
{
    public class JobDriver_ProposeDate : JobDriver
    {
        private bool successfulPass = true;
        public bool WasSuccessfulPass => successfulPass;
        private Pawn Actor => GetActor();
        private Pawn TargetPawn => TargetThingA as Pawn;
        private TargetIndex TargetPawnIndex => TargetIndex.A;
        private bool IsDate => job.def == RomanceDefOf.ProposeDate;
        private const int ticksToFail = 1000;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        private bool IsTargetPawnOkay()
        {
            return !TargetPawn.Dead && !TargetPawn.Downed;
        }

        private bool DoesTargetPawnAcceptDate()
        {
            return TargetPawn.IsFree(IsDate ? RomanticActivityType.Date : RomanticActivityType.Hangout, out _) && (IsDate ? DateUtility.IsDateAppealing(TargetPawn, Actor) : DateUtility.IsHangoutAppealing(TargetPawn, Actor));
        }

        private bool TryGetDateJobs(out Job dateLeadJob, out Job dateFollowJob)
        {
            if (JoyUtility.EnjoyableOutsideNow(pawn))
            {
                if (TryFindMostBeautifulRootInDistance(40, pawn, TargetPawn, out IntVec3 root))
                {
                    if (TryFindUnforbiddenDatePath(pawn, TargetPawn, root, out List<IntVec3> list))
                    {
                        //Create date lead job
                        dateLeadJob = JobMaker.MakeJob(IsDate ? RomanceDefOf.JobDateLead : RomanceDefOf.JobHangoutLead);
                        //Add the path info to the job info
                        Log.Message("Date walk path found.");
                        dateLeadJob.targetQueueB = new List<LocalTargetInfo>();
                        for (int i = 1; i < list.Count; i++)
                        {
                            dateLeadJob.targetQueueB.Add(list[i]);
                        }
                        //Wander along path
                        dateLeadJob.locomotionUrgency = LocomotionUrgency.Amble;
                        //Add the target pawn to the job info
                        dateLeadJob.targetA = TargetPawn;

                        //Create date follow job, with wander and actor info
                        dateFollowJob = JobMaker.MakeJob(IsDate ? RomanceDefOf.JobDateFollow : RomanceDefOf.JobHangoutFollow);
                        dateFollowJob.locomotionUrgency = LocomotionUrgency.Amble;
                        dateFollowJob.targetA = Actor;

                        return true;
                    }
                }
            }
            //There can only be three targets for a job and one of them needs to be the other pawn
            //So rework this to output just a building, and make a double bed a possibility
            //A is the partner, B is the tv, C is the chair
            //else if (TryFindTelevision(out Building television, out IntVec3 p1Cell, out IntVec3 p2Cell, out Building p1Chair, out Building p2Chair))
            //{
            //    dateLeadJob = JobMaker.MakeJob(RomanceDefOf.JobDateMovie, television, p1Cell, p1Chair);
            //    dateLeadJob.locomotionUrgency = LocomotionUrgency.Walk;
            //    dateFollowJob = JobMaker.MakeJob(RomanceDefOf.JobDateMovie, television, p2Cell, p2Chair);
            //    dateFollowJob.locomotionUrgency = LocomotionUrgency.Walk;
            //    return true;
            //}
            //else if (DoesBeerExist())
            //{
            //}
            //else if (FoodAndTableAvailable())
            //{
            //}

            dateLeadJob = null;
            dateFollowJob = null;
            return false;
        }

        //No idea how this works
        private bool TryFindUnforbiddenDatePath(Pawn p1, Pawn p2, IntVec3 root, out List<IntVec3> result)
        {
            int StartRadialIndex = GenRadial.NumCellsInRadius(14f);
            int EndRadialIndex = GenRadial.NumCellsInRadius(2f);
            int RadialIndexStride = 3;
            List<IntVec3> intVec3s = new List<IntVec3> { root };
            IntVec3 intVec3 = root;
            for (int i = 0; i < 8; i++)
            {
                IntVec3 invalid = IntVec3.Invalid;
                float single1 = -1f;
                for (int j = StartRadialIndex; j > EndRadialIndex; j -= RadialIndexStride)
                {
                    IntVec3 radialPattern = intVec3 + GenRadial.RadialPattern[j];
                    if (!radialPattern.InBounds(p1.Map) || !radialPattern.Standable(p1.Map) ||
                        radialPattern.IsForbidden(p1) || radialPattern.IsForbidden(p2) ||
                        radialPattern.GetTerrain(p1.Map).avoidWander ||
                        !GenSight.LineOfSight(intVec3, radialPattern, p1.Map) || radialPattern.Roofed(p1.Map) ||
                        PawnUtility.KnownDangerAt(radialPattern, p1.Map, p1) ||
                        PawnUtility.KnownDangerAt(radialPattern, p1.Map, p2))
                    {
                        continue;
                    }

                    float lengthManhattan = 10000f;
                    foreach (IntVec3 vec3 in intVec3s)
                    {
                        lengthManhattan += (vec3 - radialPattern).LengthManhattan;
                    }

                    float lengthManhattan1 = (radialPattern - root).LengthManhattan;
                    if (lengthManhattan1 > 40f)
                    {
                        lengthManhattan *= Mathf.InverseLerp(70f, 40f, lengthManhattan1);
                    }

                    if (intVec3s.Count >= 2)
                    {
                        IntVec3 item = intVec3s[intVec3s.Count - 1] - intVec3s[intVec3s.Count - 2];
                        float angleFlat = item.AngleFlat;
                        float angleFlat1 = (radialPattern - intVec3).AngleFlat;
                        float single;
                        if (angleFlat1 <= angleFlat)
                        {
                            angleFlat -= 360f;
                            single = angleFlat1 - angleFlat;
                        }
                        else
                        {
                            single = angleFlat1 - angleFlat;
                        }

                        if (single > 110f)
                        {
                            lengthManhattan *= 0.01f;
                        }
                    }

                    if (intVec3s.Count >= 4 &&
                        (intVec3 - root).LengthManhattan < (radialPattern - root).LengthManhattan)
                    {
                        lengthManhattan *= 1E-05f;
                    }

                    if (!(lengthManhattan > single1))
                    {
                        continue;
                    }

                    invalid = radialPattern;
                    single1 = lengthManhattan;
                }

                if (single1 < 0f)
                {
                    result = null;
                    return false;
                }

                intVec3s.Add(invalid);
                intVec3 = invalid;
            }

            intVec3s.Add(root);
            result = intVec3s;
            return true;
        }

        //No idea how this works either
        private bool TryFindMostBeautifulRootInDistance(int distance, Pawn p1, Pawn p2, out IntVec3 best)
        {
            best = default;
            List<IntVec3> list = new List<IntVec3>();
            for (int i = 0; i < 200; i++)
            {
                if (CellFinder.TryFindRandomCellNear(p1.Position, p1.Map, distance,
                    c => c.InBounds(p1.Map) && !c.IsForbidden(p1) && !c.IsForbidden(p2) &&
                         p1.CanReach(c, PathEndMode.OnCell, Danger.Some), out IntVec3 item))
                {
                    list.Add(item);
                }
            }

            bool result;
            if (list.Count == 0)
            {
                result = false;
                Log.Message("No date walk destination found.");
            }
            else
            {
                List<IntVec3> list2 = (from c in list
                                       orderby BeautyUtility.AverageBeautyPerceptible(c, p1.Map) descending
                                       select c).ToList();
                best = list2.FirstOrDefault();
                list2.Reverse();
                Log.Message("Date walk destinations found from beauty " + BeautyUtility.AverageBeautyPerceptible(best, p1.Map) + " to " + BeautyUtility.AverageBeautyPerceptible(list2.FirstOrDefault(), p1.Map));
                result = true;
            }

            return result;
        }

        //private bool TryFindTelevision(out Building television, out IntVec3 p1Cell, out IntVec3 p2Cell, out Building p1Chair, out Building p2Chair)
        //{
        //    List<ThingDef> tvDefs = DefDatabase<JoyGiverDef>.GetNamed("WatchTelevision").thingDefs;
        //    IEnumerable<Building> possibleTelevisions = Actor.Map.listerBuildings.allBuildingsColonist.Where(tv => tvDefs.Contains(tv.def) && DateUtility.CanInteractWith(Actor, tv) && DateUtility.CanInteractWith(TargetPawn, tv));
        //    foreach(Building tv in possibleTelevisions)
        //    {
        //        if (WatchBuildingUtility.TryFindBestWatchCell(tv, Actor, true, out IntVec3 watchCell1, out Building chair1))
        //        {
        //            if (DateUtility.TryFindBestWatchCellNear(tv, TargetPawn, chair1, true, out IntVec3 watchCell2, out Building chair2))
        //            {
        //                television = tv;
        //                p1Cell = watchCell1;
        //                p2Cell = watchCell2;
        //                p1Chair = chair1;
        //                p2Chair = chair2;
        //                return true;
        //            }
        //        }
        //    }
        //    television = null;
        //    p1Cell = IntVec3.Invalid;
        //    p2Cell = IntVec3.Invalid;
        //    p1Chair = null;
        //    p2Chair = null;
        //    return false;
        //}

        protected override IEnumerable<Toil> MakeNewToils()
        {
            //Fail if partner gets despawned, wasn't assigned, or wanders into an area forbidden to actor
            this.FailOnDespawnedNullOrForbidden(TargetPawnIndex);
            //Walk to the target
            Toil walkToTarget = Toils_Interpersonal.GotoInteractablePosition(TargetPawnIndex);
            //attempt to make job fail if it takes too long to walk to target
            //This just makes them stand in place after one attempt to reach the target and then times out :(
            //walkToTarget.initAction = delegate
            //{
            //    ticksLeftThisToil = ticksToFail;
            //};
            //walkToTarget.tickAction = delegate
            //{
            //    if (ticksLeftThisToil <= 1)
            //    {
            //        Actor.jobs.EndCurrentJob(JobCondition.Incompletable);
            //    }
            //};
            walkToTarget.socialMode = RandomSocialMode.Off;
            yield return walkToTarget;

            //Wait if needed
            Toil wait = Toils_Interpersonal.WaitToBeAbleToInteract(pawn);
            wait.socialMode = RandomSocialMode.Off;
            yield return wait;

            //Start new toil
            Toil askOut = new Toil
            {
                defaultCompleteMode = ToilCompleteMode.Delay,
                //Make heart fleck
                initAction = delegate
                {
                    ticksLeftThisToil = 50;
                    FleckMaker.ThrowMetaIcon(GetActor().Position, GetActor().Map, IsDate ? FleckDefOf.Heart : RomanceDefOf.FriendHeart);
                },
            };
            //Fail if target is downed or dead
            askOut.AddFailCondition(() => !IsTargetPawnOkay());
            yield return askOut;

            //Start new toil
            Toil awaitResponse = new Toil
            {
                defaultCompleteMode = ToilCompleteMode.Instant,
                initAction = delegate
                {
                    List<RulePackDef> list = new List<RulePackDef>();
                    successfulPass = DoesTargetPawnAcceptDate();
                    //Make heart or ! fleck depending on success
                    if (successfulPass)
                    {
                        //Make hearts and add correct string to the log
                        FleckMaker.ThrowMetaIcon(TargetPawn.Position, TargetPawn.Map, IsDate ? FleckDefOf.Heart : RomanceDefOf.FriendHeart);
                        list.Add(IsDate ? RomanceDefOf.DateSucceeded : RomanceDefOf.HangoutSucceeded);
                    }
                    else
                    {
                        //Make ! mote, add rebuffed memories, and add correct string to the log
                        FleckMaker.ThrowMetaIcon(TargetPawn.Position, TargetPawn.Map, FleckDefOf.IncapIcon);
                        Actor.needs.mood.thoughts.memories.TryGainMemory(IsDate ? RomanceDefOf.RebuffedMyDateAttempt : RomanceDefOf.RebuffedMyHangoutAttempt, TargetPawn);
                        TargetPawn.needs.mood.thoughts.memories.TryGainMemory(IsDate ? RomanceDefOf.FailedDateAttemptOnMe : RomanceDefOf.FailedHangoutAttemptOnMe, Actor);
                        list.Add(IsDate ? RomanceDefOf.DateFailed : RomanceDefOf.HangoutFailed);
                        Actor.GetComp<Comp_PartnerList>().Date.list.Remove(TargetPawn);
                    }
                    //Add "asked on a date" to the log, with the result
                    Find.PlayLog.Add(new PlayLogEntry_Interaction(IsDate ? RomanceDefOf.AskedForDate : RomanceDefOf.AskedForHangout, pawn, TargetPawn, list));
                }
            };
            //Fails if target doesn't agree
            awaitResponse.AddFailCondition(() => !WasSuccessfulPass);
            yield return awaitResponse;

            if (WasSuccessfulPass)
            {
                //Start new toil
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
                        //If no date activities were found, end the toil
                        if (!TryGetDateJobs(out Job leadJob, out Job followJob))
                        {
                            return;
                        }
                        //Give the jobs we just created
                        Actor.jobs.jobQueue.EnqueueFirst(leadJob);
                        TargetPawn.jobs.jobQueue.EnqueueFirst(followJob);
                        //Stop the current job
                        TargetPawn.jobs.EndCurrentJob(JobCondition.InterruptOptional);
                        Actor.jobs.EndCurrentJob(JobCondition.InterruptOptional);
                    }
                };
            }
        }
    }
}