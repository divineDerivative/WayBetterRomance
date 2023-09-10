using RimWorld;
using System;
using Verse;

namespace BetterRomance
{
    public class RaceSettings
    {
        public ThingDef race;

        public class OrientationChanceRace
        {
            public float? asexualChance;
            public float? bisexualChance;
            public float? gayChance;
            public float? straightChance;

            public float? aceAroChance;
            public float? aceBiChance;
            public float? aceHomoChance;
            public float? aceHeteroChance;
        }
        public OrientationChanceRace orientation;

        public class CasualSexRace
        {
            public bool? caresAboutCheating;
            public bool? willDoHookup;
            public bool? canDoOrderedHookup;
            public float? hookupRate;
            public float? alienLoveChance;
            public int? minOpinionForHookup;
            public int? minOpinionForOrderedHookup;
            public bool? forBreedingOnly;
            //Maybe put the hookup traits here
        }
        public CasualSexRace casualSex;

        public class RegularSexRace
        {
            public float? minAgeForSex;
            public float? maxAgeForSex;
            public float? maxAgeGap;
            public float? declineAtAge;
        }
        public RegularSexRace regularSex;

        public class RelationsRace
        {
            public bool? spousesAllowed;
            public bool? childrenAllowed;

            public PawnKindDef pawnKindForParentGlobal;
            public PawnKindDef pawnKindForParentFemale;
            public PawnKindDef pawnKindForParentMale;

            public float? minFemaleAgeToHaveChildren;
            public float? usualFemaleAgeToHaveChildren;
            public float? maxFemaleAgeToHaveChildren;

            public float? minMaleAgeToHaveChildren;
            public float? usualMaleAgeToHaveChildren;
            public float? maxMaleAgeToHaveChildren;

            public int? maxChildrenDesired;
            public int? minOpinionRomance;
        }
        public RelationsRace relations;

        public class BiotechRace
        {
            public SimpleCurve maleFertilityAgeFactor;
            public SimpleCurve femaleFertilityAgeFactor;
            public SimpleCurve noneFertilityAgeFactor;
            public SimpleCurve ageEffectOnChildbirth;
            public int[] growthMoments;
        }
        public BiotechRace biotech;

        //Misc Settings
        public float? minAgeForAdulthood;
        public int? childAge;
        public int? adultAgeForLearning;
        public int? ageReversalDemandAge;
        public SimpleCurve ageSkillFactor;
        public SimpleCurve ageSkillMaxFactorCurve;

        public RaceSettings(ThingDef race)
        {
            this.race = race;
            orientation = new OrientationChanceRace();
            casualSex = new CasualSexRace();
            regularSex = new RegularSexRace();
            relations = new RelationsRace();
            biotech = new BiotechRace();
        }

        public void SetOrientationChances()
        {
            if (race.HasModExtension<SexualityChances>())
            {
                SexualityChances chances = race.GetModExtension<SexualityChances>();

                orientation.asexualChance = chances.asexualChance;
                orientation.bisexualChance = chances.bisexualChance;
                orientation.gayChance = chances.gayChance;
                orientation.straightChance = chances.straightChance;

                orientation.aceAroChance = chances.aceAroChance;
                orientation.aceBiChance = chances.aceBiChance;
                orientation.aceHomoChance = chances.aceHomoChance;
                orientation.aceHeteroChance = chances.aceHeteroChance;
            }
        }

        public void SetCasualSexSettings()
        {
            if (race.HasModExtension<CasualSexSettings>())
            {
                CasualSexSettings settings = race.GetModExtension<CasualSexSettings>();
                casualSex.caresAboutCheating = settings.caresAboutCheating;
                casualSex.willDoHookup = settings.willDoHookup;
                casualSex.canDoOrderedHookup = settings.canDoOrderedHookup;
                casualSex.hookupRate = settings.hookupRate;
                casualSex.alienLoveChance = settings.alienLoveChance;
                if (settings.hookupTriggers != null)
                {
                    casualSex.minOpinionForHookup = settings.hookupTriggers.minOpinion;
                }
                if (settings.orderedHookupTriggers != null)
                {
                    casualSex.minOpinionForOrderedHookup = settings.orderedHookupTriggers.minOpinion;
                    casualSex.forBreedingOnly = settings.orderedHookupTriggers.forBreedingOnly;
                }
            }
        }

        public void SetRegularSexSettings()
        {
            if (race.HasModExtension<RegularSexSettings>())
            {
                RegularSexSettings settings = race.GetModExtension<RegularSexSettings>();
                regularSex.minAgeForSex = settings.minAgeForSex;
                regularSex.maxAgeForSex = settings.maxAgeForSex;
                regularSex.maxAgeGap = settings.maxAgeGap;
                regularSex.declineAtAge = settings.declineAtAge;
            }
        }

        public void SetRelationSettings()
        {
            if (race.HasModExtension<RelationSettings>())
            {
                RelationSettings settings = race.GetModExtension<RelationSettings>();
                relations.spousesAllowed = settings.spousesAllowed;
                relations.childrenAllowed = settings.childrenAllowed;

                relations.pawnKindForParentGlobal = settings.pawnKindForParentGlobal;
                relations.pawnKindForParentFemale = settings.pawnKindForParentFemale;
                relations.pawnKindForParentMale = settings.pawnKindForParentMale;

                relations.minFemaleAgeToHaveChildren = settings.minFemaleAgeToHaveChildren;
                relations.usualFemaleAgeToHaveChildren = settings.usualFemaleAgeToHaveChildren;
                relations.maxFemaleAgeToHaveChildren = settings.maxFemaleAgeToHaveChildren;

                relations.minMaleAgeToHaveChildren = settings.minMaleAgeToHaveChildren;
                relations.usualMaleAgeToHaveChildren = settings.usualMaleAgeToHaveChildren;
                relations.maxMaleAgeToHaveChildren = settings.maxMaleAgeToHaveChildren;

                relations.maxChildrenDesired = settings.maxChildrenDesired;
                relations.minOpinionRomance = settings.minOpinionRomance;
            }
        }

        public void SetBiotechSettings()
        {
            if (race.HasModExtension<BiotechSettings>())
            {
                BiotechSettings settings = race.GetModExtension<BiotechSettings>();

                biotech.maleFertilityAgeFactor = settings.maleFertilityAgeFactor;
                biotech.femaleFertilityAgeFactor = settings.femaleFertilityAgeFactor;
                biotech.noneFertilityAgeFactor = settings.noneFertilityAgeFactor;
                biotech.ageEffectOnChildbirth = settings.ageEffectOnChildbirth;
            }
            if (Settings.HARActive)
            {
                biotech.growthMoments = HAR_Integration.GetGrowthMoments(race);
            }
            else
            {
                biotech.growthMoments = GrowthUtility.GrowthMomentAges;
            }
            //Do some stuff here to make sure the ages are reasonable
            if (biotech.growthMoments != null)
            {
                //We cast to int in case people have used decimals in lifestages, because growth moments should be int
                int adultAge = (int)Math.Ceiling(race.race.lifeStageAges.FirstOrDefault((LifeStageAge lifeStageAge) => lifeStageAge.def.developmentalStage.Adult())?.minAge ?? 0f);
                int childAge = (int)Math.Ceiling(race.race.lifeStageAges.FirstOrDefault((LifeStageAge lifeStageAge) => lifeStageAge.def.developmentalStage.Child())?.minAge ?? 0f);
                if (biotech.growthMoments[2] != adultAge || childAge > biotech.growthMoments[0])
                {
                    //Something has gone wrong and growth moments need to be calculated
                    Log.Warning("Growth moment ages for " + race.defName + " do not make sense. Child at " + childAge + ", adult at " + adultAge + ", growth ages are " + biotech.growthMoments[0] + ", " + biotech.growthMoments[1] + ", " + biotech.growthMoments[2]);
                    biotech.growthMoments = GrowthMomentArray(childAge, adultAge);
                    Log.Warning("New ages are " + biotech.growthMoments[0] + ", " + biotech.growthMoments[1] + ", " + biotech.growthMoments[2]);
                }
            }
            //We don't assign a default because some races intentionally have null growthAges
        }

        private int[] GrowthMomentArray(int childAge, int adultAge)
        {
            int difference = adultAge - childAge;
            int interval = difference / 3;
            if (difference % 3 == 0)
            {
                return new int[] {
                            childAge + interval,
                            childAge + (interval * 2),
                            adultAge,
                        };
            }
            else if (difference % 3 == 1)
            {
                return new int[] {
                            childAge + interval + 1,
                            childAge + (interval * 2) + 1,
                            adultAge,
                        };
            }
            else if (difference % 3 == 2)
            {
                return new int[] {
                            childAge + interval + 1,
                            childAge + (interval * 2) + 2,
                            adultAge,
                        };
            }
            else
            {
                throw new Exception("Somehow " + difference + " % 3 does not equal 0, 1, or 2");
            }
        }
    }
}
