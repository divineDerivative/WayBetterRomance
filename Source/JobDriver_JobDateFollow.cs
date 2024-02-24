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
                initAction = delegate
                {
                    ticksLeftThisToil = 1;
                    LogUtil.Message($"Date follow job started for {Actor.Name.ToStringShort}", true);
                }
            };
            yield return WaitForPartnerJob;
            //New toil
            Toil FollowPartner = new Toil
            {
                defaultCompleteMode = ToilCompleteMode.Delay,
                //Chase after partner
                initAction = delegate
                {
                    ticksLeftThisToil = 200;
                    Actor.pather.StartPath(Partner, PathEndMode.Touch);
                },
                //Gain joy
                tickAction = delegate { DateUtility.DateTickAction(Actor, IsDate); },
                debugName = "DateFollowPartner",
            };
            //Fail if partner despawns, dies, or stops the lead job
            FollowPartner.AddFailCondition(() => DateUtility.FailureCheck(Partner, IsDate ? RomanceDefOf.JobDateLead : RomanceDefOf.JobHangoutLead));
            //Yield this toil 100 times
            //Date lead job has a finite end, so the date won't go on forever
            for (int i = 0; i < 100; i++)
            {
                yield return FollowPartner;
            }
        }
    }
}