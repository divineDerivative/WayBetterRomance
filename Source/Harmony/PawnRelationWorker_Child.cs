using HarmonyLib;
using RimWorld;
using Verse;

namespace BetterRomance.HarmonyPatches
{
    //Do not force a spouse relation on a newly generated parent if settings do not allow spouses
    //Actually need to have it run for parents with orientation mismatch
    [HarmonyPatch(typeof(PawnRelationWorker_Child), nameof(PawnRelationWorker_Child.CreateRelation))]
    public static class PawnRelationWorker_Child_CreateRelation
    {
        //generated is the parent, other is the child
        public static bool Prefix(Pawn generated, Pawn other, ref PawnGenerationRequest request, PawnRelationWorker_Child __instance)
        {
            //This patch only runs if spouses are not allowed by the new parent's race/pawnkind settings or they're gay
            //I think I also need to check if spouses are allowed for the kid's existing parent
            bool existingParentNoSpouse = false;
            if (other.GetMother() != null)
            {
                existingParentNoSpouse = !other.GetMother().SpouseAllowed();
            }
            else if (other.GetFather() != null)
            {
                existingParentNoSpouse = !other.GetFather().SpouseAllowed();
            }

            if (!generated.SpouseAllowed() || generated.IsHomo() || existingParentNoSpouse)
            {
                if (generated.gender == Gender.Male)
                {
                    other.SetFather(generated);
                    Pawn mother = other.GetMother();
                    AccessTools.Method(typeof(PawnRelationWorker_Child), "ResolveMyName").Invoke(__instance, [request, other, mother]);
                    if (mother != null)
                    {
                        if (Rand.Value < 0.85f && !LovePartnerRelationUtility.HasAnyLovePartner(mother))
                        {
                            generated.relations.AddDirectRelation(RomanceUtilities.GetAppropriateParentRelationship(generated, mother), mother);
                        }
                        else
                        {
                            generated.relations.AddDirectRelation(PawnRelationDefOf.ExLover, mother);
                        }
                    }
                }
                else if (generated.gender == Gender.Female)
                {
                    other.SetMother(generated);
                    Pawn father = other.GetFather();
                    AccessTools.Method(typeof(PawnRelationWorker_Child), "ResolveMyName").Invoke(__instance, [request, other, father]);
                    if (father != null)
                    {
                        if (Rand.Value < 0.85f && !LovePartnerRelationUtility.HasAnyLovePartner(father))
                        {
                            generated.relations.AddDirectRelation(RomanceUtilities.GetAppropriateParentRelationship(generated, father), father);
                        }
                        else
                        {
                            generated.relations.AddDirectRelation(PawnRelationDefOf.ExLover, father);
                        }
                    }
                }
                return false;
            }
            return true;
        }
    }
}