﻿using HarmonyLib;
using RimWorld;
using Verse;

namespace BetterRomance
{
    //Do not force a spouse relation on a newly generated parent if settings do not allow spouses
    [HarmonyPatch(typeof(PawnRelationWorker_Child), "CreateRelation")]
    public static class PawnRelationWorker_Child_CreateRelation
    {
        public static bool Prefix(Pawn generated, Pawn other, ref PawnGenerationRequest request, PawnRelationWorker_Child __instance)
        {
            //This patch only runs if spouses are not allowed by the new parent's race/pawnkind settings
            if (!generated.SpouseAllowed())
            {
                if (generated.gender == Gender.Male)
                {
                    other.SetFather(generated);
                    AccessTools.Method(typeof(PawnRelationWorker_Child), "ResolveMyName").Invoke(__instance, new object[] { request, other, other.GetMother() });
                    if (other.GetMother() != null)
                    {
                        if (Rand.Value < 0.85f && !LovePartnerRelationUtility.HasAnyLovePartner(other.GetMother()) && !other.GetMother().story.traits.HasTrait(TraitDefOf.Gay))
                        {
                            generated.relations.AddDirectRelation(PawnRelationDefOf.Lover, other.GetMother());
                        }
                        else
                        {
                            generated.relations.AddDirectRelation(PawnRelationDefOf.ExLover, other.GetMother());
                        }
                    }
                }
                else if (generated.gender == Gender.Female)
                {
                    other.SetMother(generated);
                    AccessTools.Method(typeof(PawnRelationWorker_Child), "ResolveMyName").Invoke(__instance, new object[] { request, other, other.GetFather() });
                    if (other.GetFather() != null)
                    {
                        if (Rand.Value < 0.85f && !LovePartnerRelationUtility.HasAnyLovePartner(other.GetFather()) && !other.GetFather().story.traits.HasTrait(TraitDefOf.Gay))
                        {
                            generated.relations.AddDirectRelation(PawnRelationDefOf.Lover, other.GetFather());
                        }
                        else
                        {
                            generated.relations.AddDirectRelation(PawnRelationDefOf.ExLover, other.GetFather());
                        }
                    }
                }
                return false;
            }
            return true;
        }
    }
}