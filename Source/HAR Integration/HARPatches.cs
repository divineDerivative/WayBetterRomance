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
            harmony.Unpatch(typeof(Pawn_RelationsTracker).GetMethod("CompatibilityWith"), typeof(AlienRace.HarmonyPatches).GetMethod("CompatibilityWithPostfix"));
        }
    }
}
