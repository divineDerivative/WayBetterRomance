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
    [HarmonyPatch(typeof(Trait), nameof(Trait.TipString))]
    public static class Trait_TipString
    {
        public static void Postfix(Pawn pawn, ref string __result, Trait __instance)
        {
            if (SexualityUtility.asexualTraits.Contains(__instance.def))
            {
                var asdfd = new StringBuilder(__result);
                asdfd.AppendLine();
                asdfd.AppendLine();
                asdfd.AppendLine("WBR.MoreInfo".Translate().Colorize(ColoredText.SubtleGrayColor));
                __result = asdfd.ToString();
            }
        }
    }
}
