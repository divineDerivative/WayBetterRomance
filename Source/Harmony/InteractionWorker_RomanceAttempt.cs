using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using static BetterRomance.WBRLogger;

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
            if (!initiator.CanInitiateRomance() || recipient.DroneCheck())
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
            if (initiator.needs.mood.thoughts.memories.Memories.Any(x => x.def == ThoughtDefOf.RebuffedMyRomanceAttempt && x.otherPawn == recipient))
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
            //Only check the initiator's opinion
            if (opinionOfTarget < minOpinion)
            {
                __result = 0f;
                return false;
            }

            float cheatChance = 1f;
            //Exclude cheating precept because that is included in RotR's postfix
            if (!RomanceUtilities.WillPawnContinue(initiator, recipient, out _, true, true))
            {
                //Do not allow if they've decided not to cheat
                __result = 0f;
                return false;
            }
            //Opinion of partner is already checked in WillPawnContinue

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
            if (RomanceUtilities.IsThisCheating(pawn, null, out List<Pawn> cheatList, true))
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
        public static bool Prefix(Pawn pawn) => pawn.CaresAboutCheating();
    }

    //Determines factors based on opinion of other pawn, used by SuccessChance
    //Needs patched to use min romance setting instead of static 5f from vanilla
    [HarmonyPatch(typeof(InteractionWorker_RomanceAttempt), "OpinionFactor")]
    public static class InteractionWorker_RomanceAttempt_OpinionFactor
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) => instructions.MinOpinionRomanceTranspiler(OpCodes.Ldarg_1);

        //Compatibility with mods that remove mood need
        public static bool Prefix(Pawn initiator, Pawn recipient, ref float __result)
        {
            if (recipient.needs.mood is null)
            {
                __result = 0f;
                return false;
            }
            return true;
        }
    }

    //Determines factors based on relationships with other pawns, used by SuccessChance
    //Needs patched to use my smarter method of looking at existing partners
    [HarmonyPatch(typeof(InteractionWorker_RomanceAttempt), "PartnerFactor")]
    public static class InteractionWorker_RomanceAttempt_PartnerFactor
    {
        public static bool Prefix(Pawn initiator, Pawn recipient, ref float __result)
        {
            RomanceUtilities.WillPawnContinue(recipient, initiator, out float chance, true, true);
            __result = chance;
            return false;
        }
    }

    //Adds a sexuality factor to the romance success chance tooltip
    //Adjust tooltip for psychic bonding
    [HarmonyPatch(typeof(InteractionWorker_RomanceAttempt), nameof(InteractionWorker_RomanceAttempt.RomanceFactors))]
    public static class InteractionWorker_RomanceAttempt_RomanceFactors
    {
        //If psychic bonding will result, only show that on the tooltip
        public static bool Prefix(Pawn romancer, Pawn romanceTarget, ref string __result)
        {
            Gene_PsychicBonding initiatorGene = romancer.genes?.GetFirstGeneOfType<Gene_PsychicBonding>();
#if v1_4
            if (initiatorGene is not null && InteractionWorker_RomanceAttempt.CanCreatePsychicBondBetween_NewTemp(romancer, romanceTarget))
#else
            if (initiatorGene is not null && InteractionWorker_RomanceAttempt.CanCreatePsychicBondBetween(romancer, romanceTarget))
#endif
            {
                StringBuilder stringBuilder = new();
                stringBuilder.AppendLine((string)InfoHelper.RomanceFactorLine.Invoke(null, [initiatorGene.LabelCap, 1f]));
                __result = stringBuilder.ToString();
                return false;
            }
            return true;
        }
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
        {
            MethodInfo PrettinessFactor = AccessTools.Method(typeof(Pawn_RelationsTracker), nameof(Pawn_RelationsTracker.PrettinessFactor));
            Label newLabel = ilg.DefineLabel();
            Label oldLabel = new();
            LocalBuilder num = ilg.DeclareLocal(typeof(float));
            bool startFound = false;

            foreach (CodeInstruction code in instructions)
            {
                if (startFound && code.Branches(out _))
                {
                    oldLabel = (Label)code.operand;
                    code.operand = newLabel;
                }

                yield return code;

                if (startFound && code.opcode == OpCodes.Pop)
                {
                    //num = RomanceUtilities.SexualityFactor(romanceTarget, romancer);
                    yield return new CodeInstruction(OpCodes.Ldarg_1).WithLabels(newLabel);
                    yield return new(OpCodes.Ldarg_0);
                    yield return CodeInstruction.Call(typeof(RomanceUtilities), nameof(RomanceUtilities.SexualityFactor));
                    yield return new(OpCodes.Stloc, num);
                    //if (num != 1f)
                    yield return new(OpCodes.Ldloc, num);
                    yield return new(OpCodes.Ldc_R4, 1f);
                    yield return new(OpCodes.Beq_S, oldLabel);
                    //stringBuilder.AppendLine(RomanceFactorLine("WBR.HookupChanceSexuality".Translate(), num);
                    yield return new(OpCodes.Ldloc_0);
                    yield return new(OpCodes.Ldstr, "WBR.HookupChanceSexuality");
                    yield return CodeInstruction.Call(typeof(Translator), nameof(Translator.Translate), [typeof(string)]);
                    yield return CodeInstruction.Call(typeof(TaggedString), "op_Implicit", [typeof(TaggedString)]);
                    yield return new(OpCodes.Ldloc, num);
                    yield return CodeInstruction.Call(typeof(InteractionWorker_RomanceAttempt), "RomanceFactorLine");
                    yield return CodeInstruction.Call(typeof(StringBuilder), nameof(StringBuilder.AppendLine), [typeof(string)]);
                    yield return new(OpCodes.Pop);

                    startFound = false;
                }
                //We want to insert our stuff after the beauty line
                if (code.Calls(PrettinessFactor))
                {
                    startFound = true;
                }
            }
        }
    }

    //Prevent psychic bonding if the initiator doesn't have the gene
#if v1_4
    [HarmonyPatch(typeof(InteractionWorker_RomanceAttempt), nameof(InteractionWorker_RomanceAttempt.CanCreatePsychicBondBetween_NewTemp))]
#else
    [HarmonyPatch(typeof(InteractionWorker_RomanceAttempt), nameof(InteractionWorker_RomanceAttempt.CanCreatePsychicBondBetween))]
#endif
    public static class InteractionWorker_RomanceAttempt_CanCreatePsychicBondBetween
    {
        public static void Postfix(Pawn initiator, Pawn recipient, ref bool __result)
        {
            if (__result)
            {
                Gene_PsychicBonding initiatorGene = initiator.genes?.GetFirstGeneOfType<Gene_PsychicBonding>();
                Gene_PsychicBonding recipientGene = recipient.genes?.GetFirstGeneOfType<Gene_PsychicBonding>();
                if (recipientGene is not null && initiatorGene is null)
                {
                    __result = false;
                }
            }
        }
    }
}
