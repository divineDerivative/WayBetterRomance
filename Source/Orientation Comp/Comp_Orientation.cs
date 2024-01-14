using RimWorld;
using System.Collections.Generic;
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

        public static void ConvertOrientation(Pawn pawn, bool force = false)
        {
            Gender gender = pawn.gender;
            Comp_Orientation comp = pawn.CheckForComp<Comp_Orientation>();
            if (!comp.converted || force)
            {
                if (pawn.IsAro())
                {
                    comp.SetAttraction(Gender.Male, false, true, true);
                    comp.SetAttraction(Gender.Female, false, true, true);
                    comp.SetAttraction((Gender)3, false, true, true);
                }
                else
                {
                    if (pawn.IsBi())
                    {
                        comp.SetAttraction(Gender.Male, true, true, true);
                        comp.SetAttraction(Gender.Female, true, true, true);
                    }
                    else if (pawn.IsHetero())
                    {
                        comp.SetAttraction(gender, false, true, true);
                        comp.SetAttraction(gender.Opposite(), true, true, true);
                    }
                    else if (pawn.IsHomo())
                    {
                        comp.SetAttraction(gender, true, true, true);
                        comp.SetAttraction(gender.Opposite(), false, true, true);
                    }
                    if (pawn.IsAsexual())
                    {
                        comp.SetAttraction(Gender.Male, false, true, false);
                        comp.SetAttraction(Gender.Female, false, true, false);
                        comp.SetAttraction((Gender)3, false, true, false);
                    }
                    if (!Settings.NonBinaryActive)
                    {
                        comp.SetAttraction((Gender)3, false, true, true);
                    }
                }
                //Remove the orientation trait here probably
                List<Trait> toRemove = new List<Trait>();
                foreach (Trait trait in pawn.story.traits.allTraits)
                {
                    if (SexualityUtility.OrientationTraits.Contains(trait.def))
                    {
                        toRemove.Add(trait);
                    }
                }
                foreach (Trait trait in toRemove)
                {
                    pawn.story.traits.RemoveTrait(trait);
                }
                if (!pawn.story.traits.HasTrait(RomanceDefOf.DynamicOrientation))
                {
                    pawn.story.traits.GainTrait(new Trait(RomanceDefOf.DynamicOrientation));
                }
                comp.converted = true;
            }
        }

        private void SetAttraction(Gender gender, bool result, bool sexual, bool romantic)
        {
            if (sexual)
            {
                switch (gender)
                {
                    case Gender.Male:
                        this.sexual.men = result;
                        break;
                    case Gender.Female:
                        this.sexual.women = result;
                        break;
                    default:
                        this.sexual.enby = result;
                        break;
                }
            }
            if (romantic)
            {
                switch (gender)
                {
                    case Gender.Male:
                        this.romantic.men = result;
                        break;
                    case Gender.Female:
                        this.romantic.women = result;
                        break;
                    default:
                        this.romantic.enby = result;
                        break;
                }
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
