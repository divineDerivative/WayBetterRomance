﻿using HarmonyLib;
using RimWorld;
using Verse;

namespace BetterRomance.HarmonyPatches
{
    [HarmonyPatch(typeof(TraitSet), nameof(TraitSet.GainTrait))]
    public static class TraitSet_GainTrait
    {
        public static bool Prefix(Trait trait, ref Trait __state, Pawn ___pawn)
        {
            //Only care about orientation traits
            if (OrientationUtility.OrientationTraits.Contains(trait.def))
            {
                //Check if pawn already has one
                if (___pawn.story.traits.HasTrait(RomanceDefOf.DynamicOrientation))
                {
                    __state = ___pawn.story.traits.GetTrait(RomanceDefOf.DynamicOrientation);
                }
                //Keep going just in case they somehow ended up with two orientation traits
                foreach (TraitDef traitDef in OrientationUtility.OrientationTraits)
                {
                    if (___pawn.story.traits.HasTrait(traitDef))
                    {
                        //Remember it for later
                        __state = ___pawn.story.traits.GetTrait(traitDef);
                        return true;
                    }
                }
            }
            return true;
        }

        public static void Postfix(Trait trait, ref Trait __state, Pawn ___pawn, bool suppressConflicts = false)
        {
            if (__state != null && __state.def != RomanceDefOf.DynamicOrientation)
            {
                //Check if a second orientation trait got added
                int traitCount = 0;
                foreach (TraitDef traitDef in OrientationUtility.OrientationTraits)
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
            //Convert the new trait
            if (OrientationUtility.OrientationTraits.Contains(trait.def))
            {
                Comp_Orientation.ConvertOrientation(___pawn, trait);
            }
        }
    }
}
