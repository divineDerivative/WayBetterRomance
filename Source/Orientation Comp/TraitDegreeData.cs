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
    //Will need to figure out how to make this translatable, since I'm replacing the label entirely
    [HarmonyPatch(typeof(TraitDegreeData), nameof(TraitDegreeData.GetLabelFor), [typeof(Pawn)])]
    public static class TraitDegreeData_GetLabelFor
    {
        public static bool Prefix(Pawn pawn, ref string __result, TraitDegreeData __instance)
        {
            if (__instance.untranslatedLabel == "dynamic orientation")
            {
                __result = pawn.CheckForComp<Comp_Orientation>().Label.ToLower();
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(TraitDegreeData), nameof(TraitDegreeData.GetLabelCapFor), [typeof(Pawn)])]
    public static class TraitDegreeData_GetLabelCapFor
    {
        public static bool Prefix(Pawn pawn, ref string __result, TraitDegreeData __instance)
        {
            if (__instance.untranslatedLabel == "dynamic orientation")
            {
                __result = pawn.CheckForComp<Comp_Orientation>().Label;
                return false;
            }
            return true;
        }
    }
}
