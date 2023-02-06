using System.Runtime.CompilerServices;
using HarmonyLib;
using RimWorld;

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
