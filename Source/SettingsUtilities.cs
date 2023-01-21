using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

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
            CasualSexSettings settings = GetCasualSexSettings(pawn);
            return settings == null || settings.caresAboutCheating;
        }

        public static bool HookupAllowed(this Pawn pawn)
        {
            CasualSexSettings settings = GetCasualSexSettings(pawn);
            return settings == null || settings.willDoHookup;
        }

        public static float HookupRate(this Pawn pawn)
        {
            if (BetterRomanceMod.settings.hookupRate == 0)
            {
                return 0f;
            }
            CasualSexSettings settings = GetCasualSexSettings(pawn);
            return (settings != null) ? settings.hookupRate : BetterRomanceMod.settings.hookupRate;
        }

        public static float AlienLoveChance(this Pawn pawn)
        {
            if (BetterRomanceMod.settings.alienLoveChance == 0)
            {
                return 0f;
            }
            CasualSexSettings settings = GetCasualSexSettings(pawn);
            return (settings != null) ? settings.alienLoveChance : BetterRomanceMod.settings.alienLoveChance;
        }

        /// <summary>
        /// Get settings related to casual hookups
        /// </summary>
        /// <param name="pawn"></param>
        /// <returns></returns>
        public static HookupTrigger GetHookupSettings(Pawn pawn, bool ordered = false)
        {
            if (pawn.kindDef.HasModExtension<CasualSexSettings>())
            {
                return ordered ? pawn.kindDef.GetModExtension<CasualSexSettings>().orderedHookupTriggers : pawn.kindDef.GetModExtension<CasualSexSettings>().hookupTriggers;
            }
            else if (pawn.def.HasModExtension<CasualSexSettings>())
            {
                return ordered ? pawn.def.GetModExtension<CasualSexSettings>().orderedHookupTriggers : pawn.def.GetModExtension<CasualSexSettings>().hookupTriggers;
            }
            return null;
        }

        public static float MinOpinionForHookup(this Pawn pawn, bool ordered = false)
        {
            HookupTrigger triggers = GetHookupSettings(pawn, ordered);
            return (triggers != null) ? triggers.minOpinion : BetterRomanceMod.settings.minOpinionHookup;
        }

        public static bool MustBeFertileForHookup(this Pawn pawn, bool ordered)
        {
            HookupTrigger triggers = GetHookupSettings(pawn, ordered);
            //If triggers are not provided, default is false
            return triggers != null && triggers.mustBeFertile;
        }

        public static bool MeetsHookupFertilityRequirement(this Pawn pawn, bool ordered = false)
        {
            if (pawn.MustBeFertileForHookup(ordered))
            {
                if (ModsConfig.BiotechActive)
                {
                    return pawn.GetStatValue(StatDefOf.Fertility) > 0.05f;
                }
                else if (ModsConfig.IsActive("dylan.csl"))
                {
                    return pawn.health.capacities.GetLevel(RomanceDefOf.Fertility) > 0.05f;
                }
                else if (ModsConfig.IsActive("rim.job.world"))
                {
                    return pawn.health.capacities.GetLevel(RomanceDefOf.RJW_Fertility) > 0.05f;
                }
                Log.Message("");
            }
            return true;
        }

        public static bool MeetsHookupTraitRequirment(this Pawn pawn, out TraitDef trait, bool ordered = false)
        {
            HookupTrigger triggers = GetHookupSettings(pawn, ordered);
            if (triggers?.hasTrait != null)
            {
                trait = triggers.hasTrait;
                return pawn.story.traits.HasTrait(trait);
            }
            //Default is no trait requirement
            trait = null;
            return true;
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
            RegularSexSettings settings = GetSexSettings(pawn);
            return (settings != null) ? settings.minAgeForSex : 16f;
        }

        public static float MaxAgeForSex(this Pawn pawn)
        {
            RegularSexSettings settings = GetSexSettings(pawn);
            return (settings != null) ? settings.maxAgeForSex : 80f;
        }

        public static float MaxAgeGap(this Pawn pawn)
        {
            RegularSexSettings settings = GetSexSettings(pawn);
            return (settings != null) ? settings.maxAgeGap : 40f;
        }

        public static float DeclineAtAge(this Pawn pawn)
        {
            RegularSexSettings settings = GetSexSettings(pawn);
            return (settings != null) ? settings.declineAtAge : 30f;
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
            RelationSettings settings = GetRelationSettings(pawn);
            return settings == null || settings.spousesAllowed;
        }

        public static bool ChildAllowed(this Pawn pawn)
        {
            RelationSettings settings = GetRelationSettings(pawn);
            return settings == null || settings.childrenAllowed;
        }
        /// <summary>
        /// Provides an appropriate pawnkind for a newly generated parent. If children are not allowed, uses a pawnkind from settings, otherwise uses the same pawnkind as the child.
        /// </summary>
        /// <param name="pawn"></param>
        /// <param name="gender"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="Exception"></exception>
        public static PawnKindDef ParentPawnkind(this Pawn pawn, Gender gender)
        {
            if (pawn.ChildAllowed())
            {
                return pawn.kindDef;
            }
            RelationSettings settings = GetRelationSettings(pawn);
            if (settings.pawnKindForParentGlobal != null)
            {
                return settings.pawnKindForParentGlobal;
            }
            if (settings.pawnKindForParentFemale != null && settings.pawnKindForParentMale != null)
            {
                if (gender == Gender.Female)
                {
                    return settings.pawnKindForParentFemale;
                }
                else if (gender == Gender.Male)
                {
                    return settings.pawnKindForParentMale;
                }
                else
                {
                    throw new ArgumentException("Invalid gender provided for finding parent pawnkind");
                }
            }
            throw new Exception("No parent pawnkind set for " + pawn.kindDef.defName + " or " + pawn.def.defName);
        }

        public static float MinAgeToHaveChildren(this Pawn pawn)
        {
            RelationSettings settings = GetRelationSettings(pawn);
            if (pawn.gender == Gender.Female)
            {
                return (settings != null) ? settings.minFemaleAgeToHaveChildren : 16f;
            }
            else if (pawn.gender == Gender.Male)
            {
                return (settings != null) ? settings.minMaleAgeToHaveChildren : 14f;
            }
            throw new ArgumentException("This pawn has no gender");
        }
        //Same as above but takes a gender argument, for use when getting age settings for pawns that haven't been generated yet
        public static float MinAgeToHaveChildren(this Pawn pawn, Gender gender)
        {
            RelationSettings settings = GetRelationSettings(pawn);
            if (gender == Gender.Female)
            {
                return (settings != null) ? settings.minFemaleAgeToHaveChildren : 16f;
            }
            else if (gender == Gender.Male)
            {
                return (settings != null) ? settings.minMaleAgeToHaveChildren : 14f;
            }
            throw new ArgumentException("No gender provided");
        }

        public static float MaxAgeToHaveChildren(this Pawn pawn)
        {
            RelationSettings settings = GetRelationSettings(pawn);
            if (pawn.gender == Gender.Female)
            {
                return (settings != null) ? settings.maxFemaleAgeToHaveChildren : 45f;
            }
            else if (pawn.gender == Gender.Male)
            {
                return (settings != null) ? settings.maxMaleAgeToHaveChildren : 50f;
            }
            throw new ArgumentException("This pawn has no gender");
        }
        //Same as above but takes a gender argument, for use when getting age settings for pawns that haven't been generated yet
        public static float MaxAgeToHaveChildren(this Pawn pawn, Gender gender)
        {
            RelationSettings settings = GetRelationSettings(pawn);
            if (gender == Gender.Female)
            {
                return (settings != null) ? settings.maxFemaleAgeToHaveChildren : 45f;
            }
            else if (gender == Gender.Male)
            {
                return (settings != null) ? settings.maxMaleAgeToHaveChildren : 50f;
            }
            throw new ArgumentException("This pawn has no gender");
        }

        public static float UsualAgeToHaveChildren(this Pawn pawn)
        {
            RelationSettings settings = GetRelationSettings(pawn);
            if (pawn.gender == Gender.Female)
            {
                return (settings != null) ? settings.usualFemaleAgeToHaveChildren : 27f;
            }
            else if (pawn.gender == Gender.Male)
            {
                return (settings != null) ? settings.usualMaleAgeToHaveChildren : 30f;
            }
            else
            {
                Log.Message("Pawn has no gender, defaulting to female setting");
                return (settings != null) ? settings.usualFemaleAgeToHaveChildren : 27f;
            }
        }
        //Same as above but takes a gender argument, for use when getting age settings for pawns that haven't been generated yet
        public static float UsualAgeToHaveChildren(this Pawn pawn, Gender gender)
        {
            RelationSettings settings = GetRelationSettings(pawn);
            if (gender == Gender.Female)
            {
                return (settings != null) ? settings.usualFemaleAgeToHaveChildren : 27f;
            }
            else if (gender == Gender.Male)
            {
                return (settings != null) ? settings.usualMaleAgeToHaveChildren : 30f;
            }
            throw new ArgumentException("No gender provided");
        }

        public static int MaxChildren(this Pawn pawn)
        {
            RelationSettings settings = GetRelationSettings(pawn);
            return (settings != null) ? settings.maxChildrenDesired : 3;
        }

        public static float MinOpinionForRomance(this Pawn pawn)
        {
            RelationSettings settings = GetRelationSettings(pawn);
            return (settings != null) ? settings.minOpinionRomance : BetterRomanceMod.settings.minOpinionRomance;
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
                    LoveRelations extension = def.GetModExtension<LoveRelations>();
                    if (extension.isLoveRelation)
                    {
                        if (extension.exLoveRelation != null)
                        {
                            result.Add(extension.exLoveRelation);
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
            BiotechSettings settings = GetBiotechSettings(pawn);
            if (pawn.gender == Gender.Male)
            {
                return settings != null ? settings.maleFertilityAgeFactor : GetDefaultFertilityAgeCurve(pawn.gender);
            }
            else if (pawn.gender == Gender.Female)
            {
                return settings != null ? settings.femaleFertilityAgeFactor : GetDefaultFertilityAgeCurve(pawn.gender);
            }
            else
            {
                return settings != null ? settings.noneFertilityAgeFactor : GetDefaultFertilityAgeCurve(pawn.gender);
            }
        }

        private static SimpleCurve GetDefaultFertilityAgeCurve(Gender gender)
        {
            if (gender == Gender.Female)
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
                return new SimpleCurve
                {
                    new CurvePoint(14f, 0f),
                    new CurvePoint(18f, 1f),
                    new CurvePoint(50f, 1f),
                    new CurvePoint(90f, 0f),
                };
            }
        }

        public static int GetGrowthMoment(Pawn pawn, int index)
        {
            if (Settings.HARActive)
            {
                int[] array = HAR_Integration.GetGrowthMoments(pawn);
                if (array != null)
                {
                    return array[index];
                }
            }
            else if (index == 2)
            {
                return (int)pawn.ageTracker.AdultMinAge;
            }
            return GrowthUtility.GrowthMomentAges[index];
        }
    }
}