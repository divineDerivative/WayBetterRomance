using Verse;

namespace BetterRomance
{
    public class WBR_SettingsComp : ThingComp
    {
        Pawn Pawn => (Pawn)parent;
        RaceSettings raceSettings;

        public CompSettingsOrientation orientation;
        public CompSettingsCasualSexPawn casualSex;
        public CompSettingsRegularSex regularSex;
        public CompSettingsRelationsPawn relations;
        public CompSettingsBiotech biotech;
        public CompSettingsMisc misc;

        public bool NoGrowth => biotech.growthMoments == null;

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            orientation = new CompSettingsOrientation();
            casualSex = new CompSettingsCasualSexPawn();
            regularSex = new CompSettingsRegularSex();
            relations = new CompSettingsRelationsPawn();
            biotech = new CompSettingsBiotech();
            misc = new CompSettingsMisc();

            foreach (RaceSettings rs in Settings.RaceSettingsList)
            {
                if (rs.race == parent.def)
                {
                    raceSettings = rs;
                    break;
                }
            }
        }

        //This needs to be done separately from initialization because newly generated pawns won't have their pawnkind set yet
        public void ApplySettings()
        {
            SetOrientationChances();
            SetCasualSexSettings();
            SetRegularSexSettings();
            SetRelationSettings();
            SetBiotechSettings();
            SetMiscSettings();
        }

        private void SetIfNotNull<T>(ref T result, T nullable)
        {
            //Treat an initial value as null
            //Make sure I'm only doing this for floats
            if (nullable is float number && number == -999f)
            {
                return;
            }
            if (nullable != null)
            {
                result = nullable;
            }
        }

        private void SetOrientationChances()
        {
            orientation = raceSettings.orientation?.Copy();

            if (Pawn.kindDef.HasModExtension<SexualityChances>())
            {
                SexualityChances chances = Pawn.kindDef.GetModExtension<SexualityChances>();
                //Check that this doesn't overwrite a non-null set with a null one
                orientation.sexual = chances.sexual?.Copy;
                orientation.asexual = chances.asexual?.Copy;
            }
        }

        private void SetCasualSexSettings()
        {
            //Take values from the race's settings, use temp for values that can't be null at the end
            casualSex = raceSettings.casualSex.CopyToPawn();
            bool? caresAboutCheatingTemp = raceSettings.casualSex.caresAboutCheating;
            bool? willDoHookupTemp = raceSettings.casualSex.willDoHookup;
            bool? canDoOrderedHookupTemp = raceSettings.casualSex.canDoOrderedHookup;
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
            }

            //Assign default values to anything that can't be null
            casualSex.caresAboutCheating = caresAboutCheatingTemp ?? true;
            casualSex.willDoHookup = willDoHookupTemp ?? true;
            casualSex.canDoOrderedHookup = canDoOrderedHookupTemp ?? true;
            casualSex.forBreedingOnly = forBreedingOnlyTemp ?? false;
        }

        private void SetRegularSexSettings()
        {
            regularSex = raceSettings.regularSex.Copy();

            if (Pawn.kindDef.HasModExtension<RegularSexSettings>())
            {
                RegularSexSettings settings = Pawn.kindDef.GetModExtension<RegularSexSettings>();
                SetIfNotNull(ref regularSex.minAgeForSex, settings.minAgeForSex);
                SetIfNotNull(ref regularSex.maxAgeForSex, settings.maxAgeForSex);
                SetIfNotNull(ref regularSex.maxAgeGap, settings.maxAgeGap);
                SetIfNotNull(ref regularSex.declineAtAge, settings.declineAtAge);
            }

            //Assign default values to everything that's still unassigned
            if (regularSex.minAgeForSex == -999f)
            {
                regularSex.minAgeForSex = 16f;
            }
            if (regularSex.maxAgeForSex == -999f)
            {
                regularSex.maxAgeForSex = 80f;
            }
            if (regularSex.maxAgeGap == -999f)
            {
                regularSex.maxAgeGap = 40f;
            }
            if (regularSex.declineAtAge == -999f)
            {
                regularSex.declineAtAge = 30f;
            }
        }

        private void SetRelationSettings()
        {
            //Copy over everything that's allowed to be null
            relations = raceSettings.relations.CopyToPawn();
            //Use temp values for the rest
            bool? spousesAllowedTemp = raceSettings.relations.spousesAllowed;
            bool? childrenAllowedTemp = raceSettings.relations.childrenAllowed;

            float? minFemaleAgeToHaveChildrenTemp = raceSettings.relations.minFemaleAgeToHaveChildren;
            float? usualFemaleAgeToHaveChildrenTemp = raceSettings.relations.usualFemaleAgeToHaveChildren;
            float? maxFemaleAgeToHaveChildrenTemp = raceSettings.relations.maxFemaleAgeToHaveChildren;

            float? minMaleAgeToHaveChildrenTemp = raceSettings.relations.minMaleAgeToHaveChildren;
            float? usualMaleAgeToHaveChildrenTemp = raceSettings.relations.usualMaleAgeToHaveChildren;
            float? maxMaleAgeToHaveChildrenTemp = raceSettings.relations.maxMaleAgeToHaveChildren;
            int? maxChildrenDesiredTemp = raceSettings.relations.maxChildrenDesired;

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
            }

            //Assign default values to anything that can't be null
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
            //Everything here is nullable, so just copy the whole thing
            biotech = raceSettings.biotech.Copy();

            if (Pawn.kindDef.HasModExtension<BiotechSettings>())
            {
                BiotechSettings settings = Pawn.kindDef.GetModExtension<BiotechSettings>();

                SetIfNotNull(ref biotech.maleFertilityAgeFactor, settings.maleFertilityAgeFactor);
                SetIfNotNull(ref biotech.femaleFertilityAgeFactor, settings.femaleFertilityAgeFactor);
                SetIfNotNull(ref biotech.noneFertilityAgeFactor, settings.noneFertilityAgeFactor);
                SetIfNotNull(ref biotech.ageEffectOnChildbirth, settings.ageEffectOnChildbirth);
            }
            //But none of it is allowed to be null, so assign default values
            biotech.maleFertilityAgeFactor = biotech.maleFertilityAgeFactor ?? SettingsUtilities.GetDefaultFertilityAgeCurve(Gender.Male);
            biotech.femaleFertilityAgeFactor = biotech.femaleFertilityAgeFactor ?? SettingsUtilities.GetDefaultFertilityAgeCurve(Gender.Female);
            biotech.noneFertilityAgeFactor = biotech.noneFertilityAgeFactor ?? SettingsUtilities.GetDefaultFertilityAgeCurve(Gender.None);
            biotech.ageEffectOnChildbirth = biotech.ageEffectOnChildbirth ?? SettingsUtilities.GetDefaultChildbirthAgeCurve();
        }

        private void SetMiscSettings()
        {
            //None of this should change at the pawn level, so we just copy directly
            misc = raceSettings.misc.Copy();
            //Except this one I guess
            if (regularSex.declineAtAge != raceSettings.regularSex.declineAtAge)
            {
                //redo age reversal, but don't account for senescence
                float adultAge = misc.minAgeForAdulthood;
                float declineAge = regularSex.declineAtAge;
                float result = adultAge + 5f;
                if (declineAge - adultAge < 10f)
                {
                    result = adultAge + ((declineAge - adultAge) / 2);
                }
                misc.ageReversalDemandAge = (int)result;
            }
        }
    }
}
