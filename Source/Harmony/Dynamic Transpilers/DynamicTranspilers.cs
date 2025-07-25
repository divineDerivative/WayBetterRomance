﻿using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Verse;

namespace BetterRomance.HarmonyPatches
{
    public static partial class DynamicTranspilers
    {
        /// <summary>
        /// Patches a method to use LovePartnerRelationUtility.Is(Ex)LovePartnerRelation instead of just looking for each def individually
        /// </summary>
        /// <param name="instructions">Instructions from the original transpiler</param>
        /// <param name="ilg">ILGenerator from the original transpiler</param>
        /// <param name="def">The first PawnRelationDef the original looks for</param>
        /// <param name="condition">Whether we want to use brtrue or brfalse</param>
        /// <param name="ex">Whether we want the ex version or not</param>
        /// <param name="stopSkipping">A validator for the instruction where we want to stop skipping. This is also where the label is added if <paramref name="condition"/> is true</param>
        /// <param name="addLabel">A validator for the instruction where we want to add our label. Only used if <paramref name="condition"/> is false</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static IEnumerable<CodeInstruction> LoveRelationUtilityTranspiler(this IEnumerable<CodeInstruction> instructions, ILGenerator ilg, PawnRelationDef def, bool condition, bool ex, Func<CodeInstruction, bool> stopSkipping, Func<CodeInstruction, bool> addLabel = null)
        {
            bool skip = false;
            Label label = ilg.DefineLabel();
            foreach (CodeInstruction code in instructions)
            {
                //Pretty sure this is right, if we're using true the label should go at the same place we stop skipping
                if (condition)
                {
                    if (skip && stopSkipping(code))
                    {
                        code.labels.Add(label);
                        skip = false;
                    }
                }
                else if (addLabel is null)
                {
                    throw new ArgumentException($"Error in LoveRelationUtilityTranspiler; addLabel validator must be provided if condition is false");
                }
                else
                {
                    //If it's false then that means the jump to point is different from the stop skipping point
                    //So for the VSIE patch
                    if (skip && stopSkipping(code))
                    {
                        //So this is required for the VSIE patch, but I'm not sure how to account for it here
                        //Just gonna leave it for now and figure something out if I ever add another patch that needs this
                        //I bet my idea of a dictionary with MethodInfo and arguments could help, since I could check which key I'm using
                        #if !v1_6
                        yield return new CodeInstruction(OpCodes.Ldarg_0);
#endif
                        skip = false;
                    }
                    if (addLabel(code))
                    {
                        code.labels.Add(label);
                    }
                }

                if (code.LoadsField(AccessTools.Field(typeof(PawnRelationDefOf), def.defName)))
                {
                    yield return CodeInstruction.Call(typeof(LovePartnerRelationUtility), ex ? nameof(LovePartnerRelationUtility.IsExLovePartnerRelation) : nameof(LovePartnerRelationUtility.IsLovePartnerRelation));
                    yield return new CodeInstruction(condition ? OpCodes.Brtrue : OpCodes.Brfalse, label);
                    skip = true;
                }
                if (!skip)
                {
                    yield return code;
                }
            }
        }

        /// <summary>
        /// Replaces the curve from RitualOutcomeComp_Quality with a call to GetChildBirthAgeCurve
        /// </summary>
        /// <param name="instructions">Instructions from the calling transpiler</param>
        /// <param name="loadPawn">OpCode needed to load the pawn on the stack</param>
        /// <returns></returns>
        public static IEnumerable<CodeInstruction> ChildBirthAgeCurveTranspiler(this IEnumerable<CodeInstruction> instructions, OpCode loadPawn, bool useRoleID = false)
        {
            foreach (CodeInstruction code in instructions)
            {
                if (code.LoadsField(InfoHelper.RitualPawnAgeCurve))
                {
                    //The preceding code loads the instance, which we're not using so yeet
                    yield return useRoleID ? CodeInstruction.LoadField(typeof(RitualOutcomeComp_PawnAge), nameof(RitualOutcomeComp_PawnAge.roleId)) : new CodeInstruction(OpCodes.Pop);
                    yield return new CodeInstruction(loadPawn);
                    if (useRoleID)
                    {
                        yield return CodeInstruction.Call(typeof(DynamicTranspilers), nameof(RolePawn));
                    }
                    yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.GetChildBirthAgeCurve));
                }
                else
                {
                    yield return code;
                }
            }
        }

        private static Pawn RolePawn(string roleID, LordJob_Ritual ritual)
        {
            return ritual.PawnWithRole(roleID);
        }

        /// <summary>
        /// Replaces a hard coded 20 or 20f to call GetMinAgeForAdulthood
        /// </summary>
        /// <param name="instructions">Instructions from the original transpiler</param>
        /// <param name="loadPawn">List of instructions to load the pawn on the stack</param>
        /// <param name="integer">Whether to convert the result to an int</param>
        /// <returns></returns>
        public static IEnumerable<CodeInstruction> MinAgeForAdulthoodTranspiler(this IEnumerable<CodeInstruction> instructions, List<CodeInstruction> loadPawn, bool integer)
        {
            foreach (CodeInstruction code in instructions)
            {
                if (code.LoadsConstant(20) || code.LoadsConstant(20f))
                {
                    foreach (CodeInstruction instruction in loadPawn)
                    {
                        yield return instruction;
                    }
                    yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.GetMinAgeForAdulthood));
                    if (integer)
                    {
                        yield return new CodeInstruction(OpCodes.Conv_I4);
                    }
                }
                else
                {
                    yield return code;
                }
            }
        }

        /// <summary>
        /// Replaces a hard coded 20 or 20f to call GetMinAgeForAdulthood
        /// </summary>
        /// <param name="instructions">Instructions from the original transpiler</param>
        /// <param name="loadPawn">OpCode to load the pawn on the stack</param>
        /// <param name="integer">Whether to convert the result to an int</param>
        /// <returns></returns>
        public static IEnumerable<CodeInstruction> MinAgeForAdulthoodTranspiler(this IEnumerable<CodeInstruction> instructions, OpCode loadPawn, bool integer)
        {
            return instructions.MinAgeForAdulthoodTranspiler([new CodeInstruction(loadPawn)], integer);
        }

        /// <summary>
        /// Replaces trait checks with appropriate orientation check without removing a bunch of other instructions
        /// </summary>
        /// <param name="instructions">Instructions from the original transpiler</param>
        /// <param name="romance">Whether to check romantic or sexual orientation</param>
        /// <returns></returns>
        public static IEnumerable<CodeInstruction> TraitToOrientationTranspiler(this IEnumerable<CodeInstruction> instructions, bool romance)
        {
            foreach (CodeInstruction code in instructions)
            {
                if (code.Calls(AccessTools.Method(typeof(TraitSet), nameof(TraitSet.HasTrait), [typeof(TraitDef)])))
                {
                    yield return new CodeInstruction(romance ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
                    yield return CodeInstruction.Call(typeof(DynamicTranspilers), nameof(TraitConversion));
                }
                else
                {
                    yield return code;
                }
            }
        }

        private static List<TraitDef> traits = [TraitDefOf.Gay, TraitDefOf.Bisexual, TraitDefOf.Asexual, RomanceDefOf.Straight];
        private static FieldInfo TraitSetPawn = AccessTools.Field(typeof(TraitSet), "pawn");
        private static Pawn GetPawn(this TraitSet set) => TraitSetPawn.GetValue(set) as Pawn;

        /// <summary>
        /// Checks if a pawn has an orientation matching the original trait
        /// </summary>
        /// <param name="set"></param>
        /// <param name="trait"></param>
        /// <param name="romance">Whether to check romantic or sexual orientation</param>
        /// <returns></returns>
        public static bool TraitConversion(TraitSet set, TraitDef trait, bool romance)
        {
            Pawn pawn = set.GetPawn();
            if (traits.Contains(trait))
            {
                if (trait == TraitDefOf.Gay)
                {
                    return pawn.IsHomo();
                }
                if (trait == TraitDefOf.Bisexual)
                {
                    return pawn.IsBi();
                }
                if (trait == TraitDefOf.Asexual)
                {
                    return romance ? pawn.IsAro() : pawn.IsAsexual();
                }
                if (trait == RomanceDefOf.Straight)
                {
                    return pawn.IsHetero();
                }
            }
            return pawn.story.traits.HasTrait(trait);
        }

        /// <summary>
        /// Changes .def to .RaceProps.Humanlike or .IsHumanlike
        /// </summary>
        /// <param name="instructions">Instructions from the original transpiler</param>
        /// <param name="useRaceProps">Use true if former humans should not be included, false if they should be</param>
        /// <returns></returns>
        public static IEnumerable<CodeInstruction> DefToHumanlike(this IEnumerable<CodeInstruction> instructions, bool useRaceProps)
        {
            FieldInfo def = AccessTools.Field(typeof(Thing), nameof(Thing.def));
            foreach (CodeInstruction code in instructions)
            {
                if (code.LoadsField(def))
                {
                    if (useRaceProps)
                    {
                        yield return CodeInstructionMethods.Call(AccessTools.PropertyGetter(typeof(Pawn), nameof(Pawn.RaceProps)));
                        yield return CodeInstructionMethods.Call(AccessTools.PropertyGetter(typeof(RaceProperties), nameof(RaceProperties.Humanlike)));
                    }
                    else
                    {
                        yield return CodeInstruction.Call(typeof(CompatUtility), nameof(CompatUtility.IsHumanlike));
                    }
                }
                else
                {
                    yield return code;
                }
            }
        }

        /// <summary>
        /// Skips over anything that comes after checking if a pawn has the Gay trait
        /// </summary>
        /// <param name="instructions">Instructions from the original transpiler</param>
        /// <param name="label">Jump to this label. If not provided, the operand is not changed.</param>
        /// <returns></returns>
        public static IEnumerable<CodeInstruction> SkipGayCheckTranspiler(this IEnumerable<CodeInstruction> instructions, Label? label = null)
        {
            bool gayFound = false;
            foreach (CodeInstruction code in instructions)
            {
                if (code.LoadsField(InfoHelper.GayTraitDefOf))
                {
                    gayFound = true;
                }
                if (gayFound && code.Branches(out _))
                {
                    //Need to remove the bool from the stack first
                    yield return new CodeInstruction(OpCodes.Pop);
                    code.opcode = OpCodes.Br_S;
                    if (label != null)
                    {
                        code.operand = label.Value;
                    }
                    gayFound = false;
                }
                yield return code;
            }
        }

        /// <summary>
        /// Replaces hard coded Spouse relationship with a call to <see cref="RomanceUtilities.GetAppropriateParentRelationship"/>
        /// </summary>
        /// <param name="instructions">Instructions from the original transpiler</param>
        /// <param name="loadFirst"><see cref="CodeInstruction"/> to load the first pawn on the stack</param>
        /// <param name="loadSecond"><see cref="CodeInstruction"/> to load the second pawn on the stack</param>
        /// <returns></returns>
        public static IEnumerable<CodeInstruction> GetAppropriateParentRelationshipTranspiler(this IEnumerable<CodeInstruction> instructions, CodeInstruction loadFirst, CodeInstruction loadSecond)
        {
            foreach (CodeInstruction code in instructions)
            {
                if (code.LoadsField(InfoHelper.DefOfSpouse))
                {
                    //RomanceUtilities.GetAppropriateParentRelationship(first, second)
                    yield return loadFirst;
                    yield return loadSecond;
                    yield return CodeInstruction.Call(typeof(RomanceUtilities), nameof(RomanceUtilities.GetAppropriateParentRelationship));
                }
                else
                {
                    yield return code;
                }
            }
        }
    }
}
