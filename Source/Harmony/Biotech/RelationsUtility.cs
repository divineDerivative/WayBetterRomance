using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Verse;

namespace BetterRomance.HarmonyPatches
{
    [HarmonyPatch(typeof(RelationsUtility), nameof(RelationsUtility.RomanceEligible))]
    public static class RelationsUtility_RomanceEligible
    {
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> MinAgeTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            return instructions.MinAgeForSexTranspiler(OpCodes.Ldarg_0);
        }

        //Replaces the message when clicking on the disabled romance button for an aromantic pawn
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> AsexualTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction code in instructions)
            {
                if (code.LoadsConstant("CantRomanceInitiateMessageAsexual"))
                {
                    code.operand = "WBR.CantRomanceInitiateMessageAromantic";
                }
                yield return code;
            }
        }
    }

    [HarmonyPatch(typeof(RelationsUtility), nameof(RelationsUtility.RomanceEligiblePair))]
    public static class RelationsUtility_RomanceEligiblePair
    {
        [HarmonyPrefix]
        public static bool RobotPrefix(Pawn target, ref AcceptanceReport __result)
        {
            if (target.DroneCheck())
            {
                __result = false;
                return false;
            }
            return true;
        }

        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> MinAgeTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            return instructions.MinAgeForSexTranspiler(OpCodes.Ldarg_1);
        }

        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> OrientationTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            int startIndex = -1;
            int endIndex = -1;
            bool foundStart = false;
            bool foundEnd = false;

            List<CodeInstruction> codes = new(instructions);

            for (int i = 0; i < codes.Count; i++)
            {
                //Find !AttractedToGender(target, initiator.gender) section
                CodeInstruction code = codes[i];
                if (!foundStart && code.opcode == OpCodes.Ldarg_1)
                {
                    if (codes[i + 1].opcode == OpCodes.Ldarg_0)
                    {
                        startIndex = i - 1;
                        foundStart = true;
                    }
                }
                else if (foundStart && !foundEnd && code.opcode == OpCodes.Brtrue_S)
                {
                    endIndex = i - 1;
                    foundEnd = true;
                }
                //Replace the message to reference gender instead of orientation
                if (code.opcode == OpCodes.Ldstr && (string)code.operand == "CantRomanceTargetSexuality")
                {
                    code.operand = "WBR.CantHookupTargetGender";
                }
            }
            //Remove the check against the target's orientation
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

            List<CodeInstruction> codes = new(instructions);

            for (int i = 0; i < codes.Count; i++)
            {
                CodeInstruction code = codes[i];
                if (!foundStart && code.opcode == OpCodes.Ldarg_1)
                {
                    if (codes[i + 1].opcode == OpCodes.Ldfld && codes[i + 2].opcode == OpCodes.Ldarg_0)
                    {
                        foundStart = true;
                        yield return new CodeInstruction(OpCodes.Ldarg_0) { labels = code.ExtractLabels() };
                        yield return codes[i + 1];
                        yield return new CodeInstruction(OpCodes.Ldarg_1);
                        yield return codes[i + 3];
                        yield return new CodeInstruction(OpCodes.Ldarg_0);
                        yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.MinOpinionForRomance));
                        i += 4;
                    }
                    else
                    {
                        yield return code;
                    }
                }
                else
                {
                    yield return code;
                }
            }
        }
    }

    [HarmonyPatch(typeof(RelationsUtility), nameof(RelationsUtility.AttractedToGender))]
    public static class RelationsUtility_AttractedToGender
    {
        public static bool Prefix(Pawn pawn, Gender gender, ref bool __result)
        {
            pawn.EnsureTraits();
            if (pawn.story != null)
            {
                if (pawn.IsAro())
                {
                    __result = false;
                }
                else if (pawn.IsHomo())
                {
                    __result = pawn.gender == gender;
                }
                else if (pawn.IsHetero())
                {
                    __result = pawn.gender != gender;
                }
                else if (pawn.IsBi())
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
