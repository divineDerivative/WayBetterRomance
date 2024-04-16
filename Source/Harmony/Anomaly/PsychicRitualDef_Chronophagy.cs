using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using Verse.AI.Group;

#if v1_5
namespace BetterRomance.HarmonyPatches
{
    [HarmonyPatch(typeof(PsychicRitualDef_Chronophagy), nameof(PsychicRitualDef_Chronophagy.BlockingIssues), MethodType.Enumerator)]
    public static class PsychicRitualDef_Chronophagy_BlockingIssues
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            FieldInfo assignments = AccessTools.Field(original.DeclaringType, "assignments");

            int num = 0;
            foreach (CodeInstruction code in instructions)
            {
                if (code.LoadsConstant(13))
                {
                    //The first two are invokerRole

                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return assignments.LoadField();
                    yield return new CodeInstruction(OpCodes.Ldloc_2);
                    if (num <= 2)
                    {
                        yield return CodeInstruction.LoadField(typeof(PsychicRitualDef_Chronophagy), "invokerRole");
                    }
                    else
                    {
                        yield return CodeInstruction.LoadField(typeof(PsychicRitualDef_Chronophagy), "targetRole");
                    }
                    yield return CodeInstruction.Call(typeof(PsychicRitualRoleAssignments), nameof(PsychicRitualRoleAssignments.FirstAssignedPawn));
                    yield return CodeInstruction.LoadField(typeof(Pawn), nameof(Pawn.ageTracker));
                    yield return new CodeInstruction(OpCodes.Callvirt, CodeInstructionMethods.AdultMinAge);
                    yield return new CodeInstruction(OpCodes.Conv_I4);
                    num++;
                }
                else
                {
                    yield return code;
                }
            }
        }
    }

    [HarmonyPatch(typeof(PsychicRitualDef_Chronophagy), nameof(PsychicRitualDef_Chronophagy.OutcomeDescription))]
    public static class PsychicRitualDef_Chronophagy_OutcomeDescription
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction code in instructions)
            {
                if (code.LoadsConstant(13f))
                {
                    yield return new CodeInstruction(OpCodes.Ldloc_1);
                    yield return CodeInstruction.LoadField(typeof(Pawn), nameof(Pawn.ageTracker));
                    yield return new CodeInstruction(OpCodes.Callvirt, CodeInstructionMethods.AdultMinAge);
                }
                else
                {
                    yield return code;
                }
            }
        }
    }
}
#endif
