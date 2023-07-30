using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using HarmonyLib;
using System.Reflection;
using System.Reflection.Emit;

namespace BetterRomance.HarmonyPatches
{
    [HarmonyPatch(typeof(Gene), nameof(Gene.Active), MethodType.Getter)]
    public static class Gene_Active
    {

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            FieldInfo minAgeActive = AccessTools.Field(typeof(GeneDef), "minAgeActive");

            foreach (CodeInstruction code in instructions)
            {
                yield return code;

                if (code.LoadsField(minAgeActive))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return CodeInstruction.LoadField(typeof(Gene), "pawn");
                    yield return CodeInstruction.Call(typeof(Gene_Active), nameof(ConvertAge));
                }
            }
        }

        private static float ConvertAge(float age, Pawn pawn)
        {
            if (age == 3f)
            {
                return SettingsUtilities.ChildAge(pawn);
            }
            if (age == 7f)
            {
                return SettingsUtilities.GetGrowthMomentAsFloat(pawn, 0);
            }
            if (age == 10f)
            {
                return SettingsUtilities.GetGrowthMomentAsFloat(pawn, 1);
            }
            if (age == 13f)
            {
                return pawn.ageTracker.AdultMinAge;
            }
            if (age == 18f)
            {
                return SettingsUtilities.AdultAgeForLearning(pawn);
            }
            return age;
        }
    }
}
