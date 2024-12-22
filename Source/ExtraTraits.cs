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
                bool mustLikeMen = pawn.HasAnyLovePartnerOfGender(Gender.Male);
                bool mustLikeWomen = pawn.HasAnyLovePartnerOfGender(Gender.Female);
                bool mustLikeEnby = pawn.HasAnyLovePartnerOfGender((Gender)3);

                string debugString = $"{pawn.LabelShort}, {pawn.gender}, rolled: ";
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
                    //LogUtil.Error($"Unable to resolve orientation conflicts for {pawn.LabelShort}");
                    Testing.Tests.logger.Log($"Unable to resolve orientation conflicts for {pawn.LabelShort}");
                    Testing.Tests.logger.Log(debugString);
                    Testing.Tests.logger.Log($"Gender: {pawn.gender}, Sexual: {comp.sexual.Prefix()}, Romantic: {comp.romantic.Prefix()}");
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
                        debugString += "asexual,";
                    }
                    //Bisexual
                    else if (sexualOrientation < (asexualChance + bisexualChance))
                    {
                        comp.SetSexualAttraction(Gender.Male, true);
                        comp.SetSexualAttraction(Gender.Female, true);
                        //What do with enby?
                        debugString += "bisexual,";
                    }
                    //Homosexual
                    else if (sexualOrientation < (asexualChance + bisexualChance + homosexualChance))
                    {
                        comp.SetSexualAttraction(pawn.gender, true);
                        comp.SetSexualAttraction(pawn.gender.Opposite(), false);
                        //What do with enby?
                        debugString += "homosexual,";
                    }
                    //Heterosexual
                    else
                        debugString += "heterosexual,";
                    {
                        comp.SetSexualAttraction(pawn.gender, false);
                        comp.SetSexualAttraction(pawn.gender.Opposite(), true);
                        //What do with enby?
                    }

                    //Remove these if they've been fulfilled
                    if (mustLikeMen && pawn.AttractedTo(Gender.Male, false))
                    {
                        mustLikeMen = false;
                    }
                    if (mustLikeWomen && pawn.AttractedTo(Gender.Female, false))
                    {
                        mustLikeWomen = false;
                    }
                    if (mustLikeEnby && pawn.AttractedTo((Gender)3, false))
                    {
                        mustLikeEnby = false;
                    }
                }

                void RollRomanticOrientation()
                {
                    //Set up romantic orientation chances
                    //Can't be aromantic if there's still a partner they have no attraction to
                    float aromanticChance = (mustLikeMen || mustLikeWomen || mustLikeEnby) ? 0f : romanticChances.None;
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
                        debugString += " aromantic";
                    }
                    //Biromantic
                    else if ((romanticOrientation -= aromanticChance) < biromanticChance)
                    {
                        comp.SetRomanticAttraction(Gender.Male, true);
                        comp.SetRomanticAttraction(Gender.Female, true);
                        debugString += " biromantic";
                    }
                    //Homoromantic
                    else if ((romanticOrientation -= biromanticChance) < homoromanticChance)
                    {
                        comp.SetRomanticAttraction(pawn.gender, true);
                        comp.SetRomanticAttraction(pawn.gender.Opposite(), false);
                        debugString += " homoromantic";
                    }
                    //Heteroromantic
                    else if ((romanticOrientation -= homoromanticChance) < heteroromanticChance)
                    {
                        comp.SetRomanticAttraction(pawn.gender, false);
                        comp.SetRomanticAttraction(pawn.gender.Opposite(), true);
                        debugString += " heteroromantic";
                    }
                }
            }
        }
    }
}
