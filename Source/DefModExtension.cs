using System;
using System.Collections.Generic;
using Verse;
using RimWorld;
using UnityEngine;

namespace BetterRomance
{
    public class SexualityChances : DefModExtension
    {
        public float? asexualChance;
        public float? bisexualChance;
        public float? gayChance;
        public float? straightChance;

        public float? aceAroChance;
        public float? aceBiChance;
        public float? aceHomoChance;
        public float? aceHeteroChance;

        public override IEnumerable<string> ConfigErrors()
        {
            if (asexualChance + bisexualChance + gayChance + straightChance != 100f)
            {
                //Do math here to reset rates?
                yield return "Sexuality chances must add up to 100";
            }
            //If not set, default to orientation chances
            if (asexualChance != 0f && aceAroChance + aceBiChance + aceHomoChance + aceHeteroChance == 0f)
            {
                LogUtil.Warning("Romantic orientation chances for asexual pawns not found. Defaulting to match sexual orientation chances.");
                aceAroChance = asexualChance;
                aceBiChance = bisexualChance;
                aceHomoChance = gayChance;
                aceHeteroChance = straightChance;
            }
            if (aceAroChance + aceBiChance + aceHomoChance + aceHeteroChance != 100f)
            {
                //Do math here to reset rates?
                yield return "Asexual romantic orientation chances must add up to 100";
            }
        }
    }

    public class CasualSexSettings : DefModExtension
    {
        public bool? caresAboutCheating;
        public bool? willDoHookup;
        public bool? canDoOrderedHookup;
        public float? hookupRate;
        public float? alienLoveChance;
        public HookupTrigger hookupTriggers;
        public HookupTrigger orderedHookupTriggers;
        public override IEnumerable<string> ConfigErrors()
        {
            if (hookupRate == -1)
            {
                hookupRate = BetterRomanceMod.settings.hookupRate;
            }
            else if (hookupRate > 200.99)
            {
                hookupRate = 200.99f;
                yield return "Hookup rate cannot be higher than 200";
            }
            if (alienLoveChance == -1)
            {
                alienLoveChance = BetterRomanceMod.settings.alienLoveChance;
            }
            else if (alienLoveChance > 100.99)
            {
                alienLoveChance = 100.99f;
                yield return "Alien love chance cannot be higher than 100";
            }
            if (hookupTriggers != null && (hookupTriggers.forBreedingOnly ?? false))
            {
                hookupTriggers.forBreedingOnly = false;
                yield return "forBreedingOnly is for ordered hookups only. Setting to false.";
            }
            if (hookupTriggers != null && hookupTriggers.minOpinion != null)
            {
                if (hookupTriggers.minOpinion < -100 || hookupTriggers.minOpinion > 100)
                {
                    yield return "minOpinion for hookups must be between -100 and 100";
                    hookupTriggers.minOpinion = Mathf.Clamp((int)hookupTriggers.minOpinion, -100, 100);
                }
            }
            if (orderedHookupTriggers != null && orderedHookupTriggers.minOpinion != null)
            {
                if (orderedHookupTriggers.minOpinion < -100 || orderedHookupTriggers.minOpinion > 100)
                {
                    yield return "minOpinion for ordered hookups must be between -100 and 100";
                    orderedHookupTriggers.minOpinion = Mathf.Clamp((int)orderedHookupTriggers.minOpinion, -100, 100);
                }
            }
        }
    }

    public class HookupTrigger
    {
        public int? minOpinion;
        public bool? forBreedingOnly;
    }

    public class HookupTrait : DefModExtension
    {
        public List<ThingDef> races;
        public List<PawnKindDef> pawnkinds;
        public List<int> degrees;

        public override IEnumerable<string> ConfigErrors()
        {
            if (races.NullOrEmpty() && pawnkinds.NullOrEmpty())
            {
                yield return "Must provide either a race or pawnkind that trait requirement should apply to.";
            }
        }
    }

    public class RegularSexSettings : DefModExtension
    {
        public float? minAgeForSex;
        public float? maxAgeForSex;
        public float? maxAgeGap;
        public float? declineAtAge;

        public override IEnumerable<string> ConfigErrors()
        {
            if (minAgeForSex > declineAtAge)
            {
                yield return "minAgeForSex must be lower than declineAtAge";
            }
            if (declineAtAge > maxAgeForSex)
            {
                yield return "declineAtAge must be lower than maxAgeForSex";
            }
            if (minAgeForSex < 0)
            {
                yield return "minAgeForSex must be a positive number";
            }
            if (maxAgeForSex < 0)
            {
                yield return "maxAgeForSex must be a positive number";
            }
            if (maxAgeGap < 0)
            {
                yield return "maxAgeGap must be a positive number";
            }
            if (declineAtAge < 0)
            {
                yield return "declineAtAge must be a positive number";
            }
        }
    }

    public class RelationSettings : DefModExtension
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

        public override IEnumerable<string> ConfigErrors()
        {
            if (childrenAllowed != null && childrenAllowed == true)
            {
                if (pawnKindForParentGlobal == null && pawnKindForParentFemale == null && pawnKindForParentMale == null)
                {
                    yield return "Please provide valid pawnkind for parents";
                }
                else if (pawnKindForParentGlobal != null)
                {
                    if (pawnKindForParentFemale != null || pawnKindForParentMale != null)
                    {
                        pawnKindForParentMale = null;
                        pawnKindForParentFemale = null;
                        yield return "Please provide only global or male and female pawnkinds; defaulting to global";
                    }
                }
                else if (pawnKindForParentFemale == null || pawnKindForParentMale == null)
                {
                    yield return "Please provide both a male and female pawnkind";
                }
            }
            if (minFemaleAgeToHaveChildren < 0)
            {
                yield return "minFemaleAgeToHaveChildren must be a positive number";
            }
            if (usualFemaleAgeToHaveChildren < 0)
            {
                yield return "usualFemaleAgeToHaveChildren must be a positive number";
            }
            if (maxFemaleAgeToHaveChildren < 0)
            {
                yield return "maxFemaleAgeToHaveChildren must be a positive number";
            }
            if (minMaleAgeToHaveChildren < 0)
            {
                yield return "minMaleAgeToHaveChildren must be a positive number";
            }
            if (usualMaleAgeToHaveChildren < 0)
            {
                yield return "usualMaleAgeToHaveChildren must be a positive number";
            }
            if (maxMaleAgeToHaveChildren < 0)
            {
                yield return "maxMaleAgeToHaveChildren must be a positive number";
            }
            if (maxChildrenDesired < 0)
            {
                yield return "maxChildrenDesired must be a positive number";
            }
            if (minFemaleAgeToHaveChildren > usualFemaleAgeToHaveChildren)
            {
                yield return "minFemaleAgeToHaveChildren must be lower than usualFemaleAgeToHaveChildren";
            }
            if (usualFemaleAgeToHaveChildren > maxFemaleAgeToHaveChildren)
            {
                yield return "usualFemaleAgeToHaveChildren must be lower than maxFemaleAgeToHaveChildren";
            }
            if (minMaleAgeToHaveChildren > usualMaleAgeToHaveChildren)
            {
                yield return "minMaleAgeToHaveChildren must be lower than usualMaleAgeToHaveChildren";
            }
            if (usualMaleAgeToHaveChildren > maxMaleAgeToHaveChildren)
            {
                yield return "usualMaleAgeToHaveChildren must be lower than maxMaleAgeToHaveChildren";
            }
            if (minOpinionRomance > 100.99f || minOpinionRomance < -100.99f)
            {
                yield return "Minimum opinion must be between 100 and -100";
            }
        }
    }

    public class LoveRelations : DefModExtension
    {
        public bool isLoveRelation = true;
        public bool shouldBreakForNewLover = true;
        public PawnRelationDef exLoveRelation;
    }

    public class BiotechSettings : DefModExtension
    {
        public SimpleCurve maleFertilityAgeFactor = new SimpleCurve
        {
            new CurvePoint(14f, 0f),
            new CurvePoint(18f, 1f),
            new CurvePoint(50f, 1f),
            new CurvePoint(90f, 0f),
        };
        public SimpleCurve femaleFertilityAgeFactor = new SimpleCurve
        {
            new CurvePoint(14f, 0f),
            new CurvePoint(20f, 1f),
            new CurvePoint(28f, 1f),
            new CurvePoint(35f, 0.5f),
            new CurvePoint(40f, 0.1f),
            new CurvePoint(45f, 0.02f),
            new CurvePoint(50f, 0f),

        };
        public SimpleCurve noneFertilityAgeFactor = new SimpleCurve
        {
            new CurvePoint(14f, 0f),
            new CurvePoint(18f, 1f),
            new CurvePoint(50f, 1f),
            new CurvePoint(90f, 0f),
        };
        public SimpleCurve ageEffectOnChildbirth = new SimpleCurve
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