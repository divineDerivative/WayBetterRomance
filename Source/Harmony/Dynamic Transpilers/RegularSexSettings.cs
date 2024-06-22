using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;

namespace BetterRomance.HarmonyPatches
{
    public static partial class DynamicTranspilers
    {
        /// <summary>
        /// Transpiler to convert hard coded ages to the appropriate RegularSexSettings call
        /// </summary>
        /// <param name="instructions">Instructions from the original transpiler</param>
        /// <param name="loadPawn">Opcode to load the pawn onto the stack</param>
        /// <returns></returns>
        public static IEnumerable<CodeInstruction> RegularSexAges(this IEnumerable<CodeInstruction> instructions, OpCode loadPawn)
        {
            foreach (CodeInstruction code in instructions)
            {
                if (code.LoadsConstant(16f))
                {
                    yield return new CodeInstruction(loadPawn);
                    yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.MinAgeForSex));
                }
                else if (code.LoadsConstant(25f))
                {
                    yield return new CodeInstruction(loadPawn);
                    yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.DeclineAtAge));
                }
                else if (code.LoadsConstant(80f))
                {
                    yield return new CodeInstruction(loadPawn);
                    yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.MaxAgeForSex));
                }
                else
                {
                    yield return code;
                }
            }
        }

        /// <summary>
        /// Transpiler to convert hard coded age to MinAgeForSex for two different pawns. This assumes there's only one replacement needed for each pawn.
        /// </summary>
        /// <param name="instructions">Instructions from the original transpiler</param>
        /// <param name="firstCode">OpCode needed to load the first pawn on the stack</param>
        /// <param name="secondCode">OpCode needed to load the second pawn on the stack</param>
        /// <param name="ageToReplace">The exact age to replace, usually 16f or 14f</param>
        /// <param name="saveSecond">Whether to save the second pawn's min age as a local variable and use it in all but the first replacement</param>
        /// <returns></returns>
        public static IEnumerable<CodeInstruction> MinAgeForSexForTwo(this IEnumerable<CodeInstruction> instructions, OpCode firstCode, OpCode secondCode, float ageToReplace, bool saveSecond = false, ILGenerator ilg = null)
        {
            LocalBuilder local_p2MinAgeForSex = null;
            if (saveSecond)
            {
                local_p2MinAgeForSex = ilg.DeclareLocal(typeof(float));

                //Calculate and store p2MinAgeForSex first so we can call it easier later
                yield return new CodeInstruction(secondCode);
                yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.MinAgeForSex));
                yield return new CodeInstruction(OpCodes.Stloc, local_p2MinAgeForSex.LocalIndex);
            }
            bool firstFound = false;
            foreach (CodeInstruction code in instructions)
            {
                if (code.LoadsConstant(ageToReplace) && !firstFound)
                {
                    yield return new CodeInstruction(firstCode);
                    yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.MinAgeForSex));
                    firstFound = true;
                }
                else if (code.LoadsConstant(ageToReplace))
                {
                    if (saveSecond)
                    {
                        yield return new CodeInstruction(OpCodes.Ldloc, local_p2MinAgeForSex.LocalIndex);
                    }
                    else
                    {
                        yield return new CodeInstruction(secondCode);
                        yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.MinAgeForSex));
                    }
                }
                else
                {
                    yield return code;
                }
            }
        }

        /// <summary>
        /// Transpiler to convert hard codded age to MinAgeForSex
        /// </summary>
        /// <param name="instructions">Instructions from the original trasnpiler</param>
        /// <param name="loadPawn">List of CodeInstruction needed to load the pawn on the stack</param>
        /// <returns></returns>
        public static IEnumerable<CodeInstruction> MinAgeForSexTranspiler(this IEnumerable<CodeInstruction> instructions, List<CodeInstruction> loadPawn)
        {
            foreach (CodeInstruction code in instructions)
            {
                if (code.LoadsConstant(16f) || code.LoadsConstant(16))
                {
                    foreach (CodeInstruction instruction in loadPawn)
                    {
                        yield return instruction;
                    }
                    yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.MinAgeForSex));
                    if (code.LoadsConstant(16))
                    {
                        yield return new CodeInstruction(OpCodes.Conv_I4);
                    }
                }
                else if (code.LoadsConstant(14f))
                {
                    foreach (CodeInstruction instruction in loadPawn)
                    {
                        yield return instruction;
                    }
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

        /// <summary>
        /// Transpiler to convert hard codded age to MinAgeForSex
        /// </summary>
        /// <param name="instructions">Instructions from the original trasnpiler</param>
        /// <param name="loadPawn">OpCode needed to load the pawn on the stack</param>
        /// <returns></returns>
        public static IEnumerable<CodeInstruction> MinAgeForSexTranspiler(this IEnumerable<CodeInstruction> instructions, OpCode loadPawn)
        {
            return instructions.MinAgeForSexTranspiler([new CodeInstruction(loadPawn)]);
        }

        /// <summary>
        /// Converts hard coded 40f and 20f to call MaxAgeGap and MaxAgeGap / 2f respectively
        /// </summary>
        /// <param name="instructions">Instructions from original transpiler</param>
        /// <param name="ilg">ILGenerator from the original transpiler</param>
        /// <param name="firstPawn">List of instructions to get the first pawn on the stack</param>
        /// <param name="secondPawn">If there is a second pawn, list of instructions to get them on the stack</param>
        /// <returns></returns>
        public static IEnumerable<CodeInstruction> MaxAgeGapTranspiler(this IEnumerable<CodeInstruction> instructions, ILGenerator ilg, List<CodeInstruction> firstPawn, List<CodeInstruction> secondPawn)
        {
            LocalBuilder local_maxAgeGap = null;
            bool localExists = false;
            foreach (CodeInstruction code in instructions)
            {
                if (code.LoadsConstant(40f))
                {
                    if (!localExists)
                    {
                        local_maxAgeGap = ilg.DeclareLocal(typeof(float));
                        localExists = true;
                    }
                    foreach (CodeInstruction instruction in firstPawn)
                    {
                        yield return instruction;
                    }
                    yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.MaxAgeGap));
                    //If there's two pawns, we want to take the lowest result
                    if (secondPawn != null)
                    {
                        foreach (CodeInstruction instruction in secondPawn)
                        {
                            yield return instruction;
                        }
                        yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.MaxAgeGap));
                        yield return CodeInstruction.Call(typeof(Mathf), nameof(Mathf.Min), parameters: [typeof(float), typeof(float)]);
                    }
                    //need to store this value somewhere
                    yield return new CodeInstruction(OpCodes.Stloc, local_maxAgeGap.LocalIndex);
                    yield return new CodeInstruction(OpCodes.Ldloc, local_maxAgeGap.LocalIndex);
                }
                else if (code.LoadsConstant(20f))
                {
                    //If we're only replacing 20f, there's no local variable to load
                    if (localExists)
                    {
                        //put the stored value on the stack
                        yield return new CodeInstruction(OpCodes.Ldloc, local_maxAgeGap.LocalIndex);
                    }
                    else
                    {
                        //The only use for this so far only has one pawn
                        foreach (CodeInstruction instruction in firstPawn)
                        {
                            yield return instruction;
                        }
                        yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.MaxAgeGap));
                    }
                    yield return new CodeInstruction(OpCodes.Ldc_R4, operand: 2f);
                    yield return new CodeInstruction(OpCodes.Div);
                }
                else
                {
                    yield return code;
                }
            }
        }

        /// <summary>
        /// Converts hard coded 40f and 20f to call MaxAgeGap and MaxAgeGap / 2f respectively
        /// </summary>
        /// <param name="instructions">Instructions from original transpiler</param>
        /// <param name="ilg">ILGenerator from the original transpiler</param>
        /// <param name="firstPawn">OpCode to get the first pawn on the stack</param>
        /// <param name="secondPawn">If there is a second pawn, OpCode to get them on the stack</param>
        /// <returns></returns>
        public static IEnumerable<CodeInstruction> MaxAgeGapTranspiler(this IEnumerable<CodeInstruction> instructions, ILGenerator ilg, OpCode firstPawn, OpCode? secondPawn)
        {
            return instructions.MaxAgeGapTranspiler(ilg, [new CodeInstruction(firstPawn)], secondPawn is null ? null : [new CodeInstruction((OpCode)secondPawn)]);
        }
    }
}
