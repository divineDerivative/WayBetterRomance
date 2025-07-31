using RimWorld;
using Verse;
using Verse.AI;

namespace BetterRomance
{
    public class JoyGiver_CasualHookup : JoyGiver
    {
        public override Job TryGiveJob(Pawn pawn)
        {
            //Asexual pawns will never initiate sex
            if (pawn.IsAsexual())
            {
                return null;
            }
            //Don't allow for kids
            if (pawn.ageTracker.AgeBiologicalYearsFloat < pawn.MinAgeForSex())
            {
                return null;
            }
            //Checks on whether pawn should try hookup now
#if !v1_6
            if (!InteractionUtility.CanInitiateInteraction(pawn) || !HookupUtility.WillPawnTryHookup(pawn, true, false) || PawnUtility.WillSoonHaveBasicNeed(pawn))
#else
            if (!SocialInteractionUtility.CanInitiateInteraction(pawn) || !HookupUtility.WillPawnTryHookup(pawn, true, false) || PawnUtility.WillSoonHaveBasicNeed(pawn))
#endif
            {
                return null;
            }
            //Generate random number to check against hookup settings
            if (100f * Rand.Value > pawn.HookupRate() / 2)
            {
                return null;
            }
            Pawn partner = pawn.CheckForComp<Comp_PartnerList>()?.GetPartner(true);
            if (partner == null || !partner.Spawned || !partner.Awake())
            {
                return null;
            }
            //If this hookup is cheating, will pawn do it anyways?
            if (!RomanceUtilities.WillPawnContinue(pawn, partner, out _, false, false))
            {
                return null;
            }
            //Also check ideo for non spouse lovin' thoughts
            if (!IdeoUtility.DoerWillingToDo(HistoryEventDefOf.GotLovin_NonSpouse, pawn) && !pawn.relations.DirectRelationExists(PawnRelationDefOf.Spouse, partner))
            {
                return null;
            }
            //If its not cheating, or they decided to cheat, continue with making the job
            Building_Bed bed = HookupUtility.FindHookupBed(pawn, partner);
            //If no suitable bed found, do not continue
            if (bed == null)
            {
                return null;
            }
            //Create the lead hookup job with partner and bed info
            return JobMaker.MakeJob(def.jobDef, partner, bed);
        }
    }
}
