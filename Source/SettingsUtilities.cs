using HarmonyLib;
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

        public static bool HookupAllowed(this Pawn pawn, bool ordered = false)
        {
            CasualSexSettings settings = GetCasualSexSettings(pawn);
            return settings == null || (ordered ? settings.canDoOrderedHookup : settings.willDoHookup);
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

        public static int MinOpinionForHookup(this Pawn pawn, bool ordered = false)
        {
            HookupTrigger triggers = GetHookupSettings(pawn, ordered);
            return (triggers != null) ? triggers.minOpinion : BetterRomanceMod.settings.minOpinionHookup;
        }

        /// <summary>
        /// Checks if ordered hookups are only allowed for breeding purposes.
        /// </summary>
        /// <param name="pawn"></param>
        /// <param name="ordered"></param>
        /// <returns></returns>
        public static bool HookupForBreedingOnly(this Pawn pawn)
        {
            HookupTrigger triggers = GetHookupSettings(pawn, true);
            //If triggers are not provided, default is false
            return triggers != null && triggers.forBreedingOnly;
        }

        /// <summary>
        /// First checks if ordered hookups are only allowed for breeding purposes. If so, checks if pregnancy is possible between <paramref name="asker"/> and <paramref name="target"/>.
        /// </summary>
        /// <param name="asker"></param>
        /// <param name="target"></param>
        /// <param name="ordered"></param>
        /// <returns></returns>
        public static bool MeetsHookupBreedingRequirement(Pawn asker, Pawn target)
        {
            if (asker.HookupForBreedingOnly() || target.HookupForBreedingOnly())
            {
                if (BetterRomanceMod.settings.fertilityMod != "None")
                {
                    string mod = BetterRomanceMod.settings.fertilityMod;
                    if (ModsConfig.BiotechActive && mod == "ludeon.rimworld.biotech")
                    {
                        return PregnancyUtility.CanEverProduceChild(asker, target);
                    }
                    if (Settings.HARActive)
                    {
                        return HAR_Integration.CanEverProduceChild(asker, target);
                    }
                    return HookupUtility.CanEverProduceChild(asker, target);
                }
            }
            return true;
        }

        public static float GetFertilityLevel(this Pawn pawn)
        {
            if (BetterRomanceMod.settings.fertilityMod != "None")
            {
                string mod = BetterRomanceMod.settings.fertilityMod;
                switch (mod)
                {
                    case "ludeon.rimworld.biotech":
                        return pawn.GetStatValue(StatDefOf.Fertility);
                    case "dylan.csl":
                        return pawn.health.capacities.GetLevel(RomanceDefOf.Fertility);
                    case "rim.job.world":
                        return pawn.health.capacities.GetLevel(RomanceDefOf.RJW_Fertility);
                }
                Log.ErrorOnce("Unexpected value of fertilityMod: " + mod, 1798621);
                return 100f;
            }
            Log.Message("If you are using a mod that adds fertility/pregnancy, please set it in the mod options for Way Better Romance. Otherwise, ignore this message.");
            return 100f;
        }

        public static bool IsFertile(this Pawn pawn) => pawn.GetFertilityLevel() > 0f;

        public static Dictionary<ThingDef, HashSet<TraitRequirement>> RaceHookupTraits = new Dictionary<ThingDef, HashSet<TraitRequirement>>();
        public static Dictionary<PawnKindDef, HashSet<TraitRequirement>> PawnkindHookupTraits = new Dictionary<PawnKindDef, HashSet<TraitRequirement>>();

        public static void MakeTraitList()
        {
            List<TraitDef> traitList = DefDatabase<TraitDef>.AllDefsListForReading;
            foreach (TraitDef trait in traitList)
            {
                if (trait.HasModExtension<HookupTrait>())
                {
                    HookupTrait extension = trait.GetModExtension<HookupTrait>();
                    if (!extension.races.NullOrEmpty())
                    {
                        foreach (ThingDef race in extension.races)
                        {
                            if (!RaceHookupTraits.ContainsKey(race))
                            {
                                RaceHookupTraits.Add(race, new HashSet<TraitRequirement>());
                            }
                            if (extension.degrees.NullOrEmpty())
                            {
                                RaceHookupTraits[race].Add(new TraitRequirement()
                                {
                                    def = trait,
                                    degree = 0,
                                });
                            }
                            else
                            {
                                foreach (int i in extension.degrees)
                                {
                                    RaceHookupTraits[race].Add(new TraitRequirement()
                                    {
                                        def = trait,
                                        degree = i,
                                    });
                                }
                            }
                        }
                    }
                    if (!extension.pawnkinds.NullOrEmpty())
                    {
                        foreach (PawnKindDef pawnkind in extension.pawnkinds)
                        {
                            if (!PawnkindHookupTraits.ContainsKey(pawnkind))
                            {
                                PawnkindHookupTraits.Add(pawnkind, new HashSet<TraitRequirement>());
                            }
                            if (extension.degrees.NullOrEmpty())
                            {
                                PawnkindHookupTraits[pawnkind].Add(new TraitRequirement()
                                {
                                    def = trait,
                                    degree = 0,
                                });
                            }
                            else
                            {
                                foreach (int i in extension.degrees)
                                {
                                    PawnkindHookupTraits[pawnkind].Add(new TraitRequirement()
                                    {
                                        def = trait,
                                        degree = i,
                                    });
                                }
                            }
                        }
                    }
                }
            }
        }

        public static bool MeetsHookupTraitRequirment(this Pawn pawn, out List<string> list)
        {
            list = new List<string>();
            if (PawnkindHookupTraits.ContainsKey(pawn.kindDef))
            {
                foreach (TraitRequirement trait in PawnkindHookupTraits[pawn.kindDef])
                {
                    list.Add(trait.def.DataAtDegree(trait.degree ?? 0).label);
                    if (trait.HasTrait(pawn))
                    {
                        return true;
                    }
                }
                return false;
            }
            else if (RaceHookupTraits.ContainsKey(pawn.def))
            {
                foreach (TraitRequirement trait in RaceHookupTraits[pawn.def])
                {
                    list.Add(trait.def.DataAtDegree(trait.degree ?? 0).label);
                    if (trait.HasTrait(pawn))
                    {
                        return true;
                    }
                }
                return false;
            }
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
            if (pawn.IsNonSenescent())
            {
                return pawn.ageTracker.AgeBiologicalYearsFloat + 2f;
            }
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
            if (pawn.IsNonSenescent())
            {
                return pawn.ageTracker.AgeBiologicalYearsFloat + 1f;
            }
            RegularSexSettings settings = GetSexSettings(pawn);
            return (settings != null) ? settings.declineAtAge : 30f;
        }

        public static List<Pawn> CachedNonSenescentPawns = new List<Pawn>();
        public static List<Pawn> CachedSenescentPawns = new List<Pawn>();

        private static bool IsNonSenescent(this Pawn pawn)
        {
            if (ModsConfig.BiotechActive && pawn.genes != null)
            {
                if (!CachedNonSenescentPawns.Contains(pawn) && !CachedSenescentPawns.Contains(pawn))
                {

                    foreach (Gene gene in pawn.genes.GenesListForReading)
                    {
                        if (gene.def.defName == "DiseaseFree")
                        {
                            CachedNonSenescentPawns.Add(pawn);
                            break;
                        }
                    }
                    if (!CachedNonSenescentPawns.Contains(pawn))
                    {
                        CachedSenescentPawns.Add(pawn);
                    }
                }
                if (CachedNonSenescentPawns.Contains(pawn))
                {
                    return true;
                }
            }
            return false;
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

        public static int MinOpinionForRomance(this Pawn pawn)
        {
            RelationSettings settings = GetRelationSettings(pawn);
            return (settings != null) ? settings.minOpinionRomance : BetterRomanceMod.settings.minOpinionRomance;
        }

        //Love Relation Settings
        public static HashSet<PawnRelationDef> LoveRelations = new HashSet<PawnRelationDef>();
        public static HashSet<PawnRelationDef> ExLoveRelations = new HashSet<PawnRelationDef>();

        public static void MakeAdditionalLoveRelationsLists()
        {
            List<PawnRelationDef> relationList = DefDatabase<PawnRelationDef>.AllDefsListForReading;
            foreach (PawnRelationDef def in relationList)
            {
                if (def.HasModExtension<LoveRelations>())
                {
                    LoveRelations extension = def.GetModExtension<LoveRelations>();
                    if (extension.isLoveRelation)
                    {
                        LoveRelations.Add(def);
                        if (extension.exLoveRelation != null)
                        {
                            ExLoveRelations.Add(extension.exLoveRelation);
                        }
                    }
                }
            }
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
            if (pawn.MechanicalCheck())
            {
                return 0;
            }
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

        public static float GetGrowthMomentAsFloat(Pawn pawn, int index)
        {
            if (pawn.MechanicalCheck())
            {
                return -1f;
            }
            return GetGrowthMoment(pawn, index);
        }

        public static float GetMinAgeForAdulthood(Pawn pawn)
        {
            if (Settings.HARActive)
            {
                if (HAR_Integration.UseHARAgeForAdulthood(pawn, out float age))
                {
                    return age;
                }
            }
            return (float)AccessTools.Field(typeof(PawnBioAndNameGenerator), "MinAgeForAdulthood").GetValue(null);
        }
    }
}