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
            internal bool unset = true;
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
            sexual.gender = Pawn.gender;
            romantic = new AttractionVars();
            romantic.gender = Pawn.gender;
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
            cachedLabel = null;
            cachedDesc = null;
            this.sexual.unset = false;
            this.romantic.unset = false;
        }

        public void SetRomanticAttraction(Gender gender, bool value)
        {
            switch (gender)
            {
                case Gender.Male:
                    this.romantic.men = value;
                    break;
                case Gender.Female:
                    this.romantic.women = value;
                    break;
                default:
                    this.romantic.enby = value;
                    break;
            }
            cachedLabel = null;
            cachedDesc = null;
            this.romantic.unset = false;
        }

        public void SetSexualAttraction(Gender gender, bool value)
        {
            switch (gender)
            {
                case Gender.Male:
                    this.sexual.men = value;
                    break;
                case Gender.Female:
                    this.sexual.women = value;
                    break;
                default:
                    this.sexual.enby = value;
                    break;
            }
            cachedLabel = null;
            cachedDesc = null;
            this.sexual.unset = false;
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

        /// <summary>
        /// Makes sure that orientations follow overlap rules
        /// </summary>
        /// <param name="sexualChances"></param>
        /// <param name="romanticChances"></param>
        /// <returns></returns>
        public bool ResolveConflicts(OrientationChances sexualChances, OrientationChances romanticChances)
        {
            if (sexual.unset)
            {
                LogUtil.Error($"Sexual orientation was not explicitly set for {Pawn.Name.ToStringShort}");
            }
            if (romantic.unset)
            {
                LogUtil.Error($"Romantic orientation was not explicitly set for {Pawn.Name.ToStringShort}");
            }
            if (sexual.unset || romantic.unset)
            {
                return false;
            }
            //These two are allowed to have no overlap
            if (!Asexual && !Aromantic)
            {
                //At least one gender is true
                //Will need to add enby
                if (sexual.men != romantic.men && sexual.women != romantic.women)
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
                                sexual.women = romantic.women;
                                sexual.men = romantic.men;
                            }
                            else
                            {
                                romantic.women = sexual.women;
                                romantic.men = sexual.men;
                            }
                        }
                        if (sexual.Gay && romantic.Straight)
                        {
                            float subtotal = romanticChances.homo + sexualChances.hetero;
                            if (Rand.Range(0f, subtotal) < romanticChances.homo)
                            {
                                romantic.women = sexual.women;
                                romantic.men = sexual.men;
                            }
                            else
                            {
                                sexual.women = romantic.women;
                                sexual.men = romantic.men;
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
                if (sexual.men != romantic.men && sexual.women != romantic.women)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
