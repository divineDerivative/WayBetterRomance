using RimWorld;
using Verse;
using static BetterRomance.WBRLogger;

namespace BetterRomance
{
    public static class ExtraTraits
    {
        public static void EnsureTraits(this Pawn pawn)
        {
            //Don't give orientation to kids
            //Don't give them to drones either
            if (pawn.DevelopmentalStage.Adult() && !pawn.DroneCheck())
            {
                foreach (TraitDef def in OrientationUtility.OrientationTraits)
                {
                    if (pawn.story.traits.HasTrait(def))
                    {
                        Comp_Orientation.ConvertOrientation(pawn, pawn.story.traits.GetTrait(def));
                    }
                }
                if (pawn.TryGetComp<Comp_Orientation>() == null)
                {
                    AssignOrientation(pawn);
                }
            }
        }

        public static void AssignOrientation(Pawn pawn)
        {
            if (pawn.gender == Gender.None && pawn.def.defName != "RE_Asari")
            {
                return;
            }
            if (pawn.story != null)
            {
                //Give them the dynamic trait if they don't already have one
                if (!pawn.story.traits.HasTrait(RomanceDefOf.DynamicOrientation))
                {
                    pawn.story.traits.GainTrait(new Trait(RomanceDefOf.DynamicOrientation));
                }
                //Grab or create the comp
                Comp_Orientation comp = pawn.CheckForComp<Comp_Orientation>();
                //Grab the relevant orientation objects
                OrientationChances sexualChances = pawn.TryGetComp<WBR_SettingsComp>().orientation?.sexual ?? BetterRomanceMod.settings.sexualOrientations;
                OrientationChances romanticChances = pawn.TryGetComp<WBR_SettingsComp>().orientation?.asexual ?? BetterRomanceMod.settings.romanticOrientations;

                //Check for existing partners first
                bool mightBeStraight = false;
                if (LovePartnerRelationUtility.HasAnyLovePartnerOfTheOppositeGender(pawn) || LovePartnerRelationUtility.HasAnyExLovePartnerOfTheOppositeGender(pawn))
                {
                    mightBeStraight = true;
                }
                bool mightBeGay = false;
                if (LovePartnerRelationUtility.HasAnyLovePartnerOfTheSameGender(pawn) || LovePartnerRelationUtility.HasAnyExLovePartnerOfTheSameGender(pawn))
                {
                    mightBeGay = true;
                }

                RollSexualOrientation();

                //If not complex, copy from sexual
                if (!BetterRomanceMod.settings.complex)
                {
                    comp.romantic.CopyFrom(comp.sexual);
                }
                //If complex, roll for romantic
                else
                {
                    RollRomanticOrientation();
                    //If they're not aromantic and the complex roll fails, copy from sexual
                    if (!comp.Aromantic && Rand.Value >= BetterRomanceMod.settings.complexChance / 100f)
                    {
                        comp.romantic.CopyFrom(comp.sexual);
                    }
                }

                if (!comp.ResolveConflicts())
                {
                    LogUtil.Error($"Unable to resolve orientation conflicts for {pawn.LabelShort}");
                }

                void RollSexualOrientation()
                {
                    //Assign chances
                    float asexualChance = sexualChances.None;
                    float bisexualChance = sexualChances.Bi;
                    float homosexualChance = sexualChances.Homo;
                    float heterosexualChance = sexualChances.Hetero;

                    //Roll for sexual orientation
                    float sexualOrientation = Rand.Value;

                    //Asexual
                    if (sexualOrientation < asexualChance)
                    {
                        //Decide what to do here
                        if (pawn.story.traits.HasTrait(RomanceDefOf.Philanderer))
                        {
                            pawn.story.traits.GainTrait(new(TraitDefOf.Bisexual));
                        }
                        comp.SetSexualAttraction(Gender.Male, false);
                        comp.SetSexualAttraction(Gender.Female, false);
                        comp.SetSexualAttraction((Gender)3, false);
                    }
                    //Bisexual
                    else if (sexualOrientation < (asexualChance + bisexualChance))
                    {
                        comp.SetSexualAttraction(Gender.Male, true);
                        comp.SetSexualAttraction(Gender.Female, true);
                        //What do with enby?
                    }
                    //Homosexual
                    else if (sexualOrientation < (asexualChance + bisexualChance + homosexualChance))
                    {
                        comp.SetSexualAttraction(pawn.gender, true);
                        comp.SetSexualAttraction(pawn.gender.Opposite(), false);
                        //What do with enby?
                    }
                    //Heterosexual
                    else
                    {
                        comp.SetSexualAttraction(pawn.gender, false);
                        comp.SetSexualAttraction(pawn.gender.Opposite(), true);
                        //What do with enby?
                    }

                    //Remove these if they've been fulfilled
                    if (mightBeGay && pawn.AttractedTo(pawn.gender, false))
                    {
                        mightBeGay = false;
                    }
                    if (mightBeStraight && pawn.AttractedTo(pawn.gender.Opposite(), false))
                    {
                        mightBeStraight = false;
                    }
                }

                void RollRomanticOrientation()
                {
                    //Set up romantic orientation chances
                    //Can't be aromantic if there's still a partner they have no attraction to
                    float aromanticChance = (mightBeGay || mightBeStraight) ? 0f : romanticChances.None;
                    float biromanticChance = romanticChances.Bi;
                    float homoromanticChance = romanticChances.Homo;
                    float heteroromanticChance = romanticChances.Hetero;
                    //If they're asexual, we don't want to modify chances at all (except for aromantic)
                    //I think bi should always be allowed?
                    if (!comp.Asexual)
                    {
                        //In order to be homoromantic and follow the rules, they'd have to be sexually attracted to their own gender
                        if (!pawn.AttractedTo(pawn.gender, false))
                        {
                            homoromanticChance = 0f;
                        }
                        //In order to be heteroromantic and follow the rules, they'd have to be sexually attracted to the opposite gender
                        if (!pawn.AttractedTo(pawn.gender.Opposite(), false))
                        {
                            heteroromanticChance = 0f;
                        }
                        //This of course only works for binary genders :(
                    }
                    float total = aromanticChance + biromanticChance + homoromanticChance + heteroromanticChance;
                    //Roll for romantic orientation
                    float romanticOrientation = Rand.Range(0f, total);

                    //Aromantic
                    if (romanticOrientation < aromanticChance)
                    {
                        comp.SetRomanticAttraction(Gender.Male, false);
                        comp.SetRomanticAttraction(Gender.Female, false);
                        comp.SetRomanticAttraction((Gender)3, false);
                    }
                    //Biromantic
                    else if ((romanticOrientation -= aromanticChance) < biromanticChance)
                    {
                        comp.SetRomanticAttraction(Gender.Male, true);
                        comp.SetRomanticAttraction(Gender.Female, true);
                    }
                    //Homoromantic
                    else if ((romanticOrientation -= biromanticChance) < homoromanticChance)
                    {
                        comp.SetRomanticAttraction(pawn.gender, true);
                        comp.SetRomanticAttraction(pawn.gender.Opposite(), false);
                    }
                    //Heteroromantic
                    else if ((romanticOrientation -= homoromanticChance) < heteroromanticChance)
                    {
                        comp.SetRomanticAttraction(pawn.gender, false);
                        comp.SetRomanticAttraction(pawn.gender.Opposite(), true);
                    }
                }
            }
        }
    }
}
