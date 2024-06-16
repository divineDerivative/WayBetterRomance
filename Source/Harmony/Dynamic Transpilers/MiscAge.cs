﻿using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace BetterRomance.HarmonyPatches
{
    public static partial class DynamicTranspilers
    {
        public static IEnumerable<CodeInstruction> ChildAgeTranspiler(this IEnumerable<CodeInstruction> instructions, CodeInstruction toGetPawn, bool repeat = true)
        {
            bool done = false;
            foreach (CodeInstruction code in instructions)
            {
                if ((!done && code.LoadsConstant(3)) || code.LoadsField(typeof(LifeStageDefOf), nameof(LifeStageDefOf.HumanlikeChild)))
                {
                    yield return toGetPawn.MoveLabelsFrom(code);
                    yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.ChildAge));
                    done = !repeat;
                }
                else
                {
                    yield return code;
                }
            }
        }

        /// <summary>
        /// Replaces a hard coded 25 with a call to AgeReversalDemandAge
        /// </summary>
        /// <param name="instructions">Instructions from the original transpiler</param>
        /// <param name="toGetPawn">OpCode to load the pawn on the stack</param>
        /// <returns></returns>
        public static IEnumerable<CodeInstruction> AgeReversalDemandAgeTranspiler(this IEnumerable<CodeInstruction> instructions, OpCode toGetPawn)
        {
            return instructions.AgeReversalDemandAgeTranspiler([new CodeInstruction(toGetPawn)]);
        }

        /// <summary>
        /// Replaces a hard coded 25 with a call to AgeReversalDemandAge
        /// </summary>
        /// <param name="instructions">Instructions from the original transpiler</param>
        /// <param name="toGetPawn">List of instructions to load the pawn on the stack</param>
        /// <returns></returns>
        public static IEnumerable<CodeInstruction> AgeReversalDemandAgeTranspiler(this IEnumerable<CodeInstruction> instructions, List<CodeInstruction> toGetPawn)
        {
            foreach (CodeInstruction code in instructions)
            {
                if (code.LoadsConstant(25) || code.LoadsConstant(90000000L))
                {
                    foreach (var instruction in toGetPawn)
                    {
                        yield return instruction;
                    }
                    yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.AgeReversalDemandAge));
                    if (code.LoadsConstant(90000000L))
                    {
                        yield return new CodeInstruction(OpCodes.Conv_I8);
                        yield return new CodeInstruction(OpCodes.Ldc_I8, 3600000L);
                        yield return new CodeInstruction(OpCodes.Mul);
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
