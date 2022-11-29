using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using Verse.AI;
using HarmonyLib;

namespace BetterRomance
{
    public class JobDriver_JobDateMovie : JobDriver
    {
        private readonly TargetIndex PartnerInd = TargetIndex.A;
        private readonly TargetIndex TVInd = TargetIndex.B;
        private readonly TargetIndex ChairInd = TargetIndex.C;
        private Pawn Partner => (Thing)job.GetTarget(PartnerInd) as Pawn;
        private Thing TV => (Thing)job.GetTarget(TVInd);
        private Thing Chair => (Thing)job.GetTarget(ChairInd);
        private Building_Bed Bed => Chair as Building_Bed;
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            //Reserve the tv, up to max allowed pawns in def
            if (!pawn.Reserve(TargetB, job, job.def.joyMaxParticipants))
            {
                return false;
            }
            //Reserve the chair
            if (!pawn.ReserveSittableOrSpot(Chair.Position, job))
            {
                return false;
            }
            //If the 'chair' is a bed, reserve it
            //Currently doesn't allow a bed to be chosen, will need to change that
            if (Bed != null)
            {
                if (!pawn.Reserve(TargetC, job, Bed.SleepingSlotsCount))
                {
                    return false;
                }
            }
            return true;
        }

        public override bool CanBeginNowWhileLyingDown()
        {
            if (Bed != null)
            {
                return JobInBedUtility.InBedOrRestSpotNow(pawn, base.TargetC);
            }
            return false;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            //End if any of the important things get despawned or weren't assigned
            this.EndOnDespawnedOrNull(TVInd);
            this.EndOnDespawnedOrNull(ChairInd);
            this.EndOnDespawnedOrNull(PartnerInd);
            //Fail if partner is unconscious
            this.FailOn(() => !Partner.health.capacities.CanBeAwake);
            //I think everything should be reserved already
            //Make empty toil since contents depend on if Chair is a bed or not
            Toil watch;
            //Go to the correct object and create appropriate watch toil
            if (Bed != null)
            {
                this.KeepLyingDown(ChairInd);
                yield return Toils_Bed.GotoBed(ChairInd);
                watch = Toils_LayDown.LayDown(ChairInd, true, false, false);
                watch.AddFailCondition(() => !watch.actor.Awake());
            }
            else
            {
                yield return Toils_Goto.GotoThing(ChairInd, Chair.Position);
                watch = new Toil();
            }

            //Wait for partner
            yield return new Toil
            {
                initAction = delegate { ticksLeftThisToil = 300; },
                tickAction = delegate
                {
                    if (DateUtility.IsPartnerNearGoal(Chair, Partner))
                    {
                        ticksLeftThisToil = 0;
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Delay
            };

            watch.AddPreTickAction(delegate { WatchTickAction(); });
            watch.AddFinishAction(delegate { JoyUtility.TryGainRecRoomThought(pawn); });
            watch.defaultCompleteMode = ToilCompleteMode.Delay;
            watch.defaultDuration = job.def.joyDuration;
            watch.handlingFacing = true;
            watch.socialMode = RandomSocialMode.SuperActive;
            if (TV.def.building != null && TV.def.building.effectWatching != null)
            {
                watch.WithEffect(() => TV.def.building.effectWatching, EffectTargetGetter);
            }
            yield return watch;
            LocalTargetInfo EffectTargetGetter()
            {
                return TV.OccupiedRect().RandomCell + IntVec3.North.RotatedBy(TV.Rotation);
            }
        }

        private void WatchTickAction()
        {
            if (!((Building)TV).TryGetComp<CompPowerTrader>().PowerOn)
            {
                EndJobWith(JobCondition.Incompletable);
            }
            else
            {
                pawn.rotationTracker.FaceCell(TV.Position);
                pawn.GainComfortFromCellIfPossible();
                JoyUtility.JoyTickCheckEnd(pawn, joySource: (Building)TV);
            }
        }
    }
}