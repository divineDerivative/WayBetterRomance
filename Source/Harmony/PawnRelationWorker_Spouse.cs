﻿using HarmonyLib;
using RimWorld;
using Verse;

namespace BetterRomance.HarmonyPatches
{
    //Respect sex repulsion rules
    [HarmonyPatch(typeof(PawnRelationWorker_Spouse), nameof(PawnRelationWorker_Spouse.GenerationChance))]
    public static class PawnRelationWorker_Spouse_GenerationChance
    {
        public static void Postfix(Pawn generated, Pawn other, ref float __result)
        {
            if (generated.SexRepulsed(other) || other.SexRepulsed(generated))
            {
                __result = 0f;
                return;
            }
        }
    }
}
