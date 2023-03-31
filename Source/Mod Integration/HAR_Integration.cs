using AlienRace;
using HarmonyLib;
using RimWorld;
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
            return pawn.gender != Gender.Female ? alienRace.alienRace.generalSettings.reproduction.maleFertilityAgeFactor != null : alienRace.alienRace.generalSettings.reproduction.femaleFertilityAgeFactor != null;
        }

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
