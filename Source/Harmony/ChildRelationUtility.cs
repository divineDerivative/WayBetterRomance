using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using Verse;

namespace BetterRomance.HarmonyPatches
{

    //This determines the chance for a given pawn to become the child of two other pawns
    [HarmonyPatch(typeof(ChildRelationUtility), "ChanceOfBecomingChildOf")]
    public static class ChildRelationUtility_ChanceOfBecomingChildOf
    {
        //Check if settings allow children
        public static bool Prefix(Pawn child, Pawn father, Pawn mother, PawnGenerationRequest? childGenerationRequest, PawnGenerationRequest? fatherGenerationRequest, PawnGenerationRequest? motherGenerationRequest, ref float __result)
        {
            if ((father != null && !father.ChildAllowed()) || (mother != null && !mother.ChildAllowed()))
            {
                __result = 0f;
                return false;
            }
            return true;
        }

        //Use age settings and remove gay bias
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction code in instructions)
            {
                if (code.LoadsConstant(14f))
                {
                    //father
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    //Just load Gender.None, the method will use the correct gender
                    yield return new CodeInstruction(OpCodes.Ldc_I4_0);
                    yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.MinAgeToHaveChildren));
                }
                else if (code.OperandIs(50f))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Ldc_I4_0);
                    yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.MaxAgeToHaveChildren));
                }
                else if (code.OperandIs(30f))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Ldc_I4_0);
                    yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.UsualAgeToHaveChildren));
                }
                else if (code.OperandIs(16f))
                {
                    //mother
                    yield return new CodeInstruction(OpCodes.Ldarg_2);
                    yield return new CodeInstruction(OpCodes.Ldc_I4_0);
                    yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.MinAgeToHaveChildren));
                }
                else if (code.OperandIs(45f))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_2);
                    yield return new CodeInstruction(OpCodes.Ldc_I4_0);
                    yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.MaxAgeToHaveChildren));
                }
                else if (code.OperandIs(27f))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_2);
                    yield return new CodeInstruction(OpCodes.Ldc_I4_0);
                    yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.UsualAgeToHaveChildren));
                }
                else if (code.opcode == OpCodes.Ldloc_3)
                {
                    //This just replaces num4 with 1f at the end, negating the gay reduction
                    yield return new CodeInstruction (OpCodes.Ldc_R4, 1f);
                }
                else
                {
                    yield return code;
                }
            }
        }
    }

    [HarmonyPatch(typeof(ChildRelationUtility), "NumberOfChildrenFemaleWantsEver")]
    public static class ChildRelationUtility_NumberOfChildrenFemaleWantsEver
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            bool first = true;
            foreach (CodeInstruction code in instructions)
            {
                if (code.LoadsConstant(3))
                {
                    if (first)
                    {
                        yield return code;
                        first = false;
                    }
                    else
                    {
                        yield return new CodeInstruction(OpCodes.Ldarg_0);
                        yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.MaxChildren));
                    }
                }
                else
                {
                    yield return code;
                }
            }
        }
    }
}