using System.Collections.Generic;
using Verse;
using HarmonyLib;
using System;
using System.Reflection.Emit;
using System.Reflection;
using System.Text;

namespace BetterRomance.HarmonyPatches
{
    [HarmonyPatch(typeof(GeneDef), "GetDescriptionFull")]
    public static class GeneDef_GetDescriptionFull
    {

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg, MethodBase original)
        {
            FieldInfo biologicalAgeTickFactorFromAgeCurve = AccessTools.Field(typeof(GeneDef), nameof(GeneDef.biologicalAgeTickFactorFromAgeCurve));
            int fieldFound = 0;
            object label = new object();
            Type type = original.GetMethodBody().LocalVariables[0].LocalType;

            foreach (CodeInstruction code in instructions)
            {
                yield return code;

                if (code.LoadsField(biologicalAgeTickFactorFromAgeCurve))
                {
                    fieldFound++;
                }

                if (fieldFound == 1 && code.opcode == OpCodes.Brfalse)
                {
                    label = code.operand;
                }

                if (fieldFound == 2 && code.IsStloc())
                {
                    //if Settings.HARActive && geneDef.defName == "Ageless"
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return CodeInstruction.Call(typeof(GeneDef_GetDescriptionFull), nameof(CheckDef));
                    yield return new CodeInstruction(OpCodes.Brfalse_S, label);
                    yield return new CodeInstruction(OpCodes.Ldloc_0);
                    //sb.AppendLine("WBR.AgelessGeneWarning".Translate());
                    yield return CodeInstruction.LoadField(type, "sb");
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return CodeInstruction.Call(typeof(GeneDef_GetDescriptionFull), nameof(MakeString));
                    yield return CodeInstruction.Call(typeof(StringBuilder), nameof(StringBuilder.AppendLine), parameters: new Type[] { typeof(string) });
                    yield return new CodeInstruction(OpCodes.Pop);
                    fieldFound++;
                }
            }
        }

        private static bool CheckDef(GeneDef geneDef)
        {
            return Settings.HARActive && geneDef.defName == "Ageless";
        }

        private static string MakeString(GeneDef geneDef)
        {
            return "WBR.AgelessGeneWarning".Translate();
        }
    }
}
