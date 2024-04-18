using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using Verse;

namespace BetterRomance.HarmonyPatches
{
    public static class DynamicTranspilers
    {
        /// <summary>
        /// Transpiler to convert various hard coded ages to the appropriate SettingsUtilities call
        /// </summary>
        /// <param name="instructions">Instructions from the original transpiler</param>
        /// <param name="maleCode">The OpCode needed to get the appropriate pawn on the stack</param>
        /// <param name="femaleCode">The OpCode needed to get the appropriate pawn on the stack</param>
        /// <param name="careAboutGender">Whether we load a specific gender or none</param>
        /// <returns></returns>
        public static IEnumerable<CodeInstruction> AgeToHaveChildrenTranspiler(IEnumerable<CodeInstruction> instructions, OpCode maleCode, OpCode femaleCode, bool careAboutGender)
        {
            foreach (CodeInstruction code in instructions)
            {
                if (code.LoadsConstant(14f))
                {
                    //Male
                    yield return new CodeInstruction(maleCode).MoveLabelsFrom(code);
                    //Just load Gender.None, the method will use the correct gender
                    yield return new CodeInstruction(careAboutGender ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
                    yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.MinAgeToHaveChildren));
                }
                else if (code.LoadsConstant(50f))
                {
                    yield return new CodeInstruction(maleCode).MoveLabelsFrom(code);
                    yield return new CodeInstruction(careAboutGender ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
                    yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.MaxAgeToHaveChildren));
                }
                else if (code.LoadsConstant(30f))
                {
                    yield return new CodeInstruction(maleCode).MoveLabelsFrom(code);
                    yield return new CodeInstruction(careAboutGender ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
                    yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.UsualAgeToHaveChildren));
                }
                else if (code.LoadsConstant(16f))
                {
                    //Female
                    yield return new CodeInstruction(femaleCode).MoveLabelsFrom(code);
                    yield return new CodeInstruction(careAboutGender ? OpCodes.Ldc_I4_2 : OpCodes.Ldc_I4_0);
                    yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.MinAgeToHaveChildren));
                }
                else if (code.LoadsConstant(45f))
                {
                    yield return new CodeInstruction(femaleCode).MoveLabelsFrom(code);
                    yield return new CodeInstruction(careAboutGender ? OpCodes.Ldc_I4_2 : OpCodes.Ldc_I4_0);
                    yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.MaxAgeToHaveChildren));
                }
                else if (code.LoadsConstant(27f))
                {
                    yield return new CodeInstruction(femaleCode).MoveLabelsFrom(code);
                    yield return new CodeInstruction(careAboutGender ? OpCodes.Ldc_I4_2 : OpCodes.Ldc_I4_0);
                    yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.UsualAgeToHaveChildren));
                }
                else
                {
                    yield return code;
                }
            }
        }

        /// <summary>
        /// Transpiler to convert a hardcoded 13 to Pawn_AgeTracker.AdultMinAge
        /// </summary>
        /// <param name="instructions">Instructions from the calling transpiler</param>
        /// <param name="toGetPawn">Instructions needed to get the pawn on the stack</param>
        /// <returns></returns>
        public static IEnumerable<CodeInstruction> AdultMinAgeInt(IEnumerable<CodeInstruction> instructions, List<CodeInstruction> toGetPawn)
        {
            foreach (CodeInstruction code in instructions)
            {
                if (code.LoadsConstant(13))
                {
                    foreach (CodeInstruction instruction in toGetPawn)
                    {
                        yield return instruction;
                    }
                    yield return CodeInstruction.LoadField(typeof(Pawn), nameof(Pawn.ageTracker));
                    yield return new CodeInstruction(OpCodes.Callvirt, CodeInstructionMethods.AdultMinAge);
                    yield return new CodeInstruction(OpCodes.Conv_I4);
                }
                else
                {
                    yield return code;
                }
            }
        }

        /// <summary>
        /// Transpiler to convert a hardcoded 13 to Pawn_AgeTracker.AdultMinAge
        /// </summary>
        /// <param name="instructions">Instructions from the calling transpiler</param>
        /// <param name="toGetPawn">OpCode needed to get the pawn on the stack</param>
        /// <returns></returns>
        public static IEnumerable<CodeInstruction> AdultMinAgeInt(IEnumerable<CodeInstruction> instructions, OpCode toGetPawn)
        {
            return AdultMinAgeInt(instructions, [new CodeInstruction(toGetPawn)]);
        }

        /// <summary>
        /// Transpiler to convert hard coded ages with the appropriate RegularSexSettings call
        /// </summary>
        /// <param name="instructions">Instructions from the original transpiler</param>
        /// <param name="loadPawn">Opcode to load the pawn onto the stack</param>
        /// <returns></returns>
        public static IEnumerable<CodeInstruction> RegularSexAgesTranspiler(IEnumerable<CodeInstruction> instructions, OpCode loadPawn)
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

        public static IEnumerable<CodeInstruction> MinAgeForSexForTwoTranspiler(IEnumerable<CodeInstruction> instructions, OpCode firstCode, OpCode secondCode, float ageToReplace)
        {
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
                    yield return new CodeInstruction(secondCode);
                    yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.MinAgeForSex));
                }
                else
                {
                    yield return code;
                }
            }
        }

        public static IEnumerable<CodeInstruction> MinAgeForSexTranspiler(IEnumerable<CodeInstruction> instructions, List<CodeInstruction> loadPawn)
        {
            foreach (CodeInstruction code in instructions)
            {
                if (code.LoadsConstant(16f))
                {
                    foreach (CodeInstruction instruction in loadPawn)
                    {
                        yield return instruction;
                    }
                    yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.MinAgeForSex));
                }
                else
                {
                    yield return code;
                }
            }
        }

        public static IEnumerable<CodeInstruction> MinAgeForSexTranspiler(IEnumerable<CodeInstruction> instructions, OpCode loadPawn)
        {
            return MinAgeForSexTranspiler(instructions, [new CodeInstruction(loadPawn)]);
        }
    }
}
