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
        public static JobDef OrderedHookup;
        public static JobDef ProposeDate;
        public static JobDef ProposeHangout;
        public static JobDef JobHangoutLead;
        public static JobDef JobHangoutFollow;
        //public static JobDef JobDateMovie;
        public static ThoughtDef FailedHookupAttemptOnMe;
        public static ThoughtDef RebuffedMyHookupAttempt;
        [MayRequireIdeology]
        public static ThoughtDef GotLovin_Horrible;
        [MayRequireIdeology]
        public static PreceptDef Lovin_FreeApproved;
        public static ThoughtDef FailedDateAttemptOnMe;
        public static ThoughtDef RebuffedMyDateAttempt;
        public static ThoughtDef RebuffedMyHangoutAttempt;
        public static ThoughtDef FailedHangoutAttemptOnMe;
        public static TraitDef Faithful;
        public static TraitDef Philanderer;
        public static TraitDef Straight;
        public static TraitDef HeteroAce;
        public static TraitDef HomoAce;
        public static TraitDef BiAce;
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
        public static FleckDef FriendHeart;
        [MayRequirePersonalityM2]
        public static ThoughtDef SP_PassionateLovin;
        public static ThoughtDef LovinAsexualPositive;
        public static ThoughtDef LovinAsexualNegative;
        [MayRequirePersonalityM2]
        public static ThoughtDef PassionateLovinAsexualPositive;
        [MayRequirePersonalityM2]
        public static ThoughtDef PassionateLovinAsexualNegative;
        [MayRequireCSL]
        public static PawnCapacityDef Fertility;
        [MayRequireRJW]
        public static PawnCapacityDef RJW_Fertility;

        //Vanilla stuff I need a reference to
        public static JobDef CastAbilityOnThingUninterruptible;
        public static JobDef CastAbilityOnWorldTile;

        static RomanceDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(RomanceDefOf));
        }
    }
}