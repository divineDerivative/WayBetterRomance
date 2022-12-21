using AlienRace;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BetterRomance
{
    public class HAR_Integration
    {

        /// <summary>
        /// Checks HAR settings to see if pawns consider each other aliens.
        /// DO NOT CALL IF HAR IS NOT ACTIVE
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns>True or False</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool AreRacesConsideredXeno(Pawn p1, Pawn p2)
        {
            if (p1.def != p2.def)
            {
                return !(p1.def is ThingDef_AlienRace alienDef && alienDef.alienRace.generalSettings.notXenophobistTowards.Contains(p2.def));
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int[] GetGrowthMoments(Pawn pawn)
        {
            return (pawn.def as ThingDef_AlienRace)?.alienRace.generalSettings.growthAges?.ToArray();
        }

        public static bool FertilityCurveExists(Pawn pawn)
        {
            if (!(pawn.def is ThingDef_AlienRace alienRace))
            {
                return false;
            }
            if (pawn.gender != Gender.Female)
            {
                return alienRace.alienRace.generalSettings.reproduction.maleFertilityAgeFactor != null;
            }
            return alienRace.alienRace.generalSettings.reproduction.femaleFertilityAgeFactor != null;
        }
    }
}
