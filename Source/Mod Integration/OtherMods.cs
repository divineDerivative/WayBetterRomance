using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using VanillaRacesExpandedHighmate;
using Verse;
using VFECore;

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
                if (code.Calls(AccessTools.Method(typeof(TraitSet), nameof(TraitSet.HasTrait), parameters: [typeof(TraitDef)])))
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
                if (code.Calls(AccessTools.Method(typeof(TraitSet), nameof(TraitSet.HasTrait), [typeof(TraitDef)])))
                {
                    yield return CodeInstruction.Call(typeof(OtherMod_Patches), nameof(TraitConversion));
                }
                else
                {
                    yield return code;
                }
            }
        }

        private static List<TraitDef> traits = [TraitDefOf.Gay, TraitDefOf.Bisexual, TraitDefOf.Asexual, RomanceDefOf.Straight];
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

        /// <summary>
        /// Patches a method to use LovePartnerRelationUtility.Is(Ex)LovePartnerRelation instead of just looking for each def individually
        /// </summary>
        /// <param name="instructions"></param>
        /// <param name="ilg"></param>
        /// <param name="def">The first PawnRelationDef the original looks for</param>
        /// <param name="condition">Whether we want to use brtrue or brfalse</param>
        /// <param name="ex">Whether we want the ex version or not</param>
        /// <param name="stopSkipping">A validator for the instruction where we want to stop skipping. This is also where the label is added if <paramref name="condition"/> is true</param>
        /// <param name="addLabel">A validator for the instruction where we want to add our label. Only used if <paramref name="condition"/> is false</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static IEnumerable<CodeInstruction> LoveRelationUtilityTranspilerMaker(IEnumerable<CodeInstruction> instructions, ILGenerator ilg, PawnRelationDef def, bool condition, bool ex, Func<CodeInstruction, bool> stopSkipping, Func<CodeInstruction, bool> addLabel = null)
        {
            bool skip = false;
            Label label = ilg.DefineLabel();
            foreach (CodeInstruction code in instructions)
            {
                //Pretty sure this is right, if we're using true the label should go at the same place we stop skipping
                if (condition)
                {
                    if (skip && stopSkipping(code))
                    {
                        code.labels.Add(label);
                        skip = false;
                    }
                }
                else if (addLabel is null)
                {
                    throw new ArgumentException($"Error in LoveRelationUtilityTranspilerMaker; addLabel validator must be provided if condition is false");
                }
                else
                {
                    //If it's false then that means the jump to point is different from the stop skipping point
                    //So for the VSIE patch
                    if (skip && stopSkipping(code))
                    {
                        //So this is required for the VSIE patch, but I'm not sure how to account for it here
                        //Just gonna leave it for now and figure something out if I ever add another patch that needs this
                        //I bet my idea of a dictionary with MethodInfo and arguments could help, since I could check which key I'm using
                        yield return new CodeInstruction(OpCodes.Ldarg_0);
                        skip = false;
                    }
                    if (addLabel(code))
                    {
                        code.labels.Add(label);
                    }
                }

                if (code.LoadsField(AccessTools.Field(typeof(PawnRelationDefOf), def.defName)))
                {
                    yield return CodeInstruction.Call(typeof(LovePartnerRelationUtility), ex ? nameof(LovePartnerRelationUtility.IsExLovePartnerRelation) : nameof(LovePartnerRelationUtility.IsLovePartnerRelation));
                    yield return new CodeInstruction(condition ? OpCodes.Brtrue : OpCodes.Brfalse, label);
                    skip = true;
                }
                if (!skip)
                {
                    yield return code;
                }
            }
        }

        public static IEnumerable<CodeInstruction> RimderLoveRelationTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
        {
            return LoveRelationUtilityTranspilerMaker(instructions, ilg, PawnRelationDefOf.Lover, true, false, stopSkipping: (CodeInstruction code) => code.LoadsField(AccessTools.Field(AccessTools.TypeByName("RimderModCore"), "rimderSettings")));
        }

        public static IEnumerable<CodeInstruction> RimderExLoveRelationTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
        {
            return LoveRelationUtilityTranspilerMaker(instructions, ilg, PawnRelationDefOf.ExLover, true, true, stopSkipping: (CodeInstruction code) => code.LoadsField(AccessTools.Field(AccessTools.TypeByName("RimderModCore"), "rimderSettings")));
        }
    }

    public static class VREPatches
    {
        public static IEnumerable<CodeInstruction> InitiateLovinMinAgeTranspiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            return DynamicTranspilers.MinAgeForSexTranspiler(instructions, OpCodes.Ldloc_0);
        }

        public static IEnumerable<CodeInstruction> Need_LovinMinAgeTranspiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            List<CodeInstruction> codes = new()
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                CodeInstruction.LoadField(typeof(Need_Lovin), "pawn")
            };
            return DynamicTranspilers.MinAgeForSexTranspiler(instructions, codes);
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
            harmony.Patch(typeof(CompAbilityEffect_InitiateLovin).GetMethod("Valid", [typeof(LocalTargetInfo), typeof(bool)]), transpiler: new(typeof(VREPatches).GetMethod(nameof(InitiateLovinMinAgeTranspiler))));
            harmony.Patch(typeof(CompAbilityEffect_InitiateLovin).GetMethod("Valid", [typeof(LocalTargetInfo), typeof(bool)]), transpiler: new(typeof(VREPatches).GetMethod(nameof(VREAsexualTranspiler))));
            harmony.Patch(typeof(CompAbilityEffect_InitiateLovin).GetMethod("GetLovers"), prefix: new(typeof(VREPatches).GetMethod(nameof(VREGetLoversPrefix))));
            harmony.Patch(typeof(JobDriver_InitiateLovin).GetMethod("GetLovers"), prefix: new(typeof(VREPatches).GetMethod(nameof(VREGetLoversPrefix))));
            harmony.Patch(typeof(JobDriver_InitiateLovin).GetMethod("ProcessBreakups"), prefix: new(typeof(VREPatches).GetMethod(nameof(VREProcessBreakupsPrefix))), postfix: new(typeof(VREPatches).GetMethod(nameof(VREProcessBreakupsPostfix))));
            harmony.Patch(typeof(Need_Lovin).GetMethod("get_ShowOnNeedList"), transpiler: new(typeof(VREPatches).GetMethod(nameof(Need_LovinMinAgeTranspiler))));
            harmony.Patch(typeof(Need_Lovin).GetMethod("get_IsFrozen"), transpiler: new(typeof(VREPatches).GetMethod(nameof(Need_LovinMinAgeTranspiler))));
            harmony.Patch(AccessTools.Method(typeof(JobDriver_DoLovinCasual), "GenerateRandomMinTicksToNextLovin"), postfix: new(typeof(VanillaRacesExpandedHighmate_JobDriver_Lovin_GenerateRandomMinTicksToNextLovin_Patch), "PawnFucks"));
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
        [HarmonyDebug]
        public static IEnumerable<CodeInstruction> AddDirectRelation_PrefixTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
        {
            return OtherMod_Patches.LoveRelationUtilityTranspilerMaker(instructions, ilg, PawnRelationDefOf.Lover, false, false, (CodeInstruction code) => code.LoadsField(AccessTools.Field(CompilerType, "___pawn")), (CodeInstruction code) => code.opcode == OpCodes.Ret);
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
