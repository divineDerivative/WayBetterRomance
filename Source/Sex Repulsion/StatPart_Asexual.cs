using RimWorld;
using Verse;

namespace BetterRomance
{
    public class StatPart_Asexual : StatPart
    {
        private bool ActiveFor(Pawn pawn) => pawn.IsAsexual();

        public override void TransformValue(StatRequest req, ref float val)
        {
            if (req.HasThing && req.Thing is Pawn pawn && ActiveFor(pawn))
            {
                val = 1f - pawn.AsexualRating();
            }
        }

        public override string ExplanationPart(StatRequest req) => null;
    }
}
