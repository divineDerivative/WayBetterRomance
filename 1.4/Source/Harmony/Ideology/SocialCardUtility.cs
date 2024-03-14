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
        public static bool DrawIdeoExposurePrefix(Pawn baby, float rectWidth, float heightOffset)
        {
            IdeoligionChooseAgePawn = baby;
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch("DrawIdeoExposureItem")]
        public static bool DrawIdeoExposureItemPrefix(Pawn baby, Ideo ideo, Rect rect, float ideoExposure, float totalExposure, int rowIndex, float yOff)
        {
            IdeoligionChooseAgePawn = baby;
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch("IdeoligionChooseAge", MethodType.Getter)]
        public static bool IdeoligionChooseAgePrefix(ref float __result)
        {
            __result = IdeoligionChooseAgePawn.RaceProps.lifeStageAges.First((LifeStageAge lsa) => lsa.def.developmentalStage.Child()).minAge;
            return false;
        }
    }
}
