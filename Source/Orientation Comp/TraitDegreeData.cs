using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using HarmonyLib;

namespace BetterRomance.HarmonyPatches
{
    [HarmonyPatch(typeof(TraitDegreeData), nameof(TraitDegreeData.GetLabelFor), new Type[] {typeof(Pawn)})]
    public static class TraitDegreeData_GetLabelFor
    {
        public static bool Prefix(Pawn pawn, ref string __result, TraitDegreeData __instance)
        {
            if (__instance.label == "dynamic orientation")
            {
                __result = pawn.CheckForComp<Comp_Orientation>().GetLabel().ToLower();
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(TraitDegreeData), nameof(TraitDegreeData.GetLabelCapFor), new Type[] { typeof(Pawn) })]
    public static class TraitDegreeData_GetLabelCapFor
    {
        public static bool Prefix(Pawn pawn, ref string __result, TraitDegreeData __instance)
        {
            if (__instance.label == "dynamic orientation")
            {
                __result = pawn.CheckForComp<Comp_Orientation>().GetLabel();
                return false;
            }
            return true;
        }
    }
}
