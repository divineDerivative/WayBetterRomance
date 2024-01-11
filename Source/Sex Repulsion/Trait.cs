using System.Text;
using RimWorld;
using Verse;
using HarmonyLib;

namespace BetterRomance.HarmonyPatches
{
    //Adds 'Click for more info' to the end of the description of asexual traits
    [HarmonyPatch(typeof(Trait), nameof(Trait.TipString))]
    public static class Trait_TipString
    {
        public static void Postfix(Pawn pawn, ref string __result, Trait __instance)
        {
            if (SexualityUtility.asexualTraits.Contains(__instance.def))
            {
                StringBuilder str = new StringBuilder(__result);
                str.AppendLine();
                str.AppendLine();
                str.AppendLine("WBR.MoreInfo".Translate().Colorize(ColoredText.SubtleGrayColor));
                __result = str.ToString();
            }
        }
    }
}
