using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using HarmonyLib;

namespace BetterRomance
{
    //The sexual/romantic variables in Settings will be changed to this type
    //Might also change to this in the orientation comp
    public class OrientationHolder : IExposable
    {
        public OrientationChances standardOrientation = new();
        public OrientationChances orientationForMen = new();
        public OrientationChances orientationForWomen = new();
        public OrientationChances orientationForEnby = new();

        public GenderAttractionChances attractionForMen = new();
        public GenderAttractionChances attractionForWomen = new();
        public GenderAttractionChances attractionForEnby = new();

        public bool romance;

        public void ExposeData()
        {
            Scribe_Deep.Look(ref standardOrientation, "standardOrientation");
            //I'm not sure if I need to save these or not
            Scribe_Deep.Look(ref orientationForMen, "orientationForMen");
            Scribe_Deep.Look(ref orientationForWomen, "orientationForWomen");
            Scribe_Deep.Look(ref orientationForEnby, "orientationForEnby");

            Scribe_Deep.Look(ref attractionForMen, "attractionForMen");
            Scribe_Deep.Look(ref attractionForWomen, "attractionForWomen");
            Scribe_Deep.Look(ref attractionForEnby, "attractionForEnby");
        }

        //Method to update orientation chances based on the current value of the gender chances

        //Method to grab a valid object based on a pawn
        //Maybe that would go in the orientation utility?
    }
}
