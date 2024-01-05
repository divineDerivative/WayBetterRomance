using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using HarmonyLib;

namespace BetterRomance
{
    public static class CustomLoveRelationUtility
    {
        //Love Relation Settings
        public static HashSet<PawnRelationDef> LoveRelations = new HashSet<PawnRelationDef>();
        public static HashSet<PawnRelationDef> ExLoveRelations = new HashSet<PawnRelationDef>();

        public static DirectPawnRelation CheckCustomLoveRelations(Pawn pawn, Pawn otherPawn, bool ex = false)
        {
            if (ex)
            {
                if (!ExLoveRelations.EnumerableNullOrEmpty())
                {
                    foreach (PawnRelationDef rel in ExLoveRelations)
                    {
                        DirectPawnRelation tempRel = pawn.relations.GetDirectRelation(rel, otherPawn);
                        {
                            if (tempRel != null)
                            {
                                return tempRel;
                            }
                        }
                    }
                }
            }
            else if (Settings.LoveRelationsLoaded)
            {
                foreach (PawnRelationDef rel in LoveRelations)
                {
                    DirectPawnRelation tempRel = pawn.relations.GetDirectRelation(rel, otherPawn);
                    {
                        if (tempRel != null)
                        {
                            return tempRel;
                        }
                    }
                }
            }
            return null;
        }

        public static DirectPawnRelation FirstCustomLoveRelation(this Pawn pawn)
        {
            if (Settings.LoveRelationsLoaded)
            {
                foreach (var thing in pawn.relations.DirectRelations)
                {
                    if (LoveRelations.Contains(thing.def))
                    {
                        return thing;
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
    }
}
