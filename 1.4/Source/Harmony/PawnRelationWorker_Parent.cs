using HarmonyLib;
using RimWorld;
using Verse;

namespace BetterRomance.HarmonyPatches
{
    //If spouses are not allowed, look at lover relations for the generated child's other parent
    [HarmonyPatch(typeof(PawnRelationWorker_Parent), "CreateRelation")]
    public static class PawnRelationWorker_Parent_CreateRelation
    {
        public static bool Prefix(Pawn generated, Pawn other, ref PawnGenerationRequest request, PawnRelationWorker_Parent __instance)
        {
            if (!other.SpouseAllowed())
            {
                if (other.gender == Gender.Male)
                {
                    generated.SetFather(other);
                    Pawn firstLoverOfOppositeGender = RomanceUtilities.GetFirstLoverOfOppositeGender(other);
                    if (firstLoverOfOppositeGender != null)
                    {
                        generated.SetMother(firstLoverOfOppositeGender);
                    }
                    AccessTools.Method(typeof(PawnRelationWorker_Parent), "ResolveMyName").Invoke(__instance, new object[] { request, generated });
                }
                else if (other.gender == Gender.Female)
                {
                    generated.SetMother(other);
                    Pawn firstLoverOfOppositeGender = RomanceUtilities.GetFirstLoverOfOppositeGender(other);
                    if (firstLoverOfOppositeGender != null)
                    {
                        generated.SetFather(firstLoverOfOppositeGender);
                    }
                    AccessTools.Method(typeof(PawnRelationWorker_Parent), "ResolveMyName").Invoke(__instance, new object[] { request, generated });
                }
                //Just in case another mod has patches to run for genderless pawns
                else
                {
                    return true;
                }
                return false;
            }
            return true;
        }
    }
}