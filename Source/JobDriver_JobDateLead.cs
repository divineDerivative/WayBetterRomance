using DivineFramework;
using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace BetterRomance
{
    public class JobDriver_JobDateLead : JobDriver
    {
        private readonly TargetIndex PartnerInd = TargetIndex.A;
        private Pawn Actor => GetActor();
        private Pawn Partner => (Pawn)(Thing)job.GetTarget(PartnerInd);
        private bool IsDate => job.def == RomanceDefOf.JobDateLead;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Partner, job, errorOnFailed: errorOnFailed);
        }

        public override RandomSocialMode DesiredSocialMode()
        {
            return RandomSocialMode.SuperActive;
        }

        //Generates a toil to go to the next tile in the path
        public Toil GotoCell(LocalTargetInfo target)
        {
            Toil toil = Toils_Goto.GotoCell(target.Cell, PathEndMode.OnCell);
            //Gain joy while walking
            toil.tickAction = delegate { DateUtility.DateTickAction(Actor, IsDate); };
            toil.debugName = "DateLeadGoto";
            //Fail if partner despawns, dies, or stops the follow job
            toil.AddFailCondition(() => DateUtility.FailureCheck(Partner, IsDate ? RomanceDefOf.JobDateFollow : RomanceDefOf.JobHangoutFollow));
            return toil;
        }

        //Wait for partner at each tile
        private Toil WaitForPartner()
        {
            Toil toil = ToilMaker.MakeToil("DateLeadWait");
            //Hang out for 700 ticks and gain joy
            toil.defaultCompleteMode = ToilCompleteMode.Delay;
            toil.initAction = delegate { ticksLeftThisToil = 700; };
            toil.tickAction = delegate { DateUtility.DateTickAction(Actor, IsDate); };
            //Fail if partner is no longer capable of continuing
            toil.AddFailCondition(() => DateUtility.FailureCheck(Partner, IsDate ? RomanceDefOf.JobDateFollow : RomanceDefOf.JobHangoutFollow));
            return toil;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_General.Do(delegate
                {
                    LogUtil.Message($"Date lead job started for {Actor.Name.ToStringShort}", true);
                }
            );
            //Alternate goto and wait toils for each tile in path
            foreach (LocalTargetInfo target in job.targetQueueB)
            {
                yield return GotoCell(target.Cell);
                yield return WaitForPartner();
            }
            Toil finish = ToilMaker.MakeToil("DateFinish");
            finish.initAction = delegate
            {
                if (Partner.CurJobDef == (IsDate ? RomanceDefOf.JobDateFollow : RomanceDefOf.JobHangoutFollow))
                {
                    LogUtil.Message($"Date for {Actor.Name.ToStringShort} and {Partner.Name.ToStringShort} finished successfully", true);
                    Partner.jobs.EndCurrentJob(JobCondition.Succeeded);
                    Actor.jobs.EndCurrentJob(JobCondition.Succeeded);
                }
            };
            yield return finish;
        }
    }
}