using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Verse;

namespace BetterRomance.HarmonyPatches
{
    //This determines how likely a pawn is to want to have sex with another pawn
    [HarmonyPatch(typeof(Pawn_RelationsTracker), nameof(Pawn_RelationsTracker.SecondaryLovinChanceFactor))]
    [HarmonyAfter(["Pecius.PawnExtensions"])]
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
            if (!___pawn.IsHumanlike() || !otherPawn.IsHumanlike())
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

            ___pawn.EnsureTraits();
            //Don't allow for ace/aro
            if (___pawn.IsAro())
            {
                __result = 0f;
                return false;
            }

            //This changes chances based on talking/manipulation/moving stats
            float targetBaseCapabilities = 1f;
            targetBaseCapabilities *= Mathf.Lerp(0.2f, 1f, otherPawn.health.capacities.GetLevel(PawnCapacityDefOf.Talking));
            targetBaseCapabilities *= Mathf.Lerp(0.2f, 1f, otherPawn.health.capacities.GetLevel(PawnCapacityDefOf.Manipulation));
            targetBaseCapabilities *= Mathf.Lerp(0.2f, 1f, otherPawn.health.capacities.GetLevel(PawnCapacityDefOf.Moving));

            __result = __instance.LovinAgeFactor(otherPawn) * __instance.PrettinessFactor(otherPawn) * targetBaseCapabilities * crossSpecies * RomanceUtilities.SexualityFactor(___pawn, otherPawn);
            return false;
        }
    }

    //Age considerations were moved to a separate method in 1.4
    [HarmonyPatch(typeof(Pawn_RelationsTracker), nameof(Pawn_RelationsTracker.LovinAgeFactor))]
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
    [HarmonyPatch(typeof(Pawn_RelationsTracker), nameof(Pawn_RelationsTracker.CompatibilityWith))]
    public static class Pawn_RelationsTracker_CompatibilityWith
    {
        //Change from Vanilla:
        //Age adjustments
        //Check for humanlike instead of def
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            FieldInfo def = AccessTools.Field(typeof(Thing), nameof(Thing.def));

            foreach (CodeInstruction code in instructions)
            {
                if (code.LoadsConstant(20f))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return CodeInstruction.LoadField(typeof(Pawn_RelationsTracker), "pawn");
                    yield return CodeInstruction.Call(typeof(SettingsUtilities), nameof(SettingsUtilities.MaxAgeGap));
                    yield return new CodeInstruction(OpCodes.Ldc_R4, 2f);
                    yield return new CodeInstruction(OpCodes.Div);
                }
                else if (code.LoadsField(def))
                {
                    yield return CodeInstruction.Call(typeof(CompatUtility), nameof(CompatUtility.IsHumanlike));
                }
                else
                {
                    yield return code;
                }
            }
        }
    }

    //Adjusts SecondaryLovinChanceFactor based on attractiveness
    [HarmonyPatch(typeof(Pawn_RelationsTracker), nameof(Pawn_RelationsTracker.PrettinessFactor))]
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
                result = pawnBeauty < 0f ? 0.8f : 0.3f;
            }
            else if (otherBeauty > 0f)
            {
                result = pawnBeauty > 0f ? 1.2f : 1.6f;
            }
            __result = result;
            return false;
        }
    }
}