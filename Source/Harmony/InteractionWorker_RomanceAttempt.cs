using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using UnityEngine;
using Verse;

namespace BetterRomance.HarmonyPatches
{
    //This determines chances of a pawn initiating a romance attempt (not a hookup)
    [HarmonyPatch(typeof(InteractionWorker_RomanceAttempt), nameof(InteractionWorker_RomanceAttempt.RandomSelectionWeight))]
    [HarmonyAfter(["cedaro.NoHopelessRomance"])]
    public static class InteractionWorker_RomanceAttempt_RandomSelectionWeight
    {
        //Changes from Vanilla:
        //Updated with new orientation options and traits.
        //Pawn can't perform romance attempt if recently rebuffed.
        //Pawn in mental break can't be targeted.
        //Uses my catch all method for cheating considerations
        public static bool Prefix(Pawn initiator, Pawn recipient, ref float __result)
        {
            //If either party doesn't have sexuality traits, assign them
            initiator.EnsureTraits();
            recipient.EnsureTraits();
            //Skip during tutorial
            if (TutorSystem.TutorialMode)
            {
                __result = 0f;
                return false;
            }
            //Don't allow for drones
            if (initiator.DroneCheck() || recipient.DroneCheck())
            {
                __result = 0f;
                return false;
            }
            //Don't allow for juveniles
            if (initiator.DevelopmentalStage.Juvenile() || recipient.DevelopmentalStage.Juvenile())
            {
                __result = 0f;
                return false;
            }
#if v1_5
            if (initiator.Inhumanized())
            {
                __result = 0f;
                return false;
            }
#endif
            //Don't allow if target is in mental state or is already in a love relation with initiator
            if (recipient.InMentalState || LovePartnerRelationUtility.LovePartnerRelationExists(initiator, recipient))
            {
                __result = 0f;
                return false;
            }
            //Leave them alone if they said no recently enough to remember it
            if (initiator.needs.mood.thoughts.memories.NumMemoriesOfDef(ThoughtDefOf.RebuffedMyRomanceAttempt) > 0)
            {
                __result = 0f;
                return false;
            }
            //Start with basic romance factor
            float romanceChance = initiator.relations.SecondaryRomanceChanceFactor(recipient);
            float minRomanceChance = .2f;
            //If it's too low, do not allow
            if (romanceChance < minRomanceChance)
            {
                __result = 0f;
                return false;
            }
            //If opinion is too low, do not allow
            int minOpinion = initiator.MinOpinionForRomance();
            int opinionOfTarget = initiator.relations.OpinionOf(recipient);
            if (opinionOfTarget < minOpinion || recipient.relations.OpinionOf(initiator) < minOpinion)
            {
                __result = 0f;
                return false;
            }

            float cheatChance = 1f;
            if (!RomanceUtilities.WillPawnContinue(initiator, recipient, out Pawn cheatOn, true))
            {
                //Do not allow if they've decided not to cheat
                __result = 0f;
                return false;
            }
            else
            {
                //Otherwise, adjust chances based on most liked love partner
                if (cheatOn != null)
                {
                    cheatChance = Mathf.InverseLerp(50f, -50f, initiator.relations.OpinionOf(cheatOn));
                }
            }

            //Adjust romance chance to account for < minRomanceChance being thrown out above
            //0 at minRomanceChance factor, 1 at 1
            float romanceChanceFactor = Mathf.InverseLerp(minRomanceChance, 1f, romanceChance);
            //Adjust based on opinion; 0 at min opinion, 1 at 100
            float opinionFactor = Mathf.InverseLerp(minOpinion, 100f, opinionOfTarget);
            //Orientation match is already done in the secondary romance chance factor
            //Smash them all together for the final result
            __result = 1.15f * romanceChanceFactor * opinionFactor * cheatChance;
            return false;
        }
    }

    //This breaks up with existing lovers/fiances if pawn cares about cheating and a new lover is not allowed by ideo
    [HarmonyPatch(typeof(InteractionWorker_RomanceAttempt), "BreakLoverAndFianceRelations")]
    public static class InteractionWorker_RomanceAttempt_BreakLoverAndFianceRelations
    {
        //Changes from Vanilla:
        //Accounts for cheating settings
        //Checks for additional love relations
        public static bool Prefix(Pawn pawn, out List<Pawn> oldLoversAndFiances, InteractionWorker_RomanceAttempt __instance)
        {
            //This has to happen no matter what, so that it can be passed out
            oldLoversAndFiances = new();
            //If they don't think they're cheating, don't break up with anyone, but keep list for later
            if (RomanceUtilities.IsThisCheating(pawn, null, out List<Pawn> cheatList))
            {
                //Grab all relations
                int num = 200;
                //Code from vanilla to determine if a new lover is allowed by ideo
                while (num > 0 && !IdeoUtility.DoerWillingToDo(pawn.GetHistoryEventForLoveRelationCountPlusOne(), pawn))
                {
                    LogUtil.Message($"{pawn.LabelShort} is breaking up with someone because they're about to take a new lover", true);
                    //Grab the least liked lover
                    Pawn leastLikedLover = LovePartnerRelationUtility.ExistingLeastLikedPawnWithRelation(pawn, (DirectPawnRelation r) => r.def == PawnRelationDefOf.Lover);
                    if (leastLikedLover != null)
                    {
                        //Remove lover relation, add ex lover relation
                        pawn.relations.RemoveDirectRelation(PawnRelationDefOf.Lover, leastLikedLover);
                        pawn.relations.AddDirectRelation(PawnRelationDefOf.ExLover, leastLikedLover);
                        oldLoversAndFiances.Add(leastLikedLover);
                        num--;
                        continue;
                    }
                    //If no lovers are found, find least liked fiance
                    Pawn leastLikedFiance = LovePartnerRelationUtility.ExistingLeastLikedPawnWithRelation(pawn, (DirectPawnRelation r) => r.def == PawnRelationDefOf.Fiance);
                    if (leastLikedFiance != null)
                    {
                        pawn.relations.RemoveDirectRelation(PawnRelationDefOf.Fiance, leastLikedFiance);
                        pawn.relations.AddDirectRelation(PawnRelationDefOf.ExLover, leastLikedFiance);
                        oldLoversAndFiances.Add(leastLikedFiance);
                        num--;
                        continue;
                    }
                    //If no fiances found, look through other love relations
                    if (Settings.LoveRelationsLoaded)
                    {
                        foreach (PawnRelationDef rel in CustomLoveRelationUtility.LoveRelations)
                        {
                            Pawn leastLikedOther = LovePartnerRelationUtility.ExistingLeastLikedPawnWithRelation(pawn, (DirectPawnRelation r) => r.def == rel);
                            if (leastLikedOther != null && rel.GetModExtension<LoveRelations>().shouldBreakForNewLover)
                            {
                                pawn.relations.RemoveDirectRelation(rel, leastLikedOther);
                                pawn.relations.AddDirectRelation(rel.GetExRelationDef(), leastLikedOther);
                                oldLoversAndFiances.Add(leastLikedOther);
                                num--;
                                break;
                            }
                        }
                        continue;
                    }
                    break;
                }
            }
            //This will attempt to add the cheater thought even if the pawn does not break up with them
            //Need to make sure this doesn't add thought twice if they do break up with them
            if (!cheatList.NullOrEmpty())
            {
                foreach (Pawn p in cheatList)
                {
                    AccessTools.Method(typeof(InteractionWorker_RomanceAttempt), "TryAddCheaterThought").Invoke(__instance, [p, pawn]);
                }
            }
            return false;
        }
    }

    //This just skips the method if the pawn that was cheated on doesn't care about cheating
    [HarmonyPatch(typeof(InteractionWorker_RomanceAttempt), "TryAddCheaterThought")]
    public static class InteractionWorker_RomanceAttempt_TryAddCheaterThought
    {
        //Changes from Vanilla:
        //Checks if pawn cares about cheating
        public static bool Prefix(Pawn pawn)
        {
            return pawn.CaresAboutCheating();
        }
    }

    //Determines factors based on opinion of other pawn, used by SuccessChance
    //Needs patched to use min romance setting instead of static 5f from vanilla
    [HarmonyPatch(typeof(InteractionWorker_RomanceAttempt), "OpinionFactor")]
    public static class InteractionWorker_RomanceAttempt_OpinionFactor
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction code in instructions)
            {
                if (code.LoadsConstant(5f))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.MinOpinionForRomance));
                    yield return new CodeInstruction(OpCodes.Conv_R4);
                }
                else
                {
                    yield return code;
                }
            }
        }
    }

    //Determines factors based on relationships with other pawns, used by SuccessChance
    //Needs patched to use my smarter methods of looking at existing partners
    [HarmonyPatch(typeof(InteractionWorker_RomanceAttempt), "PartnerFactor")]
    public static class InteractionWorker_RomanceAttempt_PartnerFactor
    {
        internal static bool forTooltip;
        public static bool Prefix(Pawn initiator, Pawn recipient, ref float __result)
        {
            float relationFactor = 1f;
            //Check if this is cheating and whether they decide to do it anyways, also grabs the partner they would feel worst about cheating on
            if (!RomanceUtilities.WillPawnContinue(recipient, initiator, out Pawn partnerToConsider, true) && !forTooltip)
            {
                __result = 0f;
                return false;
            }
            else
            {
                //There is a partner they would be cheating on, adjustments to factor are taken from vanilla
                if (partnerToConsider != null)
                {
                    relationFactor = PartnerRelationFactor(recipient, partnerToConsider);
                }
                //If there's no partner they're cheating on, then factor remains unchanged
                __result = relationFactor;
                return false;
            }
        }

        /// <summary>
        /// Factor based on the type of relationship <paramref name="pawn"/> has with <paramref name="partner"/>
        /// </summary>
        /// <param name="pawn"></param>
        /// <param name="partner"></param>
        /// <returns></returns>
        private static float PartnerRelationFactor(Pawn pawn, Pawn partner)
        {
            float relationFactor = 1f;
            if (pawn.relations.DirectRelationExists(PawnRelationDefOf.Lover, partner))
            {
                relationFactor = 0.6f;
            }
            else if (pawn.relations.DirectRelationExists(PawnRelationDefOf.Fiance, partner))
            {
                relationFactor = 0.1f;
            }
            else if (pawn.relations.DirectRelationExists(PawnRelationDefOf.Spouse, partner))
            {
                relationFactor = 0.3f;
            }
            //Check for custom relations and use same adjustment as lover
            else if (CustomLoveRelationUtility.CheckCustomLoveRelations(pawn, partner) != null)
            {
                relationFactor = 0.6f;
            }
            //This checks opinion of existing relation
            //0 at 100 opinion, 1 at 0 opinion
            relationFactor *= Mathf.InverseLerp(100f, 0f, pawn.relations.OpinionOf(partner));
            if (pawn.story.traits.HasTrait(RomanceDefOf.Philanderer))
            {
                //Increase for philanderer trait
                relationFactor *= 1.6f;
                //Super increase if their current partner is not on the map
                if (partner.Map != pawn.Map)
                {
                    relationFactor *= 2f;
                }
            }
            //Adjust based on romance chance factor of existing relation
            float rcFactor = pawn.relations.SecondaryLovinChanceFactor(partner);
            float clamped = Mathf.Clamp01(1f - rcFactor);
            relationFactor *= clamped;
            return relationFactor;
        }

        //Reset when done
        public static void Postfix()
        {
            forTooltip = false;
        }
    }

    //Adds a sexuality factor to the romance success chance tooltip
    [HarmonyPatch(typeof(InteractionWorker_RomanceAttempt), nameof(InteractionWorker_RomanceAttempt.RomanceFactors))]
    public static class InteractionWorker_RomanceAttempt_RomanceFactors
    {
        //Lets the PartnerFactor patch know I don't care about the result of WillPawnContinue
        public static void Prefix()
        {
            InteractionWorker_RomanceAttempt_PartnerFactor.forTooltip = true;
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
        {
            MethodInfo PrettinessFactor = AccessTools.Method(typeof(Pawn_RelationsTracker), nameof(Pawn_RelationsTracker.PrettinessFactor));
            Label newLabel = ilg.DefineLabel();
            Label oldLabel = ilg.DefineLabel();
            LocalBuilder num = ilg.DeclareLocal(typeof(float));
            bool startFound = false;

            foreach (CodeInstruction code in instructions)
            {
                if (startFound && code.opcode == OpCodes.Beq_S)
                {
                    oldLabel = (Label)code.operand;
                    code.operand = newLabel;
                }

                yield return code;

                if (startFound && code.opcode == OpCodes.Pop)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_1) { labels = [newLabel] };
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return CodeInstruction.Call(typeof(RomanceUtilities), nameof(RomanceUtilities.SexualityFactor));
                    yield return new CodeInstruction(OpCodes.Stloc, num);
                    yield return new CodeInstruction(OpCodes.Ldloc, num);
                    yield return new CodeInstruction(OpCodes.Ldc_R4, 1f);
                    yield return new CodeInstruction(OpCodes.Beq_S, oldLabel);
                    yield return new CodeInstruction(OpCodes.Ldloc_0);
                    yield return new CodeInstruction(OpCodes.Ldstr, "WBR.HookupChanceSexuality");
                    yield return CodeInstruction.Call(typeof(Translator), nameof(Translator.Translate), [typeof(string)]);
                    yield return CodeInstruction.Call(typeof(TaggedString), "op_Implicit", [typeof(TaggedString)]);
                    yield return new CodeInstruction(OpCodes.Ldloc, num);
                    yield return CodeInstruction.Call(typeof(InteractionWorker_RomanceAttempt), "RomanceFactorLine");
                    yield return CodeInstruction.Call(typeof(StringBuilder), nameof(StringBuilder.AppendLine), [typeof(string)]);
                    yield return new CodeInstruction(OpCodes.Pop);

                    startFound = false;
                }

                if (code.Calls(PrettinessFactor))
                {
                    startFound = true;
                }
            }
        }
    }
}
