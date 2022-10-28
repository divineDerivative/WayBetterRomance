using RimWorld;
using Verse;

namespace BetterRomance
{
    public class StatPart_Asexual : StatPart
    {
        private bool ActiveFor(Pawn pawn)
        {
            if (pawn.story.traits.HasTrait(TraitDefOf.Asexual))
            {
                return true;
            }
            return false;
        }

        public override void TransformValue(StatRequest req, ref float val)
        {
            if (req.HasThing && req.Thing is Pawn pawn && ActiveFor(pawn))
            {
                val = 1f - pawn.AsexualRating();
            }
        }

        public override string ExplanationPart(StatRequest req)
        {
            return null;
        }
    }
}
