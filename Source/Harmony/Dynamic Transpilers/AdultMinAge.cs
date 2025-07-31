using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using Verse;

namespace BetterRomance.HarmonyPatches
{
    public static partial class DynamicTranspilers
    {
        /// <summary>
        /// Transpiler to convert a hard coded 13 to Pawn_AgeTracker.AdultMinAge
        /// </summary>
        /// <param name="instructions">Instructions from the calling transpiler</param>
        /// <param name="toGetPawn">Instructions needed to get the pawn on the stack</param>
        /// <param name="repeat">Whether to replace all occurrences of 13, or only the first one</param>
        /// <returns></returns>
        public static IEnumerable<CodeInstruction> AdultMinAgeInt(this IEnumerable<CodeInstruction> instructions, List<CodeInstruction> toGetPawn, bool repeat = true)
        {
            bool done = false;
            foreach (CodeInstruction code in instructions)
            {
                if (!done && code.LoadsConstant(13))
                {
                    foreach (CodeInstruction instruction in toGetPawn)
                    {
                        yield return instruction;
                    }
                    yield return CodeInstruction.LoadField(typeof(Pawn), nameof(Pawn.ageTracker));
                    yield return new(OpCodes.Callvirt, InfoHelper.AdultMinAge);
                    yield return new(OpCodes.Conv_I4);
                    done = !repeat;
                }
                else
                {
                    yield return code;
                }
            }
        }

        /// <summary>
        /// Transpiler to convert a hard coded 13 to Pawn_AgeTracker.AdultMinAge
        /// </summary>
        /// <param name="instructions">Instructions from the calling transpiler</param>
        /// <param name="toGetPawn">OpCode needed to get the pawn on the stack</param>
        /// <param name="repeat">Whether to replace all occurrences of 13, or only the first one</param>
        /// <returns></returns>
        public static IEnumerable<CodeInstruction> AdultMinAgeInt(this IEnumerable<CodeInstruction> instructions, OpCode toGetPawn, bool repeat = true) => instructions.AdultMinAgeInt([new CodeInstruction(toGetPawn)], repeat);

        /// <summary>
        /// Transpiler to convert a hardcoded 13f to Pawn_AgeTracker.AdultMinAge
        /// </summary>
        /// <param name="instructions">Instructions from the calling transpiler</param>
        /// <param name="toGetPawn">OpCode needed to get the pawn on the stack</param>
        /// <returns></returns>
        public static IEnumerable<CodeInstruction> AdultMinAgeFloat(this IEnumerable<CodeInstruction> instructions, OpCode toGetPawn)
        {
            foreach (CodeInstruction code in instructions)
            {
                if (code.LoadsConstant(13f))
                {
                    yield return new(toGetPawn);
                    yield return CodeInstruction.LoadField(typeof(Pawn), nameof(Pawn.ageTracker));
                    yield return new(OpCodes.Callvirt, InfoHelper.AdultMinAge);
                }
                else
                {
                    yield return code;
                }
            }
        }
    }
}
