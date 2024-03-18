using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;
using Verse;

namespace BetterRomance.HarmonyPatches
{
    //This determines if a pawn will propose marriage to another pawn
    [HarmonyPatch(typeof(InteractionWorker_MarriageProposal), nameof(InteractionWorker_MarriageProposal.RandomSelectionWeight))]
    [HarmonyAfter(["cedaro.NoHopelessRomance"])]
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
            //If genders are incorrect for initiator's sexuality, do not allow
            if (initiator.gender == recipient.gender && initiator.IsHetero())
            {
                __result = 0f;
                return false;
            }
            if (initiator.gender != recipient.gender && initiator.IsHomo())
            {
                __result = 0f;
                return false;
            }
            //Don't allow for ace/aro pawns
            if (initiator.IsAro())
            {
                __result = 0f;
                return false;
            }
            //If they are asexual and sex repulsed, do not allow unless partner is also asexual
            if (initiator.IsAsexual() && initiator.AsexualRating() < 0.2f)
            {
                if (!recipient.IsAsexual())
                {
                    __result = 0f;
                    return false;
                }
            }
            return true;
        }

        //Remove reduction for females
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            bool foundGender = false;
            foreach (CodeInstruction code in instructions)
            {
                if (code.LoadsField(AccessTools.Field(typeof(Pawn), nameof(Pawn.gender))))
                {
                    foundGender = true;
                }
                if (foundGender && code.IsStloc())
                {
                    yield return new CodeInstruction(OpCodes.Pop);
                    foundGender = false;
                }
                else
                {
                    yield return code;
                }
            }
        }
    }

    //This determines if a pawn will accept a marriage proposal
    [HarmonyPatch(typeof(InteractionWorker_MarriageProposal), nameof(InteractionWorker_MarriageProposal.AcceptanceChance))]
    [HarmonyAfter(["Telardo.RomanceOnTheRim", "cedaro.NoHopelessRomance"])]
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
            if (initiator.gender == recipient.gender && recipient.IsHetero())
            {
                __result = 0f;
                return;
            }
            if (initiator.gender != recipient.gender && recipient.IsHomo())
            {
                __result = 0f;
                return;
            }
            //Don't allow for ace/aro pawns
            if (initiator.IsAro())
            {
                __result = 0f;
                return;
            }
            //If pawn is asexual and sex repulsed, they will only accept from another asexual pawn
            //Not sure about this
            if (recipient.IsAsexual() && recipient.AsexualRating() < 0.2f)
            {
                if (!initiator.IsAsexual())
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

    //Makes necessary changes depending on outcome of interaction
    [HarmonyPatch(typeof(InteractionWorker_MarriageProposal), nameof(InteractionWorker_MarriageProposal.Interacted))]
    public static class InteractionWorker_MarriageProposal_Interacted
    {
        //If a custom love relation exists, this will remove that relation instead of vanilla lover
        public static bool Prefix(Pawn initiator, Pawn recipient, List<RulePackDef> extraSentencePacks, out string letterText, out string letterLabel, out LetterDef letterDef, out LookTargets lookTargets, InteractionWorker_MarriageProposal __instance)
        {
            //First check for a custom relation and only run patch if one exists
            if (CustomLoveRelationUtility.CheckCustomLoveRelations(initiator, recipient) is DirectPawnRelation relation)
            {
#if v1_4
                bool accepted = Rand.Value < __instance.AcceptanceChance(initiator, recipient);
#else
                bool accepted = Rand.Value < InteractionWorker_MarriageProposal.AcceptanceChance(initiator, recipient);
#endif
                bool breakup = false;
                if (accepted)
                {
                    initiator.relations.RemoveDirectRelation(relation);
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
                    initiator.needs.mood?.thoughts.memories.TryGainMemory(ThoughtDefOf.RejectedMyProposal, recipient);
                    recipient.needs.mood?.thoughts.memories.TryGainMemory(ThoughtDefOf.IRejectedTheirProposal, initiator);
                    extraSentencePacks.Add(RulePackDefOf.Sentence_MarriageProposalRejected);
                    //Determine if they break up due to the rejection
                    if (Rand.Value < 0.4f)
                    {
                        initiator.relations.RemoveDirectRelation(relation);
                        //Add custom ex relation if it exists, otherwise add ex lover
                        initiator.relations.AddDirectRelation(relation.def.GetExRelationDef(), recipient);

                        breakup = true;
                        extraSentencePacks.Add(RulePackDefOf.Sentence_MarriageProposalRejectedBrokeUp);
                    }
                }
                if (PawnUtility.ShouldSendNotificationAbout(initiator) || PawnUtility.ShouldSendNotificationAbout(recipient))
                {
                    StringBuilder stringBuilder = new();
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
