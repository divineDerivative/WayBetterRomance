using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace BetterRomance.HarmonyPatches
{

    //This determines the chance for a given pawn to become the child of two other pawns
    [HarmonyPatch(typeof(ChildRelationUtility), "ChanceOfBecomingChildOf")]
    public static class ChildRelationUtility_ChanceOfBecomingChildOf
    {
        // CHANGE: Removed bias against gays being assigned as parents.
        public static bool Prefix(Pawn child, Pawn father, Pawn mother, PawnGenerationRequest? childGenerationRequest, PawnGenerationRequest? fatherGenerationRequest, PawnGenerationRequest? motherGenerationRequest, ref float __result)
        {
            //Check if settings allow children
            if ((father != null && !father.ChildAllowed()) || (mother != null && !mother.ChildAllowed()))
            {
                __result = 0f;
                return false;
            }
            //Make sure parent genders are correct
            if (father != null && father.gender != Gender.Male)
            {
                Log.Warning(string.Concat("Tried to calculate chance for father with gender \"", father.gender, "\"."));
                __result = 0f;
                return false;
            }
            if (mother != null && mother.gender != Gender.Female)
            {
                Log.Warning(string.Concat("Tried to calculate chance for mother with gender \"", mother.gender, "\"."));
                __result = 0f;
                return false;
            }
            //If the child already has a parent that does not match one being considered, do not allow
            if (father != null && child.GetFather() != null && child.GetFather() != father)
            {
                __result = 0f;
                return false;
            }
            if (mother != null && child.GetMother() != null && child.GetMother() != mother)
            {
                __result = 0f;
                return false;
            }
            //If both parents are provided, and have never been lovers, do not allow
            if (mother != null && father != null &&
                !LovePartnerRelationUtility.LovePartnerRelationExists(mother, father) && !LovePartnerRelationUtility.ExLovePartnerRelationExists(mother, father))
            {
                __result = 0f;
                return false;
            }
            if (mother != null && !ChildRelationUtility.XenotypesCompatible(child, mother))
            {
                __result = 0f;
                return false;
            }
            if (father != null && !ChildRelationUtility.XenotypesCompatible(child, father))
            {
                __result = 0f;
                return false;
            }
            //This prevents spawning children if the potential parent grew up in the colony
            if (ModsConfig.BiotechActive)
            {
                if (father?.records != null && father.records.GetValue(RecordDefOf.TimeAsChildInColony) > 0f)
                {
                    __result = 0f;
                    return false;
                }
                if (mother?.records != null && mother.records.GetValue(RecordDefOf.TimeAsChildInColony) > 0f)
                {
                    __result = 0f;
                    return false;
                }
            }
            //Calculations based on age of parents compared to child
            float fatherAgeFactor = 1f;
            float motherAgeFactor = 1f;
            float childrenCountFactor = 1f;
            if (father != null && child.GetFather() == null)
            {
                fatherAgeFactor = (float)AccessTools.Method(typeof(ChildRelationUtility), "GetParentAgeFactor").Invoke(null, new object[] { father, child, father.MinAgeToHaveChildren(), father.UsualAgeToHaveChildren(), father.MaxAgeToHaveChildren() });
                if (fatherAgeFactor == 0f)
                {
                    __result = 0f;
                    return false;
                }
            }
            if (mother != null && child.GetMother() == null)
            {
                motherAgeFactor = (float)AccessTools.Method(typeof(ChildRelationUtility), "GetParentAgeFactor").Invoke(null, new object[] { mother, child, mother.MinAgeToHaveChildren(), mother.UsualAgeToHaveChildren(), mother.MaxAgeToHaveChildren() });
                if (motherAgeFactor == 0f)
                {
                    __result = 0f;
                    return false;
                }

                int maxChildren = NumberOfChildrenFemaleWantsEver(mother);
                if (mother.relations.ChildrenCount >= maxChildren)
                {
                    __result = 0f;
                    return false;
                }

                childrenCountFactor = 1f - mother.relations.ChildrenCount / (float)maxChildren;
            }
            //Lower chances if one parent already has a spouse that is not the other parent
            float curRelationFactor = 1f;
            Pawn motherSpouse = mother?.relations.GetFirstDirectRelationPawn(PawnRelationDefOf.Spouse);
            if (motherSpouse != null && motherSpouse != father)
            {
                curRelationFactor *= 0.15f;
            }
            Pawn fatherSpouse = father?.relations.GetFirstDirectRelationPawn(PawnRelationDefOf.Spouse);
            if (fatherSpouse != null && fatherSpouse != mother)
            {
                curRelationFactor *= 0.15f;
            }

            __result = fatherAgeFactor * motherAgeFactor * childrenCountFactor * curRelationFactor;
            return false;
        }

        //This is a copy from base game, since it's private, only gets called by the above, and needs to be changed
        /// <summary>
        /// Determines max children "randomly". Seed is based on pawn's ID, so it will always return the same number for a given pawn.
        /// </summary>
        /// <param name="female"></param>
        /// <returns>An int between 0 and max children setting</returns>
        private static int NumberOfChildrenFemaleWantsEver(Pawn female)
        {
            Rand.PushState();
            Rand.Seed = female.thingIDNumber * 3;
            int result = Rand.RangeInclusive(0, female.MaxChildren());
            Rand.PopState();
            return result;
        }
    }
}