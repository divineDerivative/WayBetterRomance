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

        public static bool IsAromantic(this Pawn pawn)
        {
            return pawn.TryGetComp<Comp_Orientation>()?.Aromantic ?? false;
        }

        public static bool IsGay(this Pawn pawn, bool romance)
        {
            Comp_Orientation comp = pawn.TryGetComp<Comp_Orientation>();
            return (romance ? comp.romantic : comp.sexual).Gay;
        }

        public static bool IsStraight(this Pawn pawn, bool romance)
        {
            Comp_Orientation comp = pawn.TryGetComp<Comp_Orientation>();
            return (romance ? comp.romantic : comp.sexual).Straight;
        }

        public static bool IsBi(this Pawn pawn, bool romance)
        {
            Comp_Orientation comp = pawn.GetComp<Comp_Orientation>();
            return (romance ? comp.romantic : comp.sexual).Bi;
        }

        public static bool AttractedTo(this Pawn pawn, Gender gender, bool romance)
        {
            Comp_Orientation comp = pawn.TryGetComp<Comp_Orientation>();
            Comp_Orientation.AttractionVars type = romance ? comp.romantic : comp.sexual;
            switch(gender)
            {
                case Gender.Male:
                    return type.men;
                case Gender.Female:
                    return type.women;
                case (Gender)3:
                    return type.enby;
                default:
                    return false;
            };
        }

        public static bool AttractedTo(this Pawn pawn, Pawn other, bool romance)
        {
            return pawn.AttractedTo(other.gender, romance);
        }

        public static bool AttractionBetween(Pawn first, Pawn second, bool romance)
        {
            return first.AttractedTo(second, romance) && second.AttractedTo(first, romance);
        }
    }
}
