using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using static BetterRomance.WBRLogger;

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
        /// <param name="cheaterList">A list of pawns who will think that <paramref name="pawn"/> cheated on them, regardless of what <paramref name="pawn"/> thinks.</param>
        /// <param name="forRomance">Whether this check is in regards to relationships, as opposed to sex; used for romance attempts and dates.</param>
        //forRomance is used to differentiate between cheating by having sex with someone and cheating by taking a new lover
        //The lovin' precept is meant to represent thoughts about sex, not relationships
        //So you can be totally fine hooking up with other people, but starting a new relationship is not cool
        //Or the other way around, you can be in as many relationships as you want, but having sex without a relationship first is not cool
        public static bool IsThisCheating(Pawn pawn, Pawn otherPawn, out List<Pawn> cheaterList, bool forRomance = false)
        {
            cheaterList = new();
            //Are they in a relationship?
            if (otherPawn != null && LovePartnerRelationUtility.LovePartnerRelationExists(pawn, otherPawn))
            {
                return false;
            }
            //See what their partners think
            foreach (Pawn p in GetAllLoveRelationPawns(pawn, false, false))
            {
                //Check if a new partner would be allowed by p's ideo according to pawn's gender
                //If it's for romance they will be upset, otherwise check if they care about cheating
                if (!pawn.GetHistoryEventForLoveRelationCountPlusOne().WillingToDoGendered(p.Ideo, pawn.gender) && (forRomance || p.CaresAboutCheating()))
                {
                    cheaterList.Add(p);
                }
            }
            //The cheater list is for use later, initiator will only look at their ideo and settings to decide if they're cheating

            //First check if they're allowed to do it
            //Are they allowed to have another lover?
            bool allowed = IdeoUtility.DoerWillingToDo(pawn.GetHistoryEventForLoveRelationCountPlusOne(), pawn);
            //If they can't have another lover, but it's for sex, check if they care about cheating
            if (!forRomance && !allowed)
            {
                allowed = !pawn.CaresAboutCheating();
            }
            //If it's not allowed, it's cheating, regardless of what partners might think
            if (!allowed)
            {
                return true;
            }
            //If it's allowed and no one's upset, it's not cheating
            if (cheaterList.NullOrEmpty())
            {
                return false;
            }
            //At this point it's allowed but some partners will be upset
            //So we're just checking if they have the faithful trait
            return pawn.story.traits.HasTrait(RomanceDefOf.Faithful);
        }

        /// <summary>
        /// Whether <paramref name="pawn"/> decides to continue with an interaction with <paramref name="otherPawn"/>. Always returns true if interaction is not cheating and is allowed by ideo. Otherwise, uses the partner they would feel the worst about cheating on to decide.
        /// </summary>
        /// <param name="chance">The chance used to determine the result, passed out for use in tool tips.</param>
        /// <param name="forRomance">Whether deciding to cheat would result in a new relationship.</param>
        public static bool WillPawnContinue(Pawn pawn, Pawn otherPawn, out float chance, bool forRomance, bool excludeCheatingPrecept)
        {
            chance = 1f;
            if (IsThisCheating(pawn, otherPawn, out List<Pawn> cheatedOnList, forRomance))
            {
                //If pawn thinks they're cheating but no one would be upset about it, just use their love relations to decide
                if (cheatedOnList.NullOrEmpty())
                {
                    cheatedOnList = GetAllLoveRelationPawns(pawn, false, false);
                }
                //Generate random value, and compare to opinion of most liked partner and base cheat chance
                if (Rand.Value > (chance = CheatingChance(pawn, excludeCheatingPrecept) * PartnerFactor(pawn, cheatedOnList, out _, forRomance)))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>Factor based on opinion of most liked partner. Higher opinion means a lower factor.</summary>
        /// <param name="pawn">The <see cref="Pawn"/> in question.</param>
        /// <param name="partnerList">List of partners that would feel cheated on, provided by <see cref="IsThisCheating(Pawn, Pawn, out List{Pawn}, bool)"/></param>
        /// <param name="partner">The partner <paramref name="pawn"/> would feel the worst about cheating on.</param>
        public static float PartnerFactor(Pawn pawn, List<Pawn> partnerList, out Pawn partner, bool forRomance)
        {
            partner = null;
            //If there's not a real person they're actually cheating on, just pretend
            if (partnerList.NullOrEmpty())
            {
                return PartnerFactor(pawn, null, forRomance);
            }
            //Find the lowest factor
            float partnerFactor = 99999f;
            foreach (Pawn p in partnerList)
            {
                float tempPartnerFactor = PartnerFactor(pawn, p, forRomance);
                if (tempPartnerFactor < partnerFactor)
                {
                    partnerFactor = tempPartnerFactor;
                    partner = p;
                }
            }
            return partnerFactor;
        }

        /// <summary>Factor based on <paramref name="cheater"/>'s feelings towards a specific <paramref name="partner"/>. Uses opinion, philanderer status, and type of relationship. Higher opinion results in a lower factor.</summary>
        public static float PartnerFactor(Pawn cheater, Pawn partner, bool forRomance)
        {
            //Opinion
            IntRange opinionRange = BetterRomanceMod.settings.cheatingOpinion;
            if (partner is null)
            {
                //This is for people who think they are cheating but there's no real people that they are cheating on
                //Which should only be those with lover and spouse count at 0
                //So judge based on a hypothetical partner they have 0 opinion of
                //With default settings this would be 0.5f
                return Mathf.InverseLerp(opinionRange.max, opinionRange.min, 0);
            }
            float opinionFactor = Mathf.InverseLerp(opinionRange.max, opinionRange.min, cheater.relations.OpinionOf(partner));
            //Increase for philanderer
            if (cheater.story.traits.HasTrait(RomanceDefOf.Philanderer))
            {
                opinionFactor += cheater.Map == partner.Map ? 0.25f : 0.5f;
            }

            //Type of relationship
            //Only consider for romance attempts, since in most situations it would mean ending the existing relationship
            float relationFactor = 1f;
            if (forRomance)
            {
                if (cheater.relations.DirectRelationExists(PawnRelationDefOf.Lover, partner))
                {
                    relationFactor = 0.6f;
                }
                else if (cheater.relations.DirectRelationExists(PawnRelationDefOf.Fiance, partner))
                {
                    relationFactor = 0.1f;
                }
                else if (cheater.relations.DirectRelationExists(PawnRelationDefOf.Spouse, partner))
                {
                    relationFactor = 0.3f;
                }
                //Check for custom relations and use same adjustment as lover
                else if (CustomLoveRelationUtility.CheckCustomLoveRelations(cheater, partner) != null)
                {
                    relationFactor = 0.6f;
                }
            }

            //Adding romance chance factor lowers things way too much, since generally people in a relationship have a high romance chance factor
            float partnerFactor = opinionFactor * relationFactor;
            return partnerFactor;
        }

        /// <summary>
        /// Base chance that a given <paramref name="pawn"/> will cheat. Based on settings and traits.
        /// </summary>
        /// <param name="excludePrecept">Whether to include the cheating precepts from RotR. For use in places where RotR has it's own patches to apply the precepts, or for tooltips, since RotR has it's own methods for those.</param>
        public static float CheatingChance(Pawn pawn, bool excludePrecept = false)
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
            if (Settings.RotRActive && pawn.Ideo != null && !excludePrecept)
            {
                cheatChance *= RotR_Integration.RotRCheatChanceModifier(pawn);
            }
            return cheatChance;
        }

        /// <summary>Grabs the first non-spouse love partner of the opposite gender. For use in generating parents.</summary>
        public static Pawn GetFirstLoverOfOppositeGender(Pawn pawn)
        {
            if (Settings.AltFertilityActive)
            {
                return pawn.GetFirstImpregnationPairLover();
            }
            foreach (Pawn lover in GetNonSpouseLovers(pawn, true))
            {
                if (pawn.gender.Opposite() == lover.gender)
                {
                    return lover;
                }
            }
            return null;
        }

        /// <summary>Generates a list of love partners that does not include spouses or fianc�es.</summary>
        public static List<Pawn> GetNonSpouseLovers(Pawn pawn, bool includeDead)
        {
            List<Pawn> list = new();
            if (!pawn.RaceProps.IsFlesh)
            {
                return list;
            }
            foreach (DirectPawnRelation rel in pawn.relations.DirectRelations)
            {
                if ((rel.def == PawnRelationDefOf.Lover || CustomLoveRelationUtility.LoveRelations.Contains(rel.def)) && (includeDead || !rel.otherPawn.Dead))
                {
                    list.Add(rel.otherPawn);
                }
            }
            return list;
        }

        /// <summary>Generates a list of pawns that are in love relations with <paramref name="pawn"/>. Pawns are only listed once, even if they are in more than one love relation.</summary>
        /// <param name="includeDead">Whether dead pawns are added to the list.</param>
        /// <param name="onMap">Whether pawns must be on the same map to be added to the list.</param>
        public static List<Pawn> GetAllLoveRelationPawns(Pawn pawn, bool includeDead, bool onMap)
        {
            List<Pawn> list = new();
            if (!pawn.RaceProps.IsFlesh)
            {
                return list;
            }
            foreach (DirectPawnRelation rel in LovePartnerRelationUtility.ExistingLovePartners(pawn, includeDead))
            {
                if (!list.Contains(rel.otherPawn))
                {
                    if (pawn.Map == rel.otherPawn.Map || !onMap)
                    {
                        list.Add(rel.otherPawn);
                    }
                }
            }
            return list;
        }

        /// <summary>Finds the most liked pawn with a specific <paramref name="relation"/> to <paramref name="pawn"/>. Any direct relation will work, no implied relations.</summary>
        public static Pawn GetMostLikedOfRel(Pawn pawn, PawnRelationDef relation, bool allowDead)
        {
            Pawn result = null;
            int maxOpinion = 0;
            foreach (DirectPawnRelation rel in pawn.relations.DirectRelations)
            {
                if (rel.def == relation && (!rel.otherPawn.Dead || allowDead))
                {
                    if (pawn.relations.OpinionOf(rel.otherPawn) > maxOpinion)
                    {
                        maxOpinion = pawn.relations.OpinionOf(rel.otherPawn);
                        result = rel.otherPawn;
                    }
                }
            }
            return result;
        }

        public static List<Pawn> GetAllSpawnedHumanlikesOnMap(Map map, bool includeFH = false)
        {
            List<Pawn> result = new();
            foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
            {
                //Exclude animals and enemies and robots
                //I want former humans to be included for dates only
                //Just changing Humanlike to IsHumanlike won't help because former humans don't pass the drone check
                if (pawn.IsHumanlike() && !pawn.HostileTo(Faction.OfPlayer) && !pawn.DroneCheck(includeFH))
                {
                    result.Add(pawn);
                }
            }
            return result;
        }

        /// <summary>Checks if a <typeparamref name="T"/> already exists on <paramref name="p"/>, adds it if needed, and then returns the comp.</summary>
        public static T CheckForComp<T>(this Pawn p) where T : ThingComp
        {
            T comp = p.GetComp<T>();
            if (comp == null)
            {
                List<ThingComp> compList = (List<ThingComp>)AccessTools.Field(typeof(ThingWithComps), "comps").GetValue(p);
                ThingComp newComp = (ThingComp)Activator.CreateInstance(typeof(T));
                newComp.parent = p;
                compList.Add(newComp);
                newComp.Initialize(new());
                newComp.PostExposeData();
                comp = p.GetComp<T>();
                if (comp == null)
                {
                    LogUtil.Error($"Unable to add {typeof(T).GetType().Name} to {p.Name}");
                }
            }
            return comp;
        }

        /// <summary>Factor based on <paramref name="pawn"/>'s orientation and <paramref name="target"/>'s gender.</summary>
        public static float OrientationFactor(Pawn pawn, Pawn target, bool romantic = false)
        {
            float factor = 1f;
            //If this is a sexual encounter for an asexual person, use their rating and adjust further for romantic attraction
            if (!romantic && pawn.IsAsexual() && !target.IsAsexual())
            {
                factor *= pawn.AsexualRating() / 2;
                if (!pawn.AttractedTo(target, true))
                {
                    factor *= 0.125f;
                }
                //Exit here because otherwise they could get deducted again for no sexual attraction
                return factor;
            }
            //I think that's really the only situation where we wouldn't just check the relevant attraction
            if (!pawn.AttractedTo(target, romantic))
            {
                factor *= 0.125f;
            }
            return factor;
        }

        public static bool EitherHasLoveEnhancer(Pawn first, Pawn second) => HasLoveEnhancer(first) || HasLoveEnhancer(second);

        public static bool HasLoveEnhancer(Pawn pawn) => pawn.health?.hediffSet?.hediffs.Any((Hediff h) => h.def == HediffDefOf.LoveEnhancer) ?? false;

        /// <summary>Whether a pawn with the given <paramref name="ideo"/> and <paramref name="gender"/> is able to do an event of <paramref name="def"/>.</summary>
        public static bool WillingToDoGendered(this HistoryEventDef def, Ideo ideo, Gender gender)
        {
            if (ideo == null)
            {
                return true;
            }
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
                        //If they are the same, then the event not allowed for the gender in question
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

        /// <summary>Finds a partner in <paramref name="pawn"/>'s bed that will agree to lovin', skipping sex repulsed partners.</summary>
        public static Pawn GetPartnerInMyBedForLovin(Pawn pawn)
        {
            Building_Bed bed = pawn.CurrentBed();
            if (bed == null || bed.SleepingSlotsCount <= 1)
            {
                return null;
            }
            if (!LovePartnerRelationUtility.HasAnyLovePartner(pawn))
            {
                return null;
            }
            foreach (Pawn curOccupant in bed.CurOccupants)
            {
                if (curOccupant != pawn && LovePartnerRelationUtility.LovePartnerRelationExists(pawn, curOccupant))
                {
                    if (curOccupant.SexRepulsed())
                    {
                        continue;
                    }
                    return curOccupant;
                }
            }
            return null;
        }

        /// <summary>Determines the appropriate relationship to give <paramref name="first"/> and <paramref name="second"/> as prospective parents.</summary>
        public static PawnRelationDef GetAppropriateParentRelationship(Pawn first, Pawn second)
        {
            Pawn mom = first.gender == Gender.Female ? first : second;
            Pawn dad = first.gender == Gender.Male ? first : second;
            //Warning if the genders are not right?
            if (mom == null || dad == null)
            {
                LogUtil.Error($"Error determining relationship for {first.Name.ToStringShort} and {second.Name.ToStringShort}, must be opposite genders");
                return null;
            }
            //I'd like to eventually allow for non-male/female parents
            if (mom.CouldWeBeMarried(dad))
            {
                return PawnRelationDefOf.Spouse;
            }
            if (mom.CouldWeBeLovers(dad))
            {
                //Maybe check for possible custom love relations?
                return PawnRelationDefOf.Lover;
            }
            return PawnRelationDefOf.ExLover;
        }

        /// <summary>Determines male/female roles for pregnancy. Returns false if both roles cannot be fulfilled.</summary>
        public static bool DetermineSexesForPregnancy(Pawn firstPawn, Pawn secondPawn, out Pawn male, out Pawn female)
        {
            male = null;
            female = null;
            if (firstPawn.CanBeMale() && secondPawn.CanBeFemale())
            {
                male = firstPawn;
                female = secondPawn;
                return true;
            }
            if (firstPawn.CanBeFemale() && secondPawn.CanBeMale())
            {
                male = secondPawn;
                female = firstPawn;
                return true;
            }
            return false;
        }
    }

    internal static class CodeInstructionMethods
    {
        public static CodeInstruction LoadField(this FieldInfo fieldInfo, bool useAddress = false)
        {
            if (fieldInfo is null)
            {
                throw new ArgumentException($"fieldInfo is null");
            }
            return new(!useAddress ? fieldInfo.IsStatic ? OpCodes.Ldsfld : OpCodes.Ldfld : fieldInfo.IsStatic ? OpCodes.Ldsflda : OpCodes.Ldflda, fieldInfo);
        }

        public static bool LoadsField(this CodeInstruction code, Type type, string name)
        {
            FieldInfo field = AccessTools.Field(type, name);
            return code.LoadsField(field);
        }

        //Need to make this determine if call or callvirt should be used
        public static CodeInstruction Call(this MethodInfo methodInfo)
        {
            if (methodInfo is null)
            {
                throw new ArgumentException($"methodInfo is null");
            }

            return new(OpCodes.Call, methodInfo);
        }

        public static bool Calls(this CodeInstruction code, Type type, string name)
        {
            MethodInfo method = AccessTools.Method(type, name);
            return code.Calls(method);
        }

        public static int LocalIndex(this CodeInstruction code)
        {
            if (code.opcode == OpCodes.Ldloc_S || code.opcode == OpCodes.Ldloc)
            {
                return ((LocalVariableInfo)code.operand).LocalIndex;
            }

            if (code.opcode == OpCodes.Stloc_S || code.opcode == OpCodes.Stloc)
            {
                return ((LocalVariableInfo)code.operand).LocalIndex;
            }

            if (code.opcode == OpCodes.Ldloca_S || code.opcode == OpCodes.Ldloca)
            {
                return ((LocalVariableInfo)code.operand).LocalIndex;
            }
#if v1_5
            return CodeInstructionExtensions.LocalIndex(code);
#else
            if (code.opcode == OpCodes.Ldloc_0 || code.opcode == OpCodes.Stloc_0)
            {
                return 0;
            }

            if (code.opcode == OpCodes.Ldloc_1 || code.opcode == OpCodes.Stloc_1)
            {
                return 1;
            }

            if (code.opcode == OpCodes.Ldloc_2 || code.opcode == OpCodes.Stloc_2)
            {
                return 2;
            }

            if (code.opcode == OpCodes.Ldloc_3 || code.opcode == OpCodes.Stloc_3)
            {
                return 3;
            }
            throw new ArgumentException("Instruction is not a load or store", "code");
#endif
        }
    }

    internal static class InfoHelper
    {
        public static MethodInfo AdultMinAge = AccessTools.PropertyGetter(typeof(Pawn_AgeTracker), nameof(Pawn_AgeTracker.AdultMinAge));
        public static FieldInfo RitualPawnAgeCurve = AccessTools.Field(typeof(RitualOutcomeComp_Quality), nameof(RitualOutcomeComp_PawnAge.curve));
        public static FieldInfo AgeTrackerPawn = AccessTools.Field(typeof(Pawn_AgeTracker), "pawn");
        public static FieldInfo GayTraitDefOf = AccessTools.Field(typeof(TraitDefOf), nameof(TraitDefOf.Gay));
        public static FieldInfo DefOfSpouse = AccessTools.Field(typeof(PawnRelationDefOf), nameof(PawnRelationDefOf.Spouse));
        public static MethodInfo RomanceFactorLine = AccessTools.Method(typeof(InteractionWorker_RomanceAttempt), "RomanceFactorLine");
        public static FieldInfo PawnGender = AccessTools.Field(typeof(Pawn), "gender");
        public static MethodInfo CanDrawTryRomance = AccessTools.Method(typeof(SocialCardUtility), "CanDrawTryRomance");
    }
}
