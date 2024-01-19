using HarmonyLib;
using RimWorld;
using System.Text;
using Verse;

namespace BetterRomance.HarmonyPatches
{
    //Adds 'Click for more info' to the end of the description of orientation traits
    [HarmonyPatch(typeof(Trait), nameof(Trait.TipString))]
    public static class Trait_TipString
    {
        [HarmonyPostfix]
        public static void AsexualPostfix(Pawn pawn, ref string __result, Trait __instance)
        {
            if (__instance.def == RomanceDefOf.DynamicOrientation && Current.ProgramState == ProgramState.Playing)
            {
                StringBuilder str = new(__result);
                str.AppendLine();
                str.AppendLine();
                str.AppendLine("WBR.MoreInfo".Translate().Colorize(ColoredText.SubtleGrayColor));
                __result = str.ToString();
            }
        }

        [HarmonyPrefix]
        public static bool DescriptionPrefix(Pawn pawn, ref string __result, Trait __instance)
        {
            if (__instance.def == RomanceDefOf.DynamicOrientation)
            {
                //var text = new StringBuilder(__result);
                __result = pawn.CheckForComp<Comp_Orientation>().Desc;
                return false;
            }
            return true;
        }
    }
}
