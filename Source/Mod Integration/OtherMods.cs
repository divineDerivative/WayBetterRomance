using System.Collections.Generic;
using RimWorld;
using Verse;
using HarmonyLib;
using VFECore;
using VanillaRacesExpandedHighmate;
using System.Reflection.Emit;
using System.Reflection;
using System;
using System.Linq;

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
            int startIndex = -1;
            int endIndex = -1;
            List<CodeInstruction> codes = instructions.ToList();
            for (int i = 0; i < codes.Count; i++)
            {
                CodeInstruction code = codes[i];
                if (code.LoadsField(AccessTools.Field(typeof(Pawn), nameof(Pawn.story))))
                {
                    startIndex = i;
                }
                if (code.Calls(AccessTools.Method(typeof(TraitSet), nameof(TraitSet.HasTrait), parameters: new Type[] { typeof(TraitDef) })))
                {
                    endIndex = i;
                }
                if (startIndex != -1 && endIndex != -1)
                {
                    break;
                }
            }

            for (int i = 0; i < codes.Count; i++)
            {
                CodeInstruction code = codes[i];
                if (i < startIndex)
                {
                    yield return code;
                }
                else if (i == startIndex)
                {
                    yield return CodeInstruction.Call(typeof(SexualityUtility), nameof(SexualityUtility.IsAsexual));
                }
                else if (i > endIndex)
                {
                    yield return code;
                }
            }
        }

        //Replaces trait checks with appropriate orientation check without removing a bunch of other instructions
        public static IEnumerable<CodeInstruction> TraitToOrientationTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction code in instructions)
            {
                if (code.Calls(AccessTools.Method(typeof(TraitSet), nameof(TraitSet.HasTrait), new Type[] { typeof(TraitDef) })))
                {
                    yield return CodeInstruction.Call(typeof(OtherMod_Patches), nameof(TraitConversion));
                }
                else
                {
                    yield return code;
                }
            }
        }

        private static List<TraitDef> traits = new List<TraitDef> { TraitDefOf.Gay, TraitDefOf.Bisexual, TraitDefOf.Asexual, RomanceDefOf.Straight };
        public static bool TraitConversion(TraitSet set, TraitDef trait)
        {
            Pawn pawn = (Pawn)AccessTools.Field(typeof(TraitSet), "pawn").GetValue(set);
            if (traits.Contains(trait))
            {
                if (trait == TraitDefOf.Gay)
                {
                    return pawn.IsHomo();
                }
                if (trait == TraitDefOf.Bisexual)
                {
                    return pawn.IsBi();
                }
                if (trait == TraitDefOf.Asexual)
                {
                    return pawn.IsAro();
                }
                if (trait == RomanceDefOf.Straight)
                {
                    return pawn.IsHetero();
                }
            }
            return pawn.story.traits.HasTrait(trait);
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
        public static bool VREProcessBreakupsPrefix(Pawn initiator, Pawn recipient, ref bool __state)
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
            return true;
        }

        public static void VREProcessBreakupsPostfix(Pawn initiator, Pawn recipient, bool __state)
        {
            //Remove ex lover if it shouldn't have been added in the first place
            if (__state)
            {
                initiator.relations.TryRemoveDirectRelation(PawnRelationDefOf.ExLover, recipient);
            }
        }

        public static void PatchVRE(this Harmony harmony)
        {
            harmony.Patch(typeof(CompAbilityEffect_InitiateLovin).GetMethod("Valid", new Type[] { typeof(LocalTargetInfo), typeof(bool) }), transpiler: new HarmonyMethod(typeof(VREPatches).GetMethod(nameof(VREMinAgeTranspiler))));
            harmony.Patch(typeof(CompAbilityEffect_InitiateLovin).GetMethod("Valid", new Type[] { typeof(LocalTargetInfo), typeof(bool) }), transpiler: new HarmonyMethod(typeof(VREPatches).GetMethod(nameof(VREAsexualTranspiler))));
            harmony.Patch(typeof(CompAbilityEffect_InitiateLovin).GetMethod("GetLovers"), prefix: new HarmonyMethod(typeof(VREPatches).GetMethod(nameof(VREGetLoversPrefix))));
            harmony.Patch(typeof(JobDriver_InitiateLovin).GetMethod("GetLovers"), prefix: new HarmonyMethod(typeof(VREPatches).GetMethod(nameof(VREGetLoversPrefix))));
            harmony.Patch(typeof(JobDriver_InitiateLovin).GetMethod("ProcessBreakups"), prefix: new HarmonyMethod(typeof(VREPatches).GetMethod(nameof(VREProcessBreakupsPrefix))), postfix: new HarmonyMethod(typeof(VREPatches).GetMethod(nameof(VREProcessBreakupsPostfix))));
            harmony.Patch(typeof(Need_Lovin).GetMethod("get_ShowOnNeedList"), transpiler: new HarmonyMethod(typeof(VREPatches).GetMethod(nameof(VREMinAgeTranspiler))));
            harmony.Patch(typeof(Need_Lovin).GetMethod("get_IsFrozen"), transpiler: new HarmonyMethod(typeof(VREPatches).GetMethod(nameof(VREMinAgeTranspiler))));
            harmony.Patch(AccessTools.Method(typeof(JobDriver_DoLovinCasual), "GenerateRandomMinTicksToNextLovin"), postfix: new HarmonyMethod(typeof(VanillaRacesExpandedHighmate_JobDriver_Lovin_GenerateRandomMinTicksToNextLovin_Patch), "PawnFucks"));
        }
    }

    public static class VSIEPatches
    {
        //Check for custom love relations
        public static void GetSpouseOrLoverOrFiancePostfix(ref Pawn __result, Pawn pawn)
        {
            if (__result == null)
            {
                __result = pawn.FirstCustomLoveRelation()?.otherPawn;
            }
        }

        public static Type CompilerType;

        public static IEnumerable<CodeInstruction> AddDirectRelation_PrefixTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            //make this use LovePartnerRelationUtility.IsLovePartnerRelation instead of just looking for each def individually
            bool skip = false;
            bool labelFound = false;
            Label? label = new Label();
            foreach (CodeInstruction code in instructions)
            {
                if (!labelFound && code.Branches(out label))
                {
                    labelFound = true;
                }
                if (skip && code.LoadsField(AccessTools.Field(CompilerType, "___pawn")))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    skip = false;
                }

                if (code.LoadsField(AccessTools.Field(typeof(PawnRelationDefOf), nameof(PawnRelationDefOf.Lover))))
                {
                    yield return CodeInstruction.Call(typeof(LovePartnerRelationUtility), nameof(LovePartnerRelationUtility.IsLovePartnerRelation));
                    yield return new CodeInstruction(OpCodes.Brfalse, label);
                    skip = true;
                }
                if (!skip)
                {
                    yield return code;
                }
            }
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
    }
}
