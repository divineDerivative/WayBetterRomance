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
                    return true;
                }
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
                    ___pawn.story.traits.RemoveTrait(__state, suppressConflicts);
                }
            }
            //Convert the new trait
            if (OrientationUtility.OrientationTraits.Contains(trait.def))
            {
                Comp_Orientation.ConvertOrientation(___pawn, trait);
            }
        }

        //private static bool HasTraitIncludeSuppressed(Pawn pawn, TraitDef traitDef)
        //{
        //    List<Trait> allTraits = pawn.story.traits.allTraits;
        //    for (int i = 0; i < allTraits.Count; i++)
        //    {
        //        if (allTraits[i].def == traitDef)
        //        {
        //            Log.Message("Trait found: " + traitDef.label);
        //            return true;
        //        }
        //    }
        //    return false;
        //}
    }
}
