using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using Verse;

namespace BetterRomance.HarmonyPatches
{
    [HarmonyPatch(typeof(Pawn_AgeTracker), nameof(Pawn_AgeTracker.BiologicalTicksPerTick), MethodType.Getter)]
    public static class Pawn_AgeTracker_BiologicalTicksPerTick
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.MinAgeForAdulthoodTranspiler(
            [
                new CodeInstruction(OpCodes.Ldarg_0),
                InfoHelper.AgeTrackerPawn.LoadField(),
            ], false);
            foreach (CodeInstruction code in codes)
            {
                if (code.LoadsConstant(11f))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0)
                    {
                        labels = code.labels
                    };
                    yield return InfoHelper.AgeTrackerPawn.LoadField();
                    yield return CodeInstruction.LoadField(typeof(Pawn), nameof(Pawn.ageTracker));
                    yield return new CodeInstruction(OpCodes.Callvirt, InfoHelper.AdultMinAge);
                    yield return new CodeInstruction(OpCodes.Ldc_R4, 2f);
                    yield return new CodeInstruction(OpCodes.Sub);
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
                if (code.LoadsConstant(7f))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return InfoHelper.AgeTrackerPawn.LoadField();
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
        //This is to initialize all the age settings on the comp, because this is right after their age has been determined and right before the settings are needed
        //This only works for new pawn generation
        public static void Prefix(Pawn ___pawn, Pawn_AgeTracker.AgeReversalReason reason)
        {
            if (reason == Pawn_AgeTracker.AgeReversalReason.Initial)
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
                    yield return InfoHelper.AgeTrackerPawn.LoadField();
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

    [HarmonyPatch(typeof(Pawn_AgeTracker), nameof(Pawn_AgeTracker.ExposeData))]
    public static class Pawn_AgeTracker_ExposeData
    {
        //This will initialize the comp for existing pawns on loading
        public static void Prefix(Pawn ___pawn)
        {
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (___pawn.IsHumanlike())
                {
                    if (Settings.PawnmorpherActive)
                    {
                        //This will add the comp to an animal that used to be a human, using the original human's race
                        //May or may not already be calculated, but race will be correct
                        ___pawn.FormerHumanCompCheck();
                    }
                    WBR_SettingsComp comp = ___pawn.TryGetComp<WBR_SettingsComp>();
                    comp?.ApplySettings();
                }
            }
        }
    }
}
