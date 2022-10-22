using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace BetterRomance
{
    public class JobDriver_ProposeDate : JobDriver
    {
        public bool successfulPass = true;
        public bool WasSuccessfulPass => successfulPass;

        private Pawn Actor => GetActor();

        private Pawn TargetPawn => TargetThingA as Pawn;

        private TargetIndex TargetPawnIndex => TargetIndex.A;
        private bool IsDate => job.def == RomanceDefOf.ProposeDate ? true : false;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }
        //No idea how this works
        private bool TryFindUnforbiddenDatePath(Pawn p1, Pawn p2, IntVec3 root, out List<IntVec3> result)
        {
            int StartRadialIndex = GenRadial.NumCellsInRadius(14f);
            int EndRadialIndex = GenRadial.NumCellsInRadius(2f);
            int RadialIndexStride = 3;
            List<IntVec3> intVec3s = new List<IntVec3> {root};
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

        private bool IsTargetPawnOkay()
        {
            return !TargetPawn.Dead && !TargetPawn.Downed;
        }

        private bool DoesTargetPawnAcceptDate()
        {
            return RomanceUtilities.IsPawnFree(TargetPawn) && ((IsDate && RomanceUtilities.IsDateAppealing(TargetPawn, Actor)) || (!IsDate && RomanceUtilities.IsHangoutAppealing(TargetPawn, Actor)));
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
                //Log.Message("No date walk destination found.");
            }
            else
            {
                List<IntVec3> list2 = (from c in list
                    orderby BeautyUtility.AverageBeautyPerceptible(c, p1.Map) descending
                    select c).ToList();
                best = list2.FirstOrDefault();
                list2.Reverse();
                //Log.Message("Date walk destinations found from beauty " +
                //            BeautyUtility.AverageBeautyPerceptible(best, p1.Map) + " to " +
                //            BeautyUtility.AverageBeautyPerceptible(list2.FirstOrDefault(), p1.Map));
                result = true;
            }

            return result;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            //Stop if target is not free
            if (!RomanceUtilities.IsPawnFree(TargetPawn))
            {
                yield break;
            }
            //Walk to the target
            yield return Toils_Goto.GotoThing(TargetPawnIndex, PathEndMode.Touch);
            //Start new toil
            Toil AskOut = new Toil();
            //Fail if target is downed or dead
            AskOut.AddFailCondition(() => !IsTargetPawnOkay());
            AskOut.defaultCompleteMode = ToilCompleteMode.Delay;
            //Make heart fleck
            AskOut.initAction = delegate
            {
                ticksLeftThisToil = 50;
                FleckMaker.ThrowMetaIcon(GetActor().Position, GetActor().Map, FleckDefOf.Heart);
            };
            yield return AskOut;
            //Start new toil
            Toil AwaitResponse = new Toil
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
                        FleckMaker.ThrowMetaIcon(TargetPawn.Position, TargetPawn.Map, FleckDefOf.Heart);
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
            AwaitResponse.AddFailCondition(() => !WasSuccessfulPass);
            yield return AwaitResponse;

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
                        //Create date lead job
                        Job jobDateLead = new Job(IsDate ? RomanceDefOf.JobDateLead : RomanceDefOf.JobHangoutLead);
                        //But stop if a starting tile isn't found
                        if (!TryFindMostBeautifulRootInDistance(40, pawn, TargetPawn, out IntVec3 root))
                        {
                            return;
                        }
                        //Find a path to walk during date
                        if (TryFindUnforbiddenDatePath(pawn, TargetPawn, root, out List<IntVec3> list))
                        {
                            //Add the path info to the job info
                            //Log.Message("Date walk path found.");
                            jobDateLead.targetQueueB = new List<LocalTargetInfo>();
                            for (int i = 1; i < list.Count; i++)
                            {
                                jobDateLead.targetQueueB.Add(list[i]);
                            }
                            //Wander along path
                            jobDateLead.locomotionUrgency = LocomotionUrgency.Amble;
                            //Add the target pawn to the job info
                            jobDateLead.targetA = TargetPawn;
                            //Give the job to the pawn
                            Actor.jobs.jobQueue.EnqueueFirst(jobDateLead);
                            //Create date follow job, with wander and actor info
                            Job jobDateFollow = new Job(IsDate ? RomanceDefOf.JobDateFollow : RomanceDefOf.JobHangoutFollow)
                            {
                                locomotionUrgency = LocomotionUrgency.Amble, targetA = Actor
                            };
                            //Give the job to the target
                            TargetPawn.jobs.jobQueue.EnqueueFirst(jobDateFollow);
                            //Allow the date to be interrupted
                            TargetPawn.jobs.EndCurrentJob(JobCondition.InterruptOptional);
                            Actor.jobs.EndCurrentJob(JobCondition.InterruptOptional);
                        }
                        else
                        {
                            //Log.Message("No date walk path found.");
                        }
                    }
                };
            }
        }
    }
}