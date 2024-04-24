using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
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
        /// <param name="loverCountOnly">Whether to look at lover count only, otherwise cheating setting/precept is also looked at</param>
        /// <returns>True or False</returns>
        public static bool IsThisCheating(Pawn pawn, Pawn otherPawn, out List<Pawn> cheaterList, bool loverCountOnly = false)
        {
            //This has to happen to get passed out
            cheaterList = new();
            //Are they in a relationship?
            if (otherPawn != null && LovePartnerRelationUtility.LovePartnerRelationExists(pawn, otherPawn))
            {
                return false;
            }
            foreach (Pawn p in GetAllLoveRelationPawns(pawn, false, false))
            {
                //If the pawns have different ideos, I think this will check if the partner would feel cheated on per their ideo and settings
                if (!pawn.GetHistoryEventForLoveRelationCountPlusOne().WillingToDoGendered(p.Ideo, pawn.gender) && (loverCountOnly || p.CaresAboutCheating()))
                {
                    cheaterList.Add(p);
                }
            }
            //The cheater list is for use later, initiator will only look at their ideo and settings to decide if they're cheating
            if (IdeoUtility.DoerWillingToDo(pawn.GetHistoryEventForLoveRelationCountPlusOne(), pawn) || (!loverCountOnly && !pawn.CaresAboutCheating()))
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
        public static bool WillPawnContinue(Pawn pawn, Pawn otherPawn, out Pawn cheatOn, bool loverCountOnly = false)
        {
            cheatOn = null;
            if (IsThisCheating(pawn, otherPawn, out List<Pawn> cheatedOnList, loverCountOnly))
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
        /// <param name="partnerList">List of partners that would feel cheated on, provided by <see cref="IsThisCheating(Pawn, Pawn, out List{Pawn}, bool)"/></param>
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
            List<Pawn> list = new();
            if (!pawn.RaceProps.IsFlesh)
            {
                return list;
            }
            foreach (DirectPawnRelation rel in pawn.relations.DirectRelations)
            {
                if (rel.def == PawnRelationDefOf.Lover && (includeDead || !rel.otherPawn.Dead))
                {
                    list.Add(rel.otherPawn);
                }
                else if (CustomLoveRelationUtility.LoveRelations.Contains(rel.def) && (includeDead || !rel.otherPawn.Dead))
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
        /// <returns>A list of pawns that have a love relation with <paramref name="pawn"/></returns>
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

        /// <summary>
        /// Finds the most liked pawn with a specific <paramref name="relation"/> to <paramref name="pawn"/>. Any direct relation will work, no implied relations.
        /// </summary>
        /// <param name="pawn"></param>
        /// <param name="relation"></param>
        /// <param name="allowDead"></param>
        /// <returns></returns>
        public static Pawn GetMostLikedOfRel(Pawn pawn, PawnRelationDef relation, bool allowDead)
        {
            Pawn result = null;
            int maxOpinion = 0;
            foreach (DirectPawnRelation rel in pawn.relations.DirectRelations)
            {
                if (rel.def == relation && (rel.otherPawn.Dead || allowDead))
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

        public static List<Pawn> GetAllSpawnedHumanlikesOnMap(Map map)
        {
            List<Pawn> result = new();
            foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
            {
                //Exclude animals and enemies and robots
                if (pawn.RaceProps.Humanlike && !pawn.HostileTo(Faction.OfPlayer) && !pawn.DroneCheck())
                {
                    result.Add(pawn);
                }
            }
            return result;
        }

        /// <summary>
        /// Checks if a <typeparamref name="T"/> already exists on <paramref name="p"/>, adds it if needed, and then returns the comp
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public static T CheckForComp<T>(this Pawn p) where T : ThingComp
        {
            T comp = p.TryGetComp<T>();
            if (comp == null)
            {
                List<ThingComp> compList = (List<ThingComp>)AccessTools.Field(typeof(ThingWithComps), "comps").GetValue(p);
                ThingComp newComp = (ThingComp)Activator.CreateInstance(typeof(T));
                newComp.parent = p;
                compList.Add(newComp);
                newComp.Initialize(new CompProperties());
                newComp.PostExposeData();
                comp = p.TryGetComp<T>();
                if (comp == null)
                {
                    LogUtil.Error($"Unable to add {typeof(T).GetType().Name}");
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
                    if (curOccupant.IsAsexual() && curOccupant.AsexualRating() < 0.2f)
                    {
                        continue;
                    }
                    return curOccupant;
                }
            }
            return null;
        }

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
    }

    internal static class LogUtil
    {
        private static string WrapMessage(string message) => $"<color=#1116e4>[WayBetterRomance]</color> {message}";
        internal static void Message(string message, bool debugOnly = false)
        {
            if ((debugOnly && Settings.debugLogging) || !debugOnly)
            {
                Log.Message(WrapMessage(message));
            }
        }

        internal static void Warning(string message, bool debugOnly = false)
        {
            if ((debugOnly && Settings.debugLogging) || !debugOnly)
            {
                Log.Warning(WrapMessage(message));
            }
        }

        internal static void WarningOnce(string message, int key, bool debugOnly = false)
        {
            if ((debugOnly && Settings.debugLogging) || !debugOnly)
            {
                Log.WarningOnce(WrapMessage(message), key);
            }
        }

        internal static void Error(string message, bool debugOnly = false)
        {
            if ((debugOnly && Settings.debugLogging) || !debugOnly)
            {
                Log.Error(WrapMessage(message));
            }
        }

        internal static void ErrorOnce(string message, int key, bool debugOnly = false)
        {
            if ((debugOnly && Settings.debugLogging) || !debugOnly)
            {
                Log.ErrorOnce(WrapMessage(message), key);
            }
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
            return new CodeInstruction((!useAddress) ? (fieldInfo.IsStatic ? OpCodes.Ldsfld : OpCodes.Ldfld) : (fieldInfo.IsStatic ? OpCodes.Ldsflda : OpCodes.Ldflda), fieldInfo);
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

            return new CodeInstruction(OpCodes.Call, methodInfo);
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

        public static MethodInfo AdultMinAge = AccessTools.PropertyGetter(typeof(Pawn_AgeTracker), nameof(Pawn_AgeTracker.AdultMinAge));
    }
}