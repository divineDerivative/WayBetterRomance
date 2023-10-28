using RimWorld;
using System;
using Verse;

namespace BetterRomance
{
    public class RaceSettings
    {
        public ThingDef race;
        public CompSettingsOrientation orientation;
        public CompSettingsCasualSexRace casualSex;
        public CompSettingsRegularSex regularSex;
        public CompSettingsRelationsRace relations;
        public CompSettingsBiotech biotech;
        public CompSettingsMisc misc;

        public RaceSettings(ThingDef race)
        {
            this.race = race;
            orientation = new CompSettingsOrientation();
            SetOrientationChances();
            casualSex = new CompSettingsCasualSexRace();
            SetCasualSexSettings();
            regularSex = new CompSettingsRegularSex();
            SetRegularSexSettings();
            relations = new CompSettingsRelationsRace();
            SetRelationSettings();
            biotech = new CompSettingsBiotech();
            SetBiotechSettings();
            misc = new CompSettingsMisc();
            SetMiscSettings();
        }

        private void SetOrientationChances()
        {
            if (race.HasModExtension<SexualityChances>())
            {
                SexualityChances chances = race.GetModExtension<SexualityChances>();
                orientation.sexual = chances.sexual?.Copy;
                orientation.asexual = chances.asexual?.Copy;
            }
        }

        private void SetCasualSexSettings()
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

        private void SetRegularSexSettings()
        {
            if (race.HasModExtension<RegularSexSettings>())
            {
                regularSex = race.GetModExtension<RegularSexSettings>().CopyToRace();
            }
        }

        private void SetRelationSettings()
        {
            if (race.HasModExtension<RelationSettings>())
            {
                RelationSettings settings = race.GetModExtension<RelationSettings>();
                relations = settings.CopyToRace();
            }
        }

        private void SetBiotechSettings()
        {
            if (race.HasModExtension<BiotechSettings>())
            {
                BiotechSettings settings = race.GetModExtension<BiotechSettings>();
                biotech = settings.CopyToRace();
            }
            if (!race.RobotGrowthCheck())
            {
                biotech.growthMoments = Settings.HARActive ? HAR_Integration.GetGrowthMoments(race) : GrowthUtility.GrowthMomentAges;
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

        private void SetMiscSettings()
        {
            //There's no extension for these, they are all directly calculated
            //Min age for adulthood
            if (biotech.growthMoments == null)
            {
                misc.minAgeForAdulthood = 0f;
            }
            else if (Settings.HARActive)
            {
                if (HAR_Integration.UseHARAgeForAdulthood(race, out float age))
                {
                    misc.minAgeForAdulthood = age;
                }
            }
            //Put something here to calculate a reasonable age?
            //HAR does the below if min age is not set
            if (misc.minAgeForAdulthood == -1)
            {
                //Calculations to determine a reasonable age would go here, leave as default for now
                misc.minAgeForAdulthood = SettingsUtilities.defaultMinAgeForAdulthood;
            }

            //Child age
            misc.childAge = (int)(race.race.lifeStageAges.FirstOrDefault((LifeStageAge lifeStageAge) => lifeStageAge.def.developmentalStage.Child())?.minAge ?? 0);

            //Adult age for learning
            float lifeStageMinAge = race.race.lifeStageAges.FirstOrDefault((LifeStageAge lifeStageAge) => lifeStageAge.def.developmentalStage.Adult())?.minAge ?? 0f;
            float backstoryMinAge = misc.minAgeForAdulthood;
            misc.adultAgeForLearning = (int)(Math.Round((backstoryMinAge - lifeStageMinAge) * .75f) + lifeStageMinAge);

            //Age reversal demand age
            float adultAge = misc.minAgeForAdulthood;
            float declineAge = regularSex.declineAtAge == -999f ? 30f : regularSex.declineAtAge;
            float result = adultAge + 5f;
            if (declineAge - adultAge < 10f)
            {
                result = adultAge + ((declineAge - adultAge) / 2);
            }
            misc.ageReversalDemandAge = (int)result;

            //Effect of age on skill
            misc.ageSkillFactor = new SimpleCurve
            {
                new CurvePoint(misc.childAge, 0.2f),
                new CurvePoint(misc.adultAgeForLearning, 1f),
            };

            //A different effect of age on skill
            misc.ageSkillMaxFactorCurve = new SimpleCurve
            {
                new CurvePoint(0f,0f),
                new CurvePoint(biotech.growthMoments[1], 0.7f),
                new CurvePoint(misc.adultAgeForLearning * 2f, 1f),
                new CurvePoint(race.race.lifeExpectancy - (race.race.lifeExpectancy/4), 1.6f),
            };
        }
    }
}
