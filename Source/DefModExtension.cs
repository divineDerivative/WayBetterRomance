using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace BetterRomance
{
    public class SexualityChances : DefModExtension
    {
        public OrientationChances sexual;
        public OrientationChances asexual;

        public float asexualChance = -999f;
        public float bisexualChance = -999f;
        public float gayChance = -999f;
        public float straightChance = -999f;

        public float aromanticChance = -999f;
        public float biromanticChance = -999f;
        public float homoromanticChance = -999f;
        public float heteroromanticChance = -999f;

        public override IEnumerable<string> ConfigErrors()
        {
            //Convert old values to the new form
            if (sexual == null && (!asexualChance.IsUnset() || !bisexualChance.IsUnset() || !gayChance.IsUnset() || !straightChance.IsUnset()))
            {
                sexual = new()
                {
                    none = asexualChance,
                    bi = bisexualChance,
                    homo = gayChance,
                    hetero = straightChance,
                };
            }
            if (asexual == null && (!aromanticChance.IsUnset() || !biromanticChance.IsUnset() || !homoromanticChance.IsUnset() || !heteroromanticChance.IsUnset()))
            {
                asexual = new()
                {
                    none = aromanticChance,
                    bi = biromanticChance,
                    homo = homoromanticChance,
                    hetero = heteroromanticChance,
                };
            }
            if (sexual != null)
            {
                if (sexual.AreAnyUnset(out string list, false))
                {
                    yield return "Chances must be set for all sexual orientations. These are missing assignment:" + list;
                }
                if (!sexual.TotalCorrect)
                {
                    //Do math here to reset rates?
                    yield return "Sexuality chances must add up to 100";
                }
            }
            if (asexual != null)
            {
                if (asexual.AreAnyUnset(out string list, true))
                {
                    yield return "Chances must be set for all asexual romantic orientations. These are missing assignment:" + list;
                }
                if (!asexual.TotalCorrect)
                {
                    //Do math here to reset rates?
                    yield return "Asexual romantic orientation chances must add up to 100";
                }
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

        //These will all need changed
        public override IEnumerable<string> ConfigErrors()
        {
            if (hookupRate > 200f)
            {
                hookupRate = 200f;
                yield return "Hookup rate cannot be higher than 200";
            }
            if (alienLoveChance > 100f)
            {
                alienLoveChance = 100f;
                yield return "Alien love chance cannot be higher than 100";
            }
            if (hookupTriggers?.forBreedingOnly != null)
            {
                hookupTriggers.forBreedingOnly = false;
                yield return "forBreedingOnly is for ordered hookups only. Setting to false.";
            }
            if (hookupTriggers?.minOpinion is < -100 or > 100)
            {
                yield return "minOpinion for hookups must be between -100 and 100";
                hookupTriggers.minOpinion = Mathf.Clamp((int)hookupTriggers.minOpinion, -100, 100);
            }
            if (orderedHookupTriggers?.minOpinion is < -100 or > 100)
            {
                yield return "minOpinion for ordered hookups must be between -100 and 100";
                orderedHookupTriggers.minOpinion = Mathf.Clamp((int)orderedHookupTriggers.minOpinion, -100, 100);
            }
        }

        public CompSettingsCasualSexRace CopyToRace()
        {
            return new()
            {
                caresAboutCheating = caresAboutCheating,
                willDoHookup = willDoHookup,
                canDoOrderedHookup = canDoOrderedHookup,
                hookupRate = hookupRate,
                alienLoveChance = alienLoveChance,
                minOpinionForHookup = hookupTriggers?.minOpinion,
                minOpinionForOrderedHookup = orderedHookupTriggers?.minOpinion,
                forBreedingOnly = orderedHookupTriggers?.forBreedingOnly,
            };
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
        public float minAgeForSex = -999f;
        public float maxAgeForSex = -999f;
        public float maxAgeGap = -999f;
        public float declineAtAge = -999f;

        //Will need to redo the config errors to account for unassigned values
        public override IEnumerable<string> ConfigErrors()
        {
            if (!minAgeForSex.IsUnset())
            {
                if (minAgeForSex < 0)
                {
                    yield return "minAgeForSex must be a positive number";
                }
                if (!declineAtAge.IsUnset() && minAgeForSex > declineAtAge)
                {
                    yield return "minAgeForSex must be lower than declineAtAge";
                }
            }
            if (!maxAgeForSex.IsUnset())
            {
                if (maxAgeForSex < 0)
                {
                    yield return "maxAgeForSex must be a positive number";
                }
                if (!declineAtAge.IsUnset() && declineAtAge > maxAgeForSex)
                {
                    yield return "declineAtAge must be lower than maxAgeForSex";
                }
            }
            if (!declineAtAge.IsUnset() && declineAtAge < 0)
            {
                yield return "declineAtAge must be a positive number";
            }
            if (!maxAgeGap.IsUnset() && maxAgeGap < 0)
            {
                yield return "maxAgeGap must be a positive number";
            }
        }

        public CompSettingsRegularSex Copy()
        {
            return new()
            {
                minAgeForSex = minAgeForSex,
                maxAgeForSex = maxAgeForSex,
                maxAgeGap = maxAgeGap,
                declineAtAge = declineAtAge,
            };
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
        public float? minEnbyAgeToHaveChildren;
        public float? usualEnbyAgeToHaveChildren;
        public float? maxEnbyAgeToHaveChildren;
        public int? maxChildrenDesired;
        public int? minOpinionRomance;

        public override IEnumerable<string> ConfigErrors()
        {
            if (childrenAllowed == false)
            {
                if (pawnKindForParentGlobal == null && pawnKindForParentFemale == null && pawnKindForParentMale == null)
                {
                    yield return "Please provide valid pawnkind for parents";
                }
                //Remove this, so global can be provided as a fall back for non-binary
                //Shouldn't ever be used, but just in case
                //else if (pawnKindForParentGlobal != null)
                //{
                //    if (pawnKindForParentFemale != null || pawnKindForParentMale != null)
                //    {
                //        pawnKindForParentMale = null;
                //        pawnKindForParentFemale = null;
                //        yield return "Please provide only global or male and female pawnkinds; defaulting to global";
                //    }
                //}
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
            if (minFemaleAgeToHaveChildren > usualFemaleAgeToHaveChildren)
            {
                yield return "minFemaleAgeToHaveChildren must be lower than usualFemaleAgeToHaveChildren";
            }
            if (usualFemaleAgeToHaveChildren > maxFemaleAgeToHaveChildren)
            {
                yield return "usualFemaleAgeToHaveChildren must be lower than maxFemaleAgeToHaveChildren";
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
            if (minMaleAgeToHaveChildren > usualMaleAgeToHaveChildren)
            {
                yield return "minMaleAgeToHaveChildren must be lower than usualMaleAgeToHaveChildren";
            }
            if (usualMaleAgeToHaveChildren > maxMaleAgeToHaveChildren)
            {
                yield return "usualMaleAgeToHaveChildren must be lower than maxMaleAgeToHaveChildren";
            }

            if (maxChildrenDesired < 0)
            {
                yield return "maxChildrenDesired must be a positive number";
            }
            if (minOpinionRomance is > (int)100f or < (int)-100f)
            {
                yield return "Minimum opinion must be between 100 and -100";
                minOpinionRomance = Mathf.Clamp((int)minOpinionRomance, -100, 100);
            }
        }

        public CompSettingsRelationsRace CopyToRace()
        {
            return new()
            {
                spousesAllowed = spousesAllowed,
                childrenAllowed = childrenAllowed,
                pawnKindForParentGlobal = pawnKindForParentGlobal,
                pawnKindForParentFemale = pawnKindForParentFemale,
                pawnKindForParentMale = pawnKindForParentMale,
                minFemaleAgeToHaveChildren = minFemaleAgeToHaveChildren,
                usualFemaleAgeToHaveChildren = usualFemaleAgeToHaveChildren,
                maxFemaleAgeToHaveChildren = maxFemaleAgeToHaveChildren,
                minMaleAgeToHaveChildren = minMaleAgeToHaveChildren,
                usualMaleAgeToHaveChildren = usualMaleAgeToHaveChildren,
                maxMaleAgeToHaveChildren = maxMaleAgeToHaveChildren,
                maxChildrenDesired = maxChildrenDesired,
                minOpinionRomance = minOpinionRomance,
            };
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
        public SimpleCurve maleFertilityAgeFactor;
        public SimpleCurve femaleFertilityAgeFactor;
        public SimpleCurve noneFertilityAgeFactor;
        public SimpleCurve ageEffectOnChildbirth;

        public CompSettingsBiotech CopyToRace()
        {
            return new()
            {
                maleFertilityAgeFactor = maleFertilityAgeFactor,
                femaleFertilityAgeFactor = femaleFertilityAgeFactor,
                noneFertilityAgeFactor = noneFertilityAgeFactor,
                ageEffectOnChildbirth = ageEffectOnChildbirth,
            };
        }
    }
}
