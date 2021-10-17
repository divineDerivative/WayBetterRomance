using System;
using System.Collections.Generic;
using Verse;
using RimWorld;

namespace BetterRomance
{
    public class SexualityChances : DefModExtension
    {
        public float asexualChance;
        public float bisexualChance;
        public float gayChance;
        public float straightChance;

        public override IEnumerable<string> ConfigErrors()
        {
            if (asexualChance + bisexualChance + gayChance + straightChance > 100.99)
            {
                //Do math here to reset rates?
                yield return "Sexuality chances cannot total more than 100";
            }
        }
    }

    public class CasualSexSettings : DefModExtension
    {
        public bool caresAboutCheating = true;
        public bool willDoHookup = true;
        public float hookupRate = BetterRomanceMod.settings.hookupRate;
        public float alienLoveChance = BetterRomanceMod.settings.alienLoveChance;
        public HookupTrigger hookupTriggers;
        public HookupTrigger orderedHookupTriggers;
        public override IEnumerable<string> ConfigErrors()
        {
            if (hookupRate > 1000.99)
            {
                hookupRate = 1000.99f;
                yield return "Hookup rate cannot be higher than 1000";
            }
            if (alienLoveChance > 100.99)
            {
                alienLoveChance = 100.99f;
                yield return "Alien love chance cannot be higher than 100";
            }
        }
    }

    public class HookupTrigger
    {
        public int minOpinion = -5;
        public TraitDef hasTrait = null;
        public bool mustBeFertile = false;
    }

    public class RegularSexSettings : DefModExtension
    {
        public float minAgeForSex = 16f;
        public float maxAgeForSex = 80f;
        public float maxAgeGap = 40f;
        public float declineAtAge = 30f;

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
                yield return "minAgeForSex must be a positve number";
            }
            if (maxAgeForSex < 0)
            {
                yield return "maxAgeForSex must be a positve number";
            }
            if (maxAgeGap < 0)
            {
                yield return "maxAgeGap must be a positve number";
            }
            if (declineAtAge < 0)
            {
                yield return "declineAtAge must be a positve number";
            }
        }
    }

    public class RelationSettings : DefModExtension
    {
        public bool spousesAllowed = true;
        public bool childrenAllowed = true;
        public PawnKindDef pawnKindForParentGlobal;
        public PawnKindDef pawnKindForParentFemale;
        public PawnKindDef pawnKindForParentMale;
        public float minFemaleAgeToHaveChildren = 16f;
        public float usualFemaleAgeToHaveChildren = 27f;
        public float maxFemaleAgeToHaveChildren = 45f;
        public float minMaleAgeToHaveChildren = 14f;
        public float usualMaleAgeToHaveChildren = 30f;
        public float maxMaleAgeToHaveChildren = 50f;
        public int maxChildrenDesired = 3;
        public float minOpinionRomance = 5f;

        public override IEnumerable<string> ConfigErrors()
        {
            if (!childrenAllowed)
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
        public bool isLoveRelation = false;
        public bool shouldBreakForNewLover = true;
        public PawnRelationDef exLoveRelation;
    }
}