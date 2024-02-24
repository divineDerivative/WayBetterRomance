using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Verse;

namespace BetterRomance.HarmonyPatches
{
    public static class PhobiaPatches
    {
        //Count qualifying custom love relations and add to the total
        public static void CountSameSexCouplesPostfix(Map map, ref int __result)
        {
            if (Settings.LoveRelationsLoaded)
            {
                int myCount = 0;
                List<Pawn> colonists = map.mapPawns.FreeColonists;

                foreach (Pawn pawn in colonists)
                {
                    if (pawn.Dead)
                    {
                        continue;
                    }

                    foreach (DirectPawnRelation relation in pawn.relations.DirectRelations)
                    {
                        if (CustomLoveRelationUtility.LoveRelations.Contains(relation.def) && pawn.gender == relation.otherPawn.gender)
                        {
                            myCount++;
                        }
                    }
                }
                __result += myCount / 2;
            }
        }

        //Check custom love relations
        public static void IsInSameSexRelationshipPostfix(Pawn pawn, ref bool __result)
        {
            //Don't do anything if it's already true or there's no custom relations
            if (!__result && Settings.LoveRelationsLoaded)
            {
                __result = pawn.relations.DirectRelations.Any(rel => CustomLoveRelationUtility.LoveRelations.Contains(rel.def) && pawn.gender == rel.otherPawn.gender);
            }
        }

        //Check custom love relations
        public static void IsInSameSexRelationshipWithPostfix(Pawn pawn, Pawn otherPawn, ref bool __result)
        {
            //Don't do anything if it's already true or there's no custom relations
            if (!__result && Settings.LoveRelationsLoaded)
            {
                __result = (pawn.gender == otherPawn.gender) && CustomLoveRelationUtility.CheckCustomLoveRelations(pawn, otherPawn) != null;
            }
        }

        //Check for orientation instead of traits
        //This will also apply to ordered hookups
        public static void AttractedToGenderPostfix(Pawn pawn, Gender gender, ref bool __result)
        {
            //If it's already true we don't need to do anything
            if (!__result)
            {
                bool adoresSameSex = pawn.Ideo?.HasPrecept(DefDatabase<PreceptDef>.GetNamed("SameSexCouples_Adored", false)) ?? false;
                bool despisesSameSex = pawn.Ideo?.HasPrecept(DefDatabase<PreceptDef>.GetNamed("SameSexCouples_Despised", false)) ?? false;
                //If they don't have either precept we also don't need to do anything
                if (adoresSameSex || despisesSameSex)
                {
                    //Make them pretend to like the acceptable gender
                    if (adoresSameSex && pawn.IsHetero())
                    {
                        __result = pawn.gender == gender;
                        return;
                    }
                    if (despisesSameSex && pawn.IsHomo())
                    {
                        __result = pawn.gender != gender;
                        return;
                    }
                    //Aromantic will fall through to here and not get changed
                }
            }
        }

        public static IEnumerable<CodeInstruction> SuccessChanceTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            bool firstFound = false;
            bool nextBranch = false;
            foreach (CodeInstruction code in instructions)
            {
                //Need to change !initiator.story.traits.HasTrait(TraitDefOf.Gay) to initiator.IsHetero(), which will be trickier than just replacing the HasTrait call
                if (!firstFound)
                {
                    if (code.LoadsField(AccessTools.Field(typeof(TraitDefOf), nameof(TraitDefOf.Gay))))
                    {
                        yield return CodeInstruction.LoadField(typeof(RomanceDefOf), nameof(RomanceDefOf.Straight));
                    }
                    else if (code.Calls(AccessTools.Method(typeof(TraitSet), nameof(TraitSet.HasTrait), new Type[] { typeof(TraitDef) })))
                    {
                        yield return CodeInstruction.Call(typeof(OtherMod_Patches), nameof(OtherMod_Patches.HelperThingy));
                        nextBranch = true;
                        firstFound = true;
                    }
                    else
                    {
                        yield return code;
                    }
                }
                else if (firstFound && nextBranch && code.Branches(out Label? label))
                {
                    yield return new CodeInstruction(OpCodes.Brfalse, label);
                    nextBranch = false;
                }
                else if (firstFound && code.Calls(AccessTools.Method(typeof(TraitSet), nameof(TraitSet.HasTrait), new Type[] { typeof(TraitDef) })))
                {
                    yield return CodeInstruction.Call(typeof(OtherMod_Patches), nameof(OtherMod_Patches.HelperThingy));
                }
                else
                {
                    yield return code;
                }
            }
        }

        public static void PatchForceLoveHate(this Harmony harmony)
        {
            List<string> typeNames = new List<string>
            {
                "Thought_HomophobicVsGay",
                "ThoughtWorker_HomophobicVsGay",
                "Thought_HeterophobicVsStraight",
                "ThoughtWorker_HeterophobicVsStraight",
                "ThoughtWorker_PositiveViewOnSameSexCouples",
                "ThoughtWorker_NegativeViewOnSameSexCouples",
            };
            HarmonyMethod TraitToOrientationTranspiler = new HarmonyMethod(typeof(OtherMod_Patches), nameof(OtherMod_Patches.TraitToOrientationTranspiler));
            HarmonyMethod IsInSameSexRelationshipPostfix = new HarmonyMethod(typeof(PhobiaPatches).GetMethod(nameof(PhobiaPatches.IsInSameSexRelationshipPostfix)));
            HarmonyMethod IsInSameSexRelationshipWithPostfix = new HarmonyMethod(typeof(PhobiaPatches).GetMethod(nameof(PhobiaPatches.IsInSameSexRelationshipWithPostfix)));
            foreach (string name in typeNames)
            {
                Type type = AccessTools.TypeByName(name);
                if (type != null)
                {
                    foreach (MethodInfo method in type.GetMethods(AccessTools.all))
                    {
                        if (method.Name == "OpinionOffset" || method.Name == "CurrentSocialStateInternal")
                        {
                            harmony.Patch(method, transpiler: TraitToOrientationTranspiler);
                        }
                        if (method.Name == "IsInSameSexRelationship")
                        {
                            harmony.Patch(method, postfix: IsInSameSexRelationshipPostfix);
                        }
                        if (method.Name == "IsInSameSexRelationshipWith")
                        {
                            harmony.Patch(method, postfix: IsInSameSexRelationshipWithPostfix);
                        }
                    }
                }
            }

            harmony.Patch(AccessTools.TypeByName("ThoughtWorker_PreceptSameSexCouples").GetMethod("CountSameSexCouples"), postfix: new HarmonyMethod(typeof(PhobiaPatches).GetMethod(nameof(CountSameSexCouplesPostfix))));
            //Replacing the original postfix, since it's unnecessarily destructive
            harmony.Unpatch(typeof(RelationsUtility).GetMethod(nameof(RelationsUtility.AttractedToGender)), Type.GetType("ForceLoveHate.Patch_AttractedToGender,ForceLoveHate").GetMethod("Postfix"));
            harmony.Patch(typeof(RelationsUtility).GetMethod(nameof(RelationsUtility.AttractedToGender)), postfix: new HarmonyMethod(typeof(PhobiaPatches), nameof(AttractedToGenderPostfix)));
            harmony.Patch(Type.GetType("ForceLoveHate.Patch_InteractionWorker_RomanceAttempt_SuccessChance,ForceLoveHate").GetMethod("Postfix"), transpiler: new HarmonyMethod(typeof(PhobiaPatches), nameof(SuccessChanceTranspiler)));
        }
    }
}