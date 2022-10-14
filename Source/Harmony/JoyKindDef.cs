using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace BetterRomance
{
    //This stops pawns who aren't allowed to do hookups from considering hookups for recreation
    [HarmonyPatch(typeof(JoyKindDef), "PawnCanDo")]
    public static class JoyKindDef_PawnCanDo
    {
        public static void PostFix(Pawn pawn, ref bool __result, JoyKindDef __instance)
        {
            if (__result && __instance.defName == "Lewd")
            {
                if (!pawn.HookupAllowed())
                {
                    __result = false;
                    return;
                }
            }
        }
    }
}