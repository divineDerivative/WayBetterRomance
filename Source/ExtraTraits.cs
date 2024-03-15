using RimWorld;
using Verse;

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
                foreach (Trait trait in pawn.story.traits.allTraits)
                {
                    if (SexualityUtility.OrientationTraits.Contains(trait.def))
                    {
                        return;
                    }
                }
                AssignOrientation(pawn);
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
                float asexualChance = sexualChances.none;
                float bisexualChance = sexualChances.bi;
                float gayChance = sexualChances.homo;
                float straightChance = sexualChances.hetero;

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
                    pawn.story.traits.GainTrait(new Trait(TraitDefOf.Bisexual));
                    return;
                }

                //Roll for orientation
                float orientation = Rand.Value;

                //Asexual chance
                if (orientation < asexualChance / 100f)
                {
                    if (pawn.story.traits.HasTrait(RomanceDefOf.Philanderer))
                    {
                        pawn.story.traits.GainTrait(new Trait(TraitDefOf.Bisexual));
                    }
                    //Need to roll again to determine romantic orientation
                    else
                    {
                        //Set up romantic orientation chances
                        OrientationChances asexualChances = pawn.TryGetComp<WBR_SettingsComp>().orientation?.asexual ?? BetterRomanceMod.settings.sexualOrientations;
                        float aceAroChance = asexualChances.none;
                        float aceBiChance = asexualChances.bi;
                        float aceHomoChance = asexualChances.homo;
                        float aceHeteroChance = asexualChances.hetero;

                        if (mightBeGay)
                        {
                            pawn.story.traits.GainTrait(new Trait(RomanceDefOf.HomoAce));
                        }
                        else if (mightBeStraight)
                        {
                            pawn.story.traits.GainTrait(new Trait(RomanceDefOf.HeteroAce));
                        }
                        else
                        {
                            float romantic = Rand.Value;
                            //Asexual chance
                            if (romantic < aceAroChance / 100f)
                            {
                                pawn.story.traits.GainTrait(new Trait(TraitDefOf.Asexual));
                            }
                            //Bisexual chance
                            else if (romantic < (aceAroChance + aceBiChance) / 100f)
                            {
                                pawn.story.traits.GainTrait(new Trait(RomanceDefOf.BiAce));
                            }
                            //Gay chance
                            else if (romantic < (aceAroChance + aceBiChance + aceHomoChance) / 100f)
                            {
                                pawn.story.traits.GainTrait(new Trait(RomanceDefOf.HomoAce));
                            }
                            //Straight chance
                            else
                            {
                                pawn.story.traits.GainTrait(new Trait(RomanceDefOf.HeteroAce));
                            }
                        }
                    }
                }
                //Bisexual chance
                else if (orientation < (asexualChance + bisexualChance) / 100f)
                {
                    pawn.story.traits.GainTrait(new Trait(TraitDefOf.Bisexual));
                }
                //Gay chance
                else if (orientation < (asexualChance + bisexualChance + gayChance) / 100f)
                {
                    if (mightBeStraight)
                    {
                        //Roll again against the relevant sexualities
                        float total = gayChance + bisexualChance + straightChance;
                        float num = Rand.Range(0f, total);
                        if (num < gayChance)
                        {
                            pawn.story.traits.GainTrait(new Trait(TraitDefOf.Gay));
                        }
                        else if (num < gayChance + bisexualChance)
                        {
                            pawn.story.traits.GainTrait(new Trait(TraitDefOf.Bisexual));
                        }
                        else if (num < total)
                        {
                            pawn.story.traits.GainTrait(new Trait(RomanceDefOf.Straight));
                        }
                    }
                    else
                    {
                        pawn.story.traits.GainTrait(new Trait(TraitDefOf.Gay));
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
                            pawn.story.traits.GainTrait(new Trait(TraitDefOf.Gay));
                        }
                        else if (num < gayChance + bisexualChance)
                        {
                            pawn.story.traits.GainTrait(new Trait(TraitDefOf.Bisexual));
                        }
                        else if (num < total)
                        {
                            pawn.story.traits.GainTrait(new Trait(RomanceDefOf.Straight));
                        }
                    }
                    else
                    {
                        pawn.story.traits.GainTrait(new Trait(RomanceDefOf.Straight));
                    }
                }
            }
        }
    }
}