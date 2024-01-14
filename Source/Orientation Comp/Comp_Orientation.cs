using RimWorld;
using Verse;
#if !v1_4
using LudeonTK;
#endif
using static BetterRomance.WBRLogger;

namespace BetterRomance
{
    public class Comp_Orientation : ThingComp
    {
        public class AttractionVars
        {
            public bool men;
            public bool women;
            public bool enby;
        }

        public AttractionVars sexual;
        public AttractionVars romantic;
        //Not sure if I'll need this, it's not being used right now
        public bool converted;
        public Pawn Pawn => parent as Pawn;
        public Gender Gender => Pawn.gender;
        public bool Asexual => !sexual.men && !sexual.women && (!Settings.NonBinaryActive || !sexual.enby);
        public bool Aromantic => !romantic.men && !romantic.women && (!Settings.NonBinaryActive || !romantic.enby);

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            sexual = new AttractionVars();
            romantic = new AttractionVars();
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref sexual.men, "sexuallyAttractedToMen", false);
            Scribe_Values.Look(ref sexual.women, "sexuallyAttractedToWomen", false);
            Scribe_Values.Look(ref sexual.enby, "sexuallyAttractedToEnby", false);
            Scribe_Values.Look(ref romantic.men, "romanticallyAttractedToMen", false);
            Scribe_Values.Look(ref romantic.women, "romanticallyAttractedToWomen", false);
            Scribe_Values.Look(ref romantic.enby, "romanticallyAttractedToEnby", false);
            Scribe_Values.Look(ref converted, "orientationConverted", false);
        }

        public static void ConvertOrientation(Pawn pawn, Trait trait)
        {
            Gender gender = pawn.gender;
            Comp_Orientation comp = pawn.CheckForComp<Comp_Orientation>();

            if (trait.def == TraitDefOf.Asexual)
            {
                comp.SetAttraction(Gender.Male, false, false);
                comp.SetAttraction(Gender.Female, false, false);
                comp.SetAttraction((Gender)3, false, false);
            }
            else
            {
                if (trait.def == TraitDefOf.Bisexual)
                {
                    comp.SetAttraction(Gender.Male, true, true);
                    comp.SetAttraction(Gender.Female, true, true);
                }
                else if (trait.def == RomanceDefOf.Straight)
                {
                    comp.SetAttraction(gender, false, false);
                    comp.SetAttraction(gender.Opposite(), true, true);
                }
                else if (trait.def == TraitDefOf.Gay)
                {
                    comp.SetAttraction(gender, true, true);
                    comp.SetAttraction(gender.Opposite(), false, false);
                }
                else if (trait.def == RomanceDefOf.BiAce)
                {
                    comp.SetAttraction(Gender.Male, false, true);
                    comp.SetAttraction(Gender.Female, false, true);
                }
                else if (trait.def == RomanceDefOf.HeteroAce)
                {
                    comp.SetAttraction(gender, false, false);
                    comp.SetAttraction(gender.Opposite(), false, true);

                }
                else if (trait.def == RomanceDefOf.HomoAce)
                {
                    comp.SetAttraction(gender, false, true);
                    comp.SetAttraction(gender.Opposite(), false, false);
                }
            }

            //LogUtil.Message($"Removing {trait.Label} from {pawn.LabelShort}");
            pawn.story.traits.RemoveTrait(trait);
            if (!pawn.story.traits.HasTrait(RomanceDefOf.DynamicOrientation))
            {
                pawn.story.traits.GainTrait(new Trait(RomanceDefOf.DynamicOrientation));
            }
            comp.converted = true;
        }

        private void SetAttraction(Gender gender, bool sexual, bool romantic)
        {

            switch (gender)
            {
                case Gender.Male:
                    this.sexual.men = sexual;
                    this.romantic.men = romantic;
                    break;
                case Gender.Female:
                    this.sexual.women = sexual;
                    this.romantic.women = romantic;
                    break;
                default:
                    this.sexual.enby = sexual;
                    this.romantic.enby = romantic;
                    break;
            }
        }

        public string GetLabel()
        {
            //Bisexual, pansexual, straight, gay, asexual
            if (sexual.men && sexual.women && sexual.enby)
            {
                return "Pansexual";
            }
            if (sexual.men && sexual.women)
            {
                return "Bisexual";
            }
            if (!sexual.men && !sexual.women && (!Settings.NonBinaryActive || !sexual.enby))
            {
                return "Asexual";
            }
            return Pawn.AttractedTo(Gender, false) ? "Gay" : "Straight";
        }
    }
}
