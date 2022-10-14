using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace BetterRomance
{
    //If settings do not allow spouses for either sibling, do not force their parents to be spouses
    [HarmonyPatch(typeof(PawnRelationWorker_Sibling), "CreateRelation")]
    public static class PawnRelationWorker_Sibling_CreateRelation
    {
        public static bool Prefix(Pawn generated, Pawn other, ref PawnGenerationRequest request, PawnRelationWorker_Sibling __instance)
        {
            if (!other.SpouseAllowed() || !generated.SpouseAllowed())
            {
                bool hasMother = other.GetMother() != null;
                bool hasFather = other.GetFather() != null;
                bool tryMakeLovers = Rand.Value < 0.85f;
                if (hasMother && LovePartnerRelationUtility.HasAnyLovePartner(other.GetMother()))
                {
                    tryMakeLovers = false;
                }
                if (hasFather && LovePartnerRelationUtility.HasAnyLovePartner(other.GetFather()))
                {
                    tryMakeLovers = false;
                }
                if (!hasMother)
                {
                    Pawn newMother = (Pawn)AccessTools.Method(typeof(PawnRelationWorker_Sibling), "GenerateParent").Invoke(__instance, new object[] { generated, other, Gender.Female, request, false });
                    other.SetMother(newMother);
                }
                generated.SetMother(other.GetMother());
                if (!hasFather)
                {
                    Pawn newFather = (Pawn)AccessTools.Method(typeof(PawnRelationWorker_Sibling), "GenerateParent").Invoke(__instance, new object[] { generated, other, Gender.Male, request, false });
                    other.SetFather(newFather);
                }
                generated.SetFather(other.GetFather());
                if (!hasMother || !hasFather)
                {
                    if (tryMakeLovers)
                    {
                        Pawn mother = other.GetMother();
                        Pawn father = other.GetFather();
                        father.relations.AddDirectRelation(PawnRelationDefOf.Lover, mother);

                    }
                    else
                    {
                        other.GetFather().relations.AddDirectRelation(PawnRelationDefOf.ExLover, other.GetMother());
                    }
                }
                AccessTools.Method(typeof(PawnRelationWorker_Sibling), "ResolveMyName").Invoke(__instance, new object[] { request, generated });
                return false;
            }
            return true;
        }
    }

    //Uses age settings of existing child to help generate an appropriate parent
    [HarmonyPatch(typeof(PawnRelationWorker_Sibling), "GenerateParent")]
    public static class PawnRelationWorker_Sibling_GenerateParent
    {
        public static bool Prefix(Pawn generatedChild, Pawn existingChild, Gender genderToGenerate, PawnGenerationRequest childRequest, bool newlyGeneratedParentsWillBeSpousesIfNotGay, PawnRelationWorker_Sibling __instance, ref Pawn __result)
        {
            float generatedAge = generatedChild.ageTracker.AgeChronologicalYearsFloat;
            float existingAge = existingChild.ageTracker.AgeChronologicalYearsFloat;
            float minAgeToHaveChild = ((genderToGenerate == Gender.Male) ? existingChild.MinAgeToHaveChildren(Gender.Male) : existingChild.MinAgeToHaveChildren(Gender.Female));
            float maxAgeToHaveChild = ((genderToGenerate == Gender.Male) ? existingChild.MaxAgeToHaveChildren(Gender.Male) : existingChild.MaxAgeToHaveChildren(Gender.Female));
            float usualAgeToHaveChild = ((genderToGenerate == Gender.Male) ? existingChild.UsualAgeToHaveChildren(Gender.Male) : existingChild.UsualAgeToHaveChildren(Gender.Female));
            float minChronologicalAge = Mathf.Max(generatedAge, existingAge) + minAgeToHaveChild;
            float maxChronologicalAge = minChronologicalAge + (maxAgeToHaveChild - minAgeToHaveChild);
            float midChronologicalAge = minChronologicalAge + (usualAgeToHaveChild - minAgeToHaveChild);

            object[] arguments = new object[] { minChronologicalAge, maxChronologicalAge, midChronologicalAge, minAgeToHaveChild, generatedChild, existingChild, childRequest, null, null, null, null };
            AccessTools.Method(typeof(PawnRelationWorker_Sibling), "GenerateParentParams").Invoke(__instance, arguments);
            float biologicalAge = (float)arguments[7];
            float chronologicalAge = (float)arguments[8];
            string lastName = (string)arguments[9];
            XenotypeDef xenotype = (XenotypeDef)arguments[10];
            Faction faction = existingChild.Faction;
            if (faction == null || faction.IsPlayer)
            {
                bool tryMedievalOrBetter = faction != null && (int)faction.def.techLevel >= 3;
                if (!Find.FactionManager.TryGetRandomNonColonyHumanlikeFaction(out faction, tryMedievalOrBetter, allowDefeated: true))
                {
                    faction = Faction.OfAncients;
                }
            }
            Pawn pawn;
            //If children are not allowed, use the pawnkind provided in settings
            if (!existingChild.ChildAllowed())
            {
                pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(existingChild.ParentPawnkind(genderToGenerate), faction: faction, forceGenerateNewPawn: true, allowDead: true, allowDowned: true, canGeneratePawnRelations: false, colonistRelationChanceFactor: 1f, fixedBiologicalAge: biologicalAge, fixedChronologicalAge: chronologicalAge, fixedGender: genderToGenerate, fixedLastName: lastName, forcedXenotype: xenotype));
            }
            else
            {
                pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(existingChild.kindDef, faction: faction, forceGenerateNewPawn: true, allowDead: true, allowDowned: true, canGeneratePawnRelations: false, colonistRelationChanceFactor: 1f, fixedBiologicalAge: biologicalAge, fixedChronologicalAge: chronologicalAge, fixedGender: genderToGenerate, fixedLastName: lastName, forcedXenotype: xenotype));
            }
            if (!Find.WorldPawns.Contains(pawn))
            {
                Find.WorldPawns.PassToWorld(pawn);
            }
            __result = pawn;
            return false;
        }
    }
}