using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
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
            List<CodeInstruction> toGetPawn = new()
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                assignments.LoadField(),
                new CodeInstruction(OpCodes.Ldloc_2),
                CodeInstruction.LoadField(typeof(PsychicRitualDef_Chronophagy), "invokerRole"),
                CodeInstruction.Call(typeof(PsychicRitualRoleAssignments), nameof(PsychicRitualRoleAssignments.FirstAssignedPawn)),
            };
            //Use invokerRole twice
            List<CodeInstruction> codes = instructions.AdultMinAgeInt(toGetPawn, false).ToList();
            codes = codes.AdultMinAgeInt(toGetPawn, false).ToList();
            //Then use targetRole for the rest
            toGetPawn[3] = CodeInstruction.LoadField(typeof(PsychicRitualDef_Chronophagy), "targetRole");
            codes = codes.AdultMinAgeInt(toGetPawn, true).ToList();
            return codes.AsEnumerable();
        }
    }

    [HarmonyPatch(typeof(PsychicRitualDef_Chronophagy), nameof(PsychicRitualDef_Chronophagy.OutcomeDescription))]
    public static class PsychicRitualDef_Chronophagy_OutcomeDescription
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return instructions.AdultMinAgeFloat(OpCodes.Ldloc_1);
        }
    }
}
#endif
