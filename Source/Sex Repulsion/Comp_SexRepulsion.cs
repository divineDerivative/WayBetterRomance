using Verse;

namespace BetterRomance
{
    public class Comp_SexRepulsion : ThingComp
    {
        public Pawn Pawn => (Pawn)parent;
        public float rating;

        float InitialRating()
        {
            Rand.PushState();
            Rand.Seed = Pawn.thingIDNumber;
            float rating = Rand.Range(0f, 1f);
            Rand.PopState();
            return rating;
        }

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            rating = InitialRating();
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref rating, "asexualRating", -1f);
        }
    }
}