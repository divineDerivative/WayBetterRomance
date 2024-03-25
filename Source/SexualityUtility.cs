using System.Collections.Generic;
using RimWorld;
using Verse;

namespace BetterRomance
{
    public static class SexualityUtility
    {
        public static bool editRepulsion;
        public static readonly List<TraitDef> OrientationTraits =
        [
            TraitDefOf.Gay,
            TraitDefOf.Bisexual,
            RomanceDefOf.Straight,
            TraitDefOf.Asexual,
            RomanceDefOf.HeteroAce,
            RomanceDefOf.HomoAce,
            RomanceDefOf.BiAce,
        ];

        public static List<TraitDef> asexualTraits = [TraitDefOf.Asexual, RomanceDefOf.BiAce, RomanceDefOf.HeteroAce, RomanceDefOf.HomoAce];

        /// <summary>
        /// A rating to use for determining sex repulsion for asexual pawns.
        /// </summary>
        /// <param name="pawn"></param>
        /// <returns>float between 0 and 1</returns>
        public static float AsexualRating(this Pawn pawn)
        {
            return pawn.CheckForComp<Comp_SexRepulsion>().rating;
        }

        /// <summary>
        /// Determines the romantic <see cref="Orientation"/> of a <paramref name="pawn"/> based on their traits
        /// </summary>
        /// <param name="pawn"></param>
        /// <returns></returns>
        public static Orientation GetOrientation(this Pawn pawn)
        {
            if (pawn.story != null && pawn.story.traits != null)
            {
                TraitSet traits = pawn.story.traits;
                if (traits.HasTrait(TraitDefOf.Gay) || traits.HasTrait(RomanceDefOf.HomoAce))
                {
                    return Orientation.Homo;
                }
                else if (traits.HasTrait(RomanceDefOf.Straight) || traits.HasTrait(RomanceDefOf.HeteroAce))
                {
                    return Orientation.Hetero;
                }
                else if (traits.HasTrait(TraitDefOf.Bisexual) || traits.HasTrait(RomanceDefOf.BiAce))
                {
                    return Orientation.Bi;
                }
                else if (traits.HasTrait(TraitDefOf.Asexual))
                {
                    return Orientation.None;
                }
            }
            return Orientation.None;
        }

        public static bool IsAsexual(this Pawn pawn)
        {
            if (pawn.story != null && pawn.story.traits != null)
            {
                foreach (Trait trait in pawn.story.traits.allTraits)
                {
                    if (asexualTraits.Contains(trait.def) && !trait.Suppressed)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool IsAro(this Pawn pawn)
        {
            return pawn.GetOrientation() == Orientation.None;
        }

        public static bool IsBi(this Pawn pawn)
        {
            return pawn.GetOrientation() == Orientation.Bi;
        }

        public static bool IsHetero(this Pawn pawn)
        {
            return pawn.GetOrientation() == Orientation.Hetero;
        }

        public static bool IsHomo(this Pawn pawn)
        {
            return pawn.GetOrientation() == Orientation.Homo;
        }
    }

    public enum Orientation
    {
        Homo,
        Hetero,
        Bi,
        None,
    }
}
