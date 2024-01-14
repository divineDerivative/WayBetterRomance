using RimWorld;
using System.Collections.Generic;
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
        public static float AsexualRating(this Pawn pawn) => pawn.CheckForComp<Comp_SexRepulsion>().rating;

        /// <summary>
        /// Determines the romantic <see cref="Orientation"/> of a <paramref name="pawn"/> based on their traits
        /// </summary>
        /// <param name="pawn"></param>
        /// <returns></returns>
        public static Orientation GetOrientation(this Pawn pawn)
        {
            if (pawn.story?.traits != null)
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
            if (pawn.story?.traits != null)
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

        public static bool IsAro(this Pawn pawn) => pawn.GetOrientation() == Orientation.None;

        public static bool IsBi(this Pawn pawn) => pawn.GetOrientation() == Orientation.Bi;

        public static bool IsHetero(this Pawn pawn) => pawn.GetOrientation() == Orientation.Hetero;

        public static bool IsHomo(this Pawn pawn) => pawn.GetOrientation() == Orientation.Homo;

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
            var comp = pawn.TryGetComp<Comp_Orientation>();
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
    }

    public enum Orientation
    {
        Homo,
        Hetero,
        Bi,
        None,
    }
}
