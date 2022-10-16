using System;
using System.Reflection;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using UnityEngine;
using AlienRace;

namespace BetterRomance
{
    //This determines how likely a pawn is to want to have sex with another pawn
    [HarmonyPatch(typeof(Pawn_RelationsTracker), "SecondaryLovinChanceFactor")]
    public static class Pawn_RelationsTracker_SecondaryLovinChanceFactor
    {
        //Changes from Vanilla:
        //Allows for non-ideal orientation match ups, at a low rate
        //Allows cross species attraction
        //Consideration of target's capabilities
        public static bool Prefix(Pawn otherPawn, ref float __result, ref Pawn_RelationsTracker __instance, Pawn ___pawn)
        {
            if (___pawn == otherPawn)
            {
                __result = 0f;
                return false;
            }
            //If at least one of the pawns is not humanlike, don't allow if they're not the same "race"
            //I think this is to weed out animals/mechs while still allowing for different alien races
            if ((!___pawn.RaceProps.Humanlike || !otherPawn.RaceProps.Humanlike) && ___pawn.def != otherPawn.def)
            {
                __result = 0f;
                return false;
            }
            //Applies cross species setting if race def is not the same
            //Also use HAR to determine if they consider each other aliens
            float crossSpecies = 1f;
            if (Settings.HARActive)
            {
                if (SettingsUtilities.AreRacesConsideredXeno(___pawn, otherPawn))
                {
                    crossSpecies = ___pawn.AlienLoveChance() / 100;
                }
            }
            else if (___pawn.def != otherPawn.def)
            {
                crossSpecies = ___pawn.AlienLoveChance() / 100;
            }
            //Not reducing chances to 0, but lowering if gender and sexuality do not match
            //Changing this to what's in romance attempt success chance and then removing it from there
            float sexualityFactor = 1f;
            if (___pawn.RaceProps.Humanlike && ___pawn.story.traits.HasTrait(TraitDefOf.Asexual))
            {
                sexualityFactor = Mathf.Min(___pawn.AsexualRating() + 0.15f, 1f);
            }
            if (___pawn.RaceProps.Humanlike && ___pawn.story.traits.HasTrait(TraitDefOf.Gay))
            {
                if (otherPawn.gender != ___pawn.gender)
                {
                    sexualityFactor = 0.15f;
                }
            }
            if (___pawn.RaceProps.Humanlike && ___pawn.story.traits.HasTrait(RomanceDefOf.Straight))
            {
                if (otherPawn.gender == ___pawn.gender)
                {
                    sexualityFactor = 0.15f;
                }
            }

            //Calculations based on age of both parties; most have been moved to a separate method in 1.4
            float targetMinAgeForSex = otherPawn.MinAgeForSex();
            float pawnMinAgeForSex = ___pawn.MinAgeForSex();
            float pawnAge = ___pawn.ageTracker.AgeBiologicalYearsFloat;
            float targetAge = otherPawn.ageTracker.AgeBiologicalYearsFloat;

            //If either one is too young for sex, do not allow
            if (targetAge < targetMinAgeForSex || pawnAge < pawnMinAgeForSex)
            {
                __result = 0f;
                return false;
            }
            
            //This changes chances based on talking/manipulation/moving stats
            float targetBaseCapabilities = 1f;
            targetBaseCapabilities *= Mathf.Lerp(0.2f, 1f, otherPawn.health.capacities.GetLevel(PawnCapacityDefOf.Talking));
            targetBaseCapabilities *= Mathf.Lerp(0.2f, 1f, otherPawn.health.capacities.GetLevel(PawnCapacityDefOf.Manipulation));
            targetBaseCapabilities *= Mathf.Lerp(0.2f, 1f, otherPawn.health.capacities.GetLevel(PawnCapacityDefOf.Moving));

            __result = __instance.LovinAgeFactor(otherPawn) * targetBaseCapabilities * __instance.PrettinessFactor(otherPawn)
                * crossSpecies * sexualityFactor;
            return false;
        }
    }

    //Age considerations were moved to a separate method in 1.4
    [HarmonyPatch(typeof(Pawn_RelationsTracker), "LovinAgeFactor")]
    public static class Pawn_RelationsTracker_LovinAgeFactor
    {
        //Changes from vanilla:
        //Gender age preferences are now the same
        //Adjusted age calculations to be more realistic with regards to young pawns
        public static bool Prefix(Pawn otherPawn, ref float __result, Pawn ___pawn)
        {
            float pawnMinAgeForSex = ___pawn.MinAgeForSex();
            float pawnMaxAgeGap = ___pawn.MaxAgeGap();
            float pawnAge = ___pawn.ageTracker.AgeBiologicalYearsFloat;
            float targetAge = otherPawn.ageTracker.AgeBiologicalYearsFloat;

            float youngestTargetAge = Mathf.Max(pawnMinAgeForSex, pawnAge - (pawnMaxAgeGap * .75f));
            //For humans, this works out to either min of(21 or age-3), or age-8
            //This allows a 16 year old to be attracted to another 16 year old, while still keeping 20 year olds out
            float youngestReasonableTargetAge = Mathf.Max(Mathf.Min(pawnMinAgeForSex + (pawnMaxAgeGap / 8), pawnAge - 3), pawnAge - (pawnMaxAgeGap / 5));
            //For humans, this returns 1 if the target's age is within 8 years of the pawn
            //The exception is when the pawn is between 16-21, in which case it returns 1 if the target's age is 8 years above or 3 years below their age
            float targetAgeLikelihood = GenMath.FlatHill(0.15f, youngestTargetAge, youngestReasonableTargetAge, pawnAge + (pawnMaxAgeGap / 5), pawnAge + (pawnMaxAgeGap * .75f), 0.15f, targetAge);
            __result = targetAgeLikelihood;
            return false;
        }
    }

    //This is used to determine chances of deep talk, slight, and insult interaction
    [HarmonyPatch(typeof(Pawn_RelationsTracker), "CompatibilityWith")]
    public static class Pawn_RelationsTracker_CompatibilityWith
    {
        //Change from Vanilla:
        //Age adjustments
        public static bool Prefix(Pawn otherPawn, ref float __result, ref Pawn_RelationsTracker __instance, Pawn ___pawn)
        {
            //This will allow for cross species calculations
            if (___pawn.RaceProps.Humanlike != otherPawn.RaceProps.Humanlike || ___pawn == otherPawn)
            {
                __result = 0f;
                return false;
            }
            float ageGap = Mathf.Abs(___pawn.ageTracker.AgeBiologicalYearsFloat - otherPawn.ageTracker.AgeBiologicalYearsFloat);
            float num = Mathf.Clamp(GenMath.LerpDouble(0f, ___pawn.MaxAgeGap() /2, 0.45f, -0.45f, ageGap), -0.45f, 0.45f);
            float num2 = __instance.ConstantPerPawnsPairCompatibilityOffset(otherPawn.thingIDNumber);
            __result = num + num2;
            return false;
        }
    }

    ////Need a version of this that patches HAR's postfix 
    //[HarmonyPatch(typeof(HarmonyPatches), "CompatibilityWithPostfix")]
    //public static class HARPatch_CompatibilityWithPostfix
    //{
    //    public static bool Prefix(Pawn_RelationsTracker __instance, Pawn otherPawn, ref float __result, Pawn ___pawn)
    //    {
    //        //It's the same as the HAR patch but with the age adjustment made
    //        if (___pawn.RaceProps.Humanlike != otherPawn.RaceProps.Humanlike || ___pawn == otherPawn)
    //        {
    //            __result = 0f;
    //            return false;
    //        }

    //        float x = Mathf.Abs(___pawn.ageTracker.AgeBiologicalYearsFloat - otherPawn.ageTracker.AgeBiologicalYearsFloat);
    //        float num = GenMath.LerpDouble(inFrom: 0f, inTo: ___pawn.MaxAgeGap() / 2, outFrom: 0.45f, outTo: -0.45f, x);
    //        num = Mathf.Clamp(num, min: -0.45f, max: 0.45f);
    //        float num2 = __instance.ConstantPerPawnsPairCompatibilityOffset(otherPawn.thingIDNumber);
    //        __result = num + num2;
    //        return false;
    //    }
    //}
}