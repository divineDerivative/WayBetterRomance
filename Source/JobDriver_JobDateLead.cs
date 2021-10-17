using System.Collections.Generic;
using System.Diagnostics;
using RimWorld;
using Verse;
using Verse.AI;

namespace BetterRomance
{
    public class JobDriver_JobDateLead : JobDriver
    {
        private readonly TargetIndex PartnerInd = TargetIndex.A;
        private int ticksLeft = 20;

        private Pawn Actor => GetActor();

        private Pawn Partner => (Pawn) (Thing) job.GetTarget(PartnerInd);

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref ticksLeft, "ticksLeft");
        }

        public override RandomSocialMode DesiredSocialMode()
        {
            return RandomSocialMode.SuperActive;
        }

        //Generates a toil to go to the next tile in the path
        public Toil GotoCell(LocalTargetInfo target)
        {
            Toil toil = new Toil();
            toil.initAction = delegate
            {
                toil.actor.pather.StartPath(target, PathEndMode.OnCell);
            };
            //Gain joy while walking
            toil.tickAction = delegate
            { JoyUtility.JoyTickCheckEnd(Actor, JoyTickFullJoyAction.None); } ;
            toil.defaultCompleteMode = ToilCompleteMode.PatherArrival;
            //Fail if partner despawns, dies, or stops the follow job
            toil.AddFailCondition(() => !Partner.Spawned);
            toil.AddFailCondition(() => Partner.Dead);
            toil.AddFailCondition(() => Partner.CurJob.def != RomanceDefOf.JobDateFollow);
            return toil;
        }
        //Wait for partner at each tile
        private Toil WaitForPartner()
        {
            Toil toil = new Toil
            {
                //Hang out for 700 ticks and gain joy
                defaultCompleteMode = ToilCompleteMode.Delay,
                initAction = delegate { ticksLeftThisToil = 700; },
                tickAction = delegate { JoyUtility.JoyTickCheckEnd(Actor, JoyTickFullJoyAction.None); }
            };
            //Fail if either participant needs to go eat or sleep
            toil.AddFailCondition(() =>
                PawnUtility.WillSoonHaveBasicNeed(Actor) || PawnUtility.WillSoonHaveBasicNeed(Partner));
            //Fail if partner despawns, dies, or stops the follow job
            toil.AddFailCondition(() => !Partner.Spawned);
            toil.AddFailCondition(() => Partner.Dead);
            toil.AddFailCondition(() => Partner.CurJob.def != RomanceDefOf.JobDateFollow);
            return toil;
        }

        [DebuggerHidden]
        protected override IEnumerable<Toil> MakeNewToils()
        {
            //Alternate goto and wait toils for each tile in path
            foreach (LocalTargetInfo target in job.targetQueueB)
            {
                yield return GotoCell(target.Cell);
                yield return WaitForPartner();
            }
        }
    }
}