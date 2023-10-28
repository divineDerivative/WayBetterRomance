using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BetterRomance
{
    //I think this needs to be changed so that nothing is nullable, and the field on the comp is simply null if nothing is provided
    //I'm worried about the possibility of a missing setting combining with user settings to result in chances that don't add up to 100
    public class CompSettingsOrientation
    {
        public UnifiedOrientationChances sexual;
        public UnifiedOrientationChances asexual;

        public CompSettingsOrientation Copy()
        {
            return new CompSettingsOrientation
            {
                sexual = sexual?.Copy,
                asexual = asexual?.Copy,
            };
        }
    }

    public class UnifiedOrientationChances
    {
        public float hetero = -999f;
        public float homo = -999f;
        public float bi = -999f;
        public float none = -999f;

        public UnifiedOrientationChances Copy => (UnifiedOrientationChances)this.MemberwiseClone();

        private bool IsUnset(float? value) => value == 999f;
        public bool AreAnyUnset(out string list, bool asexual = false)
        {
            //Need to make these strings match the old variable names I think
            list = "";
            bool result = false;
            if (IsUnset(hetero))
            {
                list += asexual ? " aceHeteroChance" : " straightChance";
                result = true;
            }
            if (IsUnset(homo))
            {
                list += " homo";
                result = true;
            }
            if (IsUnset(bi))
            {
                list += " bi";
                result = true;
            }
            if (IsUnset(none))
            {
                list += " none";
                result = true;
            }
            return result;
        }
        public bool TotalCorrect => hetero + homo + bi + none == 100f;
    }

    public class CompSettingsCasualSexRace
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

        public CompSettingsCasualSexPawn CopyToPawn()
        {
            return new CompSettingsCasualSexPawn
            {
                hookupRate = hookupRate,
                alienLoveChance = alienLoveChance,
                minOpinionForHookup = minOpinionForHookup,
                minOpinionForOrderedHookup = minOpinionForOrderedHookup
            };
        }
    }

    public class CompSettingsCasualSexPawn
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

    public class CompSettingsRegularSex
    {
        public float minAgeForSex;
        public float maxAgeForSex;
        public float maxAgeGap;
        public float declineAtAge;
        public CompSettingsRegularSex Copy()
        {
            return (CompSettingsRegularSex)this.MemberwiseClone();
        }
    }

    public class CompSettingsRelationsRace
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

        public CompSettingsRelationsPawn CopyToPawn()
        {
            return new CompSettingsRelationsPawn
            {
                pawnKindForParentGlobal = pawnKindForParentGlobal,
                pawnKindForParentFemale = pawnKindForParentFemale,
                pawnKindForParentMale = pawnKindForParentMale,
                minOpinionRomance = minOpinionRomance,
            };
        }
    }

    public class CompSettingsRelationsPawn
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

    public class CompSettingsBiotech
    {
        public SimpleCurve maleFertilityAgeFactor;
        public SimpleCurve femaleFertilityAgeFactor;
        public SimpleCurve noneFertilityAgeFactor;
        public SimpleCurve ageEffectOnChildbirth;
        public int[] growthMoments;

        public CompSettingsBiotech Copy()
        {
            return (CompSettingsBiotech)this.MemberwiseClone();
        }
    }

    public class CompSettingsMisc
    {
        public float minAgeForAdulthood = -1f;
        public int childAge;
        public int adultAgeForLearning;
        public int ageReversalDemandAge;
        public SimpleCurve ageSkillFactor;
        public SimpleCurve ageSkillMaxFactorCurve;

        public CompSettingsMisc Copy()
        {
            return (CompSettingsMisc)this.MemberwiseClone();
        }
    }
}
