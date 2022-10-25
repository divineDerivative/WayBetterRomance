using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace BetterRomance
{
    //This determines if a pawn will propose marriage to another pawn
    [HarmonyPatch(typeof(InteractionWorker_MarriageProposal), "RandomSelectionWeight")]
    public static class InteractionWorker_MarriageProposal_RandomSelectionWeight
    {
        //Changes from Vanilla:
        //Checks if spouses are allowed for the race/pawnkind
        //Do not allow if recipient's gender does not match initiator's orientation
        public static bool Prefix(Pawn initiator, Pawn recipient, ref float __result)
        {
            //If initiator doesn't have a sexuality trait, assign one
            initiator.EnsureTraits();
            //If spouses are not allowed by initiator's race/pawnkind, do not allow
            if (!initiator.SpouseAllowed())
            {
                __result = 0f;
                return false;
            }
            //If pawns are not already in a lover relationship, do not allow
            DirectPawnRelation directRelation = initiator.relations.GetDirectRelation(PawnRelationDefOf.Lover, recipient);
            if (directRelation == null)
            {
                //Check for additional love relations
                if (!SettingsUtilities.LoveRelations.NullOrEmpty())
                {
                    foreach (PawnRelationDef rel in SettingsUtilities.LoveRelations)
                    {
                        DirectPawnRelation tempRel = initiator.relations.GetDirectRelation(rel, recipient);
                        if (tempRel != null)
                        {
                            directRelation = tempRel;
                            break;
                        }
                    }
                    if (directRelation == null)
                    {
                        __result = 0f;
                        return false;
                    }
                }
                else
                {
                    __result = 0f;
                    return false;
                }
            }
            //This is vanilla code for checking if ideology allows for the new spouse relation
            HistoryEvent ev = new HistoryEvent(initiator.GetHistoryEventForSpouseAndFianceCountPlusOne(), initiator.Named(HistoryEventArgsNames.Doer));
            HistoryEvent ev2 = new HistoryEvent(recipient.GetHistoryEventForSpouseAndFianceCountPlusOne(), recipient.Named(HistoryEventArgsNames.Doer));
            if (!ev.DoerWillingToDo() || !ev2.DoerWillingToDo())
            {
                __result = 0f;
                return false;
            }
            //If genders are incorrect for initiator's sexuality, do not allow
            if (initiator.gender == recipient.gender && initiator.story.traits.HasTrait(RomanceDefOf.Straight))
            {
                __result = 0f;
                return false;
            }
            if (initiator.gender != recipient.gender && initiator.story.traits.HasTrait(TraitDefOf.Gay))
            {
                __result = 0f;
                return false;
            }
            //If they are asexual and sex repulsed, do not allow unless partner is also asexual
            //Not sure about this
            if (initiator.story.traits.HasTrait(TraitDefOf.Asexual) && initiator.AsexualRating() < 0.2f)
            {
                if (!recipient.story.traits.HasTrait(TraitDefOf.Asexual))
                {
                    __result = 0f;
                    return false;
                }
            }

            float baseChance = 0.4f;
            //This determines how long they've been lovers
            float relLength = (Find.TickManager.TicksGame - directRelation.startTicks) / 60000f;
            //Adjust chance based on length of relationship
            float lengthFactor = Mathf.InverseLerp(0f, 60f, relLength);
            //Adjust chance based on opinion
            float opinionFactor = Mathf.InverseLerp(0f, 60f, initiator.relations.OpinionOf(recipient));
            //Lower chance if they're currently mad at you
            if (recipient.relations.OpinionOf(initiator) < 0)
            {
                baseChance *= 0.3f;
            }
            //Added from vanilla to account for psychic love ability
            HediffWithTarget hediffWithTarget = (HediffWithTarget)initiator.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.PsychicLove);
            if (hediffWithTarget != null && hediffWithTarget.target == recipient)
            {
                baseChance *= 10f;
            }
            //Added from vanilla to account for pregnancy
            if (initiator.health.hediffSet.HasPregnancyHediff() || recipient.health.hediffSet.HasPregnancyHediff())
            {
                baseChance *= 3f;
            }
            //Added from vanilla, increase chance if they already have a baby together
            foreach (Pawn child in initiator.relations.Children)
            {
                if (child.DevelopmentalStage.Baby() && !child.Dead && recipient.relations.Children.Contains(child))
                {
                    baseChance *= 2f;
                }
            }
            //Add everything together
            __result = baseChance * lengthFactor * opinionFactor;
            return false;
        }
    }

    //This determines if a pawn will accept a marriage proposal
    [HarmonyPatch(typeof(InteractionWorker_MarriageProposal), "AcceptanceChance")]
    [HarmonyAfter(new string[] { "Telardo.RomanceOnTheRim" })]
    public static class InteractionWorker_MarriageProposal_AcceptanceChance
    {
        //Changes from Vanilla:
        //Will not accept if gender does not match orientation
        //Will not accept if race/pawnkind settings don't allow spouses
        public static void Postfix(Pawn initiator, Pawn recipient, ref float __result)
        {
            //If they don't have a sexuality trait, assign one.
            recipient.EnsureTraits();
            //If pawns are the wrong gender, they will not accept
            if (initiator.gender == recipient.gender && recipient.story.traits.HasTrait(RomanceDefOf.Straight))
            {
                __result = 0f;
                return;
            }
            if (initiator.gender != recipient.gender && recipient.story.traits.HasTrait(TraitDefOf.Gay))
            {
                __result = 0f;
                return;
            }
            //If pawn is asexual and sex repulsed, they will only accept from another asexual pawn
            //Not sure about this
            if (recipient.story.traits.HasTrait(TraitDefOf.Asexual) && recipient.AsexualRating() < 0.2f)
            {
                if (!initiator.story.traits.HasTrait(TraitDefOf.Asexual))
                {
                    __result = 0f;
                    return;
                }
            }
            //If pawnkind/race doesn't allow spouses, they will not accept
            if (!recipient.SpouseAllowed())
            {
                __result = 0f;
                return;
            }
        }
    }

    //Makes neccessary changes depending on outcome of interaction
    [HarmonyPatch(typeof(InteractionWorker_MarriageProposal), "Interacted")]
    public static class InteractionWorker_MarriageProposal_Interacted
    {
        //If a custom love relation exists, this will remove that relation instead of vanilla lover
        public static bool Prefix(Pawn initiator, Pawn recipient, List<RulePackDef> extraSentencePacks, out string letterText, out string letterLabel, out LetterDef letterDef, out LookTargets lookTargets, InteractionWorker_MarriageProposal __instance)
        {
            //First check for a custom relation
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
                bool accepted = Rand.Value < __instance.AcceptanceChance(initiator, recipient);
                bool breakup = false;
                if (accepted)
                {
                    initiator.relations.RemoveDirectRelation(relation, recipient);
                    initiator.relations.AddDirectRelation(PawnRelationDefOf.Fiance, recipient);
                    //Remove any thoughts related to a previous rejected proposal
                    if (recipient.needs.mood != null)
                    {
                        recipient.needs.mood.thoughts.memories.RemoveMemoriesOfDefWhereOtherPawnIs(ThoughtDefOf.RejectedMyProposal, initiator);
                        recipient.needs.mood.thoughts.memories.RemoveMemoriesOfDefWhereOtherPawnIs(ThoughtDefOf.RejectedMyProposalMood, initiator);
                        recipient.needs.mood.thoughts.memories.RemoveMemoriesOfDefWhereOtherPawnIs(ThoughtDefOf.IRejectedTheirProposal, initiator);
                    }
                    if (initiator.needs.mood != null)
                    {
                        initiator.needs.mood.thoughts.memories.RemoveMemoriesOfDefWhereOtherPawnIs(ThoughtDefOf.RejectedMyProposalMood, recipient);
                        initiator.needs.mood.thoughts.memories.RemoveMemoriesOfDefWhereOtherPawnIs(ThoughtDefOf.IRejectedTheirProposal, recipient);
                        initiator.needs.mood.thoughts.memories.RemoveMemoriesOfDefWhereOtherPawnIs(ThoughtDefOf.RejectedMyProposal, recipient);
                    }
                    extraSentencePacks.Add(RulePackDefOf.Sentence_MarriageProposalAccepted);
                }
                else
                {
                    if (initiator.needs.mood != null)
                    {
                        initiator.needs.mood.thoughts.memories.TryGainMemory(ThoughtDefOf.RejectedMyProposal, recipient);
                    }
                    if (recipient.needs.mood != null)
                    {
                        recipient.needs.mood.thoughts.memories.TryGainMemory(ThoughtDefOf.IRejectedTheirProposal, initiator);
                    }
                    extraSentencePacks.Add(RulePackDefOf.Sentence_MarriageProposalRejected);
                    //Determine if they break up due to the rejection
                    if (Rand.Value < 0.4f)
                    {
                        initiator.relations.RemoveDirectRelation(relation, recipient);
                        //Add custom ex relation if it exists, otherwise add ex lover
                        PawnRelationDef exRel = relation.GetModExtension<LoveRelations>().exLoveRelation;
                        if (exRel == null)
                        {
                            initiator.relations.AddDirectRelation(PawnRelationDefOf.ExLover, recipient);
                        }
                        else
                        {
                            initiator.relations.AddDirectRelation(exRel, recipient);
                        }
                        breakup = true;
                        extraSentencePacks.Add(RulePackDefOf.Sentence_MarriageProposalRejectedBrokeUp);
                    }
                }
                if (PawnUtility.ShouldSendNotificationAbout(initiator) || PawnUtility.ShouldSendNotificationAbout(recipient))
                {
                    StringBuilder stringBuilder = new StringBuilder();
                    if (accepted)
                    {
                        letterLabel = "LetterLabelAcceptedProposal".Translate();
                        letterDef = LetterDefOf.PositiveEvent;
                        stringBuilder.AppendLine("LetterAcceptedProposal".Translate(initiator.Named("INITIATOR"), recipient.Named("RECIPIENT")));
                        if (initiator.relations.nextMarriageNameChange != 0)
                        {
                            SpouseRelationUtility.DetermineManAndWomanSpouses(initiator, recipient, out Pawn man, out Pawn woman);
                            stringBuilder.AppendLine();
                            if (initiator.relations.nextMarriageNameChange == MarriageNameChange.MansName)
                            {
                                stringBuilder.AppendLine("LetterAcceptedProposal_NameChange".Translate(woman.Named("PAWN"), (man.Name as NameTriple).Last));
                            }
                            else
                            {
                                stringBuilder.AppendLine("LetterAcceptedProposal_NameChange".Translate(man.Named("PAWN"), (woman.Name as NameTriple).Last));
                            }
                        }
                    }
                    else
                    {
                        letterLabel = "LetterLabelRejectedProposal".Translate();
                        letterDef = LetterDefOf.NegativeEvent;
                        stringBuilder.AppendLine("LetterRejectedProposal".Translate(initiator.Named("INITIATOR"), recipient.Named("RECIPIENT")));
                        if (breakup)
                        {
                            stringBuilder.AppendLine();
                            stringBuilder.AppendLine("LetterNoLongerLovers".Translate(initiator.Named("PAWN1"), recipient.Named("PAWN2")));
                        }
                    }
                    letterText = stringBuilder.ToString().TrimEndNewlines();
                    lookTargets = new LookTargets(initiator, recipient);
                    return false;
                }
                else
                {
                    letterLabel = null;
                    letterText = null;
                    letterDef = null;
                    lookTargets = null;
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
