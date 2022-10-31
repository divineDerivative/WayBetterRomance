using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using AlienRace;
using HarmonyLib;
using RimWorld;
using Verse;

namespace BetterRomance
{
    public static class HARPatches
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void PatchHAR()
        {
            Harmony harmony = new Harmony(id: "rimworld.divineDerivative.HARinterference");
            harmony.Unpatch(typeof(Pawn_RelationsTracker).GetMethod("CompatibilityWith"), typeof(HarmonyPatches).GetMethod("CompatibilityWithPostfix"));
            harmony.Patch(AccessTools.Method(typeof(HarmonyPatches), nameof(HarmonyPatches.LovinInterval)), prefix: new HarmonyMethod(typeof(HARPatches), nameof(HARPatches.LovinInternalPrefix)));
        }

        public static bool LovinInternalPrefix(SimpleCurve humanDefault, Pawn pawn, ref SimpleCurve __result)
        {
            __result = pawn.def is ThingDef_AlienRace alienProps ? alienProps.alienRace.generalSettings.lovinIntervalHoursFromAge ?? pawn.GetLovinCurve() : pawn.GetLovinCurve();
            return false;
        }
    }
}
