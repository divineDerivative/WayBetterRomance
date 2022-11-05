using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace BetterRomance.HarmonyPatches
{
    //This stops pawns who aren't allowed to do hookups from considering hookups for recreation
    [HarmonyPatch(typeof(JoyKindDef), "PawnCanDo")]
    public static class JoyKindDef_PawnCanDo
    {
        public static void PostFix(Pawn pawn, ref bool __result, JoyKindDef __instance)
        {
            if (__result && __instance.defName == "Lewd")
            {
                if (!pawn.HookupAllowed() || BetterRomanceMod.settings.hookupRate == 0f)
                {
                    __result = false;
                    return;
                }
            }
        }
    }
}