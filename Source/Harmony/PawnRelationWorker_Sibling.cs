﻿using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;
using Verse;

namespace BetterRomance.HarmonyPatches
{
    //If settings do not allow spouses for either sibling, do not force their parents to be spouses
    [HarmonyPatch(typeof(PawnRelationWorker_Sibling), nameof(PawnRelationWorker_Sibling.CreateRelation))]
    public static class PawnRelationWorker_Sibling_CreateRelation
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
        {
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

            List<CodeInstruction> codes = instructions.SkipGayCheckTranspiler(myLabel).ToList();

            //This is get the correct local index, in case it changes
            int motherIndex = -1;
            int fatherIndex = -1;
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

            bool spouseFound = false;
            foreach (CodeInstruction code in codes.GetAppropriateParentRelationshipTranspiler(new CodeInstruction(OpCodes.Ldloc_S, fatherIndex), new CodeInstruction(OpCodes.Ldloc_S, motherIndex)))
            {
                yield return code;

                if (code.Calls(typeof(RomanceUtilities), nameof(RomanceUtilities.GetAppropriateParentRelationship)))
                {
                    spouseFound = true;
                }

                //Check if spouse was added before doing the other stuff
                if (spouseFound && code.Calls(typeof(Pawn_RelationsTracker), nameof(Pawn_RelationsTracker.AddDirectRelation)))
                {
                    // if (father.relations.DirectRelationExists(PawnRelationDefOf.Spouse, mother))
                    yield return new CodeInstruction(OpCodes.Ldloc_S, fatherIndex);
                    yield return CodeInstruction.LoadField(typeof(Pawn), nameof(Pawn.relations));
                    yield return InfoHelper.DefOfSpouse.LoadField();
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
            foreach (CodeInstruction code in instructions.AgeToHaveChildren(OpCodes.Ldarg_1, OpCodes.Ldarg_1, true))
            {
                if (code.LoadsField(AccessTools.Field(typeof(Pawn), nameof(Pawn.kindDef))))
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
            foreach (CodeInstruction code in instructions)
            {
                if (code.Is(OpCodes.Ldc_R4, 40f))
                {
                    //Mathf.Min(generated.MaxAgeForSex(), other.MaxAgeForSex()) / 2f
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.MaxAgeForSex));
                    yield return new CodeInstruction(OpCodes.Ldarg_2);
                    yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.MaxAgeForSex));
                    yield return CodeInstruction.Call(typeof(Mathf), nameof(Mathf.Min), [typeof(float), typeof(float)]);
                    yield return new CodeInstruction(OpCodes.Ldc_R4, 2f);
                    yield return new CodeInstruction(OpCodes.Div);
                }
                else if (code.Is(OpCodes.Ldc_R4, 10f))
                {
                    //Mathf.Min(generated.MaxAgeForSex(), other.MaxAgeForSex()) / 8f
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.MaxAgeForSex));
                    yield return new CodeInstruction(OpCodes.Ldarg_2);
                    yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.MaxAgeForSex));
                    yield return CodeInstruction.Call(typeof(Mathf), nameof(Mathf.Min), [typeof(float), typeof(float)]);
                    yield return new CodeInstruction(OpCodes.Ldc_R4, 8f);
                    yield return new CodeInstruction(OpCodes.Div);
                }
                else
                {
                    yield return code;
                }
            }
        }
    }
}