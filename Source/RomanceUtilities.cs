using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Verse;

namespace BetterRomance
{
    public static class RomanceUtilities
    {
        public static bool DoWeShareABed(Pawn pawn, Pawn other)
        {
            return pawn.ownership.OwnedBed != null && pawn.ownership.OwnedBed.OwnersForReading.Contains(other);
        }

        /// <summary>
        /// Determines if an interaction between <paramref name="pawn"/> and <paramref name="target"/> would be cheating from <paramref name="pawn"/>'s point of view. Includes a list of pawns that would think they are being cheated on.
        /// </summary>
        /// <param name="pawn"></param>
        /// <param name="target"></param>
        /// <param name="cheaterList">A list of pawns who will think that <paramref name="pawn"/> cheated on them, regardless of what <paramref name="pawn"/> thinks</param>
        /// <returns>True or False</returns>
        public static bool IsThisCheating(Pawn pawn, Pawn target, out List<Pawn> cheaterList)
        {
            //This has to happen to get passed out
            cheaterList = new List<Pawn>();
            //Are they in a relationship?
            if (target != null && LovePartnerRelationUtility.LovePartnerRelationExists(pawn, target))
            {
                return false;
            }
            foreach (Pawn p in GetAllLoveRelationPawns(pawn, false, false))
            {
                //If the pawns have different ideos, I think this will check if the partner would feel cheated on per their ideo and settings
                if (!new HistoryEvent(pawn.GetHistoryEventForLoveRelationCountPlusOne(), p.Named(HistoryEventArgsNames.Doer)).DoerWillingToDo() && p.CaresAboutCheating())
                {
                    cheaterList.Add(p);
                }
            }
            //The cheater list is for use later, initiator will only look at their ideo and settings to decide if they're cheating
            if (new HistoryEvent(pawn.GetHistoryEventForLoveRelationCountPlusOne(), pawn.Named(HistoryEventArgsNames.Doer)).DoerWillingToDo() || !pawn.CaresAboutCheating())
            {
                //Faithful pawns will respect their partner's ideo
                return !cheaterList.NullOrEmpty() && pawn.story.traits.HasTrait(RomanceDefOf.Faithful);
            }
            return true;
        }

        /// <summary>
        /// Always returns true if interaction between <paramref name="pawn"/> and <paramref name="target"/> is not cheating and is allowed by ideo. Otherwise, finds the partner they would feel the worst about cheating on and decides based on opinion and <paramref name="pawn"/>'s traits.
        /// </summary>
        /// <param name="pawn"></param>
        /// <param name="target"></param>
        /// <param name="cheatOn">The pawn they feel worst about cheating on</param>
        /// <returns></returns>
        public static bool WillPawnContinue(Pawn pawn, Pawn target, out Pawn cheatOn)
        {
            cheatOn = null;
            if (IsThisCheating(pawn, target, out List<Pawn> cheatedOnList))
            {
                if (!cheatedOnList.NullOrEmpty())
                {
                    float cheatChance = BetterRomanceMod.settings.cheatChance;
                    //At this point, both the pawn and a non-zero number of partners consider this cheating
                    //If they are faithful, don't do it
                    if (pawn.story.traits.HasTrait(RomanceDefOf.Faithful))
                    {
                        return false;
                    }
                    //Don't allow if user has turned cheating off
                    if (cheatChance == 0f)
                    {
                        return false;
                    }
                    //Lower chances for kind trait
                    if (pawn.story.traits.HasTrait(TraitDefOf.Kind))
                    {
                        cheatChance *= .25f;
                    }
                    //Generate random value, modify by cheat chance, and compare to opinion of most liked partner
                    if (Rand.Value * (cheatChance / 100f) < PartnerFactor(pawn, cheatedOnList, out cheatOn))
                    {
                        return false;
                    }
                }
                //Pawn thinks they are cheating, even though no partners will be upset
                //This can happen with the no spouses mod, which is a bit weird
                //Letting this continue for now, might change later
            }
            return true;
        }

        //This should find the person they would feel the worst about cheating on
        //With the philanderer map differences, I think this is the best way
        public static float PartnerFactor(Pawn pawn, List<Pawn> partnerList, out Pawn partner)
        {
            partner = null;
            float partnerFactor = 1f;
            if (!partnerList.NullOrEmpty())
            {
                partnerFactor = 99999f;
                foreach (Pawn p in partnerList)
                {
                    float opinion = pawn.relations.OpinionOf(p);
                    float tempOpinionFactor;
                    if (pawn.story.traits.HasTrait(RomanceDefOf.Philanderer))
                    {
                        tempOpinionFactor = pawn.Map == p.Map ? Mathf.InverseLerp(70f, 15f, opinion) : Mathf.InverseLerp(100f, 50f, opinion);
                    }
                    else
                    {
                        tempOpinionFactor = Mathf.InverseLerp(30f, -80f, opinion);
                    }
                    if (tempOpinionFactor < partnerFactor)
                    {
                        partnerFactor = tempOpinionFactor;
                        partner = p;
                    }
                }
            }
            return partnerFactor;
        }

        /// <summary>
        /// Determines if <paramref name="pawn"/> is available for an activity.
        /// </summary>
        /// <param name="pawn"></param>
        /// <returns><see langword="false"/> if <paramref name="pawn"/> is close to needing to eat/sleep, there's enemies nearby, they're drafted, in labor, in a mental break, or they're doing a job that should not be interrupted.</returns>
        public static AcceptanceReport IsPawnFree(Pawn pawn, bool forcedJob = false)
        {
            if (pawn.Drafted)
            {
                return forcedJob ? (AcceptanceReport)"WBR.CantHookupInitiateMessageDrafted".Translate(pawn) : AcceptanceReport.WasRejected;
            }
            if (pawn.Downed)
            {
                return forcedJob ? (AcceptanceReport)"WBR.CantHookupInitiateMessageDowned".Translate(pawn) : AcceptanceReport.WasRejected;
            }
            if (pawn.health.hediffSet.HasHediff(HediffDefOf.PregnancyLabor) || pawn.health.hediffSet.HasHediff(HediffDefOf.PregnancyLaborPushing))
            {
                return forcedJob ? (AcceptanceReport)"WBR.CantHookupInitiateMessageInLabor".Translate(pawn) : (AcceptanceReport)false;
            }
            if (pawn.mindState.mentalStateHandler.InMentalState)
            {
                return forcedJob ? (AcceptanceReport)"WBR.CantHookupInitiateMessageMentalState".Translate(pawn) : (AcceptanceReport)false;
            }
            if (forcedJob && CantInterruptJobs.Contains(pawn.CurJob.def))
            {
                return "WBR.CantHookupInitiateMessageUninterruptableJob".Translate(pawn);
            }
            if (!forcedJob && (PawnUtility.WillSoonHaveBasicNeed(pawn) || PawnUtility.EnemiesAreNearby(pawn)))
            {
                return false;
            }

            return !CantInterruptJobs.Contains(pawn.CurJobDef) && (forcedJob || !DontInterruptJobs.Contains(pawn.CurJobDef));
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

        /// <summary>
        /// Grabs the first non-spouse love partner of the opposite gender. For use in generating parents.
        /// </summary>
        /// <param name="pawn"></param>
        /// <returns></returns>
        public static Pawn GetFirstLoverOfOppositeGender(Pawn pawn)
        {
            foreach (Pawn lover in GetNonSpouseLovers(pawn, true))
            {
                if (pawn.gender.Opposite() == lover.gender)
                {
                    return lover;
                }
            }
            return null;
        }

        /// <summary>
        /// Generates a list of love partners that does not include spouses or fiances
        /// </summary>
        /// <param name="pawn"></param>
        /// <param name="includeDead"></param>
        /// <returns></returns>
        public static List<Pawn> GetNonSpouseLovers(Pawn pawn, bool includeDead)
        {
            List<Pawn> list = new List<Pawn>();
            if (!pawn.RaceProps.IsFlesh)
            {
                return list;
            }
            List<DirectPawnRelation> relations = pawn.relations.DirectRelations;
            foreach (DirectPawnRelation rel in relations)
            {
                if (rel.def == PawnRelationDefOf.Lover && (includeDead || !rel.otherPawn.Dead))
                {
                    list.Add(rel.otherPawn);
                }
                else if (SettingsUtilities.LoveRelations.Contains(rel.def) && (includeDead || !rel.otherPawn.Dead))
                {
                    list.Add(rel.otherPawn);
                }
            }
            return list;
        }

        /// <summary>
        /// Generates a list of pawns that are in love relations with <paramref name="pawn"/>. Pawns are only listed once, even if they are in more than one love relation.
        /// </summary>
        /// <param name="pawn"></param>
        /// <param name="includeDead">Whether dead pawns are added to the list</param>
        /// <param name="onMap">Whether pawns must be on the same map to be added to the list</param>
        /// <returns>A list of pawns that in a love relation with <paramref name="pawn"/></returns>
        public static List<Pawn> GetAllLoveRelationPawns(Pawn pawn, bool includeDead, bool onMap)
        {
            List<Pawn> list = new List<Pawn>();
            if (!pawn.RaceProps.IsFlesh)
            {
                return list;
            }
            foreach (DirectPawnRelation rel in LovePartnerRelationUtility.ExistingLovePartners(pawn, includeDead))
            {
                if (!list.Contains(rel.otherPawn))
                {
                    if (pawn.Map == rel.otherPawn.Map)
                    {
                        list.Add(rel.otherPawn);
                    }
                    else if (!onMap)
                    {
                        list.Add(rel.otherPawn);
                    }
                }
            }
            return list;
        }

        /// <summary>
        /// Finds the most liked pawn with a specific <paramref name="relation"/> to <paramref name="pawn"/>. Any direct relation will work, no implied relations.
        /// </summary>
        /// <param name="pawn"></param>
        /// <param name="relation"></param>
        /// <param name="allowDead"></param>
        /// <returns></returns>
        public static Pawn GetMostLikedOfRel(Pawn pawn, PawnRelationDef relation, bool allowDead)
        {
            List<DirectPawnRelation> list = pawn.relations.DirectRelations;
            Pawn result = null;
            int num = 0;
            foreach (DirectPawnRelation rel in list)
            {
                if (rel.def == relation && (rel.otherPawn.Dead || allowDead))
                {
                    if (pawn.relations.OpinionOf(rel.otherPawn) > num)
                    {
                        num = pawn.relations.OpinionOf(rel.otherPawn);
                        result = rel.otherPawn;
                    }
                }
            }
            return result;
        }

        public static List<Pawn> GetAllSpawnedHumanlikesOnMap(Map map)
        {
            List<Pawn> result = new List<Pawn>();
            List<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
            foreach (Pawn pawn in pawns)
            {
                if (pawn.RaceProps.Humanlike)
                {
                    result.Add(pawn);
                }
            }
            return result;
        }

        /// <summary>
        /// A rating to use for determining sex aversion for asexual pawns. Seed is based on pawn's ID, so it will always return the same number for a given pawn.
        /// </summary>
        /// <param name="pawn"></param>
        /// <returns>float between 0 and 1</returns>
        public static float AsexualRating(this Pawn pawn)
        {
            Rand.PushState();
            Rand.Seed = pawn.thingIDNumber;
            float rating = Rand.Range(0f, 1f);
            Rand.PopState();
            return rating;
        }

        /// <summary>
        /// Determines the romantic <see cref="Orientation"/> of a <paramref name="pawn"/> based on their traits
        /// </summary>
        /// <param name="pawn"></param>
        /// <returns></returns>
        public static Orientation GetOrientation(this Pawn pawn)
        {
            if (pawn.story != null && pawn.story.traits != null)
            {
                TraitSet traits = pawn.story.traits;
                if (traits.HasTrait(TraitDefOf.Gay) || traits.HasTrait(RomanceDefOf.HomoAce))
                {
                    return Orientation.Homo;
                }
                else if (traits.HasTrait(RomanceDefOf.Straight) || traits.HasTrait(RomanceDefOf.HeteroAce))
                {
                    return Orientation.Hetero;
                }
                else if (traits.HasTrait(TraitDefOf.Bisexual) || traits.HasTrait(RomanceDefOf.BiAce))
                {
                    return Orientation.Bi;
                }
                else if (traits.HasTrait(TraitDefOf.Asexual))
                {
                    return Orientation.None;
                }
            }
            return Orientation.None;
        }

        public static bool IsAsexual(this Pawn pawn)
        {
            if (pawn.story != null && pawn.story.traits != null)
            {
                TraitSet traits = pawn.story.traits;
                if (traits.HasTrait(TraitDefOf.Asexual) || traits.HasTrait(RomanceDefOf.BiAce) || traits.HasTrait(RomanceDefOf.HeteroAce) || traits.HasTrait(RomanceDefOf.HomoAce))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Generates points for the lovin age curve based on age settings
        /// </summary>
        /// <returns>List<CurvePoint></returns>
        public static SimpleCurve GetLovinCurve(this Pawn pawn)
        {
            float minAge = pawn.MinAgeForSex();
            float maxAge = pawn.MaxAgeForSex();
            float declineAge = pawn.DeclineAtAge();
            List<CurvePoint> points = new List<CurvePoint>
            {
                new CurvePoint(minAge, 1.5f),
                new CurvePoint((declineAge / 5) + minAge, 1.5f),
                new CurvePoint(declineAge, 4f),
                new CurvePoint((maxAge / 4) + declineAge, 12f),
                new CurvePoint(maxAge, 36f)
            };
            return new SimpleCurve(points);
        }

        public static readonly List<TraitDef> OrientationTraits = new List<TraitDef>()
        {
            TraitDefOf.Gay,
            TraitDefOf.Bisexual,
            RomanceDefOf.Straight,
            TraitDefOf.Asexual,
            RomanceDefOf.HeteroAce,
            RomanceDefOf.HomoAce,
            RomanceDefOf.BiAce,
        };

        /// <summary>
        /// Checks if a <see cref="Comp_PartnerList"/> already exists on <paramref name="p"/>, adds it if needed, and then returns the comp
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public static Comp_PartnerList CheckForPartnerComp(this Pawn p)
        {
            Comp_PartnerList comp = p.TryGetComp<Comp_PartnerList>();
            if (comp == null)
            {
                FieldInfo field = AccessTools.Field(typeof(ThingWithComps), "comps");
                List<ThingComp> compList = (List<ThingComp>)field.GetValue(p);
                ThingComp newComp = (ThingComp)Activator.CreateInstance(typeof(Comp_PartnerList));
                newComp.parent = p;
                compList.Add(newComp);
                newComp.Initialize(new CompProperties_PartnerList());
                newComp.PostExposeData();
                comp = p.TryGetComp<Comp_PartnerList>();
                if (comp == null)
                {
                    Log.Error("Unable to add Comp_PartnerList");
                }
            }
            return comp;
        }

        /// <summary>
        /// Adjustment to success chance based on <paramref name="pawn"/>'s orientation and <paramref name="target"/>'s gender
        /// </summary>
        /// <param name="pawn"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static float SexualityFactor(Pawn pawn, Pawn target)
        {
            float factor = 1f;
            if (pawn.IsAsexual())
            {
                factor *= pawn.AsexualRating() / 2;
            }
            if (pawn.GetOrientation() == Orientation.Homo)
            {
                if (target.gender != pawn.gender)
                {
                    factor *= 0.125f;
                }
            }
            if (pawn.GetOrientation() == Orientation.Hetero)
            {
                if (target.gender == pawn.gender)
                {
                    factor *= 0.125f;
                }
            }
            return factor;
        }

        public static bool HasLoveEnhancer(Pawn pawn)
        {
            return pawn.health?.hediffSet?.hediffs.Any((Hediff h) => h.def == HediffDefOf.LoveEnhancer) ?? false;
        }
    }

    public enum Orientation
    {
        Homo,
        Hetero,
        Bi,
        None,
    }
}