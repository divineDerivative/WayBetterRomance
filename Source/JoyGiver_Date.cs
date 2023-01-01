using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;
using Verse.AI;
using HarmonyLib;

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
                Comp_PartnerList comp = pawn.CheckForPartnerComp();
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
                //Create the job, ProposeDate
                else
                {
                    if (pawn.relations.SecondaryRomanceChanceFactor(partner) > 0.2f)
                    {
                        return new Job(RomanceDefOf.ProposeDate, partner);
                    }
                    else
                    {
                        return new Job(RomanceDefOf.ProposeHangout, partner);
                    }
                }
            }
        }
    }
}