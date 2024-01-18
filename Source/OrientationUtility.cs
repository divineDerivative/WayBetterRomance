using System.Collections.Generic;
using RimWorld;
using Verse;

namespace BetterRomance
{
    public static class OrientationUtility
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

        /// <summary>
        /// A rating to use for determining sex repulsion for asexual pawns.
        /// </summary>
        /// <param name="pawn"></param>
        /// <returns>float between 0 and 1</returns>
        public static float AsexualRating(this Pawn pawn)
        {
            return pawn.CheckForComp<Comp_SexRepulsion>().rating;
        }

        public static bool IsAsexual(this Pawn pawn)
        {
            return pawn.TryGetComp<Comp_Orientation>()?.Asexual ?? false;
        }

        public static bool AttractedTo(this Pawn pawn, Gender gender, bool romance)
        {
            Comp_Orientation comp = pawn.TryGetComp<Comp_Orientation>();
            switch (gender)
            {
                case Gender.Male:
                    return romance ? comp.romantic.men : comp.sexual.men;
                case Gender.Female:
                    return romance ? comp.romantic.women : comp.sexual.women;
                case (Gender)3:
                    return romance ? comp.romantic.enby : comp.sexual.enby;
                default:
                    return false;
            }
        }

        public static bool AttractedTo(this Pawn pawn, Pawn other, bool romance)
        {
            return pawn.AttractedTo(other.gender, romance);
        }

        //Might need to adjust this for HAR races that can reproduce differently
        public static bool CouldMarryOtherParent(this Pawn pawn)
        {
            Comp_Orientation comp = pawn.TryGetComp<Comp_Orientation>();
            switch (pawn.gender)
            {
                case Gender.Male:
                    return comp.sexual.women;
                case Gender.Female:
                    return comp.sexual.men;
                default:
                    return false;
            }
        }
    }
}
