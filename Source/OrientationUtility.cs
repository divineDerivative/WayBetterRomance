using RimWorld;
using System.Collections.Generic;
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
        public static float AsexualRating(this Pawn pawn) => pawn.CheckForComp<Comp_SexRepulsion>().rating;

        public static bool IsAsexual(this Pawn pawn)
        {
            return pawn.TryGetComp<Comp_Orientation>()?.Asexual ?? false;
        }

        /// <summary>
        /// Checks if a marriage between <paramref name="first"/> and <paramref name="second"/> is allowed by orientation, settings, and sex repulsion rules for both pawns
        /// </summary>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <returns></returns>
        public static bool CouldWeBeMarried(this Pawn first, Pawn second)
        {
            if (!first.SpouseAllowed() || !RelationsUtility.AttractedToGender(first, second.gender))
            {
                return false;
            }
            if (!second.SpouseAllowed() || !RelationsUtility.AttractedToGender(second, first.gender))
            {
                return false;
            }
            if (first.SexRepulsed(second) || second.SexRepulsed(first))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Checks if <paramref name="first"/> is allowed to marry <paramref name="second"/> by <paramref name="first"/>'s orientation, settings, and sex repulsion rules
        /// </summary>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <returns></returns>
        public static bool WouldConsiderMarriage(this Pawn first, Pawn second)
        {
            if (!first.SpouseAllowed() || !first.AttractedTo(second, true) || first.SexRepulsed(second))
            {
                return false;
            }
            return true;
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

        /// <summary>
        /// Checks if a love relation between <paramref name="first"/> and <paramref name="second"/> is allowed by both orientations
        /// </summary>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <returns></returns>
        public static bool CouldWeBeLovers(this Pawn first, Pawn second) => RelationsUtility.AttractedToGender(first, second.gender) && RelationsUtility.AttractedToGender(second, first.gender);

        /// <summary>
        /// If <paramref name="pawn"/>'s asexual rating is low enough to refuse all sex. If <paramref name="other"/> is provided, they must also be asexual for result to be false.
        /// </summary>
        /// <param name="pawn"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static bool SexRepulsed(this Pawn pawn, Pawn other = null) => pawn.IsAsexual() && pawn.AsexualRating() < 0.2f && (other == null || !other.IsAsexual());

        /// <summary>
        /// If <paramref name="pawn"/>'s asexual rating is low enough to only accept sex from existing partners. If <paramref name="other"/> is provided, checks whether they are in a relationship.
        /// </summary>
        /// <param name="pawn"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static bool SexAverse(this Pawn pawn, Pawn other = null) => pawn.IsAsexual() && pawn.AsexualRating() < 0.5f && (other == null || !LovePartnerRelationUtility.LovePartnerRelationExists(pawn, other));

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
