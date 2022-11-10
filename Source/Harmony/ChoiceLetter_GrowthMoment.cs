using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using HarmonyLib;
using System.Reflection.Emit;
using System.Reflection;

namespace BetterRomance.HarmonyPatches
{
    [HarmonyPatch(typeof(ChoiceLetter_GrowthMoment), "MakeChoices")]
    public static class ChoiceLetter_GrowthMoment_MakeChoices
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            FieldInfo pawn = AccessTools.Field(typeof(ChoiceLetter_GrowthMoment), "pawn");
            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.Is(OpCodes.Ldc_I4_S, 13))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, pawn);
                    yield return CodeInstruction.Call(typeof(ChoiceLetter_GrowthMoment_MakeChoices), nameof(ChoiceLetter_GrowthMoment_MakeChoices.GetLastGrowthMoment));
                }
                else
                {
                    yield return instruction;
                }
            }
        }

        private static int GetLastGrowthMoment(Pawn pawn)
        {
            //if (Settings.HARActive)
            //{
            //    int[] array = HAR_Integration.GetGrowthMoments(pawn);
            //    if (array != null)
            //    {
            //        return array[2];
            //    }
            //}
            return (int)pawn.ageTracker.AdultMinAge;
        }
    }
}
