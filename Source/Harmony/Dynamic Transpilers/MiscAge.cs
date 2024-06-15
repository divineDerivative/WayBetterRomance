using HarmonyLib;
using RimWorld;
using System.Collections.Generic;

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
    }
}
