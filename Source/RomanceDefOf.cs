using RimWorld;
using Verse;

namespace BetterRomance
{
    [DefOf]
    public static class RomanceDefOf
    {
        public static JobDef DoLovinCasual;
        public static JobDef JobDateFollow;
        public static JobDef JobDateLead;
        public static JobDef LeadHookup;
        public static JobDef ProposeDate;
        public static JobDef ProposeHangout;
        public static JobDef JobHangoutLead;
        public static JobDef JobHangoutFollow;
        public static ThoughtDef FailedHookupAttemptOnMe;
        public static ThoughtDef RebuffedMyHookupAttempt;
        public static ThoughtDef GotSomeLovinAsexual;
        [MayRequireIdeology]
        public static ThoughtDef GotLovin_Horrible;
        [MayRequireIdeology]
        public static PreceptDef Lovin_FreeApproved;
        public static ThoughtDef FailedDateAttemptOnMe;
        public static ThoughtDef RebuffedMyDateAttempt;
        public static ThoughtDef RebuffedMyHangoutAttempt;
        public static ThoughtDef FailedHangoutAttemptOnMe;
        public static TraitDef Asexual;
        public static TraitDef Bisexual;
        public static TraitDef Faithful;
        public static TraitDef Philanderer;
        public static TraitDef Straight;
        public static InteractionDef TriedHookupWith;
        public static InteractionDef AskedForDate;
        public static InteractionDef AskedForHangout;
        public static JoyKindDef Lewd;
        public static JoyKindDef Social;
        public static RulePackDef HookupSucceeded;
        public static RulePackDef HookupFailed;
        public static RulePackDef DateSucceeded;
        public static RulePackDef DateFailed;
        public static RulePackDef HangoutSucceeded;
        public static RulePackDef HangoutFailed;

        static RomanceDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(RomanceDefOf));
        }
    }
}