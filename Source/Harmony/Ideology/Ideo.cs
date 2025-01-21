using HarmonyLib;
using RimWorld;

namespace BetterRomance.HarmonyPatches
{
    [HarmonyPatch(typeof(Ideo), nameof(Ideo.RecachePrecepts))]
    public static class Ideo_RecachePrecepts
    {
        public static void Postfix(Ideo __instance)
        {
            if (PreceptUtility.lovinPreceptCache.ContainsKey(__instance))
            {
                PreceptUtility.lovinPreceptCache.Remove(__instance);
            }
        }
    }
}
