using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Verse;

namespace BetterRomance
{
    //Let HAR go first so I can patch his transpiler
    //Otherwise transpile to use age settings to generate the lovin curve
    [HarmonyPatch(typeof(JobDriver_Lovin), "GenerateRandomMinTicksToNextLovin")]
    [HarmonyAfter(new string[] { "rimworld.erdelf.alien_race.main" })]
    public static class JobDriver_Lovin_GenerateRandomMinTicksToNextLovin
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
        {
            FieldInfo ageCurveInfo = AccessTools.Field(typeof(JobDriver_Lovin), "LovinIntervalHoursFromAgeCurve");

            foreach (CodeInstruction instruction in instructions)
            {
                //Don't do anything if HAR is active, since the first part of this will still be true
                if (instruction.LoadsField(ageCurveInfo) && !Settings.HARActive)
                {
                    //Because the instruction I'm replacing is used as a jump to point, the new instruction needs to have the same label as the old one
                    CodeInstruction newInstruction = new CodeInstruction(OpCodes.Ldarg_1);
                    newInstruction.MoveLabelsFrom(instruction);
                    yield return newInstruction;
                    yield return CodeInstruction.Call(typeof(RomanceUtilities), nameof(RomanceUtilities.GetLovinCurve));
                }
                else
                {
                    yield return instruction;
                }
            }
        }
    }
}