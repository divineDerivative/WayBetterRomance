using HarmonyLib;
using RimWorld;
using Verse;

namespace BetterRomance.HarmonyPatches
{
    [HarmonyPatch(typeof(TraitSet), nameof(TraitSet.GainTrait))]
    public static class TraitSet_GainTrait
    {
        public static bool Prefix(Trait trait, ref Trait __state, Pawn ___pawn, ref bool suppressConflicts)
        {
            //Only care about orientation traits
            if (SexualityUtility.OrientationTraits.Contains(trait.def))
            {
                //Check if pawn already has one
                foreach (TraitDef traitDef in SexualityUtility.OrientationTraits)
                {
                    if (___pawn.story.traits.HasTrait(traitDef))
                    {
                        //Remember it for later
                        __state = ___pawn.story.traits.GetTrait(traitDef);
                        //Suppressing the old trait makes it hard to remove, since HasTrait will return false
                        suppressConflicts = false;
                        return true;
                    }
                }
            }
            return true;
        }

        public static void Postfix(ref Trait __state, Pawn ___pawn)
        {
            if (__state != null)
            {
                //Check if a second orientation trait got added
                int traitCount = 0;
                foreach (TraitDef traitDef in SexualityUtility.OrientationTraits)
                {
                    if (___pawn.story.traits.HasTrait(traitDef))
                    {
                        traitCount++;
                    }
                }
                //Remove the old trait
                if (traitCount > 1)
                {
                    ___pawn.story.traits.RemoveTrait(__state, true);
                }
            }
        }
    }
}
