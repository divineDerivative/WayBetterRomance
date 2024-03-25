using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
//using static BetterRomance.WBR_SettingsComp;

namespace BetterRomance
{
    public static class SettingsUtilities
    {
        //Grabbing stuff from settings comp
        //Orientation chances do not have methods, since they're only needed in the AssignOrientation function

        //Casual sex settings
        public static CompSettingsCasualSexPawn GetCasualSexSettings(Pawn pawn)
        {
            WBR_SettingsComp comp = pawn.TryGetComp<WBR_SettingsComp>();
            return comp.casualSex;
        }

        public static bool CaresAboutCheating(this Pawn pawn)
        {
            if (pawn.ideo.Ideo.PreceptsListForReading.Any((Precept x) => x.def == RomanceDefOf.Lovin_FreeApproved))
            {
                return false;
            }
            CompSettingsCasualSexPawn settings = GetCasualSexSettings(pawn);
            return settings.caresAboutCheating;
        }

        public static bool HookupAllowed(this Pawn pawn, bool ordered = false)
        {
            CompSettingsCasualSexPawn settings = GetCasualSexSettings(pawn);
            return ordered ? settings.canDoOrderedHookup : settings.willDoHookup;
        }

        public static float HookupRate(this Pawn pawn)
        {
            if (BetterRomanceMod.settings.hookupRate == 0f)
            {
                return 0f;
            }
            CompSettingsCasualSexPawn settings = GetCasualSexSettings(pawn);
            return settings.hookupRate ?? BetterRomanceMod.settings.hookupRate;
        }

        public static float AlienLoveChance(this Pawn pawn)
        {
            if (BetterRomanceMod.settings.alienLoveChance == 0f)
            {
                return 0f;
            }
            CompSettingsCasualSexPawn settings = GetCasualSexSettings(pawn);
            return settings.alienLoveChance ?? BetterRomanceMod.settings.alienLoveChance;
        }

        public static int MinOpinionForHookup(this Pawn pawn, bool ordered = false)
        {
            CompSettingsCasualSexPawn settings = GetCasualSexSettings(pawn);
            return (ordered ? settings.minOpinionForOrderedHookup : settings.minOpinionForHookup) ?? BetterRomanceMod.settings.minOpinionHookup;
        }

        /// <summary>
        /// Checks if ordered hookups are only allowed for breeding purposes.
        /// </summary>
        /// <param name="pawn"></param>
        /// <param name="ordered"></param>
        /// <returns></returns>
        public static bool HookupForBreedingOnly(this Pawn pawn)
        {
            CompSettingsCasualSexPawn settings = GetCasualSexSettings(pawn);
            return settings.forBreedingOnly;
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
                if (Settings.fertilityMod != "None")
                {
                    string mod = Settings.fertilityMod;
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
            if (Settings.fertilityMod != "None")
            {
                string mod = Settings.fertilityMod;
                switch (mod)
                {
                    case "ludeon.rimworld.biotech":
                        return pawn.GetStatValue(StatDefOf.Fertility);
                    case "dylan.csl":
                        return pawn.health.capacities.GetLevel(RomanceDefOf.Fertility);
                    case "rim.job.world":
                    case "safe.job.world":
                        return pawn.health.capacities.GetLevel(RomanceDefOf.RJW_Fertility);
                }
                LogUtil.ErrorOnce("Unexpected value of fertilityMod: " + mod, 1798621);
                return 100f;
            }
            LogUtil.WarningOnce("If you are using a mod that adds fertility/pregnancy, please set it in the mod options for Way Better Romance. Otherwise, ignore this message.", 6894123);
            return 100f;
        }

        public static bool IsFertile(this Pawn pawn) => pawn.GetFertilityLevel() > 0f;

        public static Dictionary<ThingDef, HashSet<TraitRequirement>> RaceHookupTraits = new();
        public static Dictionary<PawnKindDef, HashSet<TraitRequirement>> PawnkindHookupTraits = new();

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
        public static CompSettingsRegularSex GetSexSettings(Pawn pawn)
        {
            WBR_SettingsComp comp = pawn.TryGetComp<WBR_SettingsComp>();
            return comp?.regularSex;
        }

        public static float MinAgeForSex(this Pawn pawn)
        {
            CompSettingsRegularSex settings = GetSexSettings(pawn);
            //This gets called by GenerateInitialHediffs for all pawns, so needs to be null checked
            return settings?.minAgeForSex ?? 16f;
        }

        public static float MaxAgeForSex(this Pawn pawn)
        {
            if (pawn.IsNonSenescent())
            {
                return pawn.ageTracker.AgeBiologicalYearsFloat + 2f;
            }
            CompSettingsRegularSex settings = GetSexSettings(pawn);
            return settings.maxAgeForSex;
        }

        public static float MaxAgeGap(this Pawn pawn)
        {
            CompSettingsRegularSex settings = GetSexSettings(pawn);
            return settings.maxAgeGap;
        }

        public static float DeclineAtAge(this Pawn pawn)
        {
            if (pawn.IsNonSenescent())
            {
                return pawn.ageTracker.AgeBiologicalYearsFloat + 1f;
            }
            CompSettingsRegularSex settings = GetSexSettings(pawn);
            return settings.declineAtAge;
        }

        public static List<Pawn> CachedNonSenescentPawns = new();
        public static List<Pawn> CachedSenescentPawns = new();

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
        public static CompSettingsRelationsPawn GetRelationSettings(Pawn pawn)
        {
            WBR_SettingsComp comp = pawn.TryGetComp<WBR_SettingsComp>();
            return comp.relations;
        }

        //Add check for the no spouses precept here
        public static bool SpouseAllowed(this Pawn pawn)
        {
            CompSettingsRelationsPawn settings = GetRelationSettings(pawn);
            return settings.spousesAllowed;
        }

        public static bool ChildAllowed(this Pawn pawn)
        {
            CompSettingsRelationsPawn settings = GetRelationSettings(pawn);
            return settings.childrenAllowed;
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
            CompSettingsRelationsPawn settings = GetRelationSettings(pawn);
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

        public static float MinAgeToHaveChildren(this Pawn pawn, Gender gender = Gender.None)
        {
            if (gender == Gender.None)
            {
                gender = pawn.gender;
            }
            CompSettingsRelationsPawn settings = GetRelationSettings(pawn);
            if (gender == Gender.Female)
            {
                return settings.minFemaleAgeToHaveChildren;
            }
            else if (gender == Gender.Male)
            {
                return settings.minMaleAgeToHaveChildren;
            }
            throw new ArgumentException("No gender provided");
        }

        public static float MaxAgeToHaveChildren(this Pawn pawn, Gender gender = Gender.None)
        {
            if (gender == Gender.None)
            {
                gender = pawn.gender;
            }
            CompSettingsRelationsPawn settings = GetRelationSettings(pawn);
            if (gender == Gender.Female)
            {
                return settings.maxFemaleAgeToHaveChildren;
            }
            else if (gender == Gender.Male)
            {
                return settings.maxMaleAgeToHaveChildren;
            }
            throw new ArgumentException("This pawn has no gender");
        }

        public static float UsualAgeToHaveChildren(this Pawn pawn, Gender gender = Gender.None)
        {
            if (gender == Gender.None)
            {
                gender = pawn.gender;
            }
            CompSettingsRelationsPawn settings = GetRelationSettings(pawn);
            if (gender == Gender.Female)
            {
                return settings.usualFemaleAgeToHaveChildren;
            }
            else if (gender == Gender.Male)
            {
                return settings.usualMaleAgeToHaveChildren;
            }
            throw new ArgumentException("No gender provided");
        }

        public static int MaxChildren(this Pawn pawn)
        {
            CompSettingsRelationsPawn settings = GetRelationSettings(pawn);
            return settings.maxChildrenDesired;
        }

        public static int MinOpinionForRomance(this Pawn pawn)
        {
            CompSettingsRelationsPawn settings = GetRelationSettings(pawn);
            return settings.minOpinionRomance ?? BetterRomanceMod.settings.minOpinionRomance;
        }

        //Biotech Settings
        public static CompSettingsBiotech GetBiotechSettings(Pawn pawn)
        {
            WBR_SettingsComp comp = pawn.TryGetComp<WBR_SettingsComp>();
            return comp?.biotech;
        }

        //This will preserve any xml patches that might be made to the default curves
        internal static void GrabBiotechStuff()
        {
            if (StatDefOf.Fertility.parts != null && StatDefOf.Fertility.parts.OfType<StatPart_FertilityByGenderAge>().Any())
            {
                femaleFertilityAgeFactor = (SimpleCurve)AccessTools.Field(typeof(StatPart_FertilityByGenderAge), "femaleFertilityAgeFactor").GetValue(StatDefOf.Fertility.GetStatPart<StatPart_FertilityByGenderAge>());
                maleFertilityAgeFactor = (SimpleCurve)AccessTools.Field(typeof(StatPart_FertilityByGenderAge), "maleFertilityAgeFactor").GetValue(StatDefOf.Fertility.GetStatPart<StatPart_FertilityByGenderAge>());
            }
            childBirthByAgeCurve = RitualOutcomeEffectDefOf.ChildBirth.comps.Find(c => c is RitualOutcomeComp_PawnAge) as RitualOutcomeComp_PawnAge;
        }

        public static SimpleCurve femaleFertilityAgeFactor;
        public static SimpleCurve maleFertilityAgeFactor;
        public static RitualOutcomeComp_PawnAge childBirthByAgeCurve;

        public static SimpleCurve GetFertilityAgeCurve(this Pawn pawn)
        {
            CompSettingsBiotech settings = GetBiotechSettings(pawn);
            //This gets called on animals and mechs even though the stat never gets used
            //So need to pass something
            if (settings == null)
            {
                return GetDefaultFertilityAgeCurve(pawn.gender);
            }
            if (pawn.gender == Gender.Male)
            {
                return settings.maleFertilityAgeFactor;
            }
            else if (pawn.gender == Gender.Female)
            {
                return settings.femaleFertilityAgeFactor;
            }
            else
            {
                return settings.noneFertilityAgeFactor;
            }
        }

        public static SimpleCurve GetDefaultFertilityAgeCurve(Gender gender)
        {
            //This will preserve any xml patches that might be made to the default curves
            if (gender == Gender.Female)
            {
                return femaleFertilityAgeFactor ?? new SimpleCurve
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
                return maleFertilityAgeFactor ?? new SimpleCurve
                {
                    new CurvePoint(14f, 0f),
                    new CurvePoint(18f, 1f),
                    new CurvePoint(50f, 1f),
                    new CurvePoint(90f, 0f),
                };
            }
        }

        public static SimpleCurve GetChildBirthAgeCurve(this Pawn pawn)
        {
            CompSettingsBiotech settings = GetBiotechSettings(pawn);
            return settings.ageEffectOnChildbirth;
        }

        public static SimpleCurve GetDefaultChildbirthAgeCurve()
        {
            //This will preserve any xml patches that might be made to the default curve
            return childBirthByAgeCurve?.curve ?? new SimpleCurve
            {
                new CurvePoint(14f, 0.0f),
                new CurvePoint(15f, 0.3f),
                new CurvePoint(20f, 0.5f),
                new CurvePoint(30f, 0.5f),
                new CurvePoint(40f, 0.3f),
                new CurvePoint(65f, 0.0f),
            };
        }

        public static int GetGrowthMoment(Pawn pawn, int index)
        {
            if (pawn.HasNoGrowth())
            {
                return 0;
            }
            int[] ages = GetBiotechSettings(pawn).growthMoments;
            return ages[index];
        }

        public static float GetGrowthMomentAsFloat(Pawn pawn, int index)
        {
            if (pawn.HasNoGrowth())
            {
                return -1f;
            }
            return GetGrowthMoment(pawn, index);
        }

        //Miscellaneous age calculations
        public static CompSettingsMisc GetMiscSettings(Pawn pawn)
        {
            WBR_SettingsComp comp = pawn.TryGetComp<WBR_SettingsComp>();
            return comp?.misc;
        }

        /// <summary>
        /// Age at which a pawn is given an adult backstory. Human default is 20
        /// </summary>
        /// <param name="pawn"></param>
        /// <returns></returns>
        public static float GetMinAgeForAdulthood(Pawn pawn)
        {
            CompSettingsMisc settings = GetMiscSettings(pawn);
            //This can get called on animals/mechs
            if (settings == null)
            {
                return defaultMinAgeForAdulthood;
            }
            return settings.minAgeForAdulthood;
        }

        public static readonly float defaultMinAgeForAdulthood = (float)AccessTools.Field(typeof(PawnBioAndNameGenerator), "MinAgeForAdulthood").GetValue(null);

        /// <summary>
        /// Finds the first life stage with a developmental stage of child and returns the minimum age of that stage. Human default is 3
        /// </summary>
        /// <param name="pawn"></param>
        /// <returns>The age at which <paramref name="pawn"/> becomes a child.</returns>
        public static int ChildAge(Pawn pawn)
        {
            CompSettingsMisc settings = GetMiscSettings(pawn);
            return settings.childAge;
        }

        /// <summary>
        /// The age at which a pawn is considered an adult for the purposes of adjusting learning speed. Human default is 18
        /// </summary>
        /// <param name="pawn"></param>
        /// <returns></returns>
        public static int AdultAgeForLearning(Pawn pawn)
        {
            CompSettingsMisc settings = GetMiscSettings(pawn);
            return settings.adultAgeForLearning;
        }

        /// <summary>
        /// The age at which a pawn starts wanting an age reversal. Human default is 25
        /// </summary>
        /// <param name="pawn"></param>
        /// <returns></returns>
        public static int AgeReversalDemandAge(Pawn pawn)
        {
            CompSettingsMisc settings = GetMiscSettings(pawn);
            //This is for animals, which won't have the comp, since this gets called during generation for ALL pawns
            if (settings == null)
            {
                return 25;
            }
            return settings.ageReversalDemandAge;
        }

        public static SimpleCurve AgeSkillFactor(Pawn pawn)
        {
            CompSettingsMisc settings = GetMiscSettings(pawn);
            return settings.ageSkillFactor;
        }

        public static SimpleCurve AgeSkillMaxFactorCurve(Pawn pawn)
        {
            CompSettingsMisc settings = GetMiscSettings(pawn);
            return settings.ageSkillMaxFactorCurve;
        }

        public static SimpleCurve GetLovinCurve(this Pawn pawn)
        {
            if (pawn.IsNonSenescent())
            {
                float minAge = pawn.MinAgeForSex();
                float maxAge = pawn.ageTracker.AgeBiologicalYearsFloat + 2f;
                float declineAge = pawn.ageTracker.AgeBiologicalYearsFloat + 1f;
                List<CurvePoint> points = new()
                {
                    new CurvePoint(minAge, 1.5f),
                    new CurvePoint((declineAge / 5) + minAge, 1.5f),
                    new CurvePoint(declineAge, 4f),
                    new CurvePoint((maxAge / 4) + declineAge, 12f),
                    new CurvePoint(maxAge, 36f)
                };
                return new SimpleCurve(points);
            }
            CompSettingsMisc settings = GetMiscSettings(pawn);
            return settings.lovinCurve;
        }

        /// <summary>
        /// Converts a static age into the calculated equivalent
        /// </summary>
        /// <param name="age"></param>
        /// <param name="pawn"></param>
        /// <returns></returns>
        public static float ConvertAge(float age, Pawn pawn)
        {
            switch (age)
            {
                case 0f:
                    return 0f;
                case 3f:
                    return ChildAge(pawn);
                case 4f:
                    return ChildAge(pawn) + 1;
                case 7f:
                    float num = GetGrowthMomentAsFloat(pawn, 0);
                    return num == -1 ? 0 : num;
                case 10f:
                    return GetGrowthMomentAsFloat(pawn, 1);
                case 12f:
                    return pawn.ageTracker.AdultMinAge - 1;
                case 13f:
                    return pawn.ageTracker.AdultMinAge;
                case 14f:
                    return pawn.ageTracker.AdultMinAge + 1;
                case 16f:
                    return pawn.MinAgeForSex();
                case 18f:
                    return AdultAgeForLearning(pawn);
                case 20f:
                    return GetMinAgeForAdulthood(pawn);
                case 25f:
                    return AgeReversalDemandAge(pawn);
                default:
                    LogUtil.Warning($"Unable to translate {age} to appropriate age for race {pawn.def.defName}");
                    return age;
            }
        }
        public static bool IsUnset(this float value) => value == -999f;
        public static bool IsUnset(this int value) => value == -999;

        public static bool IsEquivalentTo(this SimpleCurve first, SimpleCurve second)
        {
            if (first.PointsCount != second.PointsCount)
            {
                return false;
            }
            for (int i = 0; i < first.PointsCount; i++)
            {
                if (first.Points[i].Loc != second.Points[i].Loc)
                {
                    return false;
                }
            }
            return true;
        }
    }
}