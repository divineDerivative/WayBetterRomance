using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using VanillaRacesExpandedHighmate;
using Verse;
#if !v1_6
using VFECore;
#else
using VEF.Pawns;
#endif
using VREAndroids;

namespace BetterRomance.HarmonyPatches
{
    public static class OtherMod_Patches
    {
        public static bool IsSexualityTraitPrefix(Trait trait, ref bool __result)
        {
            __result = SexualityUtility.OrientationTraits.Contains(trait.def);
            return false;
        }

        public static void RJWAsexualPostfix(ref bool __result, Pawn pawn)
        {
            __result = __result || pawn.IsAsexual();
        }

        public static IEnumerable<CodeInstruction> VanillaTraitCheckTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            return instructions.TraitToOrientationTranspiler(false);
        }

        public static IEnumerable<CodeInstruction> RimderLoveRelationTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
        {
            return instructions.LoveRelationUtilityTranspiler(ilg, PawnRelationDefOf.Lover, true, false, stopSkipping: (CodeInstruction code) => code.LoadsField(AccessTools.Field(AccessTools.TypeByName("RimderModCore"), "rimderSettings")));
        }

        public static IEnumerable<CodeInstruction> RimderExLoveRelationTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
        {
            return instructions.LoveRelationUtilityTranspiler(ilg, PawnRelationDefOf.ExLover, true, true, stopSkipping: (CodeInstruction code) => code.LoadsField(AccessTools.Field(AccessTools.TypeByName("RimderModCore"), "rimderSettings")));
        }

        //This is a modified version of their transpiler for PawnRelationWorker_Parent.CreateRelation to apply to my prefix
        public static IEnumerable<CodeInstruction> AltFertilityParentCreateRelationTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> list = instructions.ToList();
            int maleInd = list.FindIndex((CodeInstruction code) => code.LoadsField(InfoHelper.PawnGender));
            list[maleInd] = CodeInstructionMethods.Call(HelperClasses.CanImpregnate);
            list[maleInd + 1] = new CodeInstruction(OpCodes.Nop);
            list[maleInd + 2] = new CodeInstruction(OpCodes.Nop);

            int femaleInd = list.FindIndex(maleInd + 1, (CodeInstruction code) => code.LoadsField(InfoHelper.PawnGender));
            list[femaleInd] = CodeInstructionMethods.Call(HelperClasses.CanGetPregnant);
            list[femaleInd + 1] = new CodeInstruction(OpCodes.Nop);
            list[femaleInd + 2] = new CodeInstruction(OpCodes.Nop);

            return list.AsEnumerable();
        }
    }

    public static class VREPatches
    {
        public static IEnumerable<CodeInstruction> InitiateLovinMinAgeTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            return instructions.MinAgeForSexTranspiler(OpCodes.Ldloc_0);
        }

        public static IEnumerable<CodeInstruction> Need_LovinMinAgeTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            return instructions.MinAgeForSexTranspiler(new List<CodeInstruction>() { new(OpCodes.Ldarg_0), CodeInstruction.LoadField(typeof(Need_Lovin), "pawn") });
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
                    yield return CodeInstruction.Call(typeof(SexualityUtility), nameof(SexualityUtility.IsAsexual));
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
        public static void VREProcessBreakupsPrefix(Pawn initiator, Pawn recipient, ref bool __state)
        {
            if (Settings.LoveRelationsLoaded)
            {
                bool thoughtAdded = false;
                foreach (PawnRelationDef rel in CustomLoveRelationUtility.LoveRelations)
                {
                    if (initiator.relations.DirectRelationExists(rel, recipient))
                    {
                        initiator.relations.RemoveDirectRelation(rel, recipient);
                        initiator.relations.AddDirectRelation(rel.GetExRelationDef(), recipient);
                        //We only want to add the break up thought once
                        if (!thoughtAdded)
                        {
                            recipient.needs.mood?.thoughts.memories.TryGainMemory(ThoughtDefOf.BrokeUpWithMe, initiator);
                            thoughtAdded = true;
                        }
                    }
                }
                //If they don't have either of the vanilla non-spouse love relations, and we didn't add ex lover, then we need to prevent VRE from adding ex lover
                if (!initiator.relations.DirectRelationExists(PawnRelationDefOf.Lover, recipient) && !initiator.relations.DirectRelationExists(PawnRelationDefOf.Fiance, recipient) && !initiator.relations.DirectRelationExists(PawnRelationDefOf.ExLover, recipient))
                {
                    __state = true;
                }
            }
        }

        public static void VREProcessBreakupsPostfix(Pawn initiator, Pawn recipient, bool __state)
        {
            //Remove ex lover if it shouldn't have been added in the first place
            if (__state)
            {
                initiator.relations.TryRemoveDirectRelation(PawnRelationDefOf.ExLover, recipient);
            }
        }

        public static void PatchVREHighmates(this Harmony harmony)
        {
            harmony.Patch(typeof(CompAbilityEffect_InitiateLovin).GetMethod("Valid", [typeof(LocalTargetInfo), typeof(bool)]), transpiler: new(typeof(VREPatches).GetMethod(nameof(InitiateLovinMinAgeTranspiler))));
            harmony.Patch(typeof(CompAbilityEffect_InitiateLovin).GetMethod("Valid", [typeof(LocalTargetInfo), typeof(bool)]), transpiler: new(typeof(VREPatches).GetMethod(nameof(VREAsexualTranspiler))));
            harmony.Patch(typeof(CompAbilityEffect_InitiateLovin).GetMethod("GetLovers"), prefix: new(typeof(VREPatches).GetMethod(nameof(VREGetLoversPrefix))));
            harmony.Patch(typeof(JobDriver_InitiateLovin).GetMethod("GetLovers"), prefix: new(typeof(VREPatches).GetMethod(nameof(VREGetLoversPrefix))));
            harmony.Patch(typeof(JobDriver_InitiateLovin).GetMethod("ProcessBreakups"), prefix: new(typeof(VREPatches).GetMethod(nameof(VREProcessBreakupsPrefix))), postfix: new(typeof(VREPatches).GetMethod(nameof(VREProcessBreakupsPostfix))));
            harmony.Patch(typeof(Need_Lovin).GetMethod("get_ShowOnNeedList"), transpiler: new(typeof(VREPatches).GetMethod(nameof(Need_LovinMinAgeTranspiler))));
            harmony.Patch(typeof(Need_Lovin).GetMethod("get_IsFrozen"), transpiler: new(typeof(VREPatches).GetMethod(nameof(Need_LovinMinAgeTranspiler))));
            harmony.Patch(AccessTools.Method(typeof(JobDriver_DoLovinCasual), "GenerateRandomMinTicksToNextLovin"), postfix: new(typeof(VanillaRacesExpandedHighmate_JobDriver_Lovin_GenerateRandomMinTicksToNextLovin_Patch), "PawnFucks"));
        }

        public static void PatchVREAndroids(this Harmony harmony)
        {
            //Add orientation traits to allowed traits list
            AndroidSettings androidSettings = VREA_DefOf.VREA_AndroidSettings;
            foreach (TraitDef trait in SexualityUtility.OrientationTraits)
            {
                androidSettings.allowedTraits.Add(trait.defName);
            }
            //Add age change patches to hook up stuff
            HarmonyMethod ageChangePrefix = new(typeof(HarmonyPatches_ForRomanceReasonsChangeAge).GetMethod(nameof(HarmonyPatches_ForRomanceReasonsChangeAge.Prefix)));
            HarmonyMethod ageChangePostfix = new(typeof(HarmonyPatches_ForRomanceReasonsChangeAge).GetMethod(nameof(HarmonyPatches_ForRomanceReasonsChangeAge.Postfix)));
            harmony.Patch(typeof(HookupUtility).GetMethod(nameof(HookupUtility.CanDrawTryHookup)), prefix: ageChangePrefix, postfix: ageChangePostfix);
            harmony.Patch(typeof(HookupUtility).GetMethod(nameof(HookupUtility.HookupEligiblePair)), prefix: ageChangePrefix, postfix: ageChangePostfix);
            harmony.Patch(typeof(HookupUtility).GetMethod(nameof(HookupUtility.HookupEligible)), prefix: ageChangePrefix, postfix: ageChangePostfix);
        }
    }

    public static class VSIEPatches
    {
        //Check for custom love relations
        public static void GetSpouseOrLoverOrFiancePostfix(ref Pawn __result, Pawn pawn)
        {
            __result ??= pawn.FirstCustomLoveRelation()?.otherPawn;
        }

        public static Type CompilerType;

        public static IEnumerable<CodeInstruction> AddDirectRelation_PrefixTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
        {
#if !v1_6
            return instructions.LoveRelationUtilityTranspiler(ilg, PawnRelationDefOf.Lover, false, false, (CodeInstruction code) => code.LoadsField(AccessTools.Field(CompilerType, "___pawn")), (CodeInstruction code) => code.opcode == OpCodes.Ret);
#else
            return instructions.LoveRelationUtilityTranspiler(ilg, PawnRelationDefOf.Lover, false, false, (CodeInstruction code) => code.IsLdarg(0), (CodeInstruction code) => code.opcode == OpCodes.Ret);
#endif
        }
    }
}

namespace BetterRomance
{
    internal static class OtherMod_Methods
    {
        internal static int AdjustLovinTicks(int ticks, Pawn pawn1, Pawn pawn2)
        {
            if (pawn1.relations.GetAdditionalPregnancyApproachData().partners.TryGetValue(pawn2, out PregnancyApproachDef def))
            {
                ticks = (int)(ticks * def.lovinDurationMultiplier);
            }
            return ticks;
        }

        internal static void DoLovinResult(Pawn actor, Pawn partner)
        {
            if (actor.relations.GetAdditionalPregnancyApproachData().partners.TryGetValue(partner, out PregnancyApproachDef def))
            {
                def.Worker.PostLovinEffect(actor, partner);
            }
        }

        internal static Pawn GetFirstImpregnationPairLover(this Pawn pawn)
        {
            foreach (Pawn lover in RomanceUtilities.GetNonSpouseLovers(pawn, true))
            {
                if ((bool)HelperClasses.GetImpregnationPossible.Invoke(null, [pawn, lover]))
                {
                    return lover;
                }
            }
            return null;
        }
    }
}
