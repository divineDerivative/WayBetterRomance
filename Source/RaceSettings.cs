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
        //Sexuality Chances
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

        //Relation Settings
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

        //Biotech Settings
        public SimpleCurve maleFertilityAgeFactor;
        public SimpleCurve femaleFertilityAgeFactor;
        public SimpleCurve noneFertilityAgeFactor;
        public SimpleCurve ageEffectOnChildbirth;
        //Growth ages?

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
    }
}
