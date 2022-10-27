using RimWorld;
using System;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using Verse;
using AlienRace;
using HarmonyLib;

namespace BetterRomance
{
    public static class SettingsUtilities
    {
        //Grabbing stuff from def mod extensions
        //Sexuality chances does not have methods, since they're only needed in the AssignOrientation function

        //Casual sex settings
        public static CasualSexSettings GetCasualSexSettings(Pawn pawn)
        {
            if (pawn.kindDef.HasModExtension<CasualSexSettings>())
            {
                return pawn.kindDef.GetModExtension<CasualSexSettings>();
            }
            else if (pawn.def.HasModExtension<CasualSexSettings>())
            {
                return pawn.def.GetModExtension<CasualSexSettings>();
            }
            return null;
        }

        public static bool CaresAboutCheating(this Pawn pawn)
        {
            return GetCasualSexSettings(pawn) == null || GetCasualSexSettings(pawn).caresAboutCheating;
        }

        public static bool HookupAllowed(this Pawn pawn)
        {
            return GetCasualSexSettings(pawn) == null || GetCasualSexSettings(pawn).willDoHookup;
        }

        public static float HookupRate(this Pawn pawn)
        {
            if (BetterRomanceMod.settings.hookupRate == 0)
            {
                return 0f;
            }
            return (GetCasualSexSettings(pawn) != null) ? GetCasualSexSettings(pawn).hookupRate : BetterRomanceMod.settings.hookupRate;
        }

        public static float AlienLoveChance(this Pawn pawn)
        {
            if (BetterRomanceMod.settings.alienLoveChance == 0)
            {
                return 0f;
            }
            return (GetCasualSexSettings(pawn) != null) ? GetCasualSexSettings(pawn).alienLoveChance : BetterRomanceMod.settings.alienLoveChance;
        }

        /// <summary>
        /// Get settings related to casual hookups
        /// </summary>
        /// <param name="pawn"></param>
        /// <returns></returns>
        public static HookupTrigger GetHookupSettings(Pawn pawn)
        {
            if (pawn.kindDef.HasModExtension<CasualSexSettings>())
            {
                if (pawn.kindDef.GetModExtension<CasualSexSettings>().hookupTriggers != null)
                {
                    return pawn.kindDef.GetModExtension<CasualSexSettings>().hookupTriggers;
                }
                else if (pawn.def.HasModExtension<CasualSexSettings>())
                {
                    if (pawn.def.GetModExtension<CasualSexSettings>().hookupTriggers != null)
                    {
                        return pawn.def.GetModExtension<CasualSexSettings>().hookupTriggers;
                    }
                    return null;
                }
                return null;
            }
            else if (pawn.def.HasModExtension<CasualSexSettings>())
            {
                if (pawn.def.GetModExtension<CasualSexSettings>().hookupTriggers != null)
                {
                    return pawn.def.GetModExtension<CasualSexSettings>().hookupTriggers;
                }
                return null;
            }
            return null;
        }

        public static int MinOpinionForHookup(this Pawn pawn)
        {
            return (GetHookupSettings(pawn) != null) ? GetHookupSettings(pawn).minOpinion : 5;
        }

        public static bool MustBeFertileForHookup(this Pawn pawn)
        {
            return (GetHookupSettings(pawn) != null) && GetHookupSettings(pawn).mustBeFertile;
        }

        /// <summary>
        /// Get settings related to ordered hookups
        /// </summary>
        /// <param name="pawn"></param>
        /// <returns></returns>
        public static HookupTrigger GetOrderedHookupSettings(Pawn pawn)
        {
            if (pawn.kindDef.HasModExtension<CasualSexSettings>())
            {
                if (pawn.kindDef.GetModExtension<CasualSexSettings>().orderedHookupTriggers != null)
                {
                    return pawn.kindDef.GetModExtension<CasualSexSettings>().orderedHookupTriggers;
                }
                else if (pawn.def.HasModExtension<CasualSexSettings>())
                {
                    if (pawn.def.GetModExtension<CasualSexSettings>().orderedHookupTriggers != null)
                    {
                        return pawn.def.GetModExtension<CasualSexSettings>().orderedHookupTriggers;
                    }
                    return null;
                }
                return null;
            }
            else if (pawn.def.HasModExtension<CasualSexSettings>())
            {
                if (pawn.def.GetModExtension<CasualSexSettings>().orderedHookupTriggers != null)
                {
                    return pawn.def.GetModExtension<CasualSexSettings>().orderedHookupTriggers;
                }
                return null;
            }
            return null;
        }

        public static int MinOpinionForOrderedHookup(this Pawn pawn)
        {
            return (GetOrderedHookupSettings(pawn) != null) ? GetOrderedHookupSettings(pawn).minOpinion : 5;
        }

        public static bool MustBeFertileForOrderedHookup(this Pawn pawn)
        {
            return (GetOrderedHookupSettings(pawn) != null) && GetOrderedHookupSettings(pawn).mustBeFertile;
        }

        //Regular sex settings
        public static RegularSexSettings GetSexSettings(Pawn pawn)
        {
            if (pawn.kindDef.HasModExtension<RegularSexSettings>())
            {
                return pawn.kindDef.GetModExtension<RegularSexSettings>();
            }
            else if (pawn.def.HasModExtension<RegularSexSettings>())
            {
                return pawn.def.GetModExtension<RegularSexSettings>();
            }
            return null;
        }

        public static float MinAgeForSex(this Pawn pawn)
        {
            return (GetSexSettings(pawn) != null) ? GetSexSettings(pawn).minAgeForSex : 16f;
        }

        public static float MaxAgeForSex(this Pawn pawn)
        {
            return (GetSexSettings(pawn) != null) ? GetSexSettings(pawn).maxAgeForSex : 80f;
        }

        public static float MaxAgeGap(this Pawn pawn)
        {
            return (GetSexSettings(pawn) != null) ? GetSexSettings(pawn).maxAgeGap : 40f;
        }

        public static float DeclineAtAge(this Pawn pawn)
        {
            return (GetSexSettings(pawn) != null) ? GetSexSettings(pawn).declineAtAge : 30f;
        }

        //Relation Settings
        public static RelationSettings GetRelationSettings(Pawn pawn)
        {
            if (pawn.kindDef.HasModExtension<RelationSettings>())
            {
                return pawn.kindDef.GetModExtension<RelationSettings>();
            }
            else if (pawn.def.HasModExtension<RelationSettings>())
            {
                return pawn.def.GetModExtension<RelationSettings>();
            }
            return null;
        }

        public static bool SpouseAllowed(this Pawn pawn)
        {
            return GetRelationSettings(pawn) == null || GetRelationSettings(pawn).spousesAllowed;
        }

        public static bool ChildAllowed(this Pawn pawn)
        {
            return GetRelationSettings(pawn) == null || GetRelationSettings(pawn).childrenAllowed;
        }

        public static PawnKindDef ParentPawnkind(this Pawn pawn, Gender gender)
        {
            if (GetRelationSettings(pawn).pawnKindForParentGlobal != null)
            {
                return GetRelationSettings(pawn).pawnKindForParentGlobal;
            }
            if (GetRelationSettings(pawn).pawnKindForParentFemale != null && GetRelationSettings(pawn).pawnKindForParentMale != null)
            {
                if (gender == Gender.Female)
                {
                    return GetRelationSettings(pawn).pawnKindForParentFemale;
                }
                else if (gender == Gender.Male)
                {
                    return GetRelationSettings(pawn).pawnKindForParentMale;
                }
                else if (gender == Gender.None)
                {
                    throw new ArgumentException("Please provide a gender");
                }
            }
            throw new Exception("No parent pawnkind set for " + pawn.kindDef.defName + " or " + pawn.def.defName);
        }

        public static float MinAgeToHaveChildren(this Pawn pawn)
        {
            if (pawn.gender == Gender.Female)
            {
                return (GetRelationSettings(pawn) != null) ? GetRelationSettings(pawn).minFemaleAgeToHaveChildren : 16f;
            }
            else if (pawn.gender == Gender.Male)
            {
                return (GetRelationSettings(pawn) != null) ? GetRelationSettings(pawn).minMaleAgeToHaveChildren : 14f;
            }
            throw new ArgumentException("This pawn has no gender");
        }
        //Same as above but takes a gender argument, for use when getting age settings for pawns that haven't been generated yet
        public static float MinAgeToHaveChildren(this Pawn pawn, Gender gender)
        {
            if (gender == Gender.Female)
            {
                return (GetRelationSettings(pawn) != null) ? GetRelationSettings(pawn).minFemaleAgeToHaveChildren : 16f;
            }
            else if (gender == Gender.Male)
            {
                return (GetRelationSettings(pawn) != null) ? GetRelationSettings(pawn).minMaleAgeToHaveChildren : 14f;
            }
            throw new ArgumentException("No gender provided");
        }

        public static float MaxAgeToHaveChildren(this Pawn pawn)
        {
            if (pawn.gender == Gender.Female)
            {
                return (GetRelationSettings(pawn) != null) ? GetRelationSettings(pawn).maxFemaleAgeToHaveChildren : 45f;
            }
            else if (pawn.gender == Gender.Male)
            {
                return (GetRelationSettings(pawn) != null) ? GetRelationSettings(pawn).maxMaleAgeToHaveChildren : 50f;
            }
            throw new ArgumentException("This pawn has no gender");
        }
        //Same as above but takes a gender argument, for use when getting age settings for pawns that haven't been generated yet
        public static float MaxAgeToHaveChildren(this Pawn pawn, Gender gender)
        {
            if (gender == Gender.Female)
            {
                return (GetRelationSettings(pawn) != null) ? GetRelationSettings(pawn).maxFemaleAgeToHaveChildren : 45f;
            }
            else if (gender == Gender.Male)
            {
                return (GetRelationSettings(pawn) != null) ? GetRelationSettings(pawn).maxMaleAgeToHaveChildren : 50f;
            }
            throw new ArgumentException("This pawn has no gender");
        }

        public static float UsualAgeToHaveChildren(this Pawn pawn)
        {
            if (pawn.gender == Gender.Female)
            {
                return (GetRelationSettings(pawn) != null) ? GetRelationSettings(pawn).usualFemaleAgeToHaveChildren : 27f;
            }
            else if (pawn.gender == Gender.Male)
            {
                return (GetRelationSettings(pawn) != null) ? GetRelationSettings(pawn).usualMaleAgeToHaveChildren : 30f;
            }
            throw new ArgumentException("This pawn has no gender");
        }
        //Same as above but takes a gender argument, for use when getting age settings for pawns that haven't been generated yet
        public static float UsualAgeToHaveChildren(this Pawn pawn, Gender gender)
        {
            if (gender == Gender.Female)
            {
                return (GetRelationSettings(pawn) != null) ? GetRelationSettings(pawn).usualFemaleAgeToHaveChildren : 27f;
            }
            else if (gender == Gender.Male)
            {
                return (GetRelationSettings(pawn) != null) ? GetRelationSettings(pawn).usualMaleAgeToHaveChildren : 30f;
            }
            throw new ArgumentException("No gender provided");
        }

        public static int MaxChildren(this Pawn pawn)
        {
            return (GetRelationSettings(pawn) != null) ? GetRelationSettings(pawn).maxChildrenDesired : 3;
        }

        public static float MinOpinionForRomance(this Pawn pawn)
        {
            return (GetRelationSettings(pawn) != null) ? GetRelationSettings(pawn).minOpinionRomance : 5f;
        }

        //Love Relation Settings
        public static List<PawnRelationDef> LoveRelations;
        public static List<PawnRelationDef> ExLoveRelations;

        public static List<PawnRelationDef> AdditionalExLoveRelations()
        {
            List<PawnRelationDef> result = new List<PawnRelationDef>();
            List<PawnRelationDef> relationList = DefDatabase<PawnRelationDef>.AllDefsListForReading;
            foreach (PawnRelationDef def in relationList)
            {
                if (def.HasModExtension<LoveRelations>())
                {
                    if (def.GetModExtension<LoveRelations>().isLoveRelation)
                    {
                        if (def.GetModExtension<LoveRelations>().exLoveRelation != null)
                        {
                            result.Add(def.GetModExtension<LoveRelations>().exLoveRelation);
                        }
                    }
                }
            }
            return result;
        }

        public static List<PawnRelationDef> AdditionalLoveRelations()
        {
            List<PawnRelationDef> result = new List<PawnRelationDef>();
            List<PawnRelationDef> relationList = DefDatabase<PawnRelationDef>.AllDefsListForReading;
            foreach (PawnRelationDef def in relationList)
            {
                if (def.HasModExtension<LoveRelations>())
                {
                    if (def.GetModExtension<LoveRelations>().isLoveRelation)
                    {
                        result.Add(def);
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Checks HAR settings to see if pawns consider each other aliens.
        /// DO NOT CALL IF HAR IS NOT ACTIVE
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns>True or False</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool AreRacesConsideredXeno(Pawn p1, Pawn p2)
        {
            if (p1.def != p2.def)
            {
                return !(p1.def is ThingDef_AlienRace alienDef && alienDef.alienRace.generalSettings.notXenophobistTowards.Contains(p2.def));
            }
            return false;
        }

        //Biotech Settings
        public static BiotechSettings GetBiotechSettings(Pawn pawn)
        {
            if (pawn.kindDef.HasModExtension<BiotechSettings>())
            {
                return pawn.kindDef.GetModExtension<BiotechSettings>();
            }
            else if (pawn.def.HasModExtension<BiotechSettings>())
            {
                return pawn.def.GetModExtension<BiotechSettings>();
            }
            return null;
        }

        public static SimpleCurve GetFertilityAgeCurve(this Pawn pawn)
        {
            if (pawn.gender == Gender.Male)
            {
                return (GetBiotechSettings(pawn) != null ? GetBiotechSettings(pawn).maleFertilityAgeFactor : GetDefaultFertilityAgeCurve(pawn.gender));
            }
            else if (pawn.gender == Gender.Female)
            {
                return (GetBiotechSettings(pawn) != null ? GetBiotechSettings(pawn).maleFertilityAgeFactor : GetDefaultFertilityAgeCurve(pawn.gender));
            }
            else
            {
                Log.Message("Tried to get fertility curve for pawn with no gender");
                return null;
            }
        }

        private static SimpleCurve GetDefaultFertilityAgeCurve(Gender gender)
        {
            if (gender == Gender.Male)
            {
                return new SimpleCurve
                {
                    new CurvePoint(14f, 0f),
                    new CurvePoint(18f, 1f),
                    new CurvePoint(50f, 1f),
                    new CurvePoint(90f, 0f),
                };
            }
            else if (gender == Gender.Female)
            {
                return new SimpleCurve
                {
                    new CurvePoint(14f, 0f),
                    new CurvePoint(20f, 1f),
                    new CurvePoint(28f, 1f),
                    new CurvePoint(35f, 0.5f),
                    new CurvePoint(40f, 0.1f),
                    new CurvePoint(45f, 0.02f),
                    new CurvePoint(50f, 0f),
                };
            }
            else
            {
                Log.Message("Tried to get fertility curve for pawn with no gender");
                return null;
            }
        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void RemoveHARPatches()
        {
            Harmony harmony = new Harmony(id: "rimworld.divineDerivative.HARinterference");
            harmony.Unpatch(typeof(Pawn_RelationsTracker).GetMethod("CompatibilityWith"), typeof(HarmonyPatches).GetMethod("CompatibilityWithPostfix"));
        }
    }
}