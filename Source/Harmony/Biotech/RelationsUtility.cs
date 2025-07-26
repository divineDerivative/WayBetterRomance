using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Verse;

namespace BetterRomance.HarmonyPatches
{
    //Replaces the message when clicking on the disabled romance button for an aromantic pawn
    [HarmonyPatch(typeof(RelationsUtility), nameof(RelationsUtility.RomanceEligible))]
    public static class RelationsUtility_RomanceEligible
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction code in instructions.MinAgeForSexTranspiler(OpCodes.Ldarg_0))
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

        //Removes the check for target being attracted to initiator's gender
        //Replaces the reject message if orientation doesn't match
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> OrientationTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = new(instructions);
            int attractedToFound = 0;
            foreach (CodeInstruction code in codes)
            {
                //Find !AttractedToGender(target, initiator.gender) section
                if (code.Calls(AccessTools.Method(typeof(RelationsUtility), nameof(RelationsUtility.AttractedToGender))))
                {
                    attractedToFound++;
                }
                if (attractedToFound == 2 && code.Branches(out _))
                {
                    //Remove the bool from the stack
                    yield return new CodeInstruction(OpCodes.Pop);
                    //Change to jump unconditionally
                    code.opcode = OpCodes.Br;
                    attractedToFound++;
                }
                //Replace the message to reference gender instead of orientation
                if (code.opcode == OpCodes.Ldstr && (string)code.operand == "CantRomanceTargetSexuality")
                {
                    code.operand = "WBR.CantHookupTargetGender";
                }
                yield return code;
            }
        }

        //This changes (target.relations.OpinionOf(initiator) <= 5) to (initiator.relations.OpinionOf(target) <= initiator.MinOpinionForRomance())
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> OpinionTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = instructions.MinOpinionRomanceTranspiler(OpCodes.Ldarg_0).MinAgeForSexTranspiler(OpCodes.Ldarg_1).ToList();

            bool swap = false;
            for (int i = 0; i < codes.Count; i++)
            {
                CodeInstruction code = codes[i];
                if (!swap && code.opcode == OpCodes.Ldarg_1 && codes[i + 1].LoadsField(typeof(Pawn), nameof(Pawn.relations)))
                {
                    swap = true;
                }
                if (swap && code.opcode == OpCodes.Ldarg_1)
                {
                    code.opcode = OpCodes.Ldarg_0;
                }
                else if (swap && code.opcode == OpCodes.Ldarg_0)
                {
                    code.opcode = OpCodes.Ldarg_1;
                    swap = false;
                }
                yield return code;
            }
        }
    }

    [HarmonyPatch(typeof(RelationsUtility), nameof(RelationsUtility.AttractedToGender))]
    [HarmonyBefore("PersonalityPlus.Mod")]
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
