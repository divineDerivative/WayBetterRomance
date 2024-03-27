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
            bool attractionExists = true;
            Pawn mother = other.GetMother();
            Pawn father = other.GetFather();
            if (mother != null)
            {
                existingParentNoSpouse = !mother.SpouseAllowed();
                attractionExists = OrientationUtility.AttractionBetween(generated, mother, true);
            }
            else if (father != null)
            {
                existingParentNoSpouse = !father.SpouseAllowed();
                attractionExists = OrientationUtility.AttractionBetween(generated, father, true);
            }

            if (!generated.SpouseAllowed() || !attractionExists || existingParentNoSpouse)
            {
                if (generated.gender == Gender.Male)
                {
                    other.SetFather(generated);
                    AccessTools.Method(typeof(PawnRelationWorker_Child), "ResolveMyName").Invoke(__instance, [request, other, mother]);
                    if (mother != null)
                    {
                        if (Rand.Value < 0.85f && !LovePartnerRelationUtility.HasAnyLovePartner(mother) && OrientationUtility.AttractionBetween(generated, mother, false))
                        {
                            generated.relations.AddDirectRelation(PawnRelationDefOf.Lover, mother);
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
                    AccessTools.Method(typeof(PawnRelationWorker_Child), "ResolveMyName").Invoke(__instance, [request, other, father]);
                    if (father != null)
                    {
                        if (Rand.Value < 0.85f && !LovePartnerRelationUtility.HasAnyLovePartner(father) && OrientationUtility.AttractionBetween(generated, father, false))
                        {
                            generated.relations.AddDirectRelation(PawnRelationDefOf.Lover, father);
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