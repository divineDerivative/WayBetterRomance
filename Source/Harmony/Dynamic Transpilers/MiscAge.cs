using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace BetterRomance.HarmonyPatches
{
    public static partial class DynamicTranspilers
    {
        /// <summary>
        /// Changes a hard coded 3 or LifeStageDefOf.HumanlikeChild to a call to ChildAge
        /// </summary>
        /// <param name="instructions">Instructions from the original transpiler</param>
        /// <param name="toGetPawn"><see cref="CodeInstruction"/> needed to load the pawn on the stack</param>
        /// <param name="repeat">Whether to continue after the first replacement</param>
        /// <returns></returns>
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
        public static IEnumerable<CodeInstruction> AgeReversalDemandAgeTranspiler(this IEnumerable<CodeInstruction> instructions, OpCode toGetPawn) => instructions.AgeReversalDemandAgeTranspiler([new CodeInstruction(toGetPawn)]);

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
                //25 years is 90000000 ticks
                if (code.LoadsConstant(25) || code.LoadsConstant(90000000L))
                {
                    foreach (CodeInstruction instruction in toGetPawn)
                    {
                        yield return instruction;
                    }
                    yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.AgeReversalDemandAge));
                    //Turn it into ticks
                    if (code.LoadsConstant(90000000L))
                    {
                        yield return new(OpCodes.Conv_I8);
                        yield return new(OpCodes.Ldc_I8, 3600000L);
                        yield return new(OpCodes.Mul);
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
