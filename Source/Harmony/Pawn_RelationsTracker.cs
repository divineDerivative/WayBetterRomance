using HarmonyLib;
using RimWorld;
using Verse;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace BetterRomance.HarmonyPatches
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
            //Both should be humanlikes
            if (!___pawn.RaceProps.Humanlike || !otherPawn.RaceProps.Humanlike)
            {
                __result = 0f;
                return false;
            }

            //If either one is too young for sex, do not allow
            if (otherPawn.ageTracker.AgeBiologicalYearsFloat < otherPawn.MinAgeForSex() || ___pawn.ageTracker.AgeBiologicalYearsFloat < ___pawn.MinAgeForSex())
            {
                __result = 0f;
                return false;
            }

            //Applies cross species setting if race def is not the same
            //Also use HAR to determine if they consider each other aliens
            float crossSpecies = 1f;
            if (Settings.HARActive)
            {
                if (HAR_Integration.AreRacesConsideredXeno(___pawn, otherPawn))
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
            if ( ___pawn.story?.traits?.HasTrait(TraitDefOf.Asexual) ?? false)
            {
                sexualityFactor = ___pawn.AsexualRating() /2;
            }
            if ( ___pawn.story?.traits?.HasTrait(TraitDefOf.Gay) ?? false)
            {
                if (otherPawn.gender != ___pawn.gender)
                {
                    sexualityFactor = 0.125f;
                }
            }
            if (___pawn.story?.traits?.HasTrait(RomanceDefOf.Straight) ?? false)
            {
                if (otherPawn.gender == ___pawn.gender)
                {
                    sexualityFactor = 0.125f;
                }
            }
            
            //This changes chances based on talking/manipulation/moving stats
            float targetBaseCapabilities = 1f;
            targetBaseCapabilities *= Mathf.Lerp(0.2f, 1f, otherPawn.health.capacities.GetLevel(PawnCapacityDefOf.Talking));
            targetBaseCapabilities *= Mathf.Lerp(0.2f, 1f, otherPawn.health.capacities.GetLevel(PawnCapacityDefOf.Manipulation));
            targetBaseCapabilities *= Mathf.Lerp(0.2f, 1f, otherPawn.health.capacities.GetLevel(PawnCapacityDefOf.Moving));

            __result = __instance.LovinAgeFactor(otherPawn) * __instance.PrettinessFactor(otherPawn) * targetBaseCapabilities  * crossSpecies * sexualityFactor;
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
            float num = Mathf.Clamp(GenMath.LerpDouble(0f, ___pawn.MaxAgeGap() / 2, 0.45f, -0.45f, ageGap), -0.45f, 0.45f);
            float num2 = __instance.ConstantPerPawnsPairCompatibilityOffset(otherPawn.thingIDNumber);
            __result = num + num2;
            return false;
        }
    }

    //Adjusts SecondaryLovinChanceFactor based on attractiveness
    [HarmonyPatch(typeof(Pawn_RelationsTracker), "PrettinessFactor")]
    public static class Pawn_RelationsTracker_PrettinessFactor
    {
        public static bool Prefix(Pawn otherPawn, Pawn ___pawn, ref float __result)
        {
            if (!otherPawn.RaceProps.Humanlike)
            {
                return true;
            }
            float pawnBeauty = ___pawn.GetStatValue(StatDefOf.PawnBeauty);
            float otherBeauty = otherPawn.GetStatValue(StatDefOf.PawnBeauty);
            float result = 1f;

            if (otherBeauty < 0f && !___pawn.story.traits.HasTrait(TraitDefOf.Kind))
            {
                if (pawnBeauty < 0f)
                {
                    result = 0.8f;
                }
                else
                {
                    result = 0.3f;
                }
            }
            else if (otherBeauty > 0f)
            {
                if (pawnBeauty > 0f)
                {
                    result = 1.2f;
                }
                else
                {
                    result = 1.6f;
                }
            }
            __result = result;
            return false;
        }
    }

}