using System.Collections.Generic;
using RimWorld;
using Verse;
using HarmonyLib;
using VanillaRacesExpandedHighmate;
using System.Reflection.Emit;
using System.Reflection;
using System;

namespace BetterRomance.HarmonyPatches
{
    public static class OtherMod_Patches
    {
        public static bool IsSexualityTraitPrefix(Trait trait, ref bool __result)
        {
            __result = RomanceUtilities.OrientationTraits.Contains(trait.def);
            return false;
        }

        public static void RJWAsexualPostfix(ref bool __result, Pawn pawn)
        {
            __result = __result || pawn.IsAsexual();
        }
    }

    public static class VREPatches
    {
        public static IEnumerable<CodeInstruction> VREMinAgeTranspiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            foreach (CodeInstruction code in instructions)
            {
                if (code.LoadsConstant(16f))
                {
                    if (original.DeclaringType == typeof(CompAbilityEffect_InitiateLovin))
                    {
                        yield return new CodeInstruction(OpCodes.Ldloc_0);
                    }
                    else if (original.DeclaringType == typeof(Need_Lovin))
                    {
                        yield return new CodeInstruction(OpCodes.Ldarg_0);
                        yield return CodeInstruction.LoadField(typeof(Need_Lovin), "pawn");
                    }
                    yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.MinAgeForSex));
                }
                else
                {
                    yield return code;
                }
            }
        }

        public static IEnumerable<CodeInstruction> VREAsexualTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            FieldInfo TraitDefOfAsexual = AccessTools.Field(typeof(TraitDefOf), nameof(TraitDefOf.Asexual));
            bool asexualFound = false;
            foreach (CodeInstruction code in instructions)
            {
                if (code.LoadsField(TraitDefOfAsexual))
                {
                    asexualFound = true;
                    yield return new CodeInstruction(OpCodes.Pop).MoveLabelsFrom(code);
                    yield return new CodeInstruction(OpCodes.Ldloc_0);
                }
                else if (asexualFound)
                {
                    yield return CodeInstruction.Call(typeof(RomanceUtilities), nameof(RomanceUtilities.IsAsexual));
                    asexualFound = false;
                }
                else
                {
                    yield return code;
                }
            }
        }

        public static bool VREGetLoversPrefix(ref List<Pawn> __result, Pawn pawn)
        {
            __result = RomanceUtilities.GetAllLoveRelationPawns(pawn, false, false);
            return false;
        }

        //JobDriver_InitiateLovin.ProcessBreakups
        //prefix to remove custom relations first, then return true
        public static bool VREProcessBreakupsPrefix(Pawn initiator, Pawn recipient, ref bool __state)
        {
            if (!SettingsUtilities.LoveRelations.EnumerableNullOrEmpty())
            {
                bool thoughtAdded = false;
                foreach (PawnRelationDef rel in SettingsUtilities.LoveRelations)
                {
                    if (initiator.relations.DirectRelationExists(rel, recipient))
                    {
                        initiator.relations.RemoveDirectRelation(rel, recipient);
                        PawnRelationDef exRel = rel.GetModExtension<LoveRelations>().exLoveRelation;
                        if (exRel == null)
                        {
                            initiator.relations.AddDirectRelation(PawnRelationDefOf.ExLover, recipient);
                        }
                        else
                        {
                            initiator.relations.AddDirectRelation(exRel, recipient);
                        }
                        if (!thoughtAdded)
                        {
                            recipient.needs.mood?.thoughts.memories.TryGainMemory(ThoughtDefOf.BrokeUpWithMe, initiator);
                            thoughtAdded = true;
                        }
                    }
                }
                if (!initiator.relations.DirectRelationExists(PawnRelationDefOf.Lover, recipient) && !initiator.relations.DirectRelationExists(PawnRelationDefOf.Fiance, recipient))
                {
                    __state = true;
                }
            }
            return true;
        }

        public static void VREProcessBreakupsPostfix(Pawn initiator, Pawn recipient, bool __state)
        {
            if (__state)
            {
                initiator.relations.TryRemoveDirectRelation(PawnRelationDefOf.ExLover, recipient);
            }
        }

        public static void PatchVRE(this Harmony harmony)
        {
            harmony.Patch(typeof(CompAbilityEffect_InitiateLovin).GetMethod("Valid", new Type[] { typeof(LocalTargetInfo), typeof(bool) }), transpiler: new HarmonyMethod(typeof(VREPatches).GetMethod("VREMinAgeTranspiler")));
            harmony.Patch(typeof(CompAbilityEffect_InitiateLovin).GetMethod("Valid", new Type[] { typeof(LocalTargetInfo), typeof(bool) }), transpiler: new HarmonyMethod(typeof(VREPatches).GetMethod("VREAsexualTranspiler")));
            harmony.Patch(typeof(CompAbilityEffect_InitiateLovin).GetMethod("GetLovers"), prefix: new HarmonyMethod(typeof(VREPatches).GetMethod("VREGetLoversPrefix")));
            harmony.Patch(typeof(JobDriver_InitiateLovin).GetMethod("GetLovers"), prefix: new HarmonyMethod(typeof(VREPatches).GetMethod("VREGetLoversPrefix")));
            harmony.Patch(typeof(JobDriver_InitiateLovin).GetMethod("ProcessBreakups"), prefix: new HarmonyMethod(typeof(VREPatches).GetMethod("VREProcessBreakupsPrefix")), postfix: new HarmonyMethod(typeof(VREPatches).GetMethod("VREProcessBreakupsPostfix")));
            harmony.Patch(typeof(Need_Lovin).GetMethod("get_ShowOnNeedList"), transpiler: new HarmonyMethod(typeof(VREPatches).GetMethod("VREMinAgeTranspiler")));
            harmony.Patch(typeof(Need_Lovin).GetMethod("get_IsFrozen"), transpiler: new HarmonyMethod(typeof(VREPatches), "VREMinAgeTranspiler"));
            harmony.Patch(AccessTools.Method(typeof(JobDriver_DoLovinCasual), "GenerateRandomMinTicksToNextLovin"), postfix: new HarmonyMethod(typeof(VanillaRacesExpandedHighmate_JobDriver_Lovin_GenerateRandomMinTicksToNextLovin_Patch), "PawnFucks"));
        }
    }
}

namespace BetterRomance
{
    public static class OtherMod_Methods
    {
        public static bool HasLovinForPleasureApproach(Pawn pawn1, Pawn pawn2)
        {
            return pawn1.relations.GetAdditionalPregnancyApproachData().pawnsWithLovinForPleasure.Contains(pawn2);
        }
    }
}
