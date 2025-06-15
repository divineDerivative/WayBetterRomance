using RimWorld;
using System.Collections.Generic;
using Verse;

namespace BetterRomance
{
    public enum PawnAvailability
    {
        Free,
        NeedSoon,
        InDanger,
        DontInterruptJob,
        CantInterruptJob,
        Downed,
        Drafted,
        InLabor,
        MentalState,
        Dead,
    }

    public enum RomanticActivityType
    {
        Any,
        CasualHookup,
        OrderedHookup,
        Date,
        Hangout,
    }

    public static class PawnAvailabilityExtensions
    {
        public static bool IsFree(this Pawn pawn, RomanticActivityType activity, out string reason)
        {
            PawnAvailability availability = pawn.Availability();
            reason = AvailabilityReasons[PawnAvailability.Free];
            if (AvailabilityPerActivity[activity].Contains(availability))
            {
                reason = AvailabilityReasons[availability];
                return false;
            }
            return true;
        }

        public static bool IsIncapable(this Pawn pawn, out string reason)
        {
            PawnAvailability availability = pawn.Availability();
            reason = AvailabilityReasons[availability];
            if ((int)availability >= 5)
            {
                return true;
            }
            return false;
        }

        public static PawnAvailability Availability(this Pawn pawn)
        {
            if (pawn.Dead)
            {
                return PawnAvailability.Dead;
            }
            if (pawn.Downed)
            {
                return PawnAvailability.Downed;
            }
            if (pawn.Drafted)
            {
                return PawnAvailability.Drafted;
            }
            if (pawn.InMentalState)
            {
                return PawnAvailability.MentalState;
            }
            if (pawn.health.hediffSet.HasHediff(HediffDefOf.PregnancyLabor) || pawn.health.hediffSet.HasHediff(HediffDefOf.PregnancyLaborPushing))
            {
                return PawnAvailability.InLabor;
            }
            if ((!pawn.timetable?.CurrentAssignment.allowJoy ?? false) || pawn.IsCarryingPawn() || !pawn.jobs.IsCurrentJobPlayerInterruptible() || CantInterruptJobs.Contains(pawn.CurJobDef))
            {
                return PawnAvailability.CantInterruptJob;
            }
            if (DontInterruptJobs.Contains(pawn.CurJobDef) || pawn.CanCasuallyInteractNow(true))
            {
                return PawnAvailability.DontInterruptJob;
            }
            if (PawnUtility.WillSoonHaveBasicNeed(pawn))
            {
                return PawnAvailability.NeedSoon;
            }
            if (PawnUtility.EnemiesAreNearby(pawn))
            {
                return PawnAvailability.InDanger;
            }
            return PawnAvailability.Free;
        }

        //These are things that should not be interrupted when a pawn decides to initiate an activity on their own
        private static readonly List<JobDef> DontInterruptJobs =
        [
            //Ceremonies
            JobDefOf.SpectateCeremony,
            //Emergency work
            JobDefOf.EscortPrisonerToBed,
            JobDefOf.BringBabyToSafety,
            //Medical work
            JobDefOf.TakeToBedToOperate,
            JobDefOf.TakeWoundedPrisonerToBed,
            JobDefOf.FeedPatient,
            //Romance
            RomanceDefOf.JobDateLead,
            RomanceDefOf.JobDateFollow,
            RomanceDefOf.JobHangoutLead,
            RomanceDefOf.JobHangoutFollow,
            //Ordered jobs
            JobDefOf.ReleasePrisoner,
            JobDefOf.UseCommsConsole,
            JobDefOf.EnterTransporter,
            JobDefOf.EnterCryptosleepCasket,
            JobDefOf.EnterBiosculpterPod,
            JobDefOf.TradeWithPawn,
            JobDefOf.ApplyTechprint,

            JobDefOf.Breastfeed,
        ];

        //These are things that should never be interrupted even for an ordered hook up
        private static readonly List<JobDef> CantInterruptJobs =
        [
            //Incapacitated
            JobDefOf.ExtinguishSelf,
            JobDefOf.Flee,
            JobDefOf.FleeAndCower,
#if v1_5
            JobDefOf.FleeAndCowerShort,
#endif
            //Don't interrupt ceremonies
            JobDefOf.MarryAdjacentPawn,
            JobDefOf.GiveSpeech,
            JobDefOf.BestowingCeremony,
            JobDefOf.PrepareSkylantern,
            JobDefOf.PrisonerExecution,
            JobDefOf.Sacrifice,
            JobDefOf.Scarify,
            JobDefOf.Blind,
            JobDefOf.AcceptRole,
            JobDefOf.DeliverToAltar,
            //Romance
            RomanceDefOf.DoLovinCasual,
            JobDefOf.Lovin,
            JobDefOf.TryRomance,
            //Involves another pawn, we don't want them just dropping a baby or letting go of a prisoner
            JobDefOf.DeliverToCell,
            JobDefOf.DeliverToBed,
            JobDefOf.Steal,
            JobDefOf.Kidnap,
            JobDefOf.CarryDownedPawnToExit,
#if v1_5
            JobDefOf.CarryDownedPawnToPortal,
#endif
            JobDefOf.Rescue,
            JobDefOf.CarryToCryptosleepCasket,
            JobDefOf.Capture,
            JobDefOf.Arrest,
            JobDefOf.CarryToCryptosleepCasketDrafted,
            JobDefOf.CarryToPrisonerBedDrafted,
            JobDefOf.RopeToPen,
            JobDefOf.RopeRoamerToUnenclosedPen,
            JobDefOf.RopeRoamerToHitchingPost,
            JobDefOf.Unrope,
            JobDefOf.ReleaseAnimalToWild,

            JobDefOf.SocialFight,
            RomanceDefOf.CastAbilityOnWorldTile,
            JobDefOf.ManTurret,
            JobDefOf.ActivateArchonexusCore,
            JobDefOf.PruneGauranlenTree,
        ];

        //Turn a PawnAvailability into a string for reporting to player
        public static readonly Dictionary<PawnAvailability, string> AvailabilityReasons = new()
        {
            {PawnAvailability.Free, "free" },
            {PawnAvailability.NeedSoon, "WBR.CantHookupReasonNeed".Translate() },
            {PawnAvailability.InDanger, "WBR.CantHookupReasonDanger".Translate() },
            {PawnAvailability.DontInterruptJob, "WBR.CantHookupReasonBusy".Translate() },
            {PawnAvailability.CantInterruptJob, "WBR.CantHookupReasonBusy".Translate() },
            {PawnAvailability.Downed, "WBR.CantHookupReasonDowned".Translate() },
            {PawnAvailability.Drafted, "WBR.CantHookupReasonDrafted".Translate() },
            {PawnAvailability.InLabor, "WBR.CantHookupReasonInLabor".Translate() },
            {PawnAvailability.MentalState, "WBR.CantHookupReasonMentalState".Translate() },
            {PawnAvailability.Dead, "WBR.CantHookupReasonDead".Translate() },
        };

        //Each activity type has its own set of availabilities that prevent it
        //Currently they're all the same except for ordered hookup, but this setup will allow easier adjusting later if needed
        public static readonly Dictionary<RomanticActivityType, List<PawnAvailability>> AvailabilityPerActivity = new()
        {
            {RomanticActivityType.Any, new List<PawnAvailability>()
            {
                PawnAvailability.NeedSoon,
                PawnAvailability.InDanger,
                PawnAvailability.DontInterruptJob,
                PawnAvailability.CantInterruptJob,
                PawnAvailability.Downed,
                PawnAvailability.Drafted,
                PawnAvailability.InLabor,
                PawnAvailability.MentalState,
                PawnAvailability.Dead,
            } },
            {RomanticActivityType.CasualHookup, new List<PawnAvailability>()
            {
                PawnAvailability.NeedSoon,
                PawnAvailability.InDanger,
                PawnAvailability.DontInterruptJob,
                PawnAvailability.CantInterruptJob,
                PawnAvailability.Downed,
                PawnAvailability.Drafted,
                PawnAvailability.InLabor,
                PawnAvailability.MentalState,
                PawnAvailability.Dead,
            } },
            { RomanticActivityType.OrderedHookup, new List<PawnAvailability>()
            {
                PawnAvailability.CantInterruptJob,
                PawnAvailability.Downed,
                PawnAvailability.Drafted,
                PawnAvailability.InLabor,
                PawnAvailability.MentalState,
                PawnAvailability.Dead,
            } },
            {RomanticActivityType.Date, new List<PawnAvailability>()
            {
                PawnAvailability.NeedSoon,
                PawnAvailability.InDanger,
                PawnAvailability.DontInterruptJob,
                PawnAvailability.CantInterruptJob,
                PawnAvailability.Downed,
                PawnAvailability.Drafted,
                PawnAvailability.InLabor,
                PawnAvailability.MentalState,
                PawnAvailability.Dead,
            } },
            {RomanticActivityType.Hangout, new List<PawnAvailability>()
            {
                PawnAvailability.NeedSoon,
                PawnAvailability.InDanger,
                PawnAvailability.DontInterruptJob,
                PawnAvailability.CantInterruptJob,
                PawnAvailability.Downed,
                PawnAvailability.Drafted,
                PawnAvailability.InLabor,
                PawnAvailability.MentalState,
                PawnAvailability.Dead,
            } },
        };
    }
}
