using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using RimWorld;
using Verse;
using HarmonyLib;
using System.Reflection.Emit;
using System.Data.SqlClient;

namespace BetterRomance.HarmonyPatches
{
    [HarmonyPatch(typeof(Recipe_ExtractOvum), "AvailableReport")]
    public static class Recipe_ExtractOvum_AvailableReport
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            int replacementCount = 0;
            FieldInfo recipe = AccessTools.Field(typeof(RecipeWorker), nameof(RecipeWorker.recipe));
            FieldInfo minAllowedAge = AccessTools.Field(typeof(RecipeDef), nameof(RecipeDef.minAllowedAge));

            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldarg_0 && replacementCount < 2)
                {
                    yield return new CodeInstruction(OpCodes.Ldloc_0);
                    yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.MinAgeToHaveChildren), new Type[] { typeof(Pawn) });
                    yield return new CodeInstruction(OpCodes.Conv_I4);
                    replacementCount++;
                }
                else if (instruction.LoadsField(recipe))
                {
                    continue;
                }
                else if (instruction.LoadsField(minAllowedAge))
                {
                    continue;
                }
                else
                {
                    yield return instruction;
                }
            }
        }
    }
}
