using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse.AI;

namespace BetterRomance.HarmonyPatches
{
    [HarmonyPatch(typeof(ThinkNode_ChancePerHour_Lovin), "MtbHours")]
    public static class ThinkNode_ChancePerHour_Lovin_MtbHours
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction code in instructions)
            {
                if (code.Calls(AccessTools.Method(typeof(LovePartnerRelationUtility), nameof(LovePartnerRelationUtility.GetPartnerInMyBed))))
                {
                    yield return CodeInstruction.Call(typeof(RomanceUtilities), nameof(RomanceUtilities.GetPartnerInMyBedForLovin));
                }
                else
                {
                    yield return code;
                }
            }
        }
    }
}
