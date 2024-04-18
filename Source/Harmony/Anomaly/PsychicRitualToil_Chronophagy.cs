using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using Verse.AI.Group;

#if v1_5
namespace BetterRomance.HarmonyPatches
{
    [HarmonyPatch(typeof(PsychicRitualToil_Chronophagy), "ReverseAgePawn")]
    public static class PsychicRitualToil_Chronophagy_ReverseAgePawn
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return instructions.AdultMinAgeFloat(OpCodes.Ldarg_0);
        }
    }
}
#endif
