using RimWorld;
using Verse;
using Verse.AI;

namespace BetterRomance
{
    public class JoyGiver_Date : JoyGiver
    {
        public override Job TryGiveJob(Pawn pawn)
        {
            if (!InteractionUtility.CanInitiateInteraction(pawn) || PawnUtility.WillSoonHaveBasicNeed(pawn))
            {
                return null;
            }
            //Prisoners usually can't go outside and all they have to do is talk to each other anyways
            else if (pawn.IsPrisoner)
            {
                return null;
            }
            //Generate random number and check against date rate setting
            else if (100f * Rand.Value > BetterRomanceMod.settings.dateRate / 2)
            {
                return null;
            }
            else
            {
                Comp_PartnerList comp = pawn.CheckForComp<Comp_PartnerList>();
                Pawn partner = comp.GetPartner(false);
                //Checks on if a partner was found and is available
                if (partner == null || !partner.Spawned || !partner.Awake())
                {
                    return null;
                }
                //Change this once new activities are added
                else if (!JoyUtility.EnjoyableOutsideNow(pawn))
                {
                    return null;
                }
                //Create the job based on romance factor
                else
                {
                    return pawn.relations.SecondaryRomanceChanceFactor(partner) > 0.2f ? JobMaker.MakeJob(RomanceDefOf.ProposeDate, partner) : JobMaker.MakeJob(RomanceDefOf.ProposeHangout, partner);
                }
            }
        }
    }
}