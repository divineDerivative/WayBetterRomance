using System.Text;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace BetterRomance.HarmonyPatches
{
    //This determines chance of a breakup
    [HarmonyPatch(typeof(InteractionWorker_Breakup), "RandomSelectionWeight")]
    public static class InteractionWorker_Breakup_RandomSelectionWeight
    {
        //Changes from Vanilla:
        //Pawns are more likely to break up with someone who does not match their orientation

        //This is a postfix since the only difference was adding the orientation match
        public static void Postfix(Pawn initiator, Pawn recipient, ref float __result)
        {
            //If initiator does not have sexuality trait, add one
            initiator.EnsureTraits();
            //This increases chances if gender does not match sexuality
            if (initiator.gender == recipient.gender && initiator.GetOrientation() == Orientation.Hetero)
            {
                __result *= 2f;
            }
            if (initiator.gender != recipient.gender && initiator.GetOrientation() == Orientation.Homo)
            {
                __result *= 2f;
            }
            if (initiator.GetOrientation() == Orientation.None)
            {
                __result *= 2f;
            }
            if (initiator.IsAsexual() && !recipient.IsAsexual())
            {
                __result *= 1.5f;
            }
        }
    }

    //This only runs if there is a custom relation between the two pawns
    [HarmonyPatch(typeof(InteractionWorker_Breakup), "Interacted")]
    public static class InteractionWorker_Breakup_Interacted
    {
        public static bool Prefix(Pawn initiator, Pawn recipient, List<RulePackDef> extraSentencePacks, out string letterText, out string letterLabel, out LetterDef letterDef, out LookTargets lookTargets, InteractionWorker_Breakup __instance)
        {
            //Check if there's any custom relations loaded
            if (!SettingsUtilities.LoveRelations.EnumerableNullOrEmpty())
            {
                PawnRelationDef relation = null;
                foreach (PawnRelationDef rel in SettingsUtilities.LoveRelations)
                {
                    if (initiator.relations.DirectRelationExists(rel, recipient))
                    {
                        relation = rel;
                        break;
                    }
                }
                //Only run patch if a custom relation exists
                if (relation != null)
                {
                    Thought thought = __instance.RandomBreakupReason(initiator, recipient);
                    //At this point we already know which relation exists
                    initiator.relations.RemoveDirectRelation(relation, recipient);
                    //Use ex lover if no ex relation is provided
                    if (relation.GetModExtension<LoveRelations>().exLoveRelation == null)
                    {
                        initiator.relations.AddDirectRelation(PawnRelationDefOf.ExLover, recipient);
                    }
                    //Otherwise use the custom ex relation
                    else
                    {
                        initiator.relations.AddDirectRelation(relation.GetModExtension<LoveRelations>().exLoveRelation, recipient);
                    }
                    recipient.needs.mood?.thoughts.memories.TryGainMemory(ThoughtDefOf.BrokeUpWithMe, initiator);
                    if (initiator.ownership.OwnedBed != null && initiator.ownership.OwnedBed == recipient.ownership.OwnedBed)
                    {
                        ((Rand.Value < 0.5f) ? initiator : recipient).ownership.UnclaimBed();
                    }
                    TaleRecorder.RecordTale(TaleDefOf.Breakup, initiator, recipient);
                    if (PawnUtility.ShouldSendNotificationAbout(initiator) || PawnUtility.ShouldSendNotificationAbout(recipient))
                    {
                        StringBuilder stringBuilder = new StringBuilder();
                        stringBuilder.AppendLine("LetterNoLongerLovers".Translate(initiator.LabelShort, recipient.LabelShort, initiator.Named("PAWN1"), recipient.Named("PAWN2")));
                        stringBuilder.AppendLine();
                        if (thought != null)
                        {
                            stringBuilder.AppendLine();
                            stringBuilder.AppendLine("FinalStraw".Translate(thought.LabelCap));
                        }
                        letterLabel = "LetterLabelBreakup".Translate();
                        letterText = stringBuilder.ToString().TrimEndNewlines();
                        letterDef = LetterDefOf.NegativeEvent;
                        lookTargets = new LookTargets(initiator, recipient);
                    }
                    else
                    {
                        letterLabel = null;
                        letterText = null;
                        letterDef = null;
                        lookTargets = null;
                    }
                    return false;
                }
            }
            letterLabel = null;
            letterText = null;
            letterDef = null;
            lookTargets = null;
            return true;
        }
    }
}