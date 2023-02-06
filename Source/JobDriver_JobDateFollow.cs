using System.Collections.Generic;
using System.Diagnostics;
using RimWorld;
using Verse;
using Verse.AI;

namespace BetterRomance
{
    public class JobDriver_JobDateFollow : JobDriver
    {
        private readonly TargetIndex PartnerInd = TargetIndex.A;
        private Pawn Actor => GetActor();
        private Pawn Partner => (Pawn)(Thing)job.GetTarget(PartnerInd);
        private bool IsDate => job.def == RomanceDefOf.JobDateFollow;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        public override RandomSocialMode DesiredSocialMode()
        {
            return RandomSocialMode.SuperActive;
        }

        [DebuggerHidden]
        protected override IEnumerable<Toil> MakeNewToils()
        {
            // Wait a tick to avoid a 1.1 issue where the date leader now must end their current
            // job after the date follower, causing the date follower to think the leader was no
            // longer leading the date and end this job.
            Toil WaitForPartnerJob = new Toil
            {
                defaultCompleteMode = ToilCompleteMode.Delay,
                initAction = delegate { ticksLeftThisToil = 1; }
            };
            yield return WaitForPartnerJob;
            //New toil
            Toil FollowPartner = new Toil
            {
                defaultCompleteMode = ToilCompleteMode.Delay
            };
            //Fail if partner despawns, dies, or stops the lead job
            FollowPartner.AddFailCondition(() => !Partner.Spawned);
            FollowPartner.AddFailCondition(() => Partner.Dead);
            FollowPartner.AddFailCondition(() => Partner.CurJob.def != (IsDate ? RomanceDefOf.JobDateLead : RomanceDefOf.JobHangoutLead));
            //Chase after partner
            FollowPartner.initAction = delegate
            {
                ticksLeftThisToil = 200;
                Actor.pather.StartPath(Partner, PathEndMode.Touch);
            };
            //Gain joy
            FollowPartner.tickAction = delegate { DateUtility.DateTickAction(Actor, IsDate); };
            //Yield this toil 100 times
            //Date lead job has a finite end, so the date won't go on forever
            for (int i = 0; i < 100; i++)
            {
                yield return FollowPartner;
            }
        }
    }
}