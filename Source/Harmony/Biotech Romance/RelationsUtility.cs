using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using HarmonyLib;
using System.Reflection.Emit;
using System.Reflection;

namespace BetterRomance.HarmonyPatches
{
    [HarmonyPatch(typeof(RelationsUtility), "RomanceEligible")]
    public static class RelationsUtility_RomanceEligible
    {
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> MinAgeTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.Is(OpCodes.Ldc_R4, 16f))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.MinAgeForSex));
                }
                else
                {
                    yield return instruction;
                }
            }
        }

        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> AsexualTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            FieldInfo traitDefOf = AccessTools.Field(typeof(TraitDefOf), nameof(TraitDefOf.Asexual));
            bool foundMessageAsexual = false;
            bool foundTraitDefOf = false;
            int startIndex = -1;
            int endIndex = -1;

            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ret)
                {
                    if (foundMessageAsexual && foundTraitDefOf)
                    {
                        endIndex = i;
                        break;
                    }
                    else if (foundTraitDefOf)
                    {
                        int middleIndex = i + 1;
                        for (int k = middleIndex; i < codes.Count; k++)
                        {
                            string strOperand = codes[k].operand as string;
                            if (strOperand == "CantRomanceInitiateMessageAsexual")
                            {
                                foundMessageAsexual = true;
                                break;
                            }
                        }
                    }
                    else
                    {
                        startIndex = i + 1;

                        for (int j = startIndex; i < codes.Count; j++)
                        {
                            if (codes[j].opcode == OpCodes.Ret)
                            {
                                break;
                            }
                            if (codes[j].LoadsField(traitDefOf))
                            {
                                foundTraitDefOf = true;
                                break;
                            }

                        }
                    }
                }
            }
            if (startIndex > -1 && endIndex > -1)
            {
                codes[startIndex].opcode = OpCodes.Nop;
                codes.RemoveRange(startIndex + 1, endIndex - startIndex);
            }
            return codes.AsEnumerable();
        }
    }

    [HarmonyPatch(typeof(RelationsUtility), "RomanceEligiblePair")]
    public static class RelationsUtility_RomanceEligiblePair
    {
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> MinAgeTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.Is(OpCodes.Ldc_R4, 16f))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.MinAgeForSex));
                }
                else
                {
                    yield return instruction;
                }
            }
        }

        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> OrientationTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            int startIndex = -1;
            int endIndex = -1;
            bool foundStart = false;
            bool foundEnd = false;

            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);

            for (int i = 0; i < codes.Count; i++)
            {
                if (!foundStart && codes[i].opcode == OpCodes.Ldarg_1)
                {
                    if (codes[i + 1].opcode == OpCodes.Ldarg_0)
                    {
                        startIndex = i - 1;
                        foundStart = true;
                    }
                }
                else if (foundStart && !foundEnd && codes[i].opcode == OpCodes.Brtrue_S)
                {
                    endIndex = i - 1;
                    foundEnd = true;
                }

                if (codes[i].opcode == OpCodes.Ldstr && (string)codes[i].operand == "CantRomanceTargetSexuality")
                {
                    codes[i].operand = "WBR.CantHookupTargetGender";
                }
            }
            if (startIndex > -1 && endIndex > -1)
            {
                codes.RemoveRange(startIndex, endIndex - startIndex + 1);
            }
            return codes.AsEnumerable();
        }

        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> OpinionTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            bool foundStart = false;

            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);

            for (int i = 0; i < codes.Count; i++)
            {
                if (!foundStart && codes[i].opcode == OpCodes.Ldarg_1)
                {
                    if (codes[i + 1].opcode == OpCodes.Ldfld && codes[i + 2].opcode == OpCodes.Ldarg_0)
                    {
                        foundStart = true;
                        yield return new CodeInstruction(OpCodes.Ldarg_0) { labels = codes[i].ExtractLabels() };
                        yield return codes[i + 1];
                        yield return new CodeInstruction(OpCodes.Ldarg_1);
                        yield return codes[i + 3];
                        yield return new CodeInstruction(OpCodes.Ldarg_0);
                        yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.MinOpinionForRomance));
                        yield return new CodeInstruction(OpCodes.Conv_I4);
                        i += 4;
                    }
                    else
                    {
                        yield return codes[i];
                    }
                }
                else
                {
                    yield return codes[i];
                }
            }
        }
    }

    [HarmonyPatch(typeof(RelationsUtility), "AttractedToGender")]
    public static class RelationsUtility_AttractedToGender
    {
        public static bool Prefix(Pawn pawn, Gender gender, ref bool __result)
        {
            pawn.EnsureTraits();
            if (pawn.story != null)
            {
                if (pawn.GetOrientation() == Orientation.None)
                {
                    __result = false;
                }
                else if (pawn.GetOrientation() == Orientation.Homo)
                {
                    __result = pawn.gender == gender;
                }
                else if (pawn.GetOrientation() == Orientation.Hetero)
                {
                    __result = pawn.gender != gender;
                }
                else if (pawn.GetOrientation() == Orientation.Bi)
                {
                    __result = true;
                }
                else
                {
                    //If they don't have an orientation trait, they are probably a child
                    __result = false;
                }
                return false;
            }
            //Not really sure what it means to not have a story tracker
            __result = false;
            return false;
        }
    }
}
