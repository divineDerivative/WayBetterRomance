using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;
using Verse;

namespace BetterRomance.HarmonyPatches
{
    //If settings do not allow spouses for either sibling, do not force their parents to be spouses
    [HarmonyPatch(typeof(PawnRelationWorker_Sibling), "CreateRelation")]
    public static class PawnRelationWorker_Sibling_CreateRelation
    {
        public static bool Prefix(Pawn generated, Pawn other, ref PawnGenerationRequest request, PawnRelationWorker_Sibling __instance)
        {
            if (!other.SpouseAllowed() || !generated.SpouseAllowed())
            {
                bool hasMother = other.GetMother() != null;
                bool hasFather = other.GetFather() != null;
                bool tryMakeLovers = Rand.Value < 0.85f;
                if (hasMother && LovePartnerRelationUtility.HasAnyLovePartner(other.GetMother()))
                {
                    tryMakeLovers = false;
                }
                if (hasFather && LovePartnerRelationUtility.HasAnyLovePartner(other.GetFather()))
                {
                    tryMakeLovers = false;
                }
                if (!hasMother)
                {
                    Pawn newMother = (Pawn)AccessTools.Method(typeof(PawnRelationWorker_Sibling), "GenerateParent").Invoke(__instance, new object[] { generated, other, Gender.Female, request, false });
                    other.SetMother(newMother);
                }
                generated.SetMother(other.GetMother());
                if (!hasFather)
                {
                    Pawn newFather = (Pawn)AccessTools.Method(typeof(PawnRelationWorker_Sibling), "GenerateParent").Invoke(__instance, new object[] { generated, other, Gender.Male, request, false });
                    other.SetFather(newFather);
                }
                generated.SetFather(other.GetFather());
                if (!hasMother || !hasFather)
                {
                    Pawn mother = other.GetMother();
                    Pawn father = other.GetFather();
                    if (tryMakeLovers && !mother.IsHomo() && !father.IsHomo())
                    {
                        father.relations.AddDirectRelation(PawnRelationDefOf.Lover, mother);
                    }
                    else
                    {
                        father.relations.AddDirectRelation(PawnRelationDefOf.ExLover, mother);
                    }
                }
                AccessTools.Method(typeof(PawnRelationWorker_Sibling), "ResolveMyName").Invoke(__instance, new object[] { request, generated });
                return false;
            }
            return true;
        }
    }

    //Uses age settings of existing child to help generate an appropriate parent
    //Uses pawnkinds for parents if children are not allowed by child pawnkind/race
    [HarmonyPatch(typeof(PawnRelationWorker_Sibling), "GenerateParent")]
    public static class PawnRelationWorker_Sibling_GenerateParent
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldc_R4)
                {
                    if (instruction.OperandIs(14f))
                    {
                        //Because the instruction I'm replacing is used as a jump to point, the new instruction needs to have the same label as the old one
                        yield return new CodeInstruction(OpCodes.Ldarg_1) { labels = instruction.ExtractLabels() };
                        //Since Gender is an enum, I need to load the value of Male, which is 1
                        yield return new CodeInstruction(OpCodes.Ldc_I4_1);
                        yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.MinAgeToHaveChildren), parameters: new Type[] { typeof(Pawn), typeof(Gender) });
                    }
                    else if (instruction.OperandIs(16f))
                    {
                        yield return new CodeInstruction(OpCodes.Ldarg_1) { labels = instruction.ExtractLabels() };
                        //Since Gender is an enum, I need to load the value of Female, which is 2
                        yield return new CodeInstruction(OpCodes.Ldc_I4_2);
                        yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.MinAgeToHaveChildren), parameters: new Type[] { typeof(Pawn), typeof(Gender) });
                    }
                    else if (instruction.OperandIs(50f))
                    {
                        yield return new CodeInstruction(OpCodes.Ldarg_1) { labels = instruction.ExtractLabels() };
                        yield return new CodeInstruction(OpCodes.Ldc_I4_1);
                        yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.MaxAgeToHaveChildren), parameters: new Type[] { typeof(Pawn), typeof(Gender) });
                    }
                    else if (instruction.OperandIs(45f))
                    {
                        yield return new CodeInstruction(OpCodes.Ldarg_1) { labels = instruction.ExtractLabels() };
                        yield return new CodeInstruction(OpCodes.Ldc_I4_2);
                        yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.MaxAgeToHaveChildren), parameters: new Type[] { typeof(Pawn), typeof(Gender) });
                    }
                    else if (instruction.OperandIs(30f))
                    {
                        yield return new CodeInstruction(OpCodes.Ldarg_1) { labels = instruction.ExtractLabels() };
                        yield return new CodeInstruction(OpCodes.Ldc_I4_1);
                        yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.UsualAgeToHaveChildren), parameters: new Type[] { typeof(Pawn), typeof(Gender) });
                    }
                    else if (instruction.OperandIs(27f))
                    {
                        yield return new CodeInstruction(OpCodes.Ldarg_1) { labels = instruction.ExtractLabels() };
                        yield return new CodeInstruction(OpCodes.Ldc_I4_2);
                        yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.UsualAgeToHaveChildren), parameters: new Type[] { typeof(Pawn), typeof(Gender) });
                    }
                    else
                    {
                        yield return instruction;
                    }
                }
                else if (instruction.LoadsField(AccessTools.Field(typeof(Pawn), nameof(Pawn.kindDef))))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_2);
                    yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.ParentPawnkind));
                }
                else
                {
                    yield return instruction;
                }
            }
        }
    }

    //Add use of age settings
    [HarmonyPatch(typeof(PawnRelationWorker_Sibling), "GenerationChance")]
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
                    yield return CodeInstruction.Call(typeof(Mathf), nameof(Mathf.Min), new Type[] { typeof(float), typeof(float) });
                }
                else if (instruction.Is(OpCodes.Ldc_R4, 10f))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.MaxAgeForSex));
                    yield return new CodeInstruction(OpCodes.Ldarg_2);
                    yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.MaxAgeForSex));
                    yield return new CodeInstruction(OpCodes.Ldc_R4, 8f);
                    yield return new CodeInstruction(OpCodes.Div);
                    yield return CodeInstruction.Call(typeof(Mathf), nameof(Mathf.Min), new Type[] { typeof(float), typeof(float) });
                }
                else
                {
                    yield return instruction;
                }
            }
        }
    }
}