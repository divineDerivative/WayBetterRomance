using System.Collections.Generic;
using RimWorld;
using Verse;

namespace BetterRomance
{
    public static class CustomLoveRelationUtility
    {
        //Love Relation Settings
        public static HashSet<PawnRelationDef> LoveRelations = new();
        public static HashSet<PawnRelationDef> ExLoveRelations = new();

        public static DirectPawnRelation CheckCustomLoveRelations(Pawn pawn, Pawn otherPawn, bool ex = false)
        {
            if (ex)
            {
                if (!ExLoveRelations.EnumerableNullOrEmpty())
                {
                    foreach (PawnRelationDef rel in ExLoveRelations)
                    {
                        if (pawn.relations.GetDirectRelation(rel, otherPawn) is DirectPawnRelation tempRel)
                        {
                            return tempRel;
                        }
                    }
                }
            }
            else if (Settings.LoveRelationsLoaded)
            {
                foreach (PawnRelationDef rel in LoveRelations)
                {
                    if (pawn.relations.GetDirectRelation(rel, otherPawn) is DirectPawnRelation tempRel)
                    {
                        return tempRel;
                    }
                }
            }
            return null;
        }

        public static DirectPawnRelation FirstCustomLoveRelation(this Pawn pawn)
        {
            if (Settings.LoveRelationsLoaded)
            {
                foreach (DirectPawnRelation relation in pawn.relations.DirectRelations)
                {
                    if (LoveRelations.Contains(relation.def))
                    {
                        return relation;
                    }
                }
            }
            return null;
        }

        public static void MakeAdditionalLoveRelationsLists()
        {
            List<PawnRelationDef> relationList = DefDatabase<PawnRelationDef>.AllDefsListForReading;
            foreach (PawnRelationDef def in relationList)
            {
                if (def.HasModExtension<LoveRelations>())
                {
                    LoveRelations extension = def.GetModExtension<LoveRelations>();
                    if (extension.isLoveRelation)
                    {
                        LoveRelations.Add(def);
                        if (extension.exLoveRelation != null)
                        {
                            ExLoveRelations.Add(extension.exLoveRelation);
                        }
                    }
                }
            }
        }

        public static PawnRelationDef GetExRelationDef(this PawnRelationDef relation)
        {
            if (relation.HasModExtension<LoveRelations>())
            {
                return relation.GetModExtension<LoveRelations>().exLoveRelation ?? PawnRelationDefOf.ExLover;
            }
            LogUtil.Warning($"Tried to get the ex relation for {relation.defName} but it has no LoveRelations extension ");
            return PawnRelationDefOf.ExLover;
        }
    }
}
