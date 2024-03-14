using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace BetterRomance.HarmonyPatches
{
    //This stops asexual pawns from initiating lovin' while in bed with a partner
    [HarmonyPatch(typeof(JobGiver_DoLovin), "TryGiveJob")]
    public static class JobGiver_DoLovin_TryGiveJob
    {
        public static bool Prefix(Pawn pawn, ref Job __result)
        {
            if (pawn.IsAsexual())
            {
                __result = null;
                return false;
            }
            return true;
        }

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