using System.Collections.Generic;
using RimWorld;
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
            reason = AvailabilityReasons[availability];
            if (AvailabilityPerActivity[activity].Contains(availability))
            {
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
            if (CantInterruptJobs.Contains(pawn.CurJobDef))
            {
                return PawnAvailability.CantInterruptJob;
            }
            if (DontInterruptJobs.Contains(pawn.CurJobDef))
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

        private static readonly List<JobDef> DontInterruptJobs = new List<JobDef>
        {
            //Ceremonies
            JobDefOf.SpectateCeremony,
            //Emergency work
            JobDefOf.BeatFire,
            JobDefOf.Arrest,
            JobDefOf.Capture,
            JobDefOf.EscortPrisonerToBed,
            JobDefOf.Rescue,
            JobDefOf.CarryToBiosculpterPod,
            JobDefOf.BringBabyToSafety,
            //Medical work
            JobDefOf.TakeToBedToOperate,
            JobDefOf.TakeWoundedPrisonerToBed,
            JobDefOf.TendPatient,
            JobDefOf.FeedPatient,
            //Romance
            RomanceDefOf.JobDateLead,
            RomanceDefOf.JobDateFollow,
            RomanceDefOf.JobHangoutLead,
            RomanceDefOf.JobHangoutFollow,
            //Ordered jobs
            JobDefOf.LayDown,
            JobDefOf.ReleasePrisoner,
            JobDefOf.UseCommsConsole,
            JobDefOf.EnterTransporter,
            JobDefOf.EnterCryptosleepCasket,
            JobDefOf.EnterBiosculpterPod,
            JobDefOf.TradeWithPawn,
            JobDefOf.ApplyTechprint,

            JobDefOf.Breastfeed,
        };

        private static readonly List<JobDef> CantInterruptJobs = new List<JobDef>
        {
            //Incapacitated
            JobDefOf.Wait_Downed,
            JobDefOf.Vomit,
            JobDefOf.Deathrest,
            JobDefOf.ExtinguishSelf,
            JobDefOf.Flee,
            JobDefOf.FleeAndCower,
            //Ceremonies
            JobDefOf.MarryAdjacentPawn,
            JobDefOf.GiveSpeech,
            JobDefOf.BestowingCeremony,
            JobDefOf.PrepareSkylantern,
            JobDefOf.PrisonerExecution,
            JobDefOf.Sacrifice,
            JobDefOf.Scarify,
            JobDefOf.Blind,
            //Romance
            RomanceDefOf.DoLovinCasual,
            JobDefOf.Lovin,
            JobDefOf.TryRomance,

            JobDefOf.SocialFight,
        };

        //Turn a PawnAvailability into a string for reporting to player
        public static readonly Dictionary<PawnAvailability, string> AvailabilityReasons = new Dictionary<PawnAvailability, string>
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
        public static readonly Dictionary<RomanticActivityType, List<PawnAvailability>> AvailabilityPerActivity = new Dictionary<RomanticActivityType, List<PawnAvailability>>
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
