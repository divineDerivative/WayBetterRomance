using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using Verse;

namespace BetterRomance.HarmonyPatches
{
    public static partial class DynamicTranspilers
    {
        /// <summary>
        /// Transpiler to convert various hard coded ages to the appropriate SettingsUtilities call
        /// </summary>
        /// <param name="instructions">Instructions from the original transpiler</param>
        /// <param name="maleCode">The OpCode needed to get the appropriate pawn on the stack</param>
        /// <param name="femaleCode">The OpCode needed to get the appropriate pawn on the stack</param>
        /// <param name="careAboutGender">Whether we load a specific gender or none</param>
        /// <returns></returns>
        public static IEnumerable<CodeInstruction> AgeToHaveChildren(this IEnumerable<CodeInstruction> instructions, OpCode maleCode, OpCode femaleCode, bool careAboutGender)
        {
            foreach (CodeInstruction code in instructions)
            {
                if (code.LoadsConstant(14f))
                {
                    //Male
                    yield return new CodeInstruction(maleCode).MoveLabelsFrom(code);
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
        /// Transpiler to convert hard coded 16 to MinAgeToHaveChildren. So far this is only used for fema
        /// </summary>
        /// <param name="instructions">Instructions from the original transpiler</param>
        /// <param name="loadPawn">OpCode needed to get the pawn on the stack</param>
        /// <param name="recipe">Whether we're replacing a check to recipe.minAllowedAge, where the xml for the recipe uses 16</param>
        /// <returns></returns>
        public static IEnumerable<CodeInstruction> AgeToHaveChildrenInt(this IEnumerable<CodeInstruction> instructions, OpCode loadPawn, bool recipe = false)
        {
            foreach (CodeInstruction code in instructions)
            {
                if (code.LoadsConstant(16) || (recipe && code.LoadsField(AccessTools.Field(typeof(RecipeDef), nameof(RecipeDef.minAllowedAge)))))
                {
                    if (recipe)
                    {
                        yield return new CodeInstruction(OpCodes.Pop);
                    }
                    yield return new CodeInstruction(loadPawn);
                    yield return new CodeInstruction(OpCodes.Ldc_I4_0);
                    yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.MinAgeToHaveChildren));
                    yield return new CodeInstruction(OpCodes.Conv_I4);
                }
                else
                {
                    yield return code;
                }
            }
        }

        /// <summary>
        /// Transpiler to convert a hard codded 5 or 5f to MinOpinionForRomance
        /// </summary>
        /// <param name="instructions">Instructions from the original transpiler</param>
        /// <param name="loadPawn">OpCode needed to load the pawn on the stack</param>
        /// <returns></returns>
        public static IEnumerable<CodeInstruction> MinOpinionRomanceTranspiler(this IEnumerable<CodeInstruction> instructions, OpCode loadPawn)
        {
            foreach (CodeInstruction code in instructions)
            {
                if (code.LoadsConstant(5f))
                {
                    yield return new CodeInstruction(loadPawn);
                    yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.MinOpinionForRomance));
                    yield return new CodeInstruction(OpCodes.Conv_R4);
                }
                else if (code.LoadsConstant(5))
                {
                    yield return new CodeInstruction(loadPawn);
                    yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.MinOpinionForRomance));
                }
                else
                {
                    yield return code;
                }
            }
        }
    }
}
