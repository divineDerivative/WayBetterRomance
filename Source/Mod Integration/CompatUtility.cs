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
            return false;
        }

        public static bool HasNoGrowth(this Pawn pawn)
        {
            if (Settings.ATRActive && (bool)HelperClasses.IsConsideredMechanical?.Invoke(null, new object[] { pawn }))
            {
                return true;
            }
            if (Settings.SSSActive && pawn.def.defName == "SSS_Skeleton_Race")
            {
                return true;
            }
            return false;
        }
    }
}