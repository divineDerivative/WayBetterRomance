using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using Verse;
using HarmonyLib;
using System.Reflection.Emit;

namespace BetterRomance.HarmonyPatches
{
    [HarmonyPatch(typeof(Recipe_ExtractOvum), "AvailableReport")]
    public static class Recipe_ExtractOvum_AvailableReport
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            FieldInfo recipe = AccessTools.Field(typeof(RecipeWorker), nameof(RecipeWorker.recipe));
            FieldInfo minAllowedAge = AccessTools.Field(typeof(RecipeDef), nameof(RecipeDef.minAllowedAge));

            List<CodeInstruction> codes = instructions.ToList();

            for (int i = 0; i < codes.Count; i++)
            {
                CodeInstruction instruction = codes[i];
                if (instruction.opcode == OpCodes.Ldarg_0)
                {
                    if (codes[i + 1].LoadsField(recipe) && codes[i + 2].LoadsField(minAllowedAge))
                    {
                        yield return new CodeInstruction(OpCodes.Ldloc_0);
                        yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.MinAgeToHaveChildren), new Type[] { typeof(Pawn) });
                        yield return new CodeInstruction(OpCodes.Conv_I4);
                        i += 2;
                    }
                    else
                    {
                        yield return instruction;
                    }
                }
                else
                {
                    yield return instruction;
                }
            }
        }
    }
}
