using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;
using Verse;

namespace BetterRomance.HarmonyPatches
{
    //This determines the chance of generating a love relation, it is called by most relation workers
    [HarmonyPatch(typeof(LovePartnerRelationUtility), nameof(LovePartnerRelationUtility.LovePartnerRelationGenerationChance))]
    public static class LovePartnerRelationUtility_LovePartnerRelationGenerationChance
    {
        //Changes From Vanilla:
        //Updated with new orientation options
        //Accounts for age settings
        public static bool Prefix(Pawn generated, Pawn other, PawnGenerationRequest request, bool ex, ref float __result)
        {
            //Do not allow if either pawn is not old enough for sex
            if (generated.ageTracker.AgeBiologicalYearsFloat < generated.MinAgeForSex() || other.ageTracker.AgeBiologicalYearsFloat < other.MinAgeForSex())
            {
                __result = 0f;
                return false;
            }
            //Leaving this in for cases where the request specifically states gay is not allowed
            if (generated.gender == other.gender && !request.AllowGay)
            {
                __result = 0f;
                return false;
            }
            //Don't generate relations for pawns that grew up in the colony
            if (ModsConfig.BiotechActive)
            {
                if (generated?.records != null && generated.records.GetValue(RecordDefOf.TimeAsChildInColony) > 0f)
                {
                    __result = 0f;
                    return false;
                }
                if (other?.records != null && other.records.GetValue(RecordDefOf.TimeAsChildInColony) > 0f)
                {
                    __result = 0f;
                    return false;
                }
            }
            //Removing wrong orientation matches for randomly generated relationships, since this is used for both spouses and lovers
            //Do not allow if gender and sexuality do not match
            float sexualityFactor = 1f;
            if (generated.IsHumanlike())
            {
                if (generated.IsAsexual())
                {
                    sexualityFactor = generated.AsexualRating();
                }
                //Separate now that asexuality is a spectrum
                if (generated.IsAro())
                {
                    __result = 0f;
                    return false;
                }
                else if (generated.IsHomo())
                {
                    if (other.gender != generated.gender)
                    {
                        __result = 0f;
                        return false;
                    }
                }
                else if (generated.IsHetero())
                {
                    if (other.gender == generated.gender)
                    {
                        __result = 0f;
                        return false;
                    }
                }
            }
            float exFactor = 1f;
            //Reduce chances of generating an ex relation for each existing ex relation
            if (ex)
            {
                int num = 0;
                List<DirectPawnRelation> directRelations = other.relations.DirectRelations;
                foreach (DirectPawnRelation directPawnRelation in directRelations)
                {
                    if (LovePartnerRelationUtility.IsExLovePartnerRelation(directPawnRelation.def))
                    {
                        num++;
                    }
                }
                exFactor = Mathf.Pow(0.2f, num);
            }
            //If we're not generating an ex relation, and other already has a partner, do not allow
            else if (LovePartnerRelationUtility.HasAnyLovePartner(other))
            {
                __result = 0f;
                return false;
            }
            //Bunch of age calculations
            float generatedAgeFactor = (float)AccessTools.Method(typeof(LovePartnerRelationUtility), "GetGenerationChanceAgeFactor").Invoke(null, [generated]);
            float otherAgeFactor = (float)AccessTools.Method(typeof(LovePartnerRelationUtility), "GetGenerationChanceAgeFactor").Invoke(null, [other]);
            float ageGapFactor = (float)AccessTools.Method(typeof(LovePartnerRelationUtility), "GetGenerationChanceAgeGapFactor").Invoke(null, [generated, other, ex]);
            float existingFamilyFactor = 1f;
            //This reduces chances if they're already related by blood
            if (generated.GetRelations(other).Any(x => x.familyByBloodRelation))
            {
                existingFamilyFactor = 0.01f;
            }

            __result = exFactor * generatedAgeFactor * otherAgeFactor * ageGapFactor * existingFamilyFactor * sexualityFactor;
            return false;
        }
    }

    //Called by LovePartnerRelationGenerationChance
    [HarmonyPatch(typeof(LovePartnerRelationUtility), "GetGenerationChanceAgeFactor")]
    public static class LovePartnerRelationUtility_GetGenerationChanceAgeFactor
    {
        /// <summary>
        /// Determines chances to generate a love relation based on age.
        /// </summary>
        /// <param name="p">The pawn to check</param>
        /// <returns>A float between 0 and 1</returns>
        public static bool Prefix(Pawn p, ref float __result)
        {
            float minAgeForSex = p.MinAgeForSex();
            float usualAge = p.UsualAgeToHaveChildren();
            float num = GenMath.LerpDouble(minAgeForSex - 2f, usualAge, 0f, 1f, p.ageTracker.AgeBiologicalYearsFloat);
            __result = Mathf.Clamp(num, 0f, 1f);
            return false;
        }
    }

    [HarmonyPatch(typeof(LovePartnerRelationUtility), "GetGenerationChanceAgeGapFactor")]
    public static class LovePartnerRelationUtility_GetGenerationChanceAgeGapFactor
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
        {
            LocalBuilder local_maxAgeGap = ilg.DeclareLocal(typeof(float));
            foreach (CodeInstruction code in instructions)
            {
                if (code.LoadsConstant(40f))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.MaxAgeGap));
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.MaxAgeGap));
                    yield return CodeInstruction.Call(typeof(Mathf), nameof(Mathf.Min), parameters: [typeof(float), typeof(float)]);
                    //need to store this value somewhere
                    yield return new CodeInstruction(OpCodes.Stloc, local_maxAgeGap.LocalIndex);
                    yield return new CodeInstruction(OpCodes.Ldloc, local_maxAgeGap.LocalIndex);
                }
                else if (code.LoadsConstant(20f))
                {
                    //put the stored value on the stack
                    yield return new CodeInstruction(OpCodes.Ldloc, local_maxAgeGap.LocalIndex);
                    yield return new CodeInstruction(OpCodes.Ldc_R4, operand: 2f);
                    yield return new CodeInstruction(OpCodes.Div);
                }
                else if (code.LoadsConstant(0.001f))
                {
                    yield return new CodeInstruction(OpCodes.Ldc_R4, operand: 0.01f);
                }
                else
                {
                    yield return code;
                }
            }
        }
    }

    [HarmonyPatch(typeof(LovePartnerRelationUtility), "MinPossibleAgeGapAtMinAgeToGenerateAsLovers")]
    public static class LovePartnerRelationUtility_MinPossibleAgeGapAtMinAgeToGenerateAsLovers
    {
        //I don't really understand what this does, but it's updated to use proper ages
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
        {
            bool firstFound = false;
            LocalBuilder local_p2MinAgeForSex = ilg.DeclareLocal(typeof(float));

            //Calculate and store p2MinAgeForSex first so we can call it easier later
            yield return new CodeInstruction(OpCodes.Ldarg_1);
            yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.MinAgeForSex));
            yield return new CodeInstruction(OpCodes.Stloc, local_p2MinAgeForSex.LocalIndex);

            foreach (CodeInstruction code in instructions)
            {
                if (code.LoadsConstant(14f) && !firstFound)
                {
                    firstFound = true;

                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.MinAgeForSex));
                }
                else if (code.LoadsConstant(14f))
                {
                    yield return new CodeInstruction(OpCodes.Ldloc, local_p2MinAgeForSex.LocalIndex);
                }
                else
                {
                    yield return code;
                }
            }
        }
    }

    //Determines how often a pawn would want to have sex only considering factors on them
    [HarmonyPatch(typeof(LovePartnerRelationUtility), "LovinMtbSinglePawnFactor")]
    public static class LovePartnerRelationUtility_LovinMtbSinglePawnFactor
    {
        //Changes from vanilla:
        //Age adjustments
        //No adjustment made for asexual pawns as that is handled elsewhere
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            IEnumerable<CodeInstruction> codes = instructions.RegularSexAges(OpCodes.Ldarg_0);
            foreach (CodeInstruction code in codes)
            {
                if (code.LoadsConstant(14f))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.MinAgeForSex));
                    yield return new CodeInstruction(OpCodes.Ldc_R4, operand: 2f);
                    yield return new CodeInstruction(OpCodes.Sub);
                }
                else
                {
                    yield return code;
                }
            }
        }
    }

    //If no vanilla love relations were found, checks custom ones
    [HarmonyPatch(typeof(LovePartnerRelationUtility), nameof(LovePartnerRelationUtility.IsLovePartnerRelation))]
    public static class LovePartnerRelationUtility_IsLovePartnerRelation
    {
        public static void Postfix(PawnRelationDef relation, ref bool __result)
        {
            if (!__result && Settings.LoveRelationsLoaded)
            {
                __result = CustomLoveRelationUtility.LoveRelations.Contains(relation);
            }
        }
    }

    //If no vanilla ex love relations were found, checks custom ones
    [HarmonyPatch(typeof(LovePartnerRelationUtility), nameof(LovePartnerRelationUtility.IsExLovePartnerRelation))]
    public static class LovePartnerRelationUtility_IsExLovePartnerRelation
    {
        public static void Postfix(PawnRelationDef relation, ref bool __result)
        {
            if (!__result && !CustomLoveRelationUtility.ExLoveRelations.EnumerableNullOrEmpty())
            {
                __result = CustomLoveRelationUtility.ExLoveRelations.Contains(relation);
            }
        }
    }

    //If no vanilla love relations were found, checks custom ones
    [HarmonyPatch(typeof(LovePartnerRelationUtility), nameof(LovePartnerRelationUtility.LovePartnerRelationExists))]
    public static class LovePartnerRelationUtility_LovePartnerRelationExists
    {
        public static void Postfix(Pawn first, Pawn second, ref bool __result)
        {
            if (!__result)
            {
                __result = CustomLoveRelationUtility.CheckCustomLoveRelations(first, second) != null;
            }
        }
    }

    //If no vanilla ex love relations were found, checks custom ones
    [HarmonyPatch(typeof(LovePartnerRelationUtility), nameof(LovePartnerRelationUtility.ExLovePartnerRelationExists))]
    public static class LovePartnerRelationUtility_ExLovePartnerRelationExists
    {
        public static void Postfix(Pawn first, Pawn second, ref bool __result)
        {
            if (!__result)
            {
                __result = CustomLoveRelationUtility.CheckCustomLoveRelations(first, second, true) != null;
            }
        }
    }

    //Removes possibility of ex spouse relation if settings do not allow spouses for either pawn
    [HarmonyPatch(typeof(LovePartnerRelationUtility), nameof(LovePartnerRelationUtility.GiveRandomExLoverOrExSpouseRelation))]
    public static class LovePartnerRelationUtility_GiveRandomExLoverOrExSpouseRelation
    {
        public static bool Prefix(Pawn first, Pawn second)
        {
            if (!first.SpouseAllowed() || !second.SpouseAllowed() || !first.CouldWeBeMarried(second))
            {
                first.relations.AddDirectRelation(PawnRelationDefOf.ExLover, second);
                return false;
            }
            return true;
        }
    }

    //Simply skips this method entirely if children are not allowed for the generated pawn
    [HarmonyPatch(typeof(LovePartnerRelationUtility), nameof(LovePartnerRelationUtility.TryToShareChildrenForGeneratedLovePartner))]
    public static class LovePartnerRelationUtility_TryToShareChildrenForGeneratedLovePartner
    {
        public static bool Prefix(Pawn generated)
        {
            return generated.ChildAllowed();
        }
    }
}