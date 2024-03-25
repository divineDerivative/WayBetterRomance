using AlienRace;
using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
            return p1.def != p2.def && !(p1.def is ThingDef_AlienRace alienDef && alienDef.alienRace.generalSettings.notXenophobistTowards.Contains(p2.def));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int[] GetGrowthMoments(ThingDef race)
        {
            if (race is ThingDef_AlienRace alienRace)
            {
                if (alienRace.alienRace.generalSettings.GrowthAges.NullOrEmpty())
                {
                    return null;
                }
                return alienRace.alienRace.generalSettings.GrowthAges;
            }
            return GrowthUtility.GrowthMomentAges;
        }

        public static void SetGrowthMoments(ThingDef race, List<int> ages)
        {
            if (race is ThingDef_AlienRace alienRace)
            {
                alienRace.alienRace.generalSettings.growthAges = ages;
            }
        }

        //This is still not ever going to work right since HAR provides a default curve if none is specified
        //So I need to try again to get him to change it
        public static bool FertilityCurveExists(Pawn pawn)
        {
            if (pawn.def is ThingDef_AlienRace alienRace)
            {
                return pawn.gender != Gender.Female ? alienRace.alienRace.generalSettings.reproduction.maleFertilityAgeFactor != null : alienRace.alienRace.generalSettings.reproduction.femaleFertilityAgeFactor != null;
            }
            return false;
        }

        public static SimpleCurve FertilityCurve(ThingDef race, Gender gender)
        {
            if (race is ThingDef_AlienRace alienRace)
            {
                return gender != Gender.Female ? alienRace.alienRace.generalSettings.reproduction?.maleFertilityAgeFactor : alienRace.alienRace.generalSettings.reproduction?.femaleFertilityAgeFactor;
            }
            return null;
        }

        public static bool IsCurveDefault(SimpleCurve curve, Gender gender)
        {
            return gender switch
            {
                Gender.Female => curve.IsEquivalentTo(femaleFertilityAgeFactor),
                _ => curve.IsEquivalentTo(maleFertilityAgeFactor),
            };
        }

        public static readonly SimpleCurve maleFertilityAgeFactor =
        [
            new CurvePoint(14f, 0f),
            new CurvePoint(18f, 1f),
            new CurvePoint(50f, 1f),
            new CurvePoint(90f, 0f)
        ];

        public static readonly SimpleCurve femaleFertilityAgeFactor =
        [
            new CurvePoint(14f, 0f),
            new CurvePoint(20f, 1f),
            new CurvePoint(28f, 1f),
            new CurvePoint(35f, 0.5f),
            new CurvePoint(40f, 0.1f),
            new CurvePoint(45f, 0.02f),
            new CurvePoint(50f, 0f)
        ];

        public static AcceptanceReport CanEverProduceChild(Pawn first, Pawn second)
        {
            if (first.Dead)
            {
                return "WBR.PawnIsDead".Translate(first.Named("PAWN"));
            }
            if (second.Dead)
            {
                return "WBR.PawnIsDead".Translate(second.Named("PAWN"));
            }
            if (!ReproductionSettings.GenderReproductionCheck(first, second))
            {
                return "WBR.PawnsHaveSameGender".Translate(first.Named("PAWN1"), second.Named("PAWN2")).Resolve();
            }
            Pawn fertilizer = ReproductionSettings.ApplicableGender(first, false) ? first : second;
            Pawn gestator = ReproductionSettings.ApplicableGender(first, true) ? first : second;
            bool fertilizerFertile = fertilizer.GetFertilityLevel() <= 0f;
            bool gestatorFertile = gestator.GetFertilityLevel() <= 0f;
            if (fertilizerFertile && gestatorFertile)
            {
                return "WBR.PawnsAreInfertile".Translate(fertilizer.Named("PAWN1"), gestator.Named("PAWN2")).Resolve();
            }
            if (fertilizerFertile != gestatorFertile)
            {
                return "WBR.PawnIsInfertile".Translate((fertilizerFertile ? fertilizer : gestator).Named("PAWN")).Resolve();
            }
            bool fertilizerYoung = !fertilizer.ageTracker.CurLifeStage.reproductive;
            bool gestatorYoung = !gestator.ageTracker.CurLifeStage.reproductive;
            if (fertilizerYoung && gestatorYoung)
            {
                return "WBR.PawnsAreTooYoung".Translate(fertilizer.Named("PAWN1"), gestator.Named("PAWN2")).Resolve();
            }
            if (fertilizerYoung != gestatorYoung)
            {
                return "WBR.PawnIsTooYoung".Translate((fertilizerYoung ? fertilizer : gestator).Named("PAWN")).Resolve();
            }
            bool gestatorSterile = gestator.Sterile() && PregnancyUtility.GetPregnancyHediff(gestator) == null;
            bool fertilizerSterile = fertilizer.Sterile();
            if (fertilizerSterile && gestatorSterile)
            {
                return "WBR.PawnsAreSterile".Translate(fertilizer.Named("PAWN1"), gestator.Named("PAWN2")).Resolve();
            }
            if (fertilizerSterile != gestatorSterile)
            {
                return "WBR.PawnIsSterile".Translate((fertilizerSterile ? fertilizer : gestator).Named("PAWN")).Resolve();
            }
            if (!RaceRestrictionSettings.CanReproduce(first, second))
            {
                return $"{first.gender} {first.def.LabelCap} can not reproduce with {second.gender} {second.def.LabelCap}";
            }
            return true;
        }

        public static bool UseHARAgeForAdulthood(ThingDef race, out float age)
        {
            if (race is ThingDef_AlienRace alienThingDef)
            {
                age = alienThingDef.alienRace.generalSettings.minAgeForAdulthood;
                return true;
            }
            age = 20f;
            return false;
        }
    }

    namespace HarmonyPatches
    {
        public static class HARPatches
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void PatchHAR(this Harmony harmony)
            {
                harmony.Unpatch(typeof(Pawn_RelationsTracker).GetMethod("CompatibilityWith"), typeof(AlienRace.HarmonyPatches).GetMethod("CompatibilityWithPostfix"));
            }
        }
    }
}
