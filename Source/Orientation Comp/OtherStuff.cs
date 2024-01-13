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
    //Then make a patch for TraitDegreeData.GetLabelFor and GetLabelForCap to pass that label
    //Make a method on the comp to determine the correct description
    //Then make a patch for Trait.TipString to pass the correct description
    //Postfix that finds the description from the xml and replaces it
}
