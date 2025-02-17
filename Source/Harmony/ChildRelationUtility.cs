using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection.Emit;
using Verse;

namespace BetterRomance.HarmonyPatches
{

    //This determines the chance for a given pawn to become the child of two other pawns
    [HarmonyPatch(typeof(ChildRelationUtility), nameof(ChildRelationUtility.ChanceOfBecomingChildOf))]
    public static class ChildRelationUtility_ChanceOfBecomingChildOf
    {
        //Check if settings allow children
        public static bool Prefix(Pawn father, Pawn mother, ref float __result)
        {
            if ((father != null && (!father.IsHumanlike() || !father.ChildAllowed())) || (mother != null && (!mother.IsHumanlike() || !mother.ChildAllowed())))
            {
                __result = 0f;
                return false;
            }
            return true;
        }

        //Remove gay bias and use age settings
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction code in instructions.AgeToHaveChildren(OpCodes.Ldarg_1, OpCodes.Ldarg_2, false))
            {
                if (code.opcode == OpCodes.Ldloc_3)
                {
                    //This just replaces num4 with 1f at the end, negating the gay reduction
                    yield return new CodeInstruction(OpCodes.Ldc_R4, 1f);
                }
                else
                {
                    yield return code;
                }
            }
        }
    }

    //Use settings
    [HarmonyPatch(typeof(ChildRelationUtility), "NumberOfChildrenFemaleWantsEver")]
    public static class ChildRelationUtility_NumberOfChildrenFemaleWantsEver
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            bool first = true;
            foreach (CodeInstruction code in instructions)
            {
                if (code.LoadsConstant(3))
                {
                    if (first)
                    {
                        yield return code;
                        first = false;
                    }
                    else
                    {
                        yield return new CodeInstruction(OpCodes.Ldarg_0);
                        yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.MaxChildren));
                    }
                }
                else
                {
                    yield return code;
                }
            }
        }
    }
}