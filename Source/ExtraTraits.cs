using RimWorld;
using Verse;

namespace BetterRomance
{
    public static class ExtraTraits
    {
        public static void EnsureTraits(this Pawn pawn)
        {
            //Don't give orientation to kids, hopefully
            if (pawn.DevelopmentalStage.Adult())
            {
                foreach (var trait in pawn.story.traits.allTraits)
                {
                    if (RomanceUtilities.OrientationTraits.Contains(trait.def))
                    {
                        return;
                    }
                }
                AssignOrientation(pawn);
            }
        }
        public static void AssignOrientation(Pawn pawn)
        {
            float orientation = Rand.Value;
            if (pawn.gender == Gender.None)
            {
                return;
            }
            if (pawn.story != null)
            {
                float asexualChance = BetterRomanceMod.settings.asexualChance;
                float bisexualChance = BetterRomanceMod.settings.bisexualChance;
                float gayChance = BetterRomanceMod.settings.gayChance;
                float straightChance = BetterRomanceMod.settings.straightChance;

                if (pawn.kindDef.HasModExtension<SexualityChances>())
                {
                    asexualChance = pawn.kindDef.GetModExtension<SexualityChances>().asexualChance;
                    bisexualChance = pawn.kindDef.GetModExtension<SexualityChances>().bisexualChance;
                    gayChance = pawn.kindDef.GetModExtension<SexualityChances>().gayChance;
                    straightChance = pawn.kindDef.GetModExtension<SexualityChances>().straightChance;
                }
                else if (pawn.def.HasModExtension<SexualityChances>())
                {
                    asexualChance = pawn.def.GetModExtension<SexualityChances>().asexualChance;
                    bisexualChance = pawn.def.GetModExtension<SexualityChances>().bisexualChance;
                    gayChance = pawn.def.GetModExtension<SexualityChances>().gayChance;
                    straightChance = pawn.def.GetModExtension<SexualityChances>().straightChance;
                }
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
                            if (romantic < asexualChance / 100f)
                            {
                                pawn.story.traits.GainTrait(new Trait(TraitDefOf.Asexual));
                            }
                            //Bisexual chance
                            else if (romantic < (asexualChance + bisexualChance) / 100f)
                            {
                                pawn.story.traits.GainTrait(new Trait(RomanceDefOf.BiAce));
                            }
                            //Gay chance
                            else if (romantic < (asexualChance + bisexualChance + gayChance) / 100f)
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