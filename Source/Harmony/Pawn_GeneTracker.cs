using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using HarmonyLib;

namespace BetterRomance.HarmonyPatches
{
    [HarmonyPatch(typeof(Pawn_GeneTracker), "Notify_GenesChanged")]
    public static class Pawn_GeneTracker_Notify_GenesChanged
    {
        public static void Postfix(GeneDef addedOrRemovedGene, Pawn_GeneTracker __instance)
        {
            if (addedOrRemovedGene.defName == "DiseaseFree")
            {
                SettingsUtilities.CachedNonSenescentPawns.Remove(__instance.pawn);
                SettingsUtilities.CachedSenescentPawns.Remove(__instance.pawn);
            }
        }
    }
}
