using RimWorld;
using Verse;
using Verse.AI;

namespace BetterRomance
{
    public class JoyGiver_Date : JoyGiver
    {
        public override Job TryGiveJob(Pawn pawn)
        {
#if !v1_6
            if (!InteractionUtility.CanInitiateInteraction(pawn) || PawnUtility.WillSoonHaveBasicNeed(pawn))
#else
            if (!SocialInteractionUtility.CanInitiateInteraction(pawn) || PawnUtility.WillSoonHaveBasicNeed(pawn))
#endif
            {
                return null;
            }
            //Prisoners usually can't go outside and all they have to do is talk to each other anyways
            if (pawn.IsPrisoner)
            {
                return null;
            }
            //Check if there's even anywhere they could go
#if v1_4
            if (!RCellFinder.TryFindSkydreamingSpotOutsideColony(pawn.Position, pawn, out _, minDistanceFromColonyThing: 0, maxDistanceFromColonyThing: DateUtility.distanceLimit))
#else
            if (!RCellFinder.TryFindAllowedUnroofedSpotOutsideColony(pawn.Position, pawn, out _, 0, DateUtility.distanceLimit))
#endif
            {
                return null;
            }
            //Change this once new activities are added
            if (!JoyUtility.EnjoyableOutsideNow(pawn))
            {
                return null;
            }
            //Generate random number and check against date rate setting
            if (100f * Rand.Value > BetterRomanceMod.settings.dateRate / 2)
            {
                return null;
            }
            Pawn partner = pawn.CheckForComp<Comp_PartnerList>()?.GetPartner(false);
            //Checks on if a partner was found and is available
            if (partner == null || !partner.Spawned || !partner.Awake())
            {
                return null;
            }
            //Check for cheating and turn it into hanging out if they don't want to cheat
            //Use true because this is not about sex
            if (RomanceUtilities.IsThisCheating(pawn, partner, out _, true))
            {
                //Also check if they even want a date
                if (RomanceUtilities.CheatingChance(pawn) == 0f || !DateUtility.IsDateAppealing(pawn, partner))
                {
                    return JobMaker.MakeJob(RomanceDefOf.ProposeHangout, partner);
                }
            }
            //Create the job based on romance factor
            return pawn.relations.SecondaryRomanceChanceFactor(partner) > 0.2f ? JobMaker.MakeJob(RomanceDefOf.ProposeDate, partner) : JobMaker.MakeJob(RomanceDefOf.ProposeHangout, partner);
        }
    }
}
