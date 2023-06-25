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
    public static class OtherMod_Patches
    {
        public static bool IsSexualityTraitPrefix(Trait trait, ref bool __result)
        {
            __result = RomanceUtilities.OrientationTraits.Contains(trait.def);
            return false;
        }
    }
}
