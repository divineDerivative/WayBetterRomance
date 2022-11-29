using System;

namespace BetterRomance
{
    //Stuff that looks useful for ordered hookups
    public static AcceptanceReport RomanceEligiblePair(Pawn initiator, Pawn target, bool forOpinionExplanation)
    {
        if (initiator == target)
        {
            return false;
        }
        DirectPawnRelation directPawnRelation = LovePartnerRelationUtility.ExistingLoveRealtionshipBetween(initiator, target, allowDead: false);
        if (directPawnRelation != null)
        {
            string genderSpecificLabel = directPawnRelation.def.GetGenderSpecificLabel(target);
            return "RomanceChanceExistingRelation".Translate(initiator.Named("PAWN"), genderSpecificLabel.Named("RELATION"));
        }
        if (!RomanceEligible(initiator, initiator: true, forOpinionExplanation))
        {
            return false;
        }
        if (forOpinionExplanation && target.ageTracker.AgeBiologicalYearsFloat < 16f)
        {
            return "CantRomanceTargetYoung".Translate();
        }
        if (Incestuous(initiator, target))
        {
            return "CantRomanceTargetIncest".Translate();
        }
        if (forOpinionExplanation && target.IsPrisoner)
        {
            return "CantRomanceTargetPrisoner".Translate();
        }
        if (!AttractedToGender(initiator, target.gender) || !AttractedToGender(target, initiator.gender))
        {
            if (!forOpinionExplanation)
            {
                return AcceptanceReport.WasRejected;
            }
            return "CantRomanceTargetSexuality".Translate();
        }
        AcceptanceReport acceptanceReport = RomanceEligible(target, initiator: false, forOpinionExplanation);
        if (!acceptanceReport)
        {
            return acceptanceReport;
        }
        if (target.relations.OpinionOf(initiator) <= 5)
        {
            return "CantRomanceTargetOpinion".Translate();
        }
        if (!forOpinionExplanation && InteractionWorker_RomanceAttempt.SuccessChance(initiator, target, 1f) <= 0f)
        {
            return "CantRomanceTargetZeroChance".Translate();
        }
        if ((!forOpinionExplanation && !initiator.CanReach(target, PathEndMode.Touch, Danger.Deadly)) || target.IsForbidden(initiator))
        {
            return "CantRomanceTargetUnreachable".Translate();
        }
        return true;
    }
}
