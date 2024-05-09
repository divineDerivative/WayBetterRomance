using Verse;

namespace BetterRomance
{
    internal static class CompatUtility
    {
        public static bool AndroidCheck(this Pawn pawn)
        {
            if (Settings.ATRActive && (bool)HelperClasses.IsConsideredMechanicalAndroid?.Invoke(null, [pawn]))
            {
                return true;
            }
            return false;
        }

        public static bool DroneCheck(this Pawn pawn)
        {
            if (Settings.ATRActive && (bool)HelperClasses.IsConsideredMechanicalDrone?.Invoke(null, [pawn]))
            {
                return true;
            }
            if (Settings.AsimovActive && pawn.needs.mood == null)
            {
                return true;
            }
#if v1_4
            return !PawnCapacityUtility.BodyCanEverDoCapacity(pawn.RaceProps.body, RimWorld.PawnCapacityDefOf.Talking);
#else
            return pawn.IsMutant || !PawnCapacityUtility.BodyCanEverDoCapacity(pawn.RaceProps.body, RimWorld.PawnCapacityDefOf.Talking);
#endif
        }

        public static bool HasNoGrowth(this Pawn pawn)
        {
            WBR_SettingsComp settings = pawn.TryGetComp<WBR_SettingsComp>();
            //Null check is for animals, which won't have the comp
            return settings?.NoGrowth ?? false;
        }

        /// <summary>
        /// Checks for various robots that should not have growth moments
        /// </summary>
        /// <param name="race"></param>
        /// <returns><see langword="true"/> if race is a robot that should not have growth moments.</returns>
        public static bool RobotGrowthCheck(this ThingDef race)
        {
            if (Settings.ATRActive && (bool)HelperClasses.IsConsideredMechanical?.Invoke(null, [race]))
            {
                return true;
            }
            if (Settings.AsimovActive && race.GetType() == HelperClasses.PawnDef)
            {
                object pawnSettings = HelperClasses.pawnSettings.GetValue(race);
                if (pawnSettings != null)
                {
                    return !(bool)HelperClasses.AsimovGrowth.GetValue(pawnSettings);
                }
            }
            return false;
        }
    }
}