using HarmonyLib;
using System.Collections.Generic;
using Verse;

namespace BetterRomance.HarmonyPatches
{
#if v1_5
    [HarmonyPatch(typeof(Pawn_HealthTracker), nameof(Pawn_HealthTracker.CanCrawl), MethodType.Getter)]
    public static class Pawn_HealthTracker_CanCrawl
    {
        //Change 8 to something else
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction code in instructions)
            {
                if (code.LoadsConstant(8))
                {
                    yield return CodeInstruction.LoadArgument(0);
                    yield return CodeInstruction.LoadField(typeof(Pawn_HealthTracker), "pawn");
                    yield return CodeInstruction.Call(typeof(Pawn_HealthTracker_CanCrawl), nameof(Helper));
                }
                else
                {
                    yield return code;
                }
            }
        }

        public static int Helper(Pawn pawn)
        {
            return pawn.GetGrowthMoment(0) + 1;
        }
    }
#endif
}
