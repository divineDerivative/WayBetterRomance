using HarmonyLib;
using RimWorld;
using Verse;

namespace BetterRomance
{
    //Add orientation traits if not already present, after HAR adds traits
    [HarmonyPatch(typeof(PawnGenerator), "GenerateTraits")]
    [HarmonyAfter(new string[] { "rimworld.erdelf.alien_race.main" })]
    public static class PawnGenerator_GenerateTraits
    {
        public static void Postfix(Pawn pawn)
        {
            pawn.EnsureTraits();
        }
    }
}