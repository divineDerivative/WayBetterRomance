using Verse;

namespace BetterRomance
{
    public class WBR_SettingsComp : ThingComp
    {
        Pawn Pawn => (Pawn)parent;
        RaceSettings raceSettings;

        public OrientationChancePawn orientation;
        public CasualSexPawn casualSex;
        public RegularSexPawn regularSex;
        public RelationsPawn relations;
        public BiotechPawn biotech;

        //Misc Settings
        public float minAgeForAdulthood;
        public int childAge;
        public int adultAgeForLearning;
        public int ageReversalDemandAge;
        public SimpleCurve ageSkillFactor;
        public SimpleCurve ageSkillMaxFactorCurve;

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            orientation = new OrientationChancePawn();
            casualSex = new CasualSexPawn();
            regularSex = new RegularSexPawn();
            relations = new RelationsPawn();
            biotech = new BiotechPawn();

            foreach (RaceSettings rs in Settings.RaceSettingsList)
            {
                if (rs.race == parent.def)
                {
                    raceSettings = rs;
                    break;
                }
            }
        }

        public void ApplySettings()
        {
            //Assign any non-null values in raceSettings to the appropriate field
            //Assign any non-null values from the pawnkind to the appropriate field
            //Anything that is still null is assigned a default value
            SetOrientationChances();
            SetCasualSexSettings();
            SetRegularSexSettings();
            SetRelationSettings();
            SetBiotechSettings();
        }

        private void SetIfNotNull<T>(ref T result, T nullable)
        {
            if (nullable != null)
            {
                result = nullable;
            }
        }

        private void CopySettings<T>(T settings)
        {

        }

        private void SetOrientationChances()
        {
            orientation.asexualChance = raceSettings.orientation.asexualChance;
            orientation.bisexualChance = raceSettings.orientation.bisexualChance;
            orientation.gayChance = raceSettings.orientation.gayChance;
            orientation.straightChance = raceSettings.orientation.straightChance;

            orientation.aceAroChance = raceSettings.orientation.aceAroChance;
            orientation.aceBiChance = raceSettings.orientation.aceBiChance;
            orientation.aceHomoChance = raceSettings.orientation.aceHomoChance;
            orientation.aceHeteroChance = raceSettings.orientation.aceHeteroChance;

            if (Pawn.kindDef.HasModExtension<SexualityChances>())
            {
                SexualityChances chances = Pawn.kindDef.GetModExtension<SexualityChances>();

                SetIfNotNull(ref orientation.asexualChance, chances.asexualChance);
                SetIfNotNull(ref orientation.bisexualChance, chances.bisexualChance);
                SetIfNotNull(ref orientation.gayChance, chances.gayChance);
                SetIfNotNull(ref orientation.straightChance, chances.straightChance);
                //if (chances.asexualChance != null)
                //{
                //    orientation.asexualChance = chances.asexualChance;
                //}
                //if (chances.bisexualChance != null)
                //{
                //    orientation.bisexualChance = chances.bisexualChance;
                //}
                //if (chances.gayChance != null)
                //{
                //    orientation.gayChance = chances.gayChance;
                //}
                //if (chances.straightChance != null)
                //{
                //    orientation.straightChance = chances.straightChance;
                //}

                SetIfNotNull(ref orientation.aceAroChance, chances.aceAroChance);
                SetIfNotNull(ref orientation.aceBiChance, chances.aceBiChance);
                SetIfNotNull(ref orientation.aceHomoChance, chances.aceHomoChance);
                SetIfNotNull(ref orientation.aceHeteroChance, chances.aceHeteroChance);
                //if (chances.aceAroChance != null)
                //{
                //    orientation.aceAroChance = chances.aceAroChance;
                //}
                //if (chances.aceBiChance != null)
                //{
                //    orientation.aceBiChance = chances.aceBiChance;
                //}
                //if (chances.aceHomoChance != null)
                //{
                //    orientation.aceHomoChance = chances.aceHomoChance;
                //}
                //if (chances.aceHeteroChance != null)
                //{
                //    orientation.aceHeteroChance = chances.aceHeteroChance;
                //}
            }
        }

        private void SetCasualSexSettings()
        {
            //Take values from the race's settings, use temp for values that can't be null at the end
            bool? caresAboutCheatingTemp = raceSettings.casualSex.caresAboutCheating;
            bool? willDoHookupTemp = raceSettings.casualSex.willDoHookup;
            bool? canDoOrderedHookupTemp = raceSettings.casualSex.canDoOrderedHookup;
            casualSex.hookupRate = raceSettings.casualSex.hookupRate;
            casualSex.alienLoveChance = raceSettings.casualSex.alienLoveChance;
            casualSex.minOpinionForHookup = raceSettings.casualSex.minOpinionForHookup;
            casualSex.minOpinionForOrderedHookup = raceSettings.casualSex.minOpinionForOrderedHookup;
            bool? forBreedingOnlyTemp = raceSettings.casualSex.forBreedingOnly;

            //Override with values from the pawnkind's settings
            if (Pawn.kindDef.HasModExtension<CasualSexSettings>())
            {
                CasualSexSettings settings = Pawn.kindDef.GetModExtension<CasualSexSettings>();

                SetIfNotNull(ref caresAboutCheatingTemp, settings.caresAboutCheating);
                SetIfNotNull(ref willDoHookupTemp, settings.willDoHookup);
                SetIfNotNull(ref canDoOrderedHookupTemp, settings.canDoOrderedHookup);
                SetIfNotNull(ref casualSex.hookupRate, settings.hookupRate);
                SetIfNotNull(ref casualSex.alienLoveChance, settings.alienLoveChance);
                SetIfNotNull(ref casualSex.minOpinionForHookup, settings.hookupTriggers?.minOpinion);
                SetIfNotNull(ref casualSex.minOpinionForOrderedHookup, settings.orderedHookupTriggers?.minOpinion);
                SetIfNotNull(ref forBreedingOnlyTemp, settings.orderedHookupTriggers?.forBreedingOnly);
                //if (settings.caresAboutCheating != null)
                //{
                //    caresAboutCheatingTemp = settings.caresAboutCheating;
                //}
                //if (settings.willDoHookup != null)
                //{
                //    willDoHookupTemp = settings.willDoHookup;
                //}
                //if (settings.canDoOrderedHookup != null)
                //{
                //    canDoOrderedHookupTemp = settings.canDoOrderedHookup;
                //}
                //if (settings.hookupRate != null)
                //{
                //    casualSex.hookupRate = settings.hookupRate;
                //}
                //if (settings.alienLoveChance != null)
                //{
                //    casualSex.alienLoveChance = settings.alienLoveChance;
                //}
                //if (settings.hookupTriggers != null && settings.hookupTriggers.minOpinion != null)
                //{
                //    casualSex.minOpinionForHookup = settings.hookupTriggers.minOpinion;
                //}
                //if (settings.orderedHookupTriggers != null)
                //{
                //    if (settings.orderedHookupTriggers.minOpinion != null)
                //    {
                //        casualSex.minOpinionForOrderedHookup = settings.orderedHookupTriggers.minOpinion;
                //    }
                //    if (settings.orderedHookupTriggers.forBreedingOnly != null)
                //    {
                //        forBreedingOnlyTemp = settings.orderedHookupTriggers.forBreedingOnly;
                //    }
                //}
            }

            //Assign default values to anything that can't be null
            casualSex.caresAboutCheating = caresAboutCheatingTemp ?? true;
            casualSex.willDoHookup = willDoHookupTemp ?? true;
            casualSex.canDoOrderedHookup = canDoOrderedHookupTemp ?? true;
            casualSex.forBreedingOnly = forBreedingOnlyTemp ?? false;
        }

        private void SetRegularSexSettings()
        {
            float? minAgeForSexTemp = raceSettings.regularSex.minAgeForSex;
            float? maxAgeForSexTemp = raceSettings.regularSex.maxAgeForSex;
            float? maxAgeGapTemp = raceSettings.regularSex.maxAgeGap;
            float? declineAtAgeTemp = raceSettings?.regularSex.declineAtAge;

            if (Pawn.kindDef.HasModExtension<RegularSexSettings>())
            {
                RegularSexSettings settings = Pawn.kindDef.GetModExtension<RegularSexSettings>();
                SetIfNotNull(ref minAgeForSexTemp, settings.minAgeForSex);
                SetIfNotNull(ref maxAgeForSexTemp, settings.maxAgeForSex);
                SetIfNotNull(ref maxAgeGapTemp, settings.maxAgeGap);
                SetIfNotNull(ref declineAtAgeTemp, settings.declineAtAge);
                //if (settings.minAgeForSex != null)
                //{
                //    minAgeForSexTemp = settings.minAgeForSex;
                //}
                //if (settings.maxAgeForSex != null)
                //{
                //    maxAgeForSexTemp = settings.maxAgeForSex;
                //}
                //if (settings.maxAgeGap != null)
                //{
                //    maxAgeGapTemp = settings.maxAgeGap;
                //}
                //if (settings.declineAtAge != null)
                //{
                //    declineAtAgeTemp = settings.declineAtAge;
                //}
            }

            regularSex.minAgeForSex = minAgeForSexTemp ?? 16f;
            regularSex.maxAgeForSex = maxAgeForSexTemp ?? 80f;
            regularSex.maxAgeGap = maxAgeGapTemp ?? 40f;
            regularSex.declineAtAge = declineAtAgeTemp ?? 30f;
        }

        private void SetRelationSettings()
        {
            bool? spousesAllowedTemp = raceSettings.relations.spousesAllowed;
            bool? childrenAllowedTemp = raceSettings.relations.childrenAllowed;

            relations.pawnKindForParentGlobal = raceSettings.relations.pawnKindForParentGlobal;
            relations.pawnKindForParentFemale = raceSettings.relations.pawnKindForParentFemale;
            relations.pawnKindForParentMale = raceSettings.relations.pawnKindForParentMale;

            float? minFemaleAgeToHaveChildrenTemp = raceSettings.relations.minFemaleAgeToHaveChildren;
            float? usualFemaleAgeToHaveChildrenTemp = raceSettings.relations.usualFemaleAgeToHaveChildren;
            float? maxFemaleAgeToHaveChildrenTemp = raceSettings.relations.maxFemaleAgeToHaveChildren;

            float? minMaleAgeToHaveChildrenTemp = raceSettings.relations.minMaleAgeToHaveChildren;
            float? usualMaleAgeToHaveChildrenTemp = raceSettings.relations.usualMaleAgeToHaveChildren;
            float? maxMaleAgeToHaveChildrenTemp = raceSettings.relations.maxMaleAgeToHaveChildren;

            int? maxChildrenDesiredTemp = raceSettings.relations.maxChildrenDesired;
            relations.minOpinionRomance = raceSettings.relations.minOpinionRomance;

            if (Pawn.kindDef.HasModExtension<RelationSettings>())
            {
                RelationSettings settings = Pawn.kindDef.GetModExtension<RelationSettings>();

                SetIfNotNull(ref spousesAllowedTemp, settings.spousesAllowed);
                SetIfNotNull(ref childrenAllowedTemp, settings.childrenAllowed);
                SetIfNotNull(ref relations.pawnKindForParentGlobal, settings.pawnKindForParentGlobal);
                SetIfNotNull(ref relations.pawnKindForParentFemale, settings.pawnKindForParentFemale);
                SetIfNotNull(ref relations.pawnKindForParentMale, settings.pawnKindForParentMale);
                SetIfNotNull(ref minFemaleAgeToHaveChildrenTemp, settings.minFemaleAgeToHaveChildren);
                SetIfNotNull(ref usualFemaleAgeToHaveChildrenTemp, settings.usualFemaleAgeToHaveChildren);
                SetIfNotNull(ref maxFemaleAgeToHaveChildrenTemp, settings.maxFemaleAgeToHaveChildren);
                SetIfNotNull(ref minMaleAgeToHaveChildrenTemp, settings.minMaleAgeToHaveChildren);
                SetIfNotNull(ref usualMaleAgeToHaveChildrenTemp, settings.usualMaleAgeToHaveChildren);
                SetIfNotNull(ref maxMaleAgeToHaveChildrenTemp, settings.maxMaleAgeToHaveChildren);
                SetIfNotNull(ref maxChildrenDesiredTemp, settings.maxChildrenDesired);
                SetIfNotNull(ref relations.minOpinionRomance, settings.minOpinionRomance);
                //if (settings.spousesAllowed != null)
                //{
                //    spousesAllowedTemp = settings.spousesAllowed;
                //}
                //if (settings.childrenAllowed != null)
                //{
                //    childrenAllowedTemp = settings.childrenAllowed;
                //}
                //if (settings.pawnKindForParentGlobal != null)
                //{
                //    relations.pawnKindForParentGlobal = settings.pawnKindForParentGlobal;
                //}
                //if (settings.pawnKindForParentFemale != null)
                //{
                //    relations.pawnKindForParentFemale = settings.pawnKindForParentFemale;
                //}
                //if (settings.pawnKindForParentMale != null)
                //{
                //    relations.pawnKindForParentMale = settings.pawnKindForParentMale;
                //}
                //if (settings.minFemaleAgeToHaveChildren != null)
                //{
                //    minFemaleAgeToHaveChildrenTemp = settings.minFemaleAgeToHaveChildren;
                //}
                //if (settings.usualFemaleAgeToHaveChildren != null)
                //{
                //    usualFemaleAgeToHaveChildrenTemp = settings.usualFemaleAgeToHaveChildren;
                //}
                //if (settings.maxFemaleAgeToHaveChildren != null)
                //{
                //    maxFemaleAgeToHaveChildrenTemp = settings.maxFemaleAgeToHaveChildren;
                //}
                //if (settings.minMaleAgeToHaveChildren != null)
                //{
                //    minMaleAgeToHaveChildrenTemp = settings.minMaleAgeToHaveChildren;
                //}
                //if (settings.usualMaleAgeToHaveChildren != null)
                //{
                //    usualMaleAgeToHaveChildrenTemp = settings.usualMaleAgeToHaveChildren;
                //}
                //if (settings.maxMaleAgeToHaveChildren != null)
                //{
                //    maxMaleAgeToHaveChildrenTemp = settings.maxMaleAgeToHaveChildren;
                //}
                //if (settings.maxChildrenDesired != null)
                //{
                //    maxChildrenDesiredTemp = settings.maxChildrenDesired;
                //}
                //if (settings.minOpinionRomance != null)
                //{
                //    relations.minOpinionRomance = settings.minOpinionRomance;
                //}
            }

            relations.spousesAllowed = spousesAllowedTemp ?? true;
            relations.childrenAllowed = childrenAllowedTemp ?? true;

            relations.minFemaleAgeToHaveChildren = minFemaleAgeToHaveChildrenTemp ?? 16f;
            relations.usualFemaleAgeToHaveChildren = usualFemaleAgeToHaveChildrenTemp ?? 27f;
            relations.maxFemaleAgeToHaveChildren = maxFemaleAgeToHaveChildrenTemp ?? 45f;

            relations.minMaleAgeToHaveChildren = minMaleAgeToHaveChildrenTemp ?? 14f;
            relations.usualMaleAgeToHaveChildren = usualMaleAgeToHaveChildrenTemp ?? 30f;
            relations.maxMaleAgeToHaveChildren = maxMaleAgeToHaveChildrenTemp ?? 50f;

            relations.maxChildrenDesired = maxChildrenDesiredTemp ?? 3;
        }

        private void SetBiotechSettings()
        {
            biotech.maleFertilityAgeFactor = raceSettings.biotech.maleFertilityAgeFactor;
            biotech.femaleFertilityAgeFactor = raceSettings.biotech.femaleFertilityAgeFactor;
            biotech.noneFertilityAgeFactor = raceSettings.biotech.noneFertilityAgeFactor;
            biotech.ageEffectOnChildbirth = raceSettings.biotech.ageEffectOnChildbirth;

            if (Pawn.kindDef.HasModExtension<BiotechSettings>())
            {
                BiotechSettings settings = Pawn.kindDef.GetModExtension<BiotechSettings>();

                SetIfNotNull(ref biotech.maleFertilityAgeFactor, settings.maleFertilityAgeFactor);
                SetIfNotNull(ref biotech.femaleFertilityAgeFactor, settings.femaleFertilityAgeFactor);
                SetIfNotNull(ref biotech.noneFertilityAgeFactor, settings.noneFertilityAgeFactor);
                SetIfNotNull(ref biotech.ageEffectOnChildbirth, settings.ageEffectOnChildbirth);
                //if (settings.maleFertilityAgeFactor != null)
                //{
                //    biotech.maleFertilityAgeFactor = settings.maleFertilityAgeFactor;
                //}
                //if (settings.femaleFertilityAgeFactor != null)
                //{
                //    biotech.femaleFertilityAgeFactor = settings.femaleFertilityAgeFactor;
                //}
                //if (settings.noneFertilityAgeFactor != null)
                //{
                //    biotech.noneFertilityAgeFactor = settings.noneFertilityAgeFactor;
                //}
                //if (settings.ageEffectOnChildbirth != null)
                //{
                //    biotech.ageEffectOnChildbirth = settings.ageEffectOnChildbirth;
                //}
            }

            if (biotech.maleFertilityAgeFactor == null)
            {
                biotech.maleFertilityAgeFactor = SettingsUtilities.femaleFertilityAgeFactor ?? new SimpleCurve
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
            if (biotech.femaleFertilityAgeFactor == null)
            {
                biotech.femaleFertilityAgeFactor = SettingsUtilities.maleFertilityAgeFactor ?? new SimpleCurve
                {
                    new CurvePoint(14f, 0f),
                    new CurvePoint(18f, 1f),
                    new CurvePoint(50f, 1f),
                    new CurvePoint(90f, 0f),
                };
            }
            if (biotech.noneFertilityAgeFactor == null)
            {
                biotech.noneFertilityAgeFactor = SettingsUtilities.maleFertilityAgeFactor ?? new SimpleCurve
                {
                    new CurvePoint(14f, 0f),
                    new CurvePoint(18f, 1f),
                    new CurvePoint(50f, 1f),
                    new CurvePoint(90f, 0f),
                };
            }
            if (biotech.ageEffectOnChildbirth == null)
            {
                biotech.ageEffectOnChildbirth = SettingsUtilities.childBirthByAgeCurve.curve ?? new SimpleCurve
                {
                    new CurvePoint(14f, 0.0f),
                    new CurvePoint(15f, 0.3f),
                    new CurvePoint(20f, 0.5f),
                    new CurvePoint(30f, 0.5f),
                    new CurvePoint(40f, 0.3f),
                    new CurvePoint(65f, 0.0f),
                };
            }
        }
    }

    public class OrientationChancePawn
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

    public class CasualSexPawn
    {
        public bool caresAboutCheating;
        public bool willDoHookup;
        public bool canDoOrderedHookup;
        public float? hookupRate;
        public float? alienLoveChance;
        public int? minOpinionForHookup;
        public int? minOpinionForOrderedHookup;
        public bool forBreedingOnly;
        //Maybe put the hookup traits here
    }

    public class RegularSexPawn
    {
        public float minAgeForSex;
        public float maxAgeForSex;
        public float maxAgeGap;
        public float declineAtAge;
    }

    public class RelationsPawn
    {
        public bool spousesAllowed;
        public bool childrenAllowed;

        public PawnKindDef pawnKindForParentGlobal;
        public PawnKindDef pawnKindForParentFemale;
        public PawnKindDef pawnKindForParentMale;

        public float minFemaleAgeToHaveChildren;
        public float usualFemaleAgeToHaveChildren;
        public float maxFemaleAgeToHaveChildren;

        public float minMaleAgeToHaveChildren;
        public float usualMaleAgeToHaveChildren;
        public float maxMaleAgeToHaveChildren;

        public int maxChildrenDesired;
        public int? minOpinionRomance;
    }

    public class BiotechPawn
    {
        public SimpleCurve maleFertilityAgeFactor;
        public SimpleCurve femaleFertilityAgeFactor;
        public SimpleCurve noneFertilityAgeFactor;
        public SimpleCurve ageEffectOnChildbirth;
        //Growth ages?
    }
}
