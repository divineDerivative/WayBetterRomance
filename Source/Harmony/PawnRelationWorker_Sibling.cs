﻿using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Verse;

namespace BetterRomance.HarmonyPatches
{
    //If settings do not allow spouses for either sibling, do not force their parents to be spouses
    [HarmonyPatch(typeof(PawnRelationWorker_Sibling), "CreateRelation")]
    public static class PawnRelationWorker_Sibling_CreateRelation
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg, MethodBase original)
        {
            FieldInfo DefOfSpouse = AccessTools.Field(typeof(PawnRelationDefOf), nameof(PawnRelationDefOf.Spouse));
            Label myLabel = ilg.DefineLabel();
            Label? endLabel = new();

            //This is just to get a label for the end of the method
            bool birthNameFound = false;
            foreach (CodeInstruction code in instructions)
            {
                if (!birthNameFound && code.LoadsField(AccessTools.Field(typeof(Pawn_StoryTracker), nameof(Pawn_StoryTracker.birthLastName))))
                {
                    birthNameFound = true;
                }
                if (birthNameFound && code.Branches(out endLabel))
                {
                    break;
                }
            }

            //This is get the correct local index, in case it changes
            int motherIndex = -1;
            int fatherIndex = -1;
            List<CodeInstruction> codes = instructions.ToList();
            for (int i = 0; i < codes.Count; i++)
            {
                CodeInstruction code = codes[i];
                CodeInstruction nextCode = codes[i + 1];
                if (code.Calls(typeof(ParentRelationUtility), nameof(ParentRelationUtility.GetMother)) && nextCode.IsStloc())
                {
                    motherIndex = nextCode.LocalIndex();
                }
                if (code.Calls(typeof(ParentRelationUtility), nameof(ParentRelationUtility.GetFather)) && nextCode.IsStloc())
                {
                    fatherIndex = nextCode.LocalIndex();
                }
                if (motherIndex >= 0 && fatherIndex >= 0)
                {
                    break;
                }
            }

            //Add my label to the jump point
            for (int i = 0; i < codes.Count; ++i)
            {
                CodeInstruction code = codes[i];
                CodeInstruction nextCode = codes[i + 1];
                if (code.opcode == OpCodes.Ldloc_1 && nextCode.Branches(out _))
                {
                    code.labels.Add(myLabel);
                    break;
                }
            }

            bool gayFound = false;
            bool spouseFound = false;
            //Since I change a code in the codes list above, iterate through that instead of instructions
            foreach (CodeInstruction code in codes)
            {
                //Skip the gay stuff
                if (code.LoadsField(AccessTools.Field(typeof(TraitDefOf), nameof(TraitDefOf.Gay))))
                {
                    gayFound = true;
                }
                if (gayFound && code.Branches(out _))
                {
                    //Remove the bool from the stack and skip
                    yield return new CodeInstruction(OpCodes.Pop);
                    yield return new CodeInstruction(OpCodes.Br, myLabel);
                    gayFound = false;
                }
                //Replace spouse with the method to find the correct relationship
                else if (code.LoadsField(DefOfSpouse))
                {
                    spouseFound = true;
                    yield return new CodeInstruction(OpCodes.Ldloc_S, fatherIndex);
                    yield return new CodeInstruction(OpCodes.Ldloc_S, motherIndex);
                    yield return CodeInstruction.Call(typeof(RomanceUtilities), nameof(RomanceUtilities.GetAppropriateParentRelationship));
                }
                else
                {
                    yield return code;
                }

                //Check if spouse was added before doing the other stuff
                if (spouseFound && code.Calls(typeof(Pawn_RelationsTracker), nameof(Pawn_RelationsTracker.AddDirectRelation)))
                {
                    // if (father.relations.DirectRelationExists(PawnRelationDefOf.Spouse, mother))
                    yield return new CodeInstruction(OpCodes.Ldloc_S, fatherIndex);
                    yield return CodeInstruction.LoadField(typeof(Pawn), nameof(Pawn.relations));
                    yield return DefOfSpouse.LoadField();
                    yield return new CodeInstruction(OpCodes.Ldloc, motherIndex);
                    yield return CodeInstruction.Call(typeof(Pawn_RelationsTracker), nameof(Pawn_RelationsTracker.DirectRelationExists));
                    yield return new CodeInstruction(OpCodes.Brfalse_S, (Label)endLabel);
                    spouseFound = false;
                }
            }
        }
    }

    //Uses age settings of existing child to help generate an appropriate parent
    //Uses pawnkinds for parents if children are not allowed by child pawnkind/race
    [HarmonyPatch(typeof(PawnRelationWorker_Sibling), "GenerateParent")]
    public static class PawnRelationWorker_Sibling_GenerateParent
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction code in instructions)
            {
                if (code.opcode == OpCodes.Ldc_R4)
                {
                    if (code.OperandIs(14f))
                    {
                        //Because the instruction I'm replacing is used as a jump to point, the new instruction needs to have the same label as the old one
                        yield return new CodeInstruction(OpCodes.Ldarg_1).MoveLabelsFrom(code);
                        //Since Gender is an enum, I need to load the value of Male, which is 1
                        yield return new CodeInstruction(OpCodes.Ldc_I4_1);
                        yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.MinAgeToHaveChildren));
                    }
                    else if (code.OperandIs(16f))
                    {
                        yield return new CodeInstruction(OpCodes.Ldarg_1).MoveLabelsFrom(code);
                        //Since Gender is an enum, I need to load the value of Female, which is 2
                        yield return new CodeInstruction(OpCodes.Ldc_I4_2);
                        yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.MinAgeToHaveChildren));
                    }
                    else if (code.OperandIs(50f))
                    {
                        yield return new CodeInstruction(OpCodes.Ldarg_1).MoveLabelsFrom(code);
                        yield return new CodeInstruction(OpCodes.Ldc_I4_1);
                        yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.MaxAgeToHaveChildren));
                    }
                    else if (code.OperandIs(45f))
                    {
                        yield return new CodeInstruction(OpCodes.Ldarg_1).MoveLabelsFrom(code);
                        yield return new CodeInstruction(OpCodes.Ldc_I4_2);
                        yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.MaxAgeToHaveChildren));
                    }
                    else if (code.OperandIs(30f))
                    {
                        yield return new CodeInstruction(OpCodes.Ldarg_1).MoveLabelsFrom(code);
                        yield return new CodeInstruction(OpCodes.Ldc_I4_1);
                        yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.UsualAgeToHaveChildren));
                    }
                    else if (code.OperandIs(27f))
                    {
                        yield return new CodeInstruction(OpCodes.Ldarg_1).MoveLabelsFrom(code);
                        yield return new CodeInstruction(OpCodes.Ldc_I4_2);
                        yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.UsualAgeToHaveChildren));
                    }
                    else
                    {
                        yield return code;
                    }
                }
                else if (code.LoadsField(AccessTools.Field(typeof(Pawn), nameof(Pawn.kindDef))))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_2);
                    yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.ParentPawnkind));
                }
                else
                {
                    yield return code;
                }
            }
        }
    }

    //Add use of age settings
    [HarmonyPatch(typeof(PawnRelationWorker_Sibling), nameof(PawnRelationWorker_Sibling.GenerationChance))]
    public static class PawnRelationWorker_Sibling_GenerationChance
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.Is(OpCodes.Ldc_R4, 40f))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.MaxAgeForSex));
                    yield return new CodeInstruction(OpCodes.Ldarg_2);
                    yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.MaxAgeForSex));
                    yield return new CodeInstruction(OpCodes.Ldc_R4, 2f);
                    yield return new CodeInstruction(OpCodes.Div);
                    yield return CodeInstruction.Call(typeof(Mathf), nameof(Mathf.Min), [typeof(float), typeof(float)]);
                }
                else if (instruction.Is(OpCodes.Ldc_R4, 10f))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.MaxAgeForSex));
                    yield return new CodeInstruction(OpCodes.Ldarg_2);
                    yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.MaxAgeForSex));
                    yield return new CodeInstruction(OpCodes.Ldc_R4, 8f);
                    yield return new CodeInstruction(OpCodes.Div);
                    yield return CodeInstruction.Call(typeof(Mathf), nameof(Mathf.Min), [typeof(float), typeof(float)]);
                }
                else
                {
                    yield return instruction;
                }
            }
        }
    }
}