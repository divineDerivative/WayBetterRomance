using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using HarmonyLib;
using System.Reflection.Emit;

namespace BetterRomance.HarmonyPatches
{
    [HarmonyPatch(typeof(RelationsUtility), "RomanceEligible")]
    public static class RelationsUtility_RomanceEligible
    {
        //public static Pawn romanceEligibleAgePawn;
        //[HarmonyPrefix]
        //public static void RomanceEligiblePrefix(Pawn pawn, bool initiator, bool forOpinionExplanation)
        //{
        //    romanceEligibleAgePawn = pawn;
        //}

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldc_R4)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.MinAgeForSex));
                        
                    //yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(RomanceEligibleAgeHelper));
                }
                else
                {
                    yield return instruction;
                }
            }
        }

        //private static float RomanceEligibleAgeHelper()
        //{
        //    return romanceEligibleAgePawn.MinAgeForSex();
        //}
    }

    [HarmonyPatch(typeof(RelationsUtility), "RomanceEligiblePair")]
    public static class RelationsUtility_RomanceEligiblePair
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach(CodeInstruction instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldc_R4 && instruction.OperandIs(16f))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.MinAgeForSex));
                }
                else
                {
                    yield return instruction;
                }
            }
        }
    }
}
