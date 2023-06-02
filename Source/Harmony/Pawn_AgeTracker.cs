using System.Collections.Generic;
using Verse;
using HarmonyLib;
using System.Reflection.Emit;
using System;
using System.IO;

namespace BetterRomance.HarmonyPatches
{
    [HarmonyPatch(typeof(Pawn_AgeTracker), nameof(Pawn_AgeTracker.BiologicalTicksPerTick), MethodType.Getter)]
    public static class Pawn_AgeTracker_BiologicalTicksPerTick
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction code in instructions)
            {
                if (code.Is(OpCodes.Ldc_R4, 11f))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0)
                    {
                        labels = code.labels
                    };
                    yield return CodeInstruction.LoadField(typeof(Pawn_AgeTracker), "pawn");
                    yield return new CodeInstruction(OpCodes.Ldc_I4, 1);
                    yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.GetGrowthMomentAsFloat));
                    yield return new CodeInstruction(OpCodes.Ldc_R4, 1f);
                    yield return new CodeInstruction(OpCodes.Add);
                }
                else if (code.Is(OpCodes.Ldc_R4, 20f))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return CodeInstruction.LoadField(typeof(Pawn_AgeTracker), "pawn");
                    yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.GetMinAgeForAdulthood));
                }
                else
                {
                    yield return code;
                }
            }
        }
    }

    [HarmonyPatch(typeof(Pawn_AgeTracker), "GrowthPointsFactor", MethodType.Getter)]
    public static class Pawn_AgeTracker_GrowthPointsFactor
    {

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction code in instructions)
            {
                if (code.Is(OpCodes.Ldc_R4, 7f))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return CodeInstruction.LoadField(typeof(Pawn_AgeTracker), "pawn");
                    yield return new CodeInstruction(OpCodes.Ldc_I4, 0);
                    yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.GetGrowthMomentAsFloat));
                }
                else
                {
                    yield return code;
                }
            }
        }
    }

    [HarmonyPatch(typeof(Pawn_AgeTracker), nameof(Pawn_AgeTracker.ResetAgeReversalDemand))]
    public static class Pawn_AgeTracker_ResetAgeReversalDemand
    {
        //This is to initialize all the age settings on the comp, because this is right after their age has been determined and right before they are needed
        public static void Prefix(Pawn ___pawn, Pawn_AgeTracker.AgeReversalReason reason, bool cancelInitialization = false)
        {
            if (reason != Pawn_AgeTracker.AgeReversalReason.Initial)
            {
                WBR_SettingsComp comp = ___pawn.TryGetComp<WBR_SettingsComp>();
                comp?.ApplySettings();
            }
        }
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction code in instructions)
            {
                if (code.LoadsConstant(90000000L))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return CodeInstruction.LoadField(typeof(Pawn_AgeTracker), "pawn");
                    yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.AgeReversalDemandAge));
                    yield return new CodeInstruction(OpCodes.Conv_I8);
                    yield return new CodeInstruction(OpCodes.Ldc_I8, 3600000L);
                    yield return new CodeInstruction(OpCodes.Mul);
                }
                else
                {
                    yield return code;
                }
            }
        }
    }
}
