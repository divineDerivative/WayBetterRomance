﻿using RimWorld;
using Verse;
using DivineFramework;
#if v1_5
using LudeonTK;
#endif

namespace BetterRomance
{
    public class Comp_Orientation : ThingComp
    {
        public class AttractionVars(Comp_Orientation parent)
        {
            private Comp_Orientation parent = parent;

            private bool men;
            private bool women;
            private bool enby;
            public bool Men => men;
            public bool Women => women;
            public bool Enby => enby;

            private bool unset = true;
            public bool Unset => unset;

            private Gender Gender => parent.Pawn.gender;
            public bool Pan => men && women && enby;
            public bool Bi => men && women;
            internal bool None => !men && !women && !enby;
            public bool Gay => Gender switch
            {
                Gender.Male => men && !women,
                Gender.Female => women && !men,
                (Gender)3 => enby && !men && !women,
                _ => false
            };
            public bool Straight => Gender switch
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

            public void ExposeData(bool romance)
            {
                Scribe_Values.Look(ref men, $"{(romance ? "romantically" : "sexually")}AttractedToMen", false);
                Scribe_Values.Look(ref women, $"{(romance ? "romantically" : "sexually")}AttractedToWomen", false);
                Scribe_Values.Look(ref enby, $"{(romance ? "romantically" : "sexually")}AttractedToEnby", false);
            }

            public void SetAttraction(Gender gender, bool value)
            {
                switch (gender)
                {
                    case Gender.Male:
                        men = value;
                        break;
                    case Gender.Female:
                        women = value;
                        break;
                    default:
                        enby = value;
                        break;
                }
                unset = false;
                parent.cachedLabel = null;
                parent.cachedDesc = null;
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
            sexual = new AttractionVars(this);
            romantic = new AttractionVars(this);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            sexual.ExposeData(false);
            romantic.ExposeData(true);
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

            pawn.story.traits.RemoveTrait(trait, true);
            if (!pawn.story.traits.HasTrait(RomanceDefOf.DynamicOrientation))
            {
                pawn.story.traits.GainTrait(new Trait(RomanceDefOf.DynamicOrientation));
            }
            comp.converted = true;
            comp.cachedLabel = null;
            comp.cachedDesc = null;
        }

        public void SetAttraction(Gender gender, bool sexual, bool romantic)
        {
            this.sexual.SetAttraction(gender, sexual);
            this.romantic.SetAttraction(gender, romantic);
        }

        public void SetRomanticAttraction(Gender gender, bool value) => romantic.SetAttraction(gender, value);

        public void SetSexualAttraction(Gender gender, bool value) => sexual.SetAttraction(gender, value);

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
            return $"{sexualPrefix}sexual\\{romanticPrefix}romantic";
        }

        private string Prefix(AttractionVars type)
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

        /// <summary>
        /// Makes sure that orientations follow overlap rules
        /// </summary>
        /// <param name="sexualChances"></param>
        /// <param name="romanticChances"></param>
        /// <returns></returns>
        public bool ResolveConflicts(OrientationChances sexualChances, OrientationChances romanticChances)
        {
            if (sexual.Unset)
            {
                LogUtil.Error($"Sexual orientation was not explicitly set for {Pawn.Name.ToStringShort}");
            }
            if (romantic.Unset)
            {
                LogUtil.Error($"Romantic orientation was not explicitly set for {Pawn.Name.ToStringShort}");
            }
            if (sexual.Unset || romantic.Unset)
            {
                return false;
            }
            //These two are allowed to have no overlap
            if (!Asexual && !Aromantic)
            {
                //At least one gender is true
                //Will need to add enby
                if (sexual.Men != romantic.Men && sexual.Women != romantic.Women)
                {
                    //We've excluded asexual and aromantic, and either orientation being bi would have passed already
                    //So we would only be here if one was gay and the other was straight
                    //Then we would essentially be deciding between making them biromantic or bisexual
                    //So I guess just roll between those?
                    float total = sexualChances.bi + romanticChances.bi;
                    if (total == 0f)
                    {
                        //If bi is not allowed for either, we would need to roll for which one to use to match
                        if (romantic.Gay && sexual.Straight)
                        {
                            float subtotal = sexualChances.homo + romanticChances.hetero;
                            if (Rand.Range(0f, subtotal) < sexualChances.homo)
                            {
                                sexual.SetAttraction(Gender.Male, romantic.Men);
                                sexual.SetAttraction(Gender.Female, romantic.Women);
                            }
                            else
                            {
                                romantic.SetAttraction(Gender.Male, sexual.Men);
                                romantic.SetAttraction(Gender.Female, sexual.Women);
                            }
                        }
                        if (sexual.Gay && romantic.Straight)
                        {
                            float subtotal = romanticChances.homo + sexualChances.hetero;
                            if (Rand.Range(0f, subtotal) < romanticChances.homo)
                            {
                                romantic.SetAttraction(Gender.Male, sexual.Men);
                                romantic.SetAttraction(Gender.Female, sexual.Women);
                            }
                            else
                            {
                                sexual.SetAttraction(Gender.Male, romantic.Men);
                                sexual.SetAttraction(Gender.Female, romantic.Women);
                            }
                        }
                    }
                    //Make them bisexual
                    else if (Rand.Range(0f, total) < sexualChances.bi)
                    {
                        SetSexualAttraction(Gender.Male, true);
                        SetSexualAttraction(Gender.Female, true);
                    }
                    //Make them biromantic
                    else
                    {
                        SetRomanticAttraction(Gender.Male, true);
                        SetRomanticAttraction(Gender.Female, true);
                    }

                }
                //Check that it worked
                if (sexual.Men != romantic.Men && sexual.Women != romantic.Women)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
