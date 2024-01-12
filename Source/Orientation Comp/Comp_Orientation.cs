using RimWorld;
using Verse;

namespace BetterRomance.Orientation_Comp
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
        public Pawn Pawn => parent as Pawn;

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            sexual = new AttractionVars();
            romantic = new AttractionVars();
        }

        public static void ConvertOrientation(Pawn pawn)
        {
            Comp_Orientation comp = pawn.TryGetComp<Comp_Orientation>();
            Gender gender = pawn.gender;
            if (comp == null)
            {
                if (pawn.IsAro())
                {
                    comp.SetAttraction(Gender.Male, false, true, true);
                    comp.SetAttraction(Gender.Female, false, true, true);
                    comp.SetAttraction((Gender)3, false, true, true);
                    return;
                }
                if (pawn.IsBi())
                {
                    comp.SetAttraction(Gender.Male, true, true, true);
                    comp.SetAttraction(Gender.Female, true, true, true);
                }
                if (pawn.IsHetero())
                {
                    comp.SetAttraction(gender, false, true, true);
                    comp.SetAttraction(gender.Opposite(), true, true, true);
                }
                if (pawn.IsHomo())
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
                this.romantic.women = result;
            }
        }
    }
}
