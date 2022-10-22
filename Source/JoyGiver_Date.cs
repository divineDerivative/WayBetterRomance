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
        public static float percentRate = BetterRomanceMod.settings.dateRate / 2;

        public override Job TryGiveJob(Pawn pawn)
        {
            if (!InteractionUtility.CanInitiateInteraction(pawn) || PawnUtility.WillSoonHaveBasicNeed(pawn))
            {
                return null;
            }
            //Generate random number and check against date rate setting
            else if (100f * Rand.Value > percentRate)
            {
                return null;
            }
            else
            {
                Comp_PartnerList comp = pawn.TryGetComp<Comp_PartnerList>();
                if (comp == null)
                {
                    FieldInfo field = AccessTools.Field(typeof(ThingWithComps), "comps");
                    List<ThingComp> compList = (List<ThingComp>)field.GetValue(pawn);
                    ThingComp newComp = (ThingComp)Activator.CreateInstance(typeof(Comp_PartnerList));
                    newComp.parent = pawn;
                    compList.Add(newComp);
                    newComp.Initialize(new CompProperties_PartnerList());
                    comp = pawn.TryGetComp<Comp_PartnerList>();
                    if (comp == null)
                    {
                        Log.Error("Unable to add Comp_PartnerList");
                    }
                }
                Pawn partner = comp.GetPartner(false);
                //Checks on if a partner was found and is avilable
                if (partner == null || !partner.Spawned || !partner.Awake())
                {
                    return null;
                }
                else if (!JoyUtility.EnjoyableOutsideNow(pawn))
                {
                    return null;
                }
                //Create the job, ProposeDate
                else
                {
                    return new Job(def.jobDef, partner);
                }
            }
        }
    }
}