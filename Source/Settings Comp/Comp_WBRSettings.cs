using Verse;
using static BetterRomance.WBRLogger;

namespace BetterRomance
{
    public class WBR_SettingsComp : ThingComp
    {
        private Pawn Pawn => (Pawn)parent;
        private RaceSettings raceSettings;

        public CompSettingsOrientation orientation;
        public CompSettingsCasualSexPawn casualSex;
        public CompSettingsRegularSex regularSex;
        public CompSettingsRelationsPawn relations;
        public CompSettingsBiotech biotech;
        public CompSettingsMisc misc;

        public bool NoGrowth => biotech.growthMoments == null;
        private bool calculated = false;
        public bool Calculated => calculated;

        public void Copy(Pawn newPawn)
        {
            WBR_SettingsComp newComp = new()
            {
                parent = newPawn,
                raceSettings = raceSettings,
                orientation = orientation.Copy(),
                casualSex = casualSex.Copy(),
                regularSex = regularSex.Copy(),
                relations = relations.Copy(),
                biotech = biotech.Copy(),
                misc = misc.Copy(),
            };
            newPawn.AllComps.Add(newComp);
        }

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            orientation = new();
            casualSex = new();
            regularSex = new();
            relations = new();
            biotech = new();
            misc = new();
            calculated = false;

            FindRaceSettings();
        }

        private void FindRaceSettings()
        {
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
            if (!calculated)
            {
                SetOrientationChances();
                SetCasualSexSettings();
                SetRegularSexSettings();
                SetRelationSettings();
                SetBiotechSettings();
                SetMiscSettings();
                calculated = true;
            }
        }

        public void RedoSettings(bool forRaceChange)
        {
            if (forRaceChange && Settings.PawnmorpherActive)
            {
                Pawnmorpher_Integration.AdjustAges(this, raceSettings.race, Pawn.def);
                calculated = true;
            }
            else
            {
                Initialize(props);
                ApplySettings();
            }
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
            if (regularSex.minAgeForSex.IsUnset())
            {
                regularSex.minAgeForSex = 16f;
            }
            if (regularSex.maxAgeForSex.IsUnset())
            {
                regularSex.maxAgeForSex = 80f;
            }
            if (regularSex.maxAgeGap.IsUnset())
            {
                regularSex.maxAgeGap = 40f;
            }
            if (regularSex.declineAtAge.IsUnset())
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

            float? minEnbyAgeToHaveChildrenTemp = raceSettings.relations.minEnbyAgeToHaveChildren;
            float? usualEnbyAgeToHaveChildrenTemp = raceSettings.relations.usualEnbyAgeToHaveChildren;
            float? maxEnbyAgeToHaveChildrenTemp = raceSettings.relations.maxEnbyAgeToHaveChildren;

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

                SetIfNotNull(ref minEnbyAgeToHaveChildrenTemp, settings.minEnbyAgeToHaveChildren);
                SetIfNotNull(ref usualEnbyAgeToHaveChildrenTemp, settings.usualEnbyAgeToHaveChildren);
                SetIfNotNull(ref maxEnbyAgeToHaveChildrenTemp, settings.maxEnbyAgeToHaveChildren);
                
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

            //Use an average of the binary ages
            relations.minEnbyAgeToHaveChildren = minEnbyAgeToHaveChildrenTemp ?? (relations.minFemaleAgeToHaveChildren + relations.minMaleAgeToHaveChildren) / 2f;
            relations.usualEnbyAgeToHaveChildren = usualEnbyAgeToHaveChildrenTemp ?? (relations.usualFemaleAgeToHaveChildren + relations.usualMaleAgeToHaveChildren) / 2f;
            relations.maxEnbyAgeToHaveChildren = maxEnbyAgeToHaveChildrenTemp ?? (relations.maxFemaleAgeToHaveChildren + relations.maxMaleAgeToHaveChildren) / 2f;

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
            //But none of it is allowed to be null, so assign default values.
            //Do the same checks as we do for race
            //I think I'm making it so that the curves can't be null on the race setting; so I shouldn't need to do anything other than overwrite it with the pawnkind ones
            biotech.maleFertilityAgeFactor ??= SettingsUtilities.GetDefaultFertilityAgeCurve(Gender.Male);
            biotech.femaleFertilityAgeFactor ??= SettingsUtilities.GetDefaultFertilityAgeCurve(Gender.Female);
            biotech.noneFertilityAgeFactor ??= SettingsUtilities.GetDefaultFertilityAgeCurve(Gender.None);
            biotech.ageEffectOnChildbirth ??= SettingsUtilities.GetDefaultChildbirthAgeCurve();
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
            //Redo lovin' curve also, if any of the ages are different
            if (raceSettings.regularSex.IsEmpty() || regularSex != raceSettings.regularSex)
            {
                misc.lovinCurve =
                [
                    new CurvePoint(regularSex.minAgeForSex, 1.5f),
                    new CurvePoint((regularSex.declineAtAge / 5) + regularSex.minAgeForSex, 1.5f),
                    new CurvePoint(regularSex.declineAtAge, 4f),
                    new CurvePoint((regularSex.maxAgeForSex / 4) + regularSex.declineAtAge, 12f),
                    new CurvePoint(regularSex.maxAgeForSex, 36f),
                ];
            }
        }

        internal void LogToConsole()
        {
            LogUtil.Message("--------------------");
            LogUtil.Error($"WBR_SettingsComp info for {Pawn.Name}");
            LogUtil.Message($"Race is {raceSettings.race.defName}");
            if (orientation is null)
            {
                LogUtil.Message("Orientation is null");
            }
            else
            {
                if (orientation.sexual is null)
                {
                    LogUtil.Message($"Sexual orientation is null");
                }
                else
                {
                    LogUtil.Message($"Sexual orientation: {orientation.sexual.hetero} hetero, {orientation.sexual.homo} homo, {orientation.sexual.bi} bi, {orientation.sexual.none}");
                }
                if (orientation.asexual is null)
                {
                    LogUtil.Message($"Asexual orientation is null");
                }
                else
                {
                    LogUtil.Message($"Asexual orientation: {orientation.asexual.hetero} hetero, {orientation.asexual.homo} homo, {orientation.asexual.bi} bi, {orientation.asexual.none}");
                }
            }

            if (casualSex is null)
            {
                LogUtil.Message("Casual sex is null");
            }
            else
            {
                LogUtil.Message("========Casual sex settings========");
                LogUtil.Message($"Cares about cheating: {casualSex.caresAboutCheating}");
                LogUtil.Message($"Will do hookup: {casualSex.willDoHookup}");
                LogUtil.Message($"Can do ordered hookup: {casualSex.canDoOrderedHookup}");
                LogUtil.Message($"Hookup rate: {casualSex.hookupRate?.ToString() ?? "null"}");
                LogUtil.Message($"Alien love chance: {casualSex.alienLoveChance?.ToString() ?? "null"}");
                LogUtil.Message($"Min opinion for hookup: {casualSex.minOpinionForHookup?.ToString() ?? "null"}");
                LogUtil.Message($"Min opinion for ordered hookup: {casualSex.minOpinionForOrderedHookup?.ToString() ?? "null"}");
                LogUtil.Message($"For breeding only: {casualSex.forBreedingOnly}");
            }

            if (regularSex is null)
            {
                LogUtil.Message("Regular sex is null");
            }
            else
            {
                LogUtil.Message("========Regular sex settings========");
                LogUtil.Message($"Min age for sex: {regularSex.minAgeForSex}");
                LogUtil.Message($"Max age for sex: {regularSex.maxAgeForSex}");
                LogUtil.Message($"Max age gap: {regularSex.maxAgeGap}");
                LogUtil.Message($"Decline at age: {regularSex.declineAtAge}");
            }

            if (relations is null)
            {
                LogUtil.Message("Relations is null");
            }
            else
            {
                LogUtil.Message("========Relation settings========");
                LogUtil.Message($"Spouses allowed: {relations.spousesAllowed}");
                LogUtil.Message($"Children allowed: {relations.childrenAllowed}");
                LogUtil.Message($"PawnKind for parent, global: {relations.pawnKindForParentGlobal?.defName ?? "null"}");
                LogUtil.Message($"PawnKind for parent, female: {relations.pawnKindForParentFemale?.defName ?? "null"}");
                LogUtil.Message($"PawnKind for parent, male: {relations.pawnKindForParentMale?.defName ?? "null"}");
                LogUtil.Message($"Min female age to have children: {relations.minFemaleAgeToHaveChildren}");
                LogUtil.Message($"Usual female age to have children: {relations.usualFemaleAgeToHaveChildren}");
                LogUtil.Message($"Max female age to have children: {relations.maxFemaleAgeToHaveChildren}");
                LogUtil.Message($"Min male age to have children: {relations.minMaleAgeToHaveChildren}");
                LogUtil.Message($"Usual male age to have children: {relations.usualMaleAgeToHaveChildren}");
                LogUtil.Message($"Max male age to have children: {relations.maxMaleAgeToHaveChildren}");
                if (Settings.NonBinaryActive)
                {
                    LogUtil.Message($"Min non-binary age to have children: {relations.minEnbyAgeToHaveChildren}");
                    LogUtil.Message($"Usual non-binary age to have children: {relations.usualEnbyAgeToHaveChildren}");
                    LogUtil.Message($"Max non-binary age to have children: {relations.maxEnbyAgeToHaveChildren}");
                }
                LogUtil.Message($"Max children desired: {relations.maxChildrenDesired}");
                LogUtil.Message($"Min opinion for romance: {relations.minOpinionRomance?.ToString() ?? "null"}");
            }

            if (biotech is null)
            {
                LogUtil.Message("Biotech is null");
            }
            else
            {
                LogUtil.Message("========Biotech settings========");
                //Decide how to represent the curves
                string str = "";
                foreach (CurvePoint point in biotech.maleFertilityAgeFactor)
                {
                    str += $"\n({point.x}, {point.y})";
                }
                LogUtil.Message($"Male fertility age factor: {str}");

                str = "";
                foreach (CurvePoint point in biotech.femaleFertilityAgeFactor)
                {
                    str += $"\n({point.x}, {point.y})";
                }
                LogUtil.Message($"Female fertility age factor: {str}");

                str = "";
                foreach (CurvePoint point in biotech.noneFertilityAgeFactor)
                {
                    str += $"\n({point.x}, {point.y})";
                }
                LogUtil.Message($"None fertility age factor: {str}");

                str = "";
                foreach (CurvePoint point in biotech.ageEffectOnChildbirth)
                {
                    str += $"\n({point.x}, {point.y})";
                }
                LogUtil.Message($"Age effect on childbirth: {str}");

                LogUtil.Message($"Growth moment ages: {(NoGrowth ? "none" : biotech.growthMoments[0] + ", " + biotech.growthMoments[1] + ", " + biotech.growthMoments[2])}");
            }

            if (misc is null)
            {
                LogUtil.Message("Misc is null");
            }
            else
            {
                LogUtil.Message("========Misc settings========");
                LogUtil.Message($"Min age for adulthood: {misc.minAgeForAdulthood}");
                LogUtil.Message($"Child age: {misc.childAge}");
                LogUtil.Message($"Adult age for learning: {misc.adultAgeForLearning}");
                LogUtil.Message($"Age reversal demand age: {misc.ageReversalDemandAge}");

                string str = "";
                foreach (CurvePoint point in misc.ageSkillFactor)
                {
                    str += $"\n({point.x}, {point.y})";
                }
                LogUtil.Message($"Age skill factor: {str}");

                str = "";
                foreach (CurvePoint point in misc.ageSkillMaxFactorCurve)
                {
                    str += $"\n({point.x}, {point.y})";
                }
                LogUtil.Message($"Age skill max factor: {str}");

                str = "";
                foreach (CurvePoint point in misc.lovinCurve)
                {
                    str += $"\n({point.x}, {point.y})";
                }
                LogUtil.Message($"Lovin' MTB curve: {str}");
            }
        }
    }
}
