using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using RomanceOnTheRim;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Verse;

namespace BetterRomance
{
    public class RotR_Integration
    {
        //This is the cheating section of the original postfix patches, separated out so it can be applied to hookups and dates
        public static float RotRCheatChanceModifier(Pawn pawn)
        {
            if (pawn.HasLoverFastFromRomanceNeed())
            {
                if (pawn.Ideo.HasPrecept(CustomPreceptDefOf.RomanceOnTheRim_Cheat_Forbidden))
                {
                    return 0f;
                }
                if (pawn.Ideo.HasPrecept(CustomPreceptDefOf.RomanceOnTheRim_Cheat_Discouraged))
                {
                    return 0.5f;
                }
                if (pawn.Ideo.HasPrecept(CustomPreceptDefOf.RomanceOnTheRim_Cheat_Encouraged))
                {
                    return 1.5f;
                }
            }
            return 1f;
        }

        public static string RotRCheatingPreceptExplanation(Pawn pawn)
        {
            string result = string.Empty;
            if (pawn.Ideo.HasPrecept(CustomPreceptDefOf.RomanceOnTheRim_Cheat_Forbidden))
            {
                result = PreceptExplanation(CustomPreceptDefOf.RomanceOnTheRim_Cheat_Forbidden, 0f);
            }
            else if (pawn.Ideo.HasPrecept(CustomPreceptDefOf.RomanceOnTheRim_Cheat_Discouraged))
            {
                result = PreceptExplanation(CustomPreceptDefOf.RomanceOnTheRim_Cheat_Discouraged, 0.5f);
            }
            else if (pawn.Ideo.HasPrecept(CustomPreceptDefOf.RomanceOnTheRim_Cheat_Encouraged))
            {
                result = PreceptExplanation(CustomPreceptDefOf.RomanceOnTheRim_Cheat_Encouraged, 1.5f);
            }
            return result;
        }

        private static string PreceptExplanation(PreceptDef preceptDef, float value)
        {
            return (string)HelperClasses.RotRPreceptExplanation.Invoke(null, [preceptDef, value]);
        }

        internal static void DiscardPawn(Pawn pawn)
        {
            if (pawn.IsWorldPawn())
            {
                Find.WorldPawns.RemoveAndDiscardPawnViaGC(pawn);
            }
            else
            {
                Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.Discard);
            }
        }
    }

    namespace HarmonyPatches
    {
        public static class RotRPatches
        {
            public static void PatchRotR(this Harmony harmony)
            {
                if (ModsConfig.IdeologyActive)
                {
                    //Remove the original patch
                    harmony.Unpatch(typeof(InteractionWorker_RomanceAttempt).GetMethod("RandomSelectionWeight"), typeof(HarmonyPatch_InteractionWorker_RomanceAttempt).GetMethod("RandomSelectionWeightPostfix"));
                    //Add my version of the patch
                    harmony.Patch(typeof(InteractionWorker_RomanceAttempt).GetMethod("RandomSelectionWeight"), postfix: new(typeof(RotRPatches).GetMethod(nameof(RomanceAttempt_RandomSelectionWeight_Patch))));

                    harmony.Unpatch(typeof(InteractionWorker_RomanceAttempt).GetMethod("SuccessChance"), typeof(HarmonyPatch_InteractionWorker_RomanceAttempt).GetMethod(nameof(HarmonyPatch_InteractionWorker_RomanceAttempt.SuccessChancePostfix)));
                    harmony.Patch(typeof(InteractionWorker_RomanceAttempt).GetMethod("SuccessChance"), postfix: new(typeof(RotRPatches).GetMethod(nameof(SuccessChancePostfix))));

                    harmony.Patch(typeof(HarmonyPatch_SocialCardUtility_RomanceExplanation).GetMethod("AddPreceptExplanation"), transpiler: new(typeof(RotRPatches).GetMethod(nameof(AddPreceptExplanationTranspiler))));
                    harmony.Patch(AccessTools.Method(typeof(QuestPart_BondOfFreedom_Reject), "DoAction"), prefix: new(typeof(RotRPatches).GetMethod(nameof(BondOfFreedom_RejectPrefix))), postfix: new(typeof(RotRPatches).GetMethod(nameof(BondOfFreedom_RejectPostfix))));
                }
#if v1_4
                harmony.Patch(AccessTools.Method(typeof(QuestNode_Root_Crush), "TestRunInt"), transpiler: new(typeof(RotRPatches).GetMethod(nameof(QuestNode_Root_CrushTranspiler))));
#else
                harmony.Patch(AccessTools.Method(typeof(QuestNode_Root_Crush), "GetSinglePawns"), transpiler: new(typeof(RotRPatches).GetMethod(nameof(QuestNode_Root_Crush_GetSinglePawnsTranspiler))));
#endif
                harmony.Patch(AccessTools.Method(typeof(QuestNode_Root_DiplomaticMarriageAway), "SpawnSuitor"), postfix: new(typeof(RotRPatches).GetMethod(nameof(SpawnSuitorAwayPostfix))), transpiler: new(typeof(RotRPatches).GetMethod(nameof(SpawnSuitorTranspiler))));
                harmony.Patch(AccessTools.Method(typeof(QuestNode_Root_DiplomaticMarriage), "SpawnSuitor"), postfix: new(typeof(RotRPatches).GetMethod(nameof(SpawnSuitorPostfix))), transpiler: new(typeof(RotRPatches).GetMethod(nameof(SpawnSuitorTranspiler))));

#if v1_4
                Type[] innerTypes = typeof(Dialog_DiplomaticMarriage).GetNestedTypes(AccessTools.all);
                foreach (Type innerType in innerTypes)
                {
                    IEnumerable<MethodInfo> methods = innerType.GetMethods(AccessTools.all).Where(x => x.Name.Contains("AddRejectAndAcceptButtons"));
                    if (methods.Count() == 1)
                    {
                        AddRejectAndAcceptButtonsCompilerType = innerType;
                        harmony.Patch(methods.First(), transpiler: new(typeof(RotRPatches).GetMethod(nameof(AddRejectAndAcceptButtonsTranspiler))));
                        break;
                    }
                }
#else
                harmony.Patch(AccessTools.Method(typeof(Dialog_DiplomaticMarriage), "AcceptButtonActive"), postfix: new(typeof(RotRPatches).GetMethod(nameof(AcceptButtonActivePostfix))));
#endif
            }

            //This is the same as the original patch with the cheating section removed
            public static void RomanceAttempt_RandomSelectionWeight_Patch(ref float __result, Pawn initiator)
            {
                if (__result == 0f || initiator.Ideo == null)
                {
                    return;
                }
                if (initiator.Ideo.HasPrecept(CustomPreceptDefOf.RomanceOnTheRim_RomanceAttempt_Arranged))
                {
                    __result = 0f;
                }
                else if (initiator.Ideo.HasPrecept(CustomPreceptDefOf.RomanceOnTheRim_RomanceAttempt_Forbidden))
                {
                    __result = 0f;
                }
                else if (initiator.Ideo.HasPrecept(CustomPreceptDefOf.RomanceOnTheRim_RomanceAttempt_Discouraged))
                {
                    __result = Mathf.Clamp01(__result * 0.5f);
                }
                else if (initiator.Ideo.HasPrecept(CustomPreceptDefOf.RomanceOnTheRim_RomanceAttempt_Encouraged))
                {
                    __result = Mathf.Clamp01(__result * 1.5f);
                }
                __result *= RotR_Integration.RotRCheatChanceModifier(initiator);
            }

            //This is the same as the original patch with the cheating section removed
            public static void SuccessChancePostfix(ref float __result, Pawn initiator, Pawn recipient, float baseChance)
            {
                if (recipient.Ideo == null)
                {
                    return;
                }
#if v1_5
                if (InteractionWorker_RomanceAttempt.CanCreatePsychicBondBetween(initiator, recipient))
                {
                    return;
                }
#endif
                if (recipient.HasLoverFastFromRomanceNeed())
                {
                    if (initiator.Ideo.HasPrecept(CustomPreceptDefOf.RomanceOnTheRim_RomanceAttempt_Arranged) && recipient.Ideo.HasPrecept(CustomPreceptDefOf.RomanceOnTheRim_RomanceAttempt_Arranged))
                    {
                        return;
                    }
                }
                if (recipient.Ideo.HasPrecept(CustomPreceptDefOf.RomanceOnTheRim_RomanceAttempt_Forbidden))
                {
                    __result = 0f;
                }
                else if (recipient.Ideo.HasPrecept(CustomPreceptDefOf.RomanceOnTheRim_RomanceAttempt_Discouraged))
                {
                    __result = Mathf.Clamp01(__result * 0.5f);
                }
                else if (recipient.Ideo.HasPrecept(CustomPreceptDefOf.RomanceOnTheRim_RomanceAttempt_Encouraged))
                {
                    __result = Mathf.Clamp01(__result * 1.5f);
                }
                else if ((double)Math.Abs(baseChance - 1f) > 1E-06 && recipient.Ideo.HasPrecept(CustomPreceptDefOf.RomanceOnTheRim_RomanceAttempt_Arranged) && initiator.CurJobDef != JobDefOf.TryRomance && initiator.CurJobDef != CustomJobDefOf.RomanceOnTheRim_RomanceAttempt)
                {
                    __result = 0f;
                }
                __result *= RotR_Integration.RotRCheatChanceModifier(recipient);
            }

            //Removes the check against the romancer's romance need
            public static IEnumerable<CodeInstruction> AddPreceptExplanationTranspiler(IEnumerable<CodeInstruction> instructions)
            {
                bool skip = false;
                foreach (CodeInstruction code in instructions)
                {
                    if (code.IsLdarg(1))
                    {
                        skip = true;
                    }

                    if (!skip)
                    {
                        yield return code;
                    }

                    if (skip && code.Branches(out _))
                    {
                        skip = false;
                    }
                }
            }

            //Use age settings
            public static IEnumerable<CodeInstruction> QuestNode_Root_CrushTranspiler(IEnumerable<CodeInstruction> instructions)
            {
                return instructions.MinAgeForSexTranspiler(OpCodes.Ldloc_2);
            }
            public static IEnumerable<CodeInstruction> QuestNode_Root_Crush_GetSinglePawnsTranspiler(IEnumerable<CodeInstruction> instructions)
            {
                foreach (CodeInstruction code in instructions)
                {
                    if (code.Calls(AccessTools.Method(typeof(QuestNode_Root_Crush), "GetMinAge")))
                    {
                        yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.MinAgeForSex));
                    }
                    else if (code.opcode == OpCodes.Ldarg_0)
                    {
                        yield return new CodeInstruction(OpCodes.Ldloc_2);
                    }
                    else
                    {
                        yield return code;
                    }
                }
            }

            public static PawnGenerationRequest SuitorRequest;

            //Spawn a new one if the age range is incorrect
            public static void SpawnSuitorAwayPostfix(ref Pawn ___suitor, Settlement ___settlement)
            {
                if (CheckAndRegenerate(ref ___suitor))
                {
                    ___settlement.previouslyGeneratedInhabitants.Add(___suitor);
                }
            }
            public static void SpawnSuitorPostfix(ref Pawn ___suitor)
            {
                if (CheckAndRegenerate(ref ___suitor))
                {
                    PawnComponentsUtility.AddAndRemoveDynamicComponents(___suitor, true);
                }
            }
            private static bool CheckAndRegenerate(ref Pawn suitor)
            {
                //This really only helps for a race that has supplied their own settings, since otherwise it defaults to human ages, which will give the same result as the original
                FloatRange range = new(suitor.MinAgeForSex(), suitor.DeclineAtAge() + (suitor.DeclineAtAge() / 6));
                if (!range.Includes(suitor.ageTracker.AgeBiologicalYearsFloat))
                {
                    RotR_Integration.DiscardPawn(suitor);
                    SuitorRequest.BiologicalAgeRange = range;
                    suitor = PawnGenerator.GeneratePawn(SuitorRequest);
                    if (!suitor.IsWorldPawn())
                    {
                        Find.WorldPawns.PassToWorld(suitor);
                    }
                    return true;
                }
                return false;
            }

            //Save the request to use in the postfix
            public static IEnumerable<CodeInstruction> SpawnSuitorTranspiler(IEnumerable<CodeInstruction> instructions)
            {
                foreach (CodeInstruction code in instructions)
                {
                    yield return code;
                    if (code.opcode == OpCodes.Ldloc_0)
                    {
                        yield return CodeInstruction.StoreField(typeof(RotRPatches), nameof(SuitorRequest));
                        yield return new CodeInstruction(OpCodes.Ldloc_0);
                    }
                }
            }

            static Type AddRejectAndAcceptButtonsCompilerType;

            //The original assumes that SecondaryLovinChanceFactor will be 0 for an orientation mismatch
            //Since I make it non-zero, need to do a different check
            public static IEnumerable<CodeInstruction> AddRejectAndAcceptButtonsTranspiler(IEnumerable<CodeInstruction> instructions)
            {
                FieldInfo[] fields = AddRejectAndAcceptButtonsCompilerType.GetFields();
                FieldInfo thisField = null;
                foreach (FieldInfo field in fields)
                {
                    if (field.Name.Contains("this"))
                    {
                        thisField = field;
                        break;
                    }
                }
                foreach (CodeInstruction code in instructions)
                {
                    if (code.opcode == OpCodes.Stloc_0)
                    {
                        yield return new CodeInstruction(OpCodes.Ldarg_0).MoveLabelsFrom(code);
                        yield return thisField.LoadField();
                        yield return CodeInstruction.LoadField(thisField.FieldType, "suitor");
                        yield return new CodeInstruction(OpCodes.Ldarg_0);
                        yield return thisField.LoadField();
                        yield return CodeInstruction.LoadField(thisField.FieldType, "betrothed");
                        yield return CodeInstruction.Call(typeof(RotRPatches), nameof(MarriageDialogHelper));
                    }
                    yield return code;
                }
            }

            private static bool MarriageDialogHelper(bool result, Pawn suitor, Pawn betrothed)
            {
                return result && suitor.CouldWeBeMarried(betrothed);
            }

            public static void AcceptButtonActivePostfix(Pawn ___suitor, Pawn ___betrothed, ref bool __result)
            {
                __result = __result && ___suitor.CouldWeBeMarried(___betrothed);
            }

            //Check custom love relations
            //Hasn't really been tested
            public static void BondOfFreedom_RejectPrefix(Pawn ___lover, Pawn ___slave, ref bool __state)
            {
                if (Settings.LoveRelationsLoaded)
                {
                    //If they have any of these relationships, we don't want to remove ex lover later
                    if (!___lover.relations.DirectRelationExists(PawnRelationDefOf.Lover, ___slave) && !___lover.relations.DirectRelationExists(PawnRelationDefOf.Fiance, ___slave) && !___lover.relations.DirectRelationExists(PawnRelationDefOf.ExLover, ___slave))
                    {
                        __state = true;
                    }
                }
            }

            public static void BondOfFreedom_RejectPostfix(Pawn ___lover, Pawn ___slave, bool __state)
            {
                if (Settings.LoveRelationsLoaded)
                {
                    //This should tell us that they did break up
                    if (___lover.relations.DirectRelationExists(PawnRelationDefOf.ExLover, ___slave))
                    {
                        //Remove the custom relation if it exists
                        if (CustomLoveRelationUtility.CheckCustomLoveRelations(___lover, ___slave) is DirectPawnRelation relation)
                        {
                            ___lover.relations.RemoveDirectRelation(relation);
                            if (relation.def.GetModExtension<LoveRelations>().exLoveRelation is PawnRelationDef exRelation)
                            {
                                ___lover.relations.AddDirectRelation(exRelation, ___slave);
                                //Remove ex lover if it shouldn't have been added
                                if (!__state)
                                {
                                    ___lover.relations.RemoveDirectRelation(PawnRelationDefOf.ExLover, ___slave);
                                }
                            }
                        }
                    }
                }
            }

            //The age range was changed to a mod setting, which is still static for all races so without knowing the context of the player's game I can't reasonably convert the ages
            //Maybe when I get around to making race age settings visible, I can hook into RotR's settings page to display settings per race
            //Would likely need to add adoption age range to the race settings object
            ////Use age settings
            //public static IEnumerable<CodeInstruction> CheckAdoptionChanceTranspiler(IEnumerable<CodeInstruction> instructions)
            //{
            //    IEnumerable<CodeInstruction> codes = instructions.AdultMinAgeInt(OpCodes.Ldarg_0);
            //    foreach (CodeInstruction code in codes)
            //    {
            //        if (code.LoadsConstant(8))
            //        {
            //            yield return new CodeInstruction(OpCodes.Ldarg_0);
            //            yield return CodeInstruction.Call(typeof(RotRPatches), nameof(AdoptionAge));
            //        }
            //        else
            //        {
            //            yield return code;
            //        }
            //    }
            //}

            //private static int AdoptionAge(Pawn pawn)
            //{
            //    int firstGrowth = SettingsUtilities.GetGrowthMoment(pawn, 0);
            //    return firstGrowth + ((SettingsUtilities.GetGrowthMoment(pawn, 1) - firstGrowth) / 3);
            //}
        }
    }
}
