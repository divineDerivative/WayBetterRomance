using Verse;

namespace BetterRomance
{
    public class CompSettingsOrientation
    {
        public OrientationChances sexual;
        public OrientationChances asexual;

        public CompSettingsOrientation Copy()
        {
            return new()
            {
                sexual = sexual?.Copy,
                asexual = asexual?.Copy,
            };
        }
    }

    public class OrientationChances
    {
        public float hetero = -999f;
        public float homo = -999f;
        public float bi = -999f;
        public float none = -999f;
        public float enby = 10f;

        //Use these to roll against Rand.Value
        public float Hetero => hetero / 100f;
        public float Homo => homo / 100f;
        public float Bi => bi / 100f;
        public float None => none / 100f;

        public OrientationChances Copy => (OrientationChances)MemberwiseClone();

        public void CopyFrom(OrientationChances other)
        {
            hetero = other.hetero;
            homo = other.homo;
            bi = other.bi;
            none = other.none;
            enby = other.enby;
        }

        public bool AreAnyUnset(out string list, bool asexual)
        {
            //Need to make these strings match the old variable names I think
            list = "";
            bool result = false;
            if (hetero.IsUnset())
            {
                list += asexual ? " heteroromanticChance" : " straightChance";
                result = true;
            }
            if (homo.IsUnset())
            {
                list += asexual ? " homoromanticChance" : " gayChance";
                result = true;
            }
            if (bi.IsUnset())
            {
                list += asexual ? " biromanticChance" : " bisexualChance"; ;
                result = true;
            }
            if (none.IsUnset())
            {
                list += asexual ? " aromanticChance" : " asexualChance"; ;
                result = true;
            }
            return result;
        }

        public bool TotalCorrect => hetero + homo + bi + none == 100f;
        public void Reset()
        {
            none = 10f;
            bi = 50f;
            homo = 20f;
            hetero = 20f;
            enby = 10f;
        }
    }

    public class GenderAttractionChances
    {
        public float men = 0.5f;
        public float women = 0.5f;
        public float enby = 0.25f;

        public float notMen => 1f - men;
        public float notWomen => 1f - women;
        public float notEnby => 1f- enby;

        public void CopyFrom(GenderAttractionChances other)
        {
            men = other.men;
            women = other.women;
            enby = other.enby;
        }

        public void Reset()
        {
            men = 0.5f;
            women = 0.5f;
            enby = 0.25f;
        }
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
            return new()
            {
                hookupRate = hookupRate,
                alienLoveChance = alienLoveChance,
                minOpinionForHookup = minOpinionForHookup,
                minOpinionForOrderedHookup = minOpinionForOrderedHookup,
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

        public CompSettingsCasualSexPawn Copy() => (CompSettingsCasualSexPawn)MemberwiseClone();
    }

    public class CompSettingsRegularSex
    {
        public float minAgeForSex = -999f;
        public float maxAgeForSex = -999f;
        public float maxAgeGap = -999f;
        public float declineAtAge = -999f;

        public CompSettingsRegularSex Copy() => (CompSettingsRegularSex)MemberwiseClone();

        public CompSettingsRegularSex Default()
        {
            return new()
            {
                minAgeForSex = 16f,
                maxAgeForSex = 80f,
                maxAgeGap = 40f,
                declineAtAge = 30f,
            };
        }

        public override bool Equals(object obj) => obj is CompSettingsRegularSex sex && minAgeForSex == sex.minAgeForSex && maxAgeForSex == sex.maxAgeForSex && maxAgeGap == sex.maxAgeGap && declineAtAge == sex.declineAtAge;

        public bool IsEmpty() => minAgeForSex.IsUnset() && maxAgeForSex.IsUnset() && maxAgeGap.IsUnset() && declineAtAge.IsUnset();

        public static bool operator ==(CompSettingsRegularSex left, CompSettingsRegularSex right) => left.minAgeForSex == right.minAgeForSex && left.maxAgeForSex == right.maxAgeForSex && left.maxAgeGap == right.maxAgeGap && left.declineAtAge == right.declineAtAge;
        public static bool operator !=(CompSettingsRegularSex left, CompSettingsRegularSex right) => left.minAgeForSex != right.minAgeForSex || left.maxAgeForSex != right.maxAgeForSex || left.maxAgeGap != right.maxAgeGap || left.declineAtAge != right.declineAtAge;
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

        public float? minEnbyAgeToHaveChildren;
        public float? usualEnbyAgeToHaveChildren;
        public float? maxEnbyAgeToHaveChildren;

        public int? maxChildrenDesired;
        public int? minOpinionRomance;

        public CompSettingsRelationsPawn CopyToPawn()
        {
            return new()
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

        public float minEnbyAgeToHaveChildren;
        public float usualEnbyAgeToHaveChildren;
        public float maxEnbyAgeToHaveChildren;

        public int maxChildrenDesired;
        public int? minOpinionRomance;

        public CompSettingsRelationsPawn Copy() => (CompSettingsRelationsPawn)MemberwiseClone();
    }

    public class CompSettingsBiotech
    {
        public SimpleCurve maleFertilityAgeFactor;
        public SimpleCurve femaleFertilityAgeFactor;
        public SimpleCurve noneFertilityAgeFactor;
        public SimpleCurve ageEffectOnChildbirth;
        public int[] growthMoments;

        public CompSettingsBiotech Copy() => (CompSettingsBiotech)MemberwiseClone();
    }

    public class CompSettingsMisc
    {
        public float minAgeForAdulthood = -1f;
        public int childAge;
        public int adultAgeForLearning;
        public int ageReversalDemandAge;
        public SimpleCurve ageSkillFactor;
        public SimpleCurve ageSkillMaxFactorCurve;
        public SimpleCurve lovinCurve;

        public CompSettingsMisc Copy() => (CompSettingsMisc)MemberwiseClone();
    }
}
