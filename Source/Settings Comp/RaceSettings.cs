using DivineFramework;
using RimWorld;
using System;
using System.Linq;
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
            SetGrowthMoments();
            misc = new CompSettingsMisc();
            SetMiscSettings();
            SetBiotechSettings();
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
                casualSex = settings.CopyToRace();
            }
        }

        private void SetRegularSexSettings()
        {
            if (race.HasModExtension<RegularSexSettings>())
            {
                regularSex = race.GetModExtension<RegularSexSettings>().Copy();
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

        private void SetGrowthMoments()
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
                    LogUtil.Warning("Growth moment ages for " + race.defName + " do not make sense. Child at " + childAge + ", adult at " + adultAge + ", growth ages are " + biotech.growthMoments[0] + ", " + biotech.growthMoments[1] + ", " + biotech.growthMoments[2]);
                    biotech.growthMoments = RecalculateGrowthMoments(childAge, adultAge);
                    if (biotech.growthMoments is null)
                    {
                        LogUtil.Warning($"Growth moments removed");
                    }
                    else
                    {
                        LogUtil.Warning("New ages are " + biotech.growthMoments[0] + ", " + biotech.growthMoments[1] + ", " + biotech.growthMoments[2]);
                    }
                    //Do I need to reassign the HAR growth moments?
                    if (Settings.HARActive)
                    {
                        HAR_Integration.SetGrowthMoments(race, biotech.growthMoments?.ToList());
                    }
                }
            }
            //We don't assign a default because some races intentionally have null growthAges
        }

        private int[] RecalculateGrowthMoments(int childAge, int adultAge)
        {
            if (childAge == 0 && adultAge == 0)
            {
                return null;
            }
            int difference = adultAge - childAge;
            int interval = difference / 3;
            if (difference < 3)
            {
                LogUtil.Error($"Unable to calculate three unique growth moments for {race.defName}, keeping weird ages. Race mod author might want to reconsider their life stage ages.");
                return biotech.growthMoments;
            }
            if (difference % 3 == 0)
            {
                return [
                            childAge + interval,
                            childAge + (interval * 2),
                            adultAge,
                        ];
            }
            else if (difference % 3 == 1)
            {
                return [
                            childAge + interval + 1,
                            childAge + (interval * 2) + 1,
                            adultAge,
                        ];
            }
            else if (difference % 3 == 2)
            {
                return [
                            childAge + interval + 1,
                            childAge + (interval * 2) + 2,
                            adultAge,
                        ];
            }
            else
            {
                LogUtil.Error($"Error in calculating growth moments for {race.defName}, keeping weird ages. Race mod author needs to fix their life stage ages.");
                return biotech.growthMoments;
            }
        }

        private void SetMiscSettings()
        {
            //There's no extension for these, they are all directly calculated
            //Shortcut for robot types
            if (biotech.growthMoments == null)
            {
                misc.minAgeForAdulthood = 0f;
                misc.childAge = 0;
                misc.adultAgeForLearning = 0;
                misc.ageReversalDemandAge = 0;//not sure about this
                misc.ageSkillFactor = [new CurvePoint(0f, 1f)];
                misc.ageSkillMaxFactorCurve = [new CurvePoint(0f, 1f)];
                goto LovinCurve;
            }
            //Min age for adulthood
            if (Settings.HARActive)
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
            float declineAge = regularSex.declineAtAge.IsUnset() ? 30f : regularSex.declineAtAge;
            float result = adultAge + 5f;
            if (declineAge - adultAge < 10f)
            {
                result = adultAge + ((declineAge - adultAge) / 2);
            }
            misc.ageReversalDemandAge = (int)result;

            //Effect of age on skill
            misc.ageSkillFactor =
            [
                new CurvePoint(misc.childAge, 0.2f),
                new CurvePoint(misc.adultAgeForLearning, 1f),
            ];

            //A different effect of age on skill
            misc.ageSkillMaxFactorCurve =
            [
                new CurvePoint(0f,0f),
                new CurvePoint(biotech.growthMoments?[1] ?? 0f, 0.7f),
                new CurvePoint(misc.adultAgeForLearning * 2f, 1f),
                new CurvePoint(race.race.lifeExpectancy - (race.race.lifeExpectancy/4), 1.6f),
            ];

        //Curve for lovin' MTB
        LovinCurve:
            if (!regularSex.IsEmpty())
            {
                misc.lovinCurve =
                [
                    new CurvePoint(regularSex.minAgeForSex.IsUnset() ? 16f : regularSex.minAgeForSex, 1.5f),
                    new CurvePoint((regularSex.declineAtAge.IsUnset() ? 30f : regularSex.declineAtAge / 5) + regularSex.minAgeForSex, 1.5f),
                    new CurvePoint(regularSex.declineAtAge.IsUnset() ? 30f : regularSex.declineAtAge, 4f),
                    new CurvePoint((regularSex.maxAgeForSex.IsUnset() ? 80f : regularSex.maxAgeForSex / 4) + (regularSex.declineAtAge.IsUnset() ? 30f : regularSex.declineAtAge), 12f),
                    new CurvePoint(regularSex.maxAgeForSex.IsUnset() ? 80f : regularSex.maxAgeForSex, 36f)
                ];
            }
        }

        private void SetBiotechSettings()
        {
            //Settings are already copied over in SetGrowthMoments
            biotech.femaleFertilityAgeFactor = CheckFertilityCurve(Gender.Female);
            biotech.maleFertilityAgeFactor = CheckFertilityCurve(Gender.Male);
            biotech.noneFertilityAgeFactor = CheckFertilityCurve(Gender.None);
        }

        //This is getting kind of bloated. Think about ways to pare it down
        private SimpleCurve CheckFertilityCurve(Gender gender)
        {
            //Don't do anything if they're never reproductive
            if (!race.race.lifeStageAges.Any(stage => stage.def.reproductive))
            {
                return
                [
                    new CurvePoint(0f,0f)
                ];
            }
            //First, check if there is a curve supplied via the extension
            SimpleCurve myCurve = gender switch
            {
                Gender.Female => biotech.femaleFertilityAgeFactor,
                Gender.None => biotech.noneFertilityAgeFactor,
                _ => biotech.maleFertilityAgeFactor,
            };
            //None gender is a special case because HAR doesn't have a separate setting for that
            //So if one is provided via WBR, we use that no matter what
            if (gender == Gender.None && myCurve != null)
            {
                return myCurve;
            }
            //Also check for a HAR one
            SimpleCurve HARCurve = Settings.HARActive ? HAR_Integration.FertilityCurve(race, gender) : null;

            //If neither exist, calculate one
            if (myCurve == null && HARCurve == null)
            {
                //This will currently only get hit if HAR is not active, since HAR assigns a default
                return CalculateCurve(gender);
            }
            //If only mine exists, use that one
            if (myCurve != null && HARCurve == null)
            {
                return myCurve;
            }
            //If only HAR exists, still need to check if it's reasonable
            if (myCurve == null && HARCurve != null)
            {
                //If it's not the same as the default, just use it
                if (!HAR_Integration.IsCurveDefault(HARCurve, gender))
                {
                    return HARCurve;
                }
                //Otherwise we need to check if the ages make sense
                if (DoAgesMakeSense(HARCurve))
                {
                    return HARCurve;
                }

                //If not, calculate a reasonable curve
                return CalculateCurve(gender);
            }
            //If both exist, do some checks
            if (myCurve != null && HARCurve != null)
            {
                //If they're the same then just use whatever
                if (myCurve.IsEquivalentTo(HARCurve))
                {
                    return myCurve;
                }

                //If they're not the same, and the HAR curve is the human default, use mine
                if (HAR_Integration.IsCurveDefault(HARCurve, gender))
                {
                    return myCurve;
                }
                //If they're not the same, and the HAR curve is not the human default, use HAR, yeah?
                return HARCurve;

            }
            //I don't think it should get to here, so double check that all of the above scenarios have correct results
            LogUtil.Error("Error while calculating fertility curve for " + gender.GetLabel() + " " + race.label);
            return CalculateCurve(gender);
        }

        //This still needs work
        private SimpleCurve CalculateCurve(Gender gender)
        {
            //Disabling until I figure out a better way to do this
            return SettingsUtilities.GetDefaultFertilityAgeCurve(gender);
            //Just use the default for humans
            if (race.defName == "Human")
            {
                return SettingsUtilities.GetDefaultFertilityAgeCurve(gender);
            }
            //Humans begin a reproductive life stage at 13, and the curves start at 14
            float lifeExpectancyIncrement = race.race.lifeExpectancy / 8;
            float firstReproductiveAge = race.race.lifeStageAges.First(stage => stage.def.reproductive).minAge;
            float startPoint = firstReproductiveAge + 1;
            float femaleEndPoint = race.race.lifeExpectancy - (lifeExpectancyIncrement * 3);
            float maleEndPoint = race.race.lifeExpectancy + lifeExpectancyIncrement;
            if (gender == Gender.Female)
            {
                CompSettingsRegularSex regularSettings = regularSex.IsEmpty() ? regularSex.Default() : regularSex;
                float smallerLifeExpectancyIncrement = lifeExpectancyIncrement / 2;
                float declineStart = regularSettings.declineAtAge + smallerLifeExpectancyIncrement;
                float declineMiddle = declineStart + smallerLifeExpectancyIncrement;
                float declineEnd = declineMiddle + smallerLifeExpectancyIncrement;
                if (femaleEndPoint < declineEnd)
                {
                    femaleEndPoint = declineEnd + smallerLifeExpectancyIncrement;
                }
                return
                [
                    //14f
                    new CurvePoint(startPoint, 0f),
                    //20f
                    new CurvePoint(misc.minAgeForAdulthood, 1f),
                    //28f = 18 + 10
                    new CurvePoint(misc.adultAgeForLearning + lifeExpectancyIncrement, 1f),
                    //35f
                    new CurvePoint(declineStart, 0.5f),
                    //40f
                    new CurvePoint(declineMiddle, 0.1f),
                    //45f could also be previous + smallerLifeExpectancyIncrement
                    //Check what difference that makes with
                    new CurvePoint(declineEnd, 0.02f),
                    //50f
                    new CurvePoint(femaleEndPoint, 0f),
                ];
            }
            //This will be males and none
            //Males next point is the learning age
            float malePeakStart = misc.adultAgeForLearning;
            //Males next point is the same as the female end point

            float malePeakEnd = femaleEndPoint;
            //Life expectancy is 80; males end at 90, females at 50

            return
                [
                    //14f
                    new CurvePoint(startPoint, 0f),
                    //18f
                    new CurvePoint(malePeakStart, 1f),
                    //50f
                    new CurvePoint(malePeakEnd, 1f),
                    //90f
                    new CurvePoint(maleEndPoint, 0f)
                ];
        }

        //This doesn't really work yet
        private bool DoAgesMakeSense(SimpleCurve curve)
        {
            //Disabling until I figure out a better way to do this
            return true;
            //So I guess first we could check if they even have any reproductive life stages. If not we just return true because their fertility curve won't matter
            if (!race.race.lifeStageAges.Any(stage => stage.def.reproductive))
            {
                return true;
            }
            //Then I guess I need a range of stages marked as reproductive?
            //min age of the first reproductive stage and the min age of the first stage after that that's not reproductive, if that doesn't exist use life expectancy
            float firstReproductiveAge = race.race.lifeStageAges.First(stage => stage.def.reproductive).minAge;
            float lastReproductiveAge = race.race.lifeExpectancy;
            //Check here for elder type stages
            //Defo test that this does what you think
            bool reproductiveStart = false;
            foreach (LifeStageAge stage in race.race.lifeStageAges)
            {
                if (stage.def.reproductive && !reproductiveStart)
                {
                    firstReproductiveAge = stage.minAge;
                    reproductiveStart = true;
                }
                if (!stage.def.reproductive && reproductiveStart)
                {
                    lastReproductiveAge = stage.minAge;
                    break;
                }
            }
            //Now what do we consider reasonably near the ages we've found?
            float lifeExpectancyInterval = race.race.lifeExpectancy / 8;
            float[] ages = new float[curve.PointsCount];
            for (int i = 0; i < curve.Points.Count; i++)
            {
                CurvePoint point = curve.Points[i];
                ages.SetValue(point.x, i);
            }
            float startAge = ages[0];
            float endAge = ages.Last();
            //So now I have an age range where they should be reproductive
            //And an age range where fertility is not zero
            //Keep in mind this is just from the start to the end, I haven't checked for where it peaks
            //Let me just do that real fast in case I end up needing it
            FloatRange peakRange = new();
            bool startFound = false;
            foreach (CurvePoint point in curve.Points)
            {
                if (point.y >= 1f)
                {
                    if (startFound)
                    {
                        peakRange.max = point.x;
                    }
                    else
                    {
                        peakRange.min = point.x;
                        startFound = true;
                    }
                }

            }
            //I don't want to make any assumptions about how fertility should work for an arbitrary race, I just need to know if they're definitely wrong
            //And remember that the supplied curve here is the human default. I don't need to make allowances for different values
            //So, I just need to know if a range of 14-90 or 14-50 is wildly out of place

            //I do not like the below
            //Brainstorm more tomorrow; try rubber ducking
            //If it's less than the lowest, or more than the highest
            if (startAge < firstReproductiveAge - lifeExpectancyInterval || startAge > firstReproductiveAge + lifeExpectancyInterval)
            {
                return false;
            }
            //This will only work for men, the last point for women is way lower
            if (endAge < lastReproductiveAge - lifeExpectancyInterval || endAge > lastReproductiveAge + lifeExpectancyInterval)
            {
                return false;
            }
            return true;
        }
    }
}
