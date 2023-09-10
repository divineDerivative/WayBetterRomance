using Verse;

namespace BetterRomance
{
    internal static class CompatUtility
    {
        public static bool AndroidCheck(this Pawn pawn)
        {
            if (Settings.ATRActive && (bool)HelperClasses.IsConsideredMechanicalAndroid?.Invoke(null, new object[] { pawn }))
            {
                return true;
            }
            return false;
        }

        public static bool DroneCheck(this Pawn pawn)
        {
            if (Settings.ATRActive && (bool)HelperClasses.IsConsideredMechanicalDrone?.Invoke(null, new object[] { pawn }))
            {
                return true;
            }
            if (Settings.AsimovActive && pawn.needs.mood == null)
            {
                return true;
            }
            return !PawnCapacityUtility.BodyCanEverDoCapacity(pawn.RaceProps.body, RimWorld.PawnCapacityDefOf.Talking);
        }

        public static bool HasNoGrowth(this Pawn pawn)
        {
            if (Settings.ATRActive && (bool)HelperClasses.IsConsideredMechanical?.Invoke(null, new object[] { pawn }))
            {
                return true;
            }
            if (Settings.AsimovActive && (bool)HelperClasses.IsHumanlikeAutomaton?.Invoke(null, new object[] { pawn }))
            {
                object pawnSettings = HelperClasses.pawnSettings.GetValue(pawn.def);
                if (pawnSettings != null)
                {
                    return !(bool)HelperClasses.AsimovGrowth.GetValue(pawnSettings);
                }
            }
            WBR_SettingsComp settings = pawn.TryGetComp<WBR_SettingsComp>();
            //For animals
            if (settings != null)
            {
                return settings.NoGrowth;
            }
            return false;
        }
    }
}