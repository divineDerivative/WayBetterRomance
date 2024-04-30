using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using HarmonyLib;
using UnityEngine;

namespace BetterRomance.HarmonyPatches
{
    //Not sure if this is needed, it only seems to be used in a section of the social card that is never actually drawn
    [HarmonyPatch(typeof(SocialCardUtility))]
    public static class SocialCardUtility_IdeoligionChooseAge
    {
        private static Pawn IdeoligionChooseAgePawn;

        [HarmonyPrefix]
        [HarmonyPatch("DrawIdeoExposure")]
        public static void DrawIdeoExposurePrefix(Pawn baby)
        {
            IdeoligionChooseAgePawn = baby;
        }

        [HarmonyPrefix]
        [HarmonyPatch("DrawIdeoExposureItem")]
        public static void DrawIdeoExposureItemPrefix(Pawn baby)
        {
            IdeoligionChooseAgePawn = baby;
        }

        [HarmonyPrefix]
        [HarmonyPatch("IdeoligionChooseAge", MethodType.Getter)]
        public static void IdeoligionChooseAgePrefix(ref float __result)
        {
            __result = SettingsUtilities.ChildAge(IdeoligionChooseAgePawn);
        }
    }
}
