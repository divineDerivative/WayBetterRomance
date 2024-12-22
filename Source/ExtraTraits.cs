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
                    LogUtil.Error($"Unable to resolve orientation conflicts for {pawn.LabelShort}");
                    LogUtil.Message(debugString);
                    LogUtil.Message($"Gender: {pawn.gender}, Sexual: {comp.sexual.Prefix()}, Romantic: {comp.romantic.Prefix()}");
                }

                void RollSexualOrientation()
                {
                    //Assign chances
                    float asexualChance = sexualChances.None;
                    float bisexualChance = sexualChances.Bi;
                    float homosexualChance = sexualChances.Homo;
                    float heterosexualChance = sexualChances.Hetero;

                    float enbychance = Settings.NonBinaryActive ? (BetterRomanceMod.settings.enbyChance / 100f) : 0f;

                    if (pawn.story.traits.HasTrait(RomanceDefOf.Philanderer))
                    {
                        if (asexualChance < 1f)
                        {
                            asexualChance = 0f;
                        }
                        //If asexual is the only option, remove philanderer
                        else
                        {
                            pawn.story.traits.RemoveTrait(pawn.story.traits.GetTrait(RomanceDefOf.Philanderer));
                        }
                    }

                    //If only heteroromantic is allowed and there valid sexual choices, don't allow homosexual
                    if (romanticChances.Hetero == 1f && (heterosexualChance > 0f || bisexualChance > 0f))
                    {
                        homosexualChance = 0f;
                    }
                    //If only homoromantic is allowed and there are valid sexual choices, don't allow heterosexual
                    if (romanticChances.Homo == 1f && (homosexualChance > 0f || bisexualChance > 0f))
                    {
                        heterosexualChance = 0f;
                    }

                    float total = asexualChance + bisexualChance + homosexualChance + heterosexualChance;
                    if (total == 0f)
                    {
                        LogUtil.Error($"No valid sexual orientations");
                    }
                    //Roll for sexual orientation
                    float sexualOrientation = Rand.Range(0f, total);

                    //Asexual
                    if (sexualOrientation < asexualChance)
                    {
                        comp.SetSexualAttraction(Gender.Male, false);
                        comp.SetSexualAttraction(Gender.Female, false);
                        comp.SetSexualAttraction((Gender)3, false);
                        debugString += "asexual,";
                    }
                    //Bisexual
                    else if ((sexualOrientation -= asexualChance) < bisexualChance)
                    {
                        comp.SetSexualAttraction(Gender.Male, true);
                        comp.SetSexualAttraction(Gender.Female, true);
                        //Roll separately for enby
                        if (pawn.IsEnby() && Rand.Value < enbychance)
                        {
                            comp.SetSexualAttraction((Gender)3, true);
                        }
                        debugString += "bisexual,";
                    }
                    //Homosexual
                    else if ((sexualOrientation -= bisexualChance) < homosexualChance)
                    {
                        comp.SetSexualAttraction(pawn.gender, true);
                        debugString += "homosexual,";
                    }
                    //Heterosexual
                    else if ((sexualOrientation -= homosexualChance) < heterosexualChance)
                    {
                        if (pawn.IsEnby())
                        {
                            //Need to decide between men and women
                            if (mustLikeMen)
                            {
                                comp.SetSexualAttraction(Gender.Male, true);
                            }
                            else if (mustLikeWomen)
                            {
                                comp.SetSexualAttraction(Gender.Female, true);
                            }
                            else
                            {
                                comp.SetSexualAttraction(Rand.Bool ? Gender.Male : Gender.Female, true);
                            }
                        }
                        else
                        {
                            comp.SetSexualAttraction(pawn.gender.Opposite(), true);
                        }
                        debugString += "heterosexual,";
                    }
                    //I don't want men and women to omly be attracted to enby since that's festishizing, so skip if they're asexual
                    if (!pawn.IsEnby() && !comp.Asexual)
                    {
                        //Roll separately for men and women being attracted to enby
                        if (Rand.Value < enbychance)
                        {
                            comp.SetSexualAttraction((Gender)3, true);
                        }
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

                    float enbychance = Settings.NonBinaryActive ? (BetterRomanceMod.settings.enbyChance / 100f) : 0f;

                    //If they're asexual, we don't want to modify chances at all (except for aromantic above)
                    if (!comp.Asexual)
                    {
                        //In order to be homoromantic and follow the rules, they'd have to be sexually attracted to their own gender
                        if (!pawn.AttractedTo(pawn.gender, false))
                        {
                            //Enby should be an exception here, since we can just add enby to the sexual attraction, but only if enbyromantic is actually allowed
                            if (!pawn.IsEnby() || (pawn.IsEnby() && enbychance == 0f))
                            {
                                homoromanticChance = 0f;
                            }
                        }
                        //In order to be heteroromantic and follow the rules, they'd have to be sexually attracted to the opposite gender
                        if (pawn.IsEnby())
                        {
                            //Which for non-binary just means at least one of the others
                            if (!pawn.AttractedTo(Gender.Male, false) && !pawn.AttractedTo(Gender.Female, false))
                            {
                                heteroromanticChance = 0f;
                            }
                        }
                        else if (!pawn.AttractedTo(pawn.gender.Opposite(), false))
                        {
                            heteroromanticChance = 0f;
                        }
                    }
                    float total = aromanticChance + biromanticChance + homoromanticChance + heteroromanticChance;
                    //If there's no valid choices just copy from sexual
                    if (total == 0f)
                    {
                        comp.romantic.CopyFrom(comp.sexual);
                    }
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
                        //Roll separately for enby
                        if (pawn.IsEnby() && Rand.Value < enbychance)
                        {
                            comp.SetRomanticAttraction((Gender)3, true);
                        }
                        debugString += " biromantic";
                    }
                    //Homoromantic
                    else if ((romanticOrientation -= biromanticChance) < homoromanticChance)
                    {
                        comp.SetRomanticAttraction(pawn.gender, true);
                        //Since they're only romantically attracted to one gender, we have to make sure they're sexually attracted to enby as well
                        if (pawn.IsEnby() && !pawn.AttractedTo(pawn.gender, false))
                        {
                            comp.SetSexualAttraction((Gender)3, true);
                        }
                        debugString += " homoromantic";
                    }
                    //Heteroromantic
                    else if ((romanticOrientation -= homoromanticChance) < heteroromanticChance)
                    {
                        if (pawn.IsEnby())
                        {
                            //Need to decide between men and women
                            if (mustLikeMen)
                            {
                                comp.SetRomanticAttraction(Gender.Male, true);
                            }
                            else if (mustLikeWomen)
                            {
                                comp.SetRomanticAttraction(Gender.Female, true);
                            }
                            else if (comp.sexual.Straight)
                            {
                                comp.romantic.CopyFrom(comp.sexual);
                            }
                            else
                            {
                                comp.SetRomanticAttraction(Rand.Bool ? Gender.Male : Gender.Female, true);
                            }
                        }
                        else
                        {
                            comp.SetRomanticAttraction(pawn.gender.Opposite(), true);
                        }
                        debugString += " heteroromantic";
                    }
                    
                    if (Settings.NonBinaryActive && !pawn.IsEnby() && !comp.Aromantic)
                    {
                        //Roll separately for men and women being attracted to enby
                        if (Rand.Value < enbychance)
                        {
                            comp.SetRomanticAttraction((Gender)3, true);
                        }
                    }
                }
            }
        }
    }
}
