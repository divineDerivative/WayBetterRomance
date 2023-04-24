using System.Collections.Generic;
using Verse;
using HarmonyLib;
using System.Reflection.Emit;

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

    [HarmonyPatch(typeof(Pawn_AgeTracker), nameof(Pawn_AgeTracker.TrySimulateGrowthPoints))]
    public static class Pawn_AgeTracker_TrySimulateGrowthPoints
    {
        //Reassign the growth moment ages since they can vary from pawn to pawn
        public static bool Prefix(Pawn ___pawn, ref List<int> ___growthMomentAges)
        {
            if (ModsConfig.BiotechActive && ___pawn.RaceProps.Humanlike && ___pawn.ageTracker.AgeBiologicalYearsFloat < ___pawn.ageTracker.AdultMinAge)
            {
                Pawn pawn = ___pawn;
                ___growthMomentAges = new List<int>
                {
                   SettingsUtilities.ChildAge(pawn)
                };
                ___growthMomentAges.AddRange(GrowthMomentAges(pawn));
            }
            return true;
        }

        //This is for analyzing growth point distribution, saving in case I need it later
        //public static void Postfix(Pawn ___pawn, float ___growthPoints, List<int> ___growthMomentAges)
        //{
        //    if (___pawn.RaceProps.Humanlike && ___pawn.ageTracker.AgeBiologicalYearsFloat < ___pawn.ageTracker.AdultMinAge)
        //    {
        //        decimal age = (decimal)Math.Round(___pawn.ageTracker.AgeBiologicalYearsFloat, 3);
        //        decimal points = (decimal)Math.Round(___growthPoints, 3);
        //        decimal difference = 0m;
        //        foreach (int i in ___growthMomentAges)
        //        {
        //            if (age > i)
        //            {
        //                difference = age - i;
        //            }
        //        }
        //        string result = $"{age}, {points}, {difference}";
        //        string filePath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\growth numbers.csv";
        //        using (StreamWriter writer = new StreamWriter(filePath, true))
        //        {
        //            writer.WriteLine(result);
        //        }
        //    }
        //}

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction code in instructions)
            {
                if (code.Is(OpCodes.Ldc_I4_S, 13))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return CodeInstruction.LoadField(typeof(Pawn_AgeTracker), "pawn");
                    yield return new CodeInstruction(OpCodes.Ldc_I4, 2);
                    yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.GetGrowthMoment));
                }
                else if (code.Is(OpCodes.Ldc_R4, 7f))
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

        private static int[] GrowthMomentAges(Pawn pawn)
        {
            int[] result = new int[3];
            for (int i = 0; i < 3; i++)
            {
                result[i] = SettingsUtilities.GetGrowthMoment(pawn, i);
            }
            return result;
        }
    }

    [HarmonyPatch(typeof(Pawn_AgeTracker), nameof(Pawn_AgeTracker.ResetAgeReversalDemand))]
    public static class Pawn_AgeTracker_ResetAgeReversalDemand
    {
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
