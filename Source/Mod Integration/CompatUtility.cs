using Verse;

namespace BetterRomance
{
    internal static class CompatUtility
    {
        internal static GeneDef unfeeling = DefDatabase<GeneDef>.GetNamedSilentFail("AG_Mood_Unfeeling");
        public static bool AndroidCheck(this Pawn pawn)
        {
            if (Settings.ATRActive && (bool)HelperClasses.IsConsideredMechanicalAndroid?.Invoke(null, [pawn]))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Check if <paramref name="pawn"/> is a drone that cannot participate in romance
        /// </summary>
        /// <param name="pawn"></param>
        /// <param name="includeFH">Whether former humans count as humans</param>
        /// <returns>True if <paramref name="pawn"/> should be considered a drone.</returns>
        public static bool DroneCheck(this Pawn pawn, bool includeFH = false)
        {
            if (Settings.ATRActive && (bool)HelperClasses.IsConsideredMechanicalDrone?.Invoke(null, [pawn]))
            {
                return true;
            }
            if (Settings.AsimovActive && pawn.needs?.mood == null && !pawn.Dead)
            {
                return true;
            }
            //I specifically want this to only catch former humans, and not just pass any humanlike
            if (Settings.PawnmorpherActive && includeFH && pawn.IsFormerHuman())
            {
                return false;
            }
#if v1_4
            return !PawnCapacityUtility.BodyCanEverDoCapacity(pawn.RaceProps.body, RimWorld.PawnCapacityDefOf.Talking);
#else
            return pawn.IsMutant || !PawnCapacityUtility.BodyCanEverDoCapacity(pawn.RaceProps.body, RimWorld.PawnCapacityDefOf.Talking);
#endif
        }

        public static bool CanInitiateRomance(this Pawn pawn)
        {
            return !pawn.DroneCheck(true) && pawn.needs.mood != null;
        }

        public static bool HasNoGrowth(this Pawn pawn)
        {
            WBR_SettingsComp settings = pawn.TryGetComp<WBR_SettingsComp>();
            //Null check is for animals, which won't have the comp
            return settings?.NoGrowth ?? false;
        }

        /// <summary>
        /// Allows former humans from Pawnmorpher to be considered humanlike
        /// </summary>
        /// <param name="pawn"></param>
        /// <returns></returns>
        public static bool IsHumanlike(this Pawn pawn)
        {
            if (pawn.RaceProps.Humanlike)
            {
                return true;
            }
            if (Settings.PawnmorpherActive)
            {
                bool result = (bool)HelperClasses.IsHumanlikePM.Invoke(null, [pawn]);
                return result;
            }
            return false;
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

        public static bool CanBeMale(this Pawn pawn)
        {
            if (Settings.TransActive)
            {
                return (bool)HelperClasses.CanSire.Invoke(null, [pawn]);
            }
            if (Settings.HARActive)
            {
                return pawn.CanFertilize();
            }
            return pawn.gender == Gender.Male;
        }

        public static bool CanBeFemale(this Pawn pawn)
        {
            if (Settings.TransActive)
            {
                return (bool)HelperClasses.CanCarry.Invoke(null, [pawn]);
            }
            if (Settings.HARActive)
            {
                return pawn.CanGestate();
            }
            return pawn.gender == Gender.Female;
        }
    }
}