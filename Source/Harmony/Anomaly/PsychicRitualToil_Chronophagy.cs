using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using Verse;
using Verse.AI.Group;

#if v1_5
namespace BetterRomance.HarmonyPatches
{
    [HarmonyPatch(typeof(PsychicRitualToil_Chronophagy), "ReverseAgePawn")]
    public static class PsychicRitualToil_Chronophagy_ReverseAgePawn
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction code in instructions)
            {
                if (code.LoadsConstant(13f))
                {
                    yield return CodeInstruction.LoadArgument(0);
                    yield return CodeInstruction.LoadField(typeof(Pawn), nameof(Pawn.ageTracker));
                    yield return new CodeInstruction(OpCodes.Callvirt, CodeInstructionMethods.AdultMinAge);
                }
                else
                {
                    yield return code;
                }
            }
        }
    }
}
#endif
