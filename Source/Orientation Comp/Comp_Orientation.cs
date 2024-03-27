using RimWorld;
using System;
using System.Security.Policy;
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
            internal Gender gender;
            public bool Pan => men && women && enby;
            public bool Bi => men && women;
            internal bool None => !men && !women && !enby;
            public bool Gay => gender switch
            {
                Gender.Male => men && !women,
                Gender.Female => women && !men,
                (Gender)3 => enby && !men && !women,
                _ => false
            };
            public bool Straight => gender switch
            {
                Gender.Male => women && !men,
                Gender.Female => men && !women,
                (Gender)3 => (men && !women) || (women && !men),
                _ => false
            };

            internal string GenderString()
            {
                if (Pan)
                {
                    return "WBR.TraitDescPan".Translate();
                }
                if (None)
                {
                    return null;
                }
                string firstGender = null;
                string secondGender = null;
                if (men)
                {
                    firstGender = "WBR.TraitDescMen".Translate();
                }
                if (women)
                {
                    if (firstGender == null)
                    {
                        firstGender = "WBR.TraitDescWomen".Translate();
                    }
                    else
                    {
                        secondGender = "WBR.TraitDescWomen".Translate();
                    }
                }
                if (enby)
                {
                    if (firstGender == null)
                    {
                        firstGender = "WBR.TraitDescEnby".Translate();
                    }
                    else
                    {
                        secondGender = "WBR.TraitDescEnby".Translate();
                    }
                }
                string result = firstGender;
                if (secondGender != null)
                {
                    result += "WBR.TraitDescAnd".Translate() + secondGender;
                }
                return result;
            }
        }

        public AttractionVars sexual;
        public AttractionVars romantic;
        //Not sure if I'll need this, it's not being used right now
        public bool converted;
        public Pawn Pawn => parent as Pawn;
        public Gender Gender => Pawn.gender;
        public bool Asexual => sexual.None;
        public bool Aromantic => romantic.None;
        private string cachedDesc;
        public string Desc
        {
            get
            {
                cachedDesc ??= BuildDescription();
                return cachedDesc;
            }
        }
        private string cachedLabel;
        public string Label
        {
            get
            {
                cachedLabel ??= GetLabel();
                return cachedLabel;
            }
        }

        private string BuildDescription()
        {
            //Just have two sentences
            //Well, just one sentence if they would basically be the same
            if (Asexual && Aromantic)
            {
                return "WBR.TraitDescAceAro".Translate(Pawn).CapitalizeFirst();
            }
            string sexualGenders = sexual.GenderString();
            string romanticGenders = romantic.GenderString();
            if (sexualGenders == romanticGenders)
            {
                return "WBR.TraitDescBoth".Translate(Pawn, sexualGenders).CapitalizeFirst();
            }
            else if (Asexual)
            {
                return "WBR.TraitDescAsexual".Translate(Pawn, romanticGenders).CapitalizeFirst();
            }
            else if (Aromantic)
            {
                return "WBR.TraitDescAromantic".Translate(Pawn, sexualGenders).CapitalizeFirst();
            }
            else
            {
                return "WBR.TraitDescDifferent".Translate(Pawn, sexualGenders, romanticGenders).CapitalizeFirst();
            }
        }

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
            comp.cachedLabel = null;
            comp.cachedDesc = null;
        }

        private void SetAttraction(Gender gender, bool sexual, bool romantic)
        {
            this.sexual.gender = Pawn.gender;
            this.romantic.gender = Pawn.gender;
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

        private string GetLabel()
        {
            string sexualPrefix = Prefix(sexual);
            string romanticPrefix = Prefix(romantic);
            if (sexualPrefix == romanticPrefix)
            {
                return sexualPrefix switch
                {
                    "Homo" => "Gay",
                    "Hetero" => "Straight",
                    _ => Aromantic ? romanticPrefix + "romantic" : sexualPrefix + "sexual",
                };
            }
            if (Asexual)
            {
                return $"{sexualPrefix}sexual ({romanticPrefix})";
            }
            return $"{sexualPrefix}sexual\\{romanticPrefix}romantic";
        }

        internal string Prefix(AttractionVars type)
        {
            if (type.Pan)
            {
                return "Pan";
            }
            if (type.None)
            {
                return "A";
            }
            if (type.Bi)
            {
                return "Bi";
            }
            if (type.Gay)
            {
                return "Homo";
            }
            if (type.Straight)
            {
                return "Hetero";
            }
            return string.Empty;
        }
    }
}
