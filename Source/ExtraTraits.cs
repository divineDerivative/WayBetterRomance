using RimWorld;
using Verse;
using static BetterRomance.WBRLogger;

namespace BetterRomance
{
    public static class ExtraTraits
    {
        public static void EnsureTraits(this Pawn pawn)
        {
            //Don't give orientation to kids, hopefully
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
                    if (BetterRomanceMod.settings.complex)
                    {
                        AssignComplexOrientation(pawn);
                    }
                    else
                    {
                        AssignOrientation(pawn);
                    }
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
                //Grab the relevant orientation object
                OrientationChances sexualChances = pawn.TryGetComp<WBR_SettingsComp>().orientation?.sexual ?? BetterRomanceMod.settings.sexualOrientations;
                //Assign chances
                float asexualChance = sexualChances.None;
                float bisexualChance = sexualChances.Bi;
                float gayChance = sexualChances.Homo;
                float straightChance = sexualChances.Hetero;

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
                if (mightBeGay && mightBeStraight)
                {
                    pawn.story.traits.GainTrait(new(TraitDefOf.Bisexual));
                    return;
                }

                //Roll for orientation
                float orientation = Rand.Value;

                //Asexual chance
                if (orientation < asexualChance)
                {
                    if (pawn.story.traits.HasTrait(RomanceDefOf.Philanderer))
                    {
                        pawn.story.traits.GainTrait(new(TraitDefOf.Bisexual));
                    }
                    //Need to roll again to determine romantic orientation
                    else
                    {
                        //Set up romantic orientation chances
                        OrientationChances asexualChances = pawn.TryGetComp<WBR_SettingsComp>().orientation?.asexual ?? BetterRomanceMod.settings.romanticOrientations;
                        float aceAroChance = asexualChances.None;
                        float aceBiChance = asexualChances.Bi;
                        float aceHomoChance = asexualChances.Homo;
                        float aceHeteroChance = asexualChances.Hetero;

                        if (mightBeGay)
                        {
                            pawn.story.traits.GainTrait(new(RomanceDefOf.HomoAce));
                        }
                        else if (mightBeStraight)
                        {
                            pawn.story.traits.GainTrait(new(RomanceDefOf.HeteroAce));
                        }
                        else
                        {
                            float romantic = Rand.Value;
                            //Asexual chance
                            if (romantic < aceAroChance)
                            {
                                pawn.story.traits.GainTrait(new(TraitDefOf.Asexual));
                            }
                            //Bisexual chance
                            else if (romantic < aceAroChance + aceBiChance)
                            {
                                pawn.story.traits.GainTrait(new(RomanceDefOf.BiAce));
                            }
                            //Gay chance
                            else if (romantic < aceAroChance + aceBiChance + aceHomoChance)
                            {
                                pawn.story.traits.GainTrait(new(RomanceDefOf.HomoAce));
                            }
                            //Straight chance
                            else
                            {
                                pawn.story.traits.GainTrait(new(RomanceDefOf.HeteroAce));
                            }
                        }
                    }
                }
                //Bisexual chance
                else if (orientation < asexualChance + bisexualChance)
                {
                    pawn.story.traits.GainTrait(new(TraitDefOf.Bisexual));
                }
                //Gay chance
                else if (orientation < asexualChance + bisexualChance + gayChance)
                {
                    if (mightBeStraight)
                    {
                        //Roll again against the relevant sexualities
                        float total = gayChance + bisexualChance + straightChance;
                        float num = Rand.Range(0f, total);
                        if (num < gayChance)
                        {
                            pawn.story.traits.GainTrait(new(TraitDefOf.Gay));
                        }
                        else if (num < gayChance + bisexualChance)
                        {
                            pawn.story.traits.GainTrait(new(TraitDefOf.Bisexual));
                        }
                        else if (num < total)
                        {
                            pawn.story.traits.GainTrait(new(RomanceDefOf.Straight));
                        }
                    }
                    else
                    {
                        pawn.story.traits.GainTrait(new(TraitDefOf.Gay));
                    }
                }
                //Straight chance
                else
                {
                    if (mightBeGay)
                    {
                        //Roll again against the relevant sexualities
                        float total = gayChance + bisexualChance + straightChance;
                        float num = Rand.Range(0f, total);
                        if (num < gayChance)
                        {
                            pawn.story.traits.GainTrait(new(TraitDefOf.Gay));
                        }
                        else if (num < gayChance + bisexualChance)
                        {
                            pawn.story.traits.GainTrait(new(TraitDefOf.Bisexual));
                        }
                        else if (num < total)
                        {
                            pawn.story.traits.GainTrait(new(RomanceDefOf.Straight));
                        }
                    }
                    else
                    {
                        pawn.story.traits.GainTrait(new(RomanceDefOf.Straight));
                    }
                }
            }
        }

        public static void AssignComplexOrientation(Pawn pawn)
        {
            if (!pawn.story.traits.HasTrait(RomanceDefOf.DynamicOrientation))
            {
                pawn.story.traits.GainTrait(new Trait(RomanceDefOf.DynamicOrientation));
            }
            Comp_Orientation comp = pawn.CheckForComp<Comp_Orientation>();

            OrientationChances sexualChances = pawn.TryGetComp<WBR_SettingsComp>().orientation?.sexual ?? BetterRomanceMod.settings.sexualOrientations;
            float asexualChance = sexualChances.none / 100f;
            float bisexualChance = sexualChances.bi / 100f;
            float homosexualChance = sexualChances.homo / 100f;
            float heterosexualChance = sexualChances.hetero / 100f;

            float sexualOrientation = Rand.Value;

            //Asexual
            if (sexualOrientation < asexualChance)
            {
                comp.SetSexualAttraction(Gender.Male, false);
                comp.SetSexualAttraction(Gender.Female, false);
                comp.SetSexualAttraction((Gender)3, false);
            }
            //Bisexual
            if (sexualOrientation < asexualChance + bisexualChance)
            {
                comp.SetSexualAttraction(Gender.Male, true);
                comp.SetSexualAttraction(Gender.Female, true);
            }
            //Homosexual
            else if (sexualOrientation < asexualChance + bisexualChance + homosexualChance)
            {
                comp.SetSexualAttraction(pawn.gender, true);
                comp.SetSexualAttraction(pawn.gender.Opposite(), false);
            }
            //Heterosexual
            else
            {
                comp.SetSexualAttraction(pawn.gender, false);
                comp.SetSexualAttraction(pawn.gender.Opposite(), true);
            }

            OrientationChances romanticChances = pawn.TryGetComp<WBR_SettingsComp>().orientation?.sexual ?? BetterRomanceMod.settings.romanticOrientations;
            float aromanticChance = romanticChances.none / 100f;
            float biromanticChance = romanticChances.bi / 100f;
            float homoromanticChance = romanticChances.homo / 100f;
            float heteroromanticChance = romanticChances.hetero / 100f;

            float romanticOrientation = Rand.Value;

            //Aromantic
            if (romanticOrientation < aromanticChance)
            {
                comp.SetRomanticAttraction(Gender.Male, false);
                comp.SetRomanticAttraction(Gender.Female, false);
            }
            //Biromantic
            else if (romanticOrientation < aromanticChance + biromanticChance)
            {
                comp.SetRomanticAttraction(Gender.Male, true);
                comp.SetRomanticAttraction(Gender.Female, true);
            }
            //Homoromantic
            else if (romanticOrientation < aromanticChance + biromanticChance + homoromanticChance)
            {
                comp.SetRomanticAttraction(pawn.gender, true);
                comp.SetRomanticAttraction(pawn.gender.Opposite(), false);
            }
            //Heteroromantic
            else
            {
                comp.SetRomanticAttraction(pawn.gender, false);
                comp.SetRomanticAttraction(pawn.gender.Opposite(), true);
            }

            if (!comp.ResolveConflicts(sexualChances, romanticChances))
            {
                LogUtil.Error($"Unable to resolve orientation conflicts for {pawn.Name.ToStringFull}");
            }
        }
    }
}
