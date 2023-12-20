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
            //We don't care if either pawn has no need to sleep
            if (pawn.needs.rest == null || other.needs.rest == null)
            {
                return false;
            }
            return pawn.ownership.OwnedBed != null && pawn.ownership.OwnedBed.OwnersForReading.Contains(other);
        }

        /// <summary>
        /// Determines if an interaction between <paramref name="pawn"/> and <paramref name="otherPawn"/> would be cheating from <paramref name="pawn"/>'s point of view. Includes a list of pawns that would think they are being cheated on.
        /// </summary>
        /// <param name="pawn"></param>
        /// <param name="otherPawn"></param>
        /// <param name="cheaterList">A list of pawns who will think that <paramref name="pawn"/> cheated on them, regardless of what <paramref name="pawn"/> thinks</param>
        /// <returns>True or False</returns>
        public static bool IsThisCheating(Pawn pawn, Pawn otherPawn, out List<Pawn> cheaterList)
        {
            //This has to happen to get passed out
            cheaterList = new List<Pawn>();
            //Are they in a relationship?
            if (otherPawn != null && LovePartnerRelationUtility.LovePartnerRelationExists(pawn, otherPawn))
            {
                return false;
            }
            foreach (Pawn p in GetAllLoveRelationPawns(pawn, false, false))
            {
                //If the pawns have different ideos, I think this will check if the partner would feel cheated on per their ideo and settings
                if (!pawn.GetHistoryEventForLoveRelationCountPlusOne().WillingToDoGendered(p.Ideo, pawn.gender) && p.CaresAboutCheating())
                {
                    cheaterList.Add(p);
                }
            }
            //The cheater list is for use later, initiator will only look at their ideo and settings to decide if they're cheating
            if (pawn.IsEventAllowed(pawn.GetHistoryEventForLoveRelationCountPlusOne()) || !pawn.CaresAboutCheating())
            {
                //Faithful pawns will respect their partner's ideo
                return !cheaterList.NullOrEmpty() && pawn.story.traits.HasTrait(RomanceDefOf.Faithful);
            }
            return true;
        }

        /// <summary>
        /// Whether <paramref name="pawn"/> decides to continue with an interaction with <paramref name="otherPawn"/>. Always returns true if interaction is not cheating and is allowed by ideo. Otherwise, finds the partner they would feel the worst about cheating on and decides based on opinion and <paramref name="pawn"/>'s traits.
        /// </summary>
        /// <param name="pawn"></param>
        /// <param name="otherPawn"></param>
        /// <param name="cheatOn">The pawn they feel worst about cheating on</param>
        /// <returns></returns>
        public static bool WillPawnContinue(Pawn pawn, Pawn otherPawn, out Pawn cheatOn)
        {
            cheatOn = null;
            if (IsThisCheating(pawn, otherPawn, out List<Pawn> cheatedOnList))
            {
                if (!cheatedOnList.NullOrEmpty())
                {
                    //At this point, both the pawn and a non-zero number of partners consider this cheating
                    //Generate random value, and compare to opinion of most liked partner and base cheat chance
                    if (Rand.Value > CheatingChance(pawn) * PartnerFactor(pawn, cheatedOnList, out cheatOn))
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

        /// <summary>
        /// Factor based on opinion of most liked partner. Higher opinion means a lower factor. Only call if <paramref name="partnerList"/> is not empty
        /// </summary>
        /// <param name="pawn">The <see cref="Pawn"/> in question</param>
        /// <param name="partnerList">List of partners that would feel cheated on, provided by <see cref="IsThisCheating(Pawn, Pawn, out List{Pawn})"/></param>
        /// <param name="partner">The partner <paramref name="pawn"/> would feel the worst about cheating on</param>
        /// <returns></returns>
        public static float PartnerFactor(Pawn pawn, List<Pawn> partnerList, out Pawn partner)
        {
            partner = null;
            float partnerFactor = 99999f;
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
            return partnerFactor;
        }

        /// <summary>
        /// Base chance that a given <paramref name="pawn"/> will cheat. Based on settings and traits.
        /// </summary>
        /// <param name="pawn"></param>
        /// <returns></returns>
        public static float CheatingChance(Pawn pawn)
        {
            //If they are faithful, don't do it
            if (pawn.story.traits.HasTrait(RomanceDefOf.Faithful))
            {
                return 0f;
            }
            float cheatChance = BetterRomanceMod.settings.cheatChance / 100f;
            //Don't allow if user has turned cheating off
            if (cheatChance == 0f)
            {
                return 0f;
            }
            //Lower chances for kind trait
            if (pawn.story.traits.HasTrait(TraitDefOf.Kind))
            {
                cheatChance *= .25f;
            }
            //Adjust for RotR precepts
            if (Settings.RotRActive && pawn.Ideo != null)
            {
                cheatChance *= RotR_Integration.RotRCheatChanceModifier(pawn);
            }
            return cheatChance;
        }

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
                //Exclude animals and enemies
                if (pawn.RaceProps.Humanlike && !pawn.HostileTo(Faction.OfPlayer))
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
            Comp_SexRepulsion comp = pawn.CheckForAsexualComp();
            return comp.rating;
        }

        public static Comp_SexRepulsion CheckForAsexualComp(this Pawn p)
        {
            Comp_SexRepulsion comp = p.TryGetComp<Comp_SexRepulsion>();
            if (comp == null)
            {
                FieldInfo field = AccessTools.Field(typeof(ThingWithComps), "comps");
                List<ThingComp> compList = (List<ThingComp>)field.GetValue(p);
                ThingComp newComp = (ThingComp)Activator.CreateInstance(typeof(Comp_SexRepulsion));
                newComp.parent = p;
                compList.Add(newComp);
                newComp.Initialize(new CompProperties());
                newComp.PostExposeData();
                comp = p.TryGetComp<Comp_SexRepulsion>();
                if (comp == null)
                {
                    LogUtil.Error("Unable to add Comp_SexRepulsion");
                }
            }
            return comp;
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

        private static List<TraitDef> asexualTraits = new List<TraitDef> { TraitDefOf.Asexual, RomanceDefOf.BiAce, RomanceDefOf.HeteroAce, RomanceDefOf.HomoAce };

        public static bool IsAsexual(this Pawn pawn)
        {
            if (pawn.story != null && pawn.story.traits != null)
            {
                foreach (Trait trait in pawn.story.traits.allTraits)
                {
                    if (asexualTraits.Contains(trait.def) && !trait.Suppressed)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool IsHomo(this Pawn pawn)
        {
            return pawn.GetOrientation() == Orientation.Homo;
        }

        public static bool IsHetero(this Pawn pawn)
        {
            return pawn.GetOrientation() == Orientation.Hetero;
        }

        public static bool IsBi(this Pawn pawn)
        {
            return pawn.GetOrientation() == Orientation.Bi;
        }

        public static bool IsAro(this Pawn pawn)
        {
            return pawn.GetOrientation() == Orientation.None;
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
                    LogUtil.Error("Unable to add Comp_PartnerList");
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
            if (pawn.IsHomo())
            {
                if (target.gender != pawn.gender)
                {
                    factor *= 0.125f;
                }
            }
            if (pawn.IsHetero())
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

        /// <summary>
        /// Whether a pawn with the given <paramref name="ideo"/> and <paramref name="gender"/> is able to do an event of <paramref name="def"/>
        /// </summary>
        /// <param name="def"></param>
        /// <param name="ideo"></param>
        /// <param name="gender"></param>
        /// <returns></returns>
        public static bool WillingToDoGendered(this HistoryEventDef def, Ideo ideo, Gender gender)
        {
            //Look at each precept
            foreach (Precept precept in (List<Precept>)AccessTools.Field(typeof(Ideo), "precepts").GetValue(ideo))
            {
                //Look at the comps on the precept
                foreach (PreceptComp comp in precept.def.comps)
                {
                    //We only care if it has a gendered comp
                    if (comp is PreceptComp_UnwillingToDo_Gendered genderedComp)
                    {
                        //First check if this precept is for the event in question
                        if (genderedComp.eventDef != def)
                        {
                            continue;
                        }
                        //Now we compare the gender we're asking about with the gender of the comp
                        //If they are the same, then the extra lover is not allowed for the gender in question
                        if (gender == genderedComp.gender)
                        {
                            return false;
                        }
                    }
                }
            }
            //If nothing got hit, then we're good to go
            return true;
        }
    }

    public enum Orientation
    {
        Homo,
        Hetero,
        Bi,
        None,
    }

    internal static class LogUtil
    {
        private static string WrapMessage(string message) => $"<color=#1116e4>[WayBetterRomance]</color> {message}";

        internal static void Message(string message) => Log.Message(WrapMessage(message));
        internal static void Warning(string message) => Log.Warning(WrapMessage(message));
        internal static void WarningOnce(string message, int key) => Log.WarningOnce(WrapMessage(message), key);
        internal static void Error(string message) => Log.Error(WrapMessage(message));
        internal static void ErrorOnce(string message, int key) => Log.ErrorOnce(WrapMessage(message), key);
    }
}