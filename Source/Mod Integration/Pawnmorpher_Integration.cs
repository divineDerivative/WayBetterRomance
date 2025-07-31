using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Pawnmorph;
using RimWorld;
using Verse;
using static BetterRomance.WBRLogger;

namespace BetterRomance
{
    public static class Pawnmorpher_Integration
    {
        /// <summary>
        /// If <paramref name="pawn"/> is a former human, try to get the original and copy their <see cref="WBR_SettingsComp"/>. Gets called when loading pawns from the save file.
        /// </summary>
        /// <param name="pawn"></param>
        /// <returns></returns>
        public static void FormerHumanCompCheck(this Pawn pawn)
        {
            //So far a null original happens when the 'real' pawn is no longer a former human, i.e. has been reverted back to actual human
            //So the animal pawn in this case is just an artifact of the transformation hanging around in the save file
            //And I'm pretty sure they don't need the comp
            if (pawn.IsFormerHuman(false) && FormerHumanUtilities.GetOriginalPawnOfFormerHuman(pawn) is Pawn original)
            {
                if (original.TryGetComp<WBR_SettingsComp>() is WBR_SettingsComp originalComp)
                {
                    //Copy the comp and then redo the ages
                    originalComp.Copy(pawn);
                    if (originalComp.Calculated)
                    {
                        pawn.TryGetComp<WBR_SettingsComp>().RedoSettings(true);
                    }
                    else
                    {
                        //Not sure what to do if this happens. At least it shouldn't throw any errors because it will get calculated with the human ages
                        LogUtil.Error($"Can't redo ages for {pawn.Name} ({pawn.def.defName}) because the original hasn't been calculated yet");
                    }
                    return;
                }
                //I'm not sure it will ever get to here
                LogUtil.Error($"FormerHumanCompCheck didn't find the original's comp for {pawn.Name} ({pawn.def.defName})");
            }
        }

        public static bool IsFormerHuman(this Pawn pawn) => pawn.IsFormerHuman(false);

        public static void AdjustAges(WBR_SettingsComp comp, ThingDef oldRace, ThingDef newRace)
        {
            float oldLE = oldRace.race.lifeExpectancy;
            float newLE = newRace.race.lifeExpectancy;
            //Regular sex settings
            comp.regularSex.minAgeForSex = TransformerUtility.ConvertAge(comp.regularSex.minAgeForSex, oldLE, newLE);
            comp.regularSex.maxAgeForSex = TransformerUtility.ConvertAge(comp.regularSex.maxAgeForSex, oldLE, newLE);
            comp.regularSex.maxAgeGap = TransformerUtility.ConvertAge(comp.regularSex.maxAgeGap, oldLE, newLE);
            comp.regularSex.declineAtAge = TransformerUtility.ConvertAge(comp.regularSex.declineAtAge, oldLE, newLE);
            //Relation settings
            comp.relations.minFemaleAgeToHaveChildren = TransformerUtility.ConvertAge(comp.relations.minFemaleAgeToHaveChildren, oldLE, newLE);
            comp.relations.usualFemaleAgeToHaveChildren = TransformerUtility.ConvertAge(comp.relations.usualFemaleAgeToHaveChildren, oldLE, newLE);
            comp.relations.maxFemaleAgeToHaveChildren = TransformerUtility.ConvertAge(comp.relations.maxFemaleAgeToHaveChildren, oldLE, newLE);
            comp.relations.minMaleAgeToHaveChildren = TransformerUtility.ConvertAge(comp.relations.minMaleAgeToHaveChildren, oldLE, newLE);
            comp.relations.usualMaleAgeToHaveChildren = TransformerUtility.ConvertAge(comp.relations.usualMaleAgeToHaveChildren, oldLE, newLE);
            comp.relations.maxMaleAgeToHaveChildren = TransformerUtility.ConvertAge(comp.relations.maxMaleAgeToHaveChildren, oldLE, newLE);
            //Biotech settings
            comp.biotech.maleFertilityAgeFactor = comp.biotech.maleFertilityAgeFactor.ConvertCurve(oldLE, newLE);
            comp.biotech.femaleFertilityAgeFactor = comp.biotech.femaleFertilityAgeFactor.ConvertCurve(oldLE, newLE);
            comp.biotech.noneFertilityAgeFactor = comp.biotech.noneFertilityAgeFactor.ConvertCurve(oldLE, newLE);
            comp.biotech.ageEffectOnChildbirth = comp.biotech.ageEffectOnChildbirth.ConvertCurve(oldLE, newLE);
            if (!comp.NoGrowth)
            {
                comp.biotech.growthMoments = comp.biotech.growthMoments.ConvertGrowthMoments(oldLE, newLE);
            }
            //Misc settings
            comp.misc.minAgeForAdulthood = TransformerUtility.ConvertAge(comp.misc.minAgeForAdulthood, oldLE, newLE);
            comp.misc.childAge = (int)TransformerUtility.ConvertAge(comp.misc.childAge, oldLE, newLE);
            comp.misc.adultAgeForLearning = (int)TransformerUtility.ConvertAge(comp.misc.adultAgeForLearning, oldLE, newLE);
            comp.misc.ageReversalDemandAge = (int)TransformerUtility.ConvertAge(comp.misc.ageReversalDemandAge, oldLE, newLE);
            comp.misc.ageSkillFactor = comp.misc.ageSkillFactor.ConvertCurve(oldLE, newLE);
            comp.misc.ageSkillMaxFactorCurve = comp.misc.ageSkillMaxFactorCurve.ConvertCurve(oldLE, newLE);
            comp.misc.lovinCurve = comp.misc.lovinCurve.ConvertCurve(oldLE, newLE);
        }

        private static SimpleCurve ConvertCurve(this SimpleCurve curve, float oldLE, float newLE)
        {
            SimpleCurve newCurve = new();
            foreach (CurvePoint point in curve)
            {
                newCurve.Points.Add(new(TransformerUtility.ConvertAge(point.x, oldLE, newLE), point.y));
            }
            return newCurve;
        }

        private static int[] ConvertGrowthMoments(this int[] array, float oldLE, float newLE)
        {
            return
            [
                (int)TransformerUtility.ConvertAge(array[0], oldLE, newLE),
                (int)TransformerUtility.ConvertAge(array[1], oldLE, newLE),
                (int)TransformerUtility.ConvertAge(array[2], oldLE, newLE),
            ];
        }
    }

    namespace HarmonyPatches
    {
        public static class PawnmorpherPatches
        {
            public static void PatchPawnmorpher(this Harmony harmony)
            {
                harmony.Patch(typeof(FormerHumanUtilities).GetMethod(nameof(FormerHumanUtilities.TransferEverything)), postfix: new(typeof(PawnmorpherPatches).GetMethod(nameof(TransferEverythingPostfix))));
                Type InteractionPatches = AccessTools.TypeByName("Pawnmorph.HPatches.InteractionPatches");
                MethodInfo lovin = AccessTools.Method(AccessTools.Inner(InteractionPatches, "SecondaryLoveFactorPatch"), "SecondaryLovinChanceFactor");
                harmony.Unpatch(typeof(Pawn_RelationsTracker).GetMethod(nameof(Pawn_RelationsTracker.SecondaryLovinChanceFactor)), lovin);
                MethodInfo compatibility = AccessTools.Method(AccessTools.Inner(InteractionPatches, "RelationshipPatches"), "CompatibilityWithPostfix");
                harmony.Unpatch(typeof(Pawn_RelationsTracker).GetMethod("CompatibilityWith"), compatibility);
                harmony.Patch(AccessTools.PropertyGetter(typeof(TraitSet), nameof(TraitSet.TraitsSorted)), postfix: new(typeof(PawnmorpherPatches).GetMethod(nameof(TraitSetPostfix))));
            }

            //This happens whenever a former human is generated, either on its own or by transforming an existing pawn
            public static void TransferEverythingPostfix(Pawn original, Pawn transformedPawn)
            {
                if (original.TryGetComp<WBR_SettingsComp>() is WBR_SettingsComp comp && transformedPawn.TryGetComp<WBR_SettingsComp>() is null)
                {
                    //First copy the original's comp
                    comp.Copy(transformedPawn);
                    //Then adjust the ages
                    transformedPawn.TryGetComp<WBR_SettingsComp>().RedoSettings(true);
                }
            }

            //This stops orientation traits from being displayed for former humans with animal sapience
            public static void TraitSetPostfix(Pawn ___pawn, ref List<Trait> ___tmpTraits)
            {
                if (___tmpTraits.Any(x => SexualityUtility.OrientationTraits.Contains(x.def)) && !FormerHumanUtilities.IsHumanlike(___pawn))
                {
                    ___tmpTraits.RemoveAll(x => SexualityUtility.OrientationTraits.Contains(x.def));
                }
            }
        }
    }
}
