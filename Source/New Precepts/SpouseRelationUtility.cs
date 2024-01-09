using System.Collections.Generic;
using RimWorld;
using HarmonyLib;
using System.Reflection.Emit;

namespace BetterRomance.HarmonyPatches
{
    //Adds a check against custom love relations so they don't get counted as spouses
    [HarmonyPatch(typeof(SpouseRelationUtility), nameof(SpouseRelationUtility.GetHistoryEventForSpouseAndFianceCountPlusOne))]
    public static class SpouseRelationUtility_GetHistoryEventForSpouseAndFianceCountPlusOne
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            bool insert = false;
            foreach (CodeInstruction code in instructions)
            {
                yield return code;

                if (code.LoadsField(AccessTools.Field(typeof(PawnRelationDefOf), nameof(PawnRelationDefOf.Lover))))
                {
                    insert = true;
                }
                if (insert && code.Branches(out Label? label))
                {
                    yield return CodeInstruction.LoadField(typeof(CustomLoveRelationUtility), nameof(CustomLoveRelationUtility.LoveRelations));
                    yield return new CodeInstruction(OpCodes.Ldloc_0);
                    yield return new CodeInstruction(OpCodes.Ldloc_2);
                    yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(List<DirectPawnRelation>), "get_Item"));
                    yield return CodeInstruction.LoadField(typeof(DirectPawnRelation), nameof(DirectPawnRelation.def));
                    yield return CodeInstruction.Call(typeof(HashSet<PawnRelationDef>), nameof(HashSet<PawnRelationDef>.Contains));
                    yield return new CodeInstruction(OpCodes.Brtrue_S, label);
                    insert = false;
                }
            }
        }
    }
}
