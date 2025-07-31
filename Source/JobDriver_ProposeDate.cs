using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using static BetterRomance.WBRLogger;
#if !v1_4
using LudeonTK;
#endif

namespace BetterRomance
{
    public class JobDriver_ProposeDate : JobDriver
    {
        private bool successfulPass = true;
        private int waitCount = 0;
        public bool WasSuccessfulPass => successfulPass;
        private Pawn Actor => GetActor();
        private Pawn TargetPawn => TargetThingA as Pawn;
        private TargetIndex TargetPawnIndex => TargetIndex.A;
        private bool IsDate => job.def == RomanceDefOf.ProposeDate;
        private string ActorName => Actor.Name.ToStringShort;
        private string TargetName => TargetPawn.Name.ToStringShort;

        public override bool TryMakePreToilReservations(bool errorOnFailed) => pawn.Reserve(TargetPawn, job, errorOnFailed: errorOnFailed);

        private bool IsTargetPawnOkay() => !TargetPawn.Dead && !TargetPawn.Downed;

        private bool DoesTargetPawnAcceptDate() => IsDate ? TargetPawn.IsFree(RomanticActivityType.Date, out _) && DateUtility.IsDateAppealing(TargetPawn, Actor) : TargetPawn.IsFree(RomanticActivityType.Hangout, out _) && DateUtility.IsHangoutAppealing(TargetPawn, Actor);

        private bool TryGetDateJobs(out Job dateLeadJob, out Job dateFollowJob)
        {
            if (JoyUtility.EnjoyableOutsideNow(pawn))
            {
                if (TryFindMostBeautifulRootInDistance(40, pawn, TargetPawn, out IntVec3 root))
                {
                    if (TryFindUnforbiddenDatePath(pawn, TargetPawn, root, out List<IntVec3> list))
                    {
                        //Create date lead job
                        dateLeadJob = JobMaker.MakeJob(IsDate ? RomanceDefOf.JobDateLead : RomanceDefOf.JobHangoutLead, TargetPawn, list[0]);
                        //Add the path info to the job info
                        LogUtil.Message("Date walk path found.", true);
                        dateLeadJob.targetQueueB = new();
                        for (int i = 1; i < list.Count; i++)
                        {
                            dateLeadJob.targetQueueB.Add(list[i]);
                        }
                        //Wander along path
                        dateLeadJob.locomotionUrgency = LocomotionUrgency.Amble;

                        //Create date follow job, with wander and actor info
                        dateFollowJob = JobMaker.MakeJob(IsDate ? RomanceDefOf.JobDateFollow : RomanceDefOf.JobHangoutFollow, Actor);
                        dateFollowJob.locomotionUrgency = LocomotionUrgency.Amble;
                        LogUtil.Message($"Jobs created for {ActorName} and {TargetName}", true);
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

        //This is the same as WalkPathFinder.TryFindWalkPath, which is used for the 'go for a walk' job, with checks added for the second pawn
        //Finds a path that starts and ends at root; if given completely open ground it starts as a diamond shape, but the last two cells go in a different direction
        private static bool TryFindUnforbiddenDatePath(Pawn p1, Pawn p2, IntVec3 root, out List<IntVec3> result)
        {
            int StartRadialIndex = GenRadial.NumCellsInRadius(14f);
            int EndRadialIndex = GenRadial.NumCellsInRadius(2f);
            int RadialIndexStride = 3;
            //root gets added again at the end and then the first root is skipped when yielding the results
            List<IntVec3> cellList = [root];
            //currentCell is the cell that was most recently added to the result list
            IntVec3 currentCell = root;
            //We want to add 8 cells to the list
            for (int i = 0; i < 8; i++)
            {
                IntVec3 tempCell = IntVec3.Invalid;
                float tempDistance = -1f;
                //This loop happens 200 times! D:
                for (int j = StartRadialIndex; j > EndRadialIndex; j -= RadialIndexStride)
                {
                    //nextCell is the cell being evaluated to potentially be added to the result list
                    IntVec3 nextCell = currentCell + GenRadial.RadialPattern[j];
                    if (nextCell.WalkCellValidator(p1.Map) && nextCell.WalkCellValidator(p1) && nextCell.WalkCellValidator(p2) && GenSight.LineOfSight(currentCell, nextCell, p1.Map))
                    {
                        //Not sure why we start with 10k
                        float score = 10000f;
                        foreach (IntVec3 vec3 in cellList)
                        {
                            //LengthManhattan is |x| + |z|; the number of cells needed to travel between the two vectors using only adjacent cells
                            score += (vec3 - nextCell).LengthManhattan;
                        }

                        float distanceFromRoot = (nextCell - root).LengthManhattan;
                        if (distanceFromRoot > 40f)
                        {
                            //Lower score if it's too far away; will be 0 at > 70f
                            score *= Mathf.InverseLerp(70f, 40f, distanceFromRoot);
                        }

                        if (cellList.Count >= 2)
                        {
                            //Difference between the last two cells in the list
                            IntVec3 item = cellList[cellList.Count - 1] - cellList[cellList.Count - 2];
                            //Not sure what AngleFlat means, but we're comparing it for the difference above and the difference between the last cell in the list and the potential next cell
                            //I think it might be the angle of the line between the two points with reference to some axis, but that doesn't always match up
                            float angleFlat = item.AngleFlat;
                            float angleFlat1 = (nextCell - currentCell).AngleFlat;
                            //This just makes sure the result will be a positive number
                            if (angleFlat1 <= angleFlat)
                            {
                                angleFlat -= 360f;
                            }
                            float single = angleFlat1 - angleFlat;
                            //If whatever single means is too big, lower the score by a lot
                            if (single > 110f)
                            {
                                score *= 0.01f;
                            }
                        }
                        //If we have at least four cells already, and the distance between the last cell and the root is less than the distance between the potential next cell and the root, super decrease the score
                        //This might be what makes it start circling around back towards the root
                        if (cellList.Count >= 4 && (currentCell - root).LengthManhattan < (nextCell - root).LengthManhattan)
                        {
                            score *= 0.00001f;
                        }
                        //And we're looking for the cell with the highest score
                        if (score > tempDistance)
                        {
                            tempCell = nextCell;
                            tempDistance = score;
                        }
                    }
                }//end of inner for loop
                //Fail if we never found a suitable cell
                if (tempDistance < 0f)
                {
                    LogUtil.Message($"Failed to find path after {cellList.Count} cells", true);
                    result = null;
                    return false;
                }
                //Add the cell to the list and make it the new currentCell
                cellList.Add(tempCell);
                currentCell = tempCell;
            }//end of outer for loop
            //Add root again at the end and return
            cellList.Add(root);
            result = cellList;
            return true;
        }

        public static void DebugFlashDatePath(IntVec3 root, int numEntries = 8)
        {
            Map currentMap = Find.CurrentMap;
            if (!TryFindUnforbiddenDatePath(currentMap.mapPawns.FreeColonistsSpawned.First(), currentMap.mapPawns.FreeColonistsSpawned.Last(), root, out List<IntVec3> result))
            {
                currentMap.debugDrawer.FlashCell(root, 0.2f, "NOPATH");
                return;
            }
            for (int i = 0; i < result.Count; i++)
            {
                currentMap.debugDrawer.FlashCell(result[i], i / (float)numEntries, i.ToString());
                if (i > 0)
                {
                    currentMap.debugDrawer.FlashLine(result[i], result[i - 1]);
                }
            }
        }

        [DebugAction(category = "General", actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.PlayingOnMap, hideInSubMenu = true)]
        private static void FlashDatePath() => DebugFlashDatePath(UI.MouseCell());

        //No idea how this works either
        private bool TryFindMostBeautifulRootInDistance(int distance, Pawn p1, Pawn p2, out IntVec3 best)
        {
            best = default;
            List<IntVec3> list = new();
            for (int i = 0; i < 200; i++)
            {
                if (CellFinder.TryFindRandomCellNear(p1.Position, p1.Map, distance,
                    c => c.WalkCellValidator(p1.Map) && c.WalkCellValidator(p1) && c.WalkCellValidator(p2), out IntVec3 item))
                {
                    list.Add(item);
                }
            }

            bool result;
            if (list.Count == 0)
            {
                result = false;
                LogUtil.Message("No date walk destination found.", true);
            }
            else
            {
                List<IntVec3> list2 = (from c in list
                                       orderby BeautyUtility.AverageBeautyPerceptible(c, p1.Map) descending
                                       select c).ToList();
                best = list2.FirstOrDefault();
                list2.Reverse();
                LogUtil.Message("Date walk destinations found from beauty " + BeautyUtility.AverageBeautyPerceptible(best, p1.Map) + " to " + BeautyUtility.AverageBeautyPerceptible(list2.FirstOrDefault(), p1.Map), true);
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
            walkToTarget.AddPreInitAction(delegate
            {
                ticksLeftThisToil = DateUtility.walkingTicks;
                LogUtil.Message($"{ActorName} is going to ask {TargetName} {(IsDate ? "on a date" : "to hang out")}", true);
            });
            walkToTarget.AddPreTickAction(delegate
            {
                //Fail if it takes too long to walk to target
                if (ticksLeftThisToil <= 1)
                {
                    if (DateUtility.DistanceFailure(Actor, TargetPawn, ref waitCount, ref ticksLeftThisToil))
                    {
                        LogUtil.Message($"{ActorName} gave up asking {TargetName} {(IsDate ? "for a date" : "to hang out")} because it took too long to find them", true);
                        Actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                    }
                }
            });
            walkToTarget.socialMode = RandomSocialMode.Off;
            yield return walkToTarget;

            //Wait if needed
            Toil wait = Toils_Interpersonal.WaitToBeAbleToInteract(pawn);
            yield return wait;

            //Start new toil
            Toil askOut = Toils_General.Do(delegate
            {
                //Make heart fleck
                ticksLeftThisToil = 50;
                FleckMaker.ThrowMetaIcon(GetActor().Position, GetActor().Map, IsDate ? FleckDefOf.Heart : RomanceDefOf.FriendHeart);
                LogUtil.Message($"It took {Find.TickManager.TicksGame - startTick} ticks for {ActorName} to walk to {TargetName}", true);
            });
            askOut.defaultCompleteMode = ToilCompleteMode.Delay;

            //Fail if target is downed or dead
            askOut.AddFailCondition(() => !IsTargetPawnOkay());
            yield return askOut;

            //Start new toil
            Toil awaitResponse = ToilMaker.MakeToil("AwaitResponse");
            awaitResponse.defaultCompleteMode = ToilCompleteMode.Instant;
            awaitResponse.initAction = delegate
            {
                List<RulePackDef> list = new();
                successfulPass = DoesTargetPawnAcceptDate();
                //Make heart or ! fleck depending on success
                if (successfulPass)
                {
                    //Make hearts and add correct string to the log
                    FleckMaker.ThrowMetaIcon(TargetPawn.Position, TargetPawn.Map, IsDate ? FleckDefOf.Heart : RomanceDefOf.FriendHeart);
                    list.Add(IsDate ? RomanceDefOf.DateSucceeded : RomanceDefOf.HangoutSucceeded);
                    LogUtil.Message($"{TargetName} agreed to {(IsDate ? "a date" : "hang out")} with {ActorName}", true);
                }
                else
                {
                    //Make ! mote, add rebuffed memories, and add correct string to the log
                    FleckMaker.ThrowMetaIcon(TargetPawn.Position, TargetPawn.Map, FleckDefOf.IncapIcon);
                    Actor.needs.mood.thoughts.memories.TryGainMemory(IsDate ? RomanceDefOf.RebuffedMyDateAttempt : RomanceDefOf.RebuffedMyHangoutAttempt, TargetPawn);
                    TargetPawn.needs.mood.thoughts.memories.TryGainMemory(IsDate ? RomanceDefOf.FailedDateAttemptOnMe : RomanceDefOf.FailedHangoutAttemptOnMe, Actor);
                    list.Add(IsDate ? RomanceDefOf.DateFailed : RomanceDefOf.HangoutFailed);
                    Actor.GetComp<Comp_PartnerList>().Date.list.Remove(TargetPawn);
                    LogUtil.Message($"{TargetName} did not agree to {(IsDate ? "a date" : "hang out")} with {ActorName}", true);
                }
                //Add "asked on a date" to the log, with the result
                Find.PlayLog.Add(new PlayLogEntry_Interaction(IsDate ? RomanceDefOf.AskedForDate : RomanceDefOf.AskedForHangout, pawn, TargetPawn, list));
            };
            //Fails if target doesn't agree
            awaitResponse.AddFailCondition(() => !WasSuccessfulPass);
            yield return awaitResponse;

            if (WasSuccessfulPass)
            {
                //Start new toil
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
                    //If no date activities were found, end the toil
                    if (!TryGetDateJobs(out Job leadJob, out Job followJob))
                    {
                        LogUtil.Message($"Unable to create jobs for {(IsDate ? "date" : "hang out")}", true);
                        Actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                        return;
                    }
                    //Give the jobs we just created
                    Actor.jobs.jobQueue.EnqueueFirst(leadJob, JobTag.SatisfyingNeeds);
                    TargetPawn.jobs.jobQueue.EnqueueFirst(followJob, JobTag.SatisfyingNeeds);
                    //Stop the current job
                    TargetPawn.jobs.EndCurrentJob(JobCondition.InterruptOptional);
                    Actor.jobs.EndCurrentJob(JobCondition.InterruptOptional);
                    LogUtil.Message($"JobDriver_ProposeDate finished", true);
                };
                yield return makeJobs;
            }
        }
    }
}
