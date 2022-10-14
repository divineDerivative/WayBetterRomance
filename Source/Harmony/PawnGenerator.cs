using HarmonyLib;
using RimWorld;
using Verse;

namespace BetterRomance
{
    //Orientation traits are now added with a new method, don't allow that method to run in order to use user settings
    [HarmonyPatch(typeof(PawnGenerator), "TryGenerateSexualityTraitFor")]
    public static class PawnGenerator_TryGenerateSexualityTraitFor
    {
        public static bool Prefix(Pawn pawn, bool allowGay)
        {
            //Just use my method instead
            pawn.EnsureTraits();
            //Do anything with the allowGay bool?
            return false;
        }
    }
}