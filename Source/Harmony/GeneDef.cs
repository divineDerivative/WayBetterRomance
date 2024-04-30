using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Verse;

namespace BetterRomance.HarmonyPatches
{
    [HarmonyPatch(typeof(GeneDef), "GetDescriptionFull")]
    public static class GeneDef_GetDescriptionFull
    {
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> AgeCurveTranspiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            FieldInfo biologicalAgeTickFactorFromAgeCurve = AccessTools.Field(typeof(GeneDef), nameof(GeneDef.biologicalAgeTickFactorFromAgeCurve));
            int fieldFound = 0;
            Label label = new();
            Type type = original.GetMethodBody().LocalVariables[0].LocalType;

            foreach (CodeInstruction code in instructions)
            {
                yield return code;

                if (code.LoadsField(biologicalAgeTickFactorFromAgeCurve))
                {
                    fieldFound++;
                }

                if (fieldFound == 1 && code.Branches(out _))
                {
                    //I do this because every time this if is checked it will overwrite the out variable and I need it to not change after matching
                    label = (Label)code.operand;
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
                    yield return CodeInstruction.Call(typeof(GeneDef_GetDescriptionFull), nameof(MakeAgelessString));
                    yield return CodeInstruction.Call(typeof(StringBuilder), nameof(StringBuilder.AppendLine), parameters: [typeof(string)]);
                    yield return new CodeInstruction(OpCodes.Pop);
                    fieldFound++;
                }
            }
        }

        //[HarmonyTranspiler]
        //public static IEnumerable<CodeInstruction> MinAgeTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg, MethodBase original)
        //{
        //    FieldInfo minAgeActive = AccessTools.Field(typeof(GeneDef), nameof(GeneDef.minAgeActive));
        //    int fieldFound = 0;
        //    object label = new object();
        //    Type type = original.GetMethodBody().LocalVariables[0].LocalType;

        //    foreach (CodeInstruction code in instructions)
        //    {
        //        yield return code;

        //        if (code.LoadsField(minAgeActive))
        //        {
        //            fieldFound++;
        //        }

        //        if (fieldFound == 1 && code.opcode == OpCodes.Ble_Un_S)
        //        {
        //            label = code.operand;
        //        }

        //        if (fieldFound == 2 && code.IsStloc())
        //        {
        //            //if Settings.HARActive
        //            yield return CodeInstruction.LoadField(typeof(Settings), nameof(Settings.HARActive));
        //            yield return new CodeInstruction(OpCodes.Brfalse_S, label);
        //            yield return new CodeInstruction(OpCodes.Ldloc_0);
        //            //sb.AppendLine("WBR.MinAgeGeneWarning".Translate());
        //            yield return CodeInstruction.LoadField(type, "sb");
        //            yield return CodeInstruction.Call(typeof(GeneDef_GetDescriptionFull), nameof(MakeMinAgeString));
        //            yield return CodeInstruction.Call(typeof(StringBuilder), nameof(StringBuilder.AppendLine), parameters: new Type[] { typeof(string) });
        //            yield return new CodeInstruction(OpCodes.Pop);
        //            fieldFound++;
        //        }
        //    }
        //}

        private static bool CheckDef(GeneDef geneDef)
        {
            return Settings.HARActive && geneDef.defName == "Ageless";
        }

        private static string MakeAgelessString()
        {
            return "WBR.AgelessGeneWarning".Translate();
        }

        //private static string MakeMinAgeString()
        //{
        //    return "WBR.MinAgeGeneWarning".Translate();
        //}
    }
}
