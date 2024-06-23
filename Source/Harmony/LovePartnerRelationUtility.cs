using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Verse;

namespace BetterRomance.HarmonyPatches
{
    //This determines the chance of generating a love relation, it is called by most relation workers
    [HarmonyPatch(typeof(LovePartnerRelationUtility), nameof(LovePartnerRelationUtility.LovePartnerRelationGenerationChance))]
    public static class LovePartnerRelationUtility_LovePartnerRelationGenerationChance
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
        {
            //Use age settings
            IEnumerable<CodeInstruction> codes = instructions.MinAgeForSexForTwo(OpCodes.Ldarg_0, OpCodes.Ldarg_1, 14f);

            MethodInfo BiotechActive = AccessTools.PropertyGetter(typeof(ModsConfig), nameof(ModsConfig.BiotechActive));
            Label firstLabel = ilg.DefineLabel();
            Label secondLabel = ilg.DefineLabel();
            int step = 0;
            foreach (CodeInstruction code in codes)
            {
                if (code.Calls(typeof(SettingsUtilities), nameof(SettingsUtilities.MinAgeForSex)))
                {
                    step++;
                }
                //Add our stuff right before the Biotech section
                if (code.Calls(BiotechActive))
                {
                    //Removing wrong orientation matches for randomly generated relationships, since this is used for both spouses and lovers
                    //if (!generated.CouldWeBeLovers(other)) { return 0f; }
                    yield return new CodeInstruction(OpCodes.Ldarg_0).MoveLabelsFrom(code).WithLabels(firstLabel);
                    code.labels.Add(secondLabel);
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return CodeInstruction.Call(typeof(SexualityUtility), nameof(SexualityUtility.CouldWeBeLovers));
                    yield return new CodeInstruction(OpCodes.Brtrue, secondLabel);
                    yield return new CodeInstruction(OpCodes.Ldc_R4, 0f);
                    yield return new CodeInstruction(OpCodes.Ret);
                }
                //Remove the gay reduction by storing 1f no matter what
                if (code.opcode == OpCodes.Stloc_1)
                {
                    yield return new CodeInstruction(OpCodes.Pop);
                    yield return new CodeInstruction(OpCodes.Ldc_R4, 1f);
                }

                yield return code;

                //Make this jump to my section, skipping the gay checks entirely
                if (step == 2 && code.Branches(out _))
                {
                    code.operand = firstLabel;
                    step++;
                }
            }
        }

        public static void Postfix(Pawn generated, Pawn other, ref float __result)
        {
            //Adjust with asexual rating
            float sexualityFactor = 1f;
            if (generated.IsAsexual())
            {
                sexualityFactor = generated.SexRepulsed(other) ? 0f : generated.AsexualRating();
            }
            __result *= sexualityFactor;
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
            IEnumerable<CodeInstruction> codes = instructions.MaxAgeGapTranspiler(ilg, OpCodes.Ldarg_0, OpCodes.Ldarg_1);
            foreach (CodeInstruction code in codes)
            {
                if (code.LoadsConstant(0.001f))
                {
                    yield return new CodeInstruction(OpCodes.Ldc_R4, 0.01f);
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
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg) => instructions.MinAgeForSexForTwo(OpCodes.Ldarg_0, OpCodes.Ldarg_1, 14f, true, ilg);
    }

    //Determines how often a pawn would want to have sex only considering factors on them
    [HarmonyPatch(typeof(LovePartnerRelationUtility), "LovinMtbSinglePawnFactor")]
    public static class LovePartnerRelationUtility_LovinMtbSinglePawnFactor
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) => instructions.RegularSexAges(OpCodes.Ldarg_0).MinAgeForSexTranspiler(OpCodes.Ldarg_0);
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
            if (!__result && Settings.LoveRelationsLoaded)
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
            if (!__result && Settings.LoveRelationsLoaded)
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