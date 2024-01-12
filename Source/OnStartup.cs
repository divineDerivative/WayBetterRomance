using System;
using System.Linq;
using System.Reflection;
using BetterRomance.HarmonyPatches;
using HarmonyLib;
using RimWorld;
using Verse;
using static BetterRomance.WBRLogger;

namespace BetterRomance
{
    [StaticConstructorOnStartup]
    public static class OnStartup
    {
        static OnStartup()
        {
            CustomLoveRelationUtility.MakeAdditionalLoveRelationsLists();
            SettingsUtilities.MakeTraitList();
            if (ModsConfig.BiotechActive)
            {
                SettingsUtilities.GrabBiotechStuff();
            }
            Harmony harmony = new(id: "rimworld.divineDerivative.romance");
            harmony.PatchAll();

            PatchAliens(harmony);
            PatchRobots(harmony);
            PatchSex(harmony);
            PatchRomance(harmony);
            PatchMisc(harmony);

            MakeFertilityModList();
            Settings.ApplyJoySettings();
            MakeRaceSettingsList();
        }

        private static void PatchAliens(Harmony harmony)
        {
            if (ModsConfig.IsActive("erdelf.humanoidalienraces") || ModsConfig.IsActive("erdelf.humanoidalienraces.dev"))
            {
                Settings.HARActive = true;
                harmony.PatchHAR();
            }
            if (ModsConfig.IsActive("tachyonite.pawnmorpherpublic"))
            {
                try
                {
                    Settings.PawnmorpherActive = true;
                    harmony.PatchPawnmorpher();
                    HelperClasses.IsHumanlikePM = AccessTools.Method(Type.GetType("Pawnmorph.FormerHumanUtilities, Pawnmorph"), "IsHumanlike");
                }
                catch (Exception ex)
                {
                    LogUtil.Error($"Error encountered while patching Pawnmorpher: {ex}");
                }
            }
        }

        private static void PatchRobots(Harmony harmony)
        {
            if (ModsConfig.IsActive("Killathon.AndroidTiersReforged"))
            {
                try
                {
                    Settings.ATRActive = true;
                    HelperClasses.IsConsideredMechanicalAndroid = AccessTools.DeclaredMethod(Type.GetType("ATReforged.Utils,Android Tiers Reforged"), "IsConsideredMechanicalAndroid", [typeof(Pawn)]);
                    HelperClasses.IsConsideredMechanicalDrone = AccessTools.DeclaredMethod(Type.GetType("ATReforged.Utils,Android Tiers Reforged"), "IsConsideredMechanicalDrone", [typeof(Pawn)]);
                    HelperClasses.IsConsideredMechanical = AccessTools.DeclaredMethod(Type.GetType("ATReforged.Utils,Android Tiers Reforged"), "IsConsideredMechanical", [typeof(ThingDef)]);
                }
                catch (Exception ex)
                {
                    LogUtil.Error($"Error encountered while patching Android Tiers Reforged: {ex}");
                }
            }
            else if (ModsConfig.IsActive("Killathon.MechHumanlikes.AndroidTiersCore"))
            {
                try
                {
                    Settings.ATRActive = true;
                    HelperClasses.IsConsideredMechanicalAndroid = AccessTools.DeclaredMethod(Type.GetType("MechHumanlikes.MHC_Utils,Mechanical Humanlikes Core"), "IsConsideredMechanicalAndroid", [typeof(Pawn)]);
                    HelperClasses.IsConsideredMechanicalDrone = AccessTools.DeclaredMethod(Type.GetType("MechHumanlikes.MHC_Utils,Mechanical Humanlikes Core"), "IsConsideredMechanicalDrone", [typeof(Pawn)]);
                    HelperClasses.IsConsideredMechanical = AccessTools.DeclaredMethod(Type.GetType("MechHumanlikes.MHC_Utils,Mechanical Humanlikes Core"), "IsConsideredMechanical", [typeof(ThingDef)]);
                }
                catch (Exception ex)
                {
                    LogUtil.Error($"Error encountered while patching MH: Android Tiers Core: {ex}");
                }
            }
            if (ModsConfig.IsActive("Neronix17.Asimov"))
            {
                try
                {
                    Settings.AsimovActive = true;
                    HelperClasses.IsHumanlikeAutomaton = AccessTools.DeclaredMethod(Type.GetType("Asimov.AutomatonUtil,Asimov"), "IsHumanlikeAutomaton");
                    HelperClasses.PawnDef = Type.GetType("Asimov.PawnDef,Asimov");
                    HelperClasses.pawnSettings = AccessTools.Field(HelperClasses.PawnDef, "pawnSettings");
                    HelperClasses.AsimovGrowth = AccessTools.Field(Type.GetType("Asimov.PawnSettings,Asimov"), "hasGrowthMoments");
                }
                catch (Exception ex)
                {
                    LogUtil.Error($"Error encountered while patching Asimov: {ex}");
                }
            }
            if (ModsConfig.IsActive("vanillaracesexpanded.android"))
            {
                try
                {
                    Settings.VREAndroidActive = true;
                    HelperClasses.Emotionless = AccessTools.Method(Type.GetType("VREAndroids.Utils, VREAndroids"), "Emotionless");
                    harmony.PatchVREAndroids();
                }
                catch (Exception ex)
                {
                    LogUtil.Error($"Error encountered while patching Vanilla Races Expanded - Android: {ex}");
                }
            }
        }

        private static void PatchSex(Harmony harmony)
        {
            if (ModsConfig.IsActive("dylan.csl"))
            {
                HelperClasses.CSLLoved = AccessTools.DeclaredMethod(Type.GetType("Children.MorePawnUtilities,ChildrenHelperClasses"), "Loved", [typeof(Pawn), typeof(Pawn), typeof(bool)]);
            }
            if (ModsConfig.IsActive("rim.job.world") || ModsConfig.IsActive("safe.job.world"))
            {
                try
                {
                    harmony.Patch(AccessTools.DeclaredMethod(Type.GetType("rjw.xxx,RJW"), "is_asexual"), postfix: new(typeof(OtherMod_Patches), nameof(OtherMod_Patches.RJWAsexualPostfix)));
                    harmony.Patch(AccessTools.DeclaredMethod(Type.GetType("rjw.CompRJW,RJW"), "VanillaTraitCheck"), transpiler: new(typeof(OtherMod_Patches), nameof(OtherMod_Patches.VanillaTraitCheckTranspiler)));
                }
                catch (Exception ex)
                {
                    LogUtil.Error($"Error encountered while patching RimJobWorld: {ex}");
                }
            }
            if (ModsConfig.IsActive("vanillaracesexpanded.highmate"))
            {
                try
                {
                    Settings.VREHighmateActive = true;
                    harmony.PatchVREHighmates();
                }
                catch (Exception ex)
                {
                    LogUtil.Error($"Error encountered while patching Vanilla Races Expanded - Highmate: {ex}");
                }
            }
            if (ModsConfig.IsActive("runaway.simpletrans"))
            {
                try
                {
                    Settings.TransActive = true;
                    HelperClasses.CanSire = AccessTools.Method(Type.GetType("Simple_Trans.SimpleTransPregnancyUtility,Simple-Trans"), "CanSire");
                    HelperClasses.CanCarry = AccessTools.Method(Type.GetType("Simple_Trans.SimpleTransPregnancyUtility,Simple-Trans"), "CanCarry");
                }
                catch (Exception ex)
                {
                    LogUtil.Error($"Error encountered while patching Simple Trans: {ex}");
                }
            }
            if (ModsConfig.IsActive("eth0net.AlternateFertility"))
            {
                try
                {
                    Settings.AltFertilityActive = true;
                    HelperClasses.GetImpregnationPossible = AccessTools.Method(Type.GetType("AlternateFertility.PatcherUtility, AlternateFertility"), "GetImpregnationPossible");
                    HelperClasses.CanImpregnate = AccessTools.Method(Type.GetType("AlternateFertility.PatcherUtility, AlternateFertility"), "CanImpregnate");
                    HelperClasses.CanGetPregnant = AccessTools.Method(Type.GetType("AlternateFertility.PatcherUtility, AlternateFertility"), "CanGetPregnant");

                    harmony.Patch(AccessTools.Method(typeof(PawnRelationWorker_Parent_CreateRelation), nameof(PawnRelationWorker_Parent_CreateRelation.Prefix)), transpiler: new(typeof(OtherMod_Patches), nameof(OtherMod_Patches.AltFertilityParentCreateRelationTranspiler)));
                    //Apply their transpiler to my similar method
                    HarmonyMethod canProduceChildTranspiler = new(AccessTools.Method(Type.GetType("AlternateFertility.Harmony.Harmony_PregnancyUtility_CanEverProduceChild, AlternateFertility"), "Transpiler"));
                    harmony.Patch(AccessTools.Method(typeof(HookupUtility), nameof(HookupUtility.CanEverProduceChild)), transpiler: canProduceChildTranspiler);
                }
                catch (Exception ex)
                {
                    LogUtil.Error($"Error encountered while patching AlternateFertility: {ex}");
                }
            }
        }

        private static void PatchRomance(Harmony harmony)
        {
            if (ModsConfig.IsActive("telardo.romanceontherim"))
            {
                try
                {
                    Settings.RotRActive = true;
                    HelperClasses.RotRFillRomanceBar = AccessTools.DeclaredMethod(Type.GetType("RomanceOnTheRim.RomanceUtility,RomanceOnTheRim"), "TryAffectRomanceNeedLevelForPawn");
#if !v1_6
                    HelperClasses.RotRPreceptExplanation = AccessTools.Method(Type.GetType("RomanceOnTheRim.HarmonyPatch_SocialCardUtility_RomanceExplanation,RomanceOnTheRim"), "PreceptExplanation");
#else
                    HelperClasses.RotRPreceptExplanation = AccessTools.Method(Type.GetType("RomanceOnTheRim.HarmonyPatch_AddExplanationForPrecepts,RomanceOnTheRim"), "PreceptExplanation");
#endif
                    harmony.PatchRotR();
                }
                catch (Exception ex)
                {
                    LogUtil.Error($"Error encountered while patching Romance on the Rim: {ex}");
                }
            }
            if (ModsConfig.IsActive("VanillaExpanded.VanillaSocialInteractionsExpanded"))
            {
                try
                {
                    MethodInfo GetSpouseOrLoverOrFiance = Type.GetType("VanillaSocialInteractionsExpanded.VSIE_Utils, VanillaSocialInteractionsExpanded").GetMethod("GetSpouseOrLoverOrFiance");
                    MethodInfo prefixTarget = null;
#if !v1_6
                    foreach (Type t in Type.GetType("VanillaSocialInteractionsExpanded.AddDirectRelation_Patch, VanillaSocialInteractionsExpanded").GetNestedTypes(AccessTools.all).Where((Type t) => t.GetCustomAttribute<System.Runtime.CompilerServices.CompilerGeneratedAttribute>() != null))
                    {
                        foreach (MethodInfo method in t.GetMethods(AccessTools.all))
                        {
                            if (method.ReturnType == typeof(void) && method.Name != "Finalize")
                            {
                                if (prefixTarget != null)
                                {
                                    LogUtil.Error($"Multiple matching methods found: {prefixTarget.Name} and {method.Name}");
                                }
                                prefixTarget = method;
                                VSIEPatches.CompilerType = t;
                            }
                        }
                    }
#else
                    prefixTarget = AccessTools.Method(AccessTools.TypeByName("VanillaSocialInteractionsExpanded.AddDirectRelation_Patch"), "Prefix");
#endif

                    harmony.Patch(GetSpouseOrLoverOrFiance, postfix: new(typeof(VSIEPatches), nameof(VSIEPatches.GetSpouseOrLoverOrFiancePostfix)));
                    if (prefixTarget == null)
                    {
                        LogUtil.Error("Could not find prefix delagate for Ad");
                    }
                    harmony.Patch(prefixTarget, transpiler: new(typeof(VSIEPatches), nameof(VSIEPatches.AddDirectRelation_PrefixTranspiler)));
                }
                catch (Exception ex)
                {
                    LogUtil.Error($"Error encountered while patching Vanilla Social Interactions Expanded: {ex}");
                }
            }
            if (ModsConfig.IsActive("RforReload.EdgesOfAcceptence"))
            {
                try
                {
                    harmony.PatchForceLoveHate();
                }
                catch (Exception ex)
                {
                    LogUtil.Error($"Error encountered while patching Edges of Acceptance: {ex}");
                }
            }
            if (ModsConfig.IsActive("Dra.RimderRomanceControl"))
            {
                try
                {
                    harmony.Patch(AccessTools.DeclaredMethod(Type.GetType("Rimder.HarmonyPatches.ChangeStatusOnRelationshipChange,RimderRomanceControl"), "Postfix"), transpiler: new(typeof(OtherMod_Patches), nameof(OtherMod_Patches.RimderLoveRelationTranspiler)));
                    harmony.Patch(AccessTools.DeclaredMethod(Type.GetType("Rimder.HarmonyPatches.ChangeStatusOnRelationshipChange,RimderRomanceControl"), "Postfix"), transpiler: new(typeof(OtherMod_Patches), nameof(OtherMod_Patches.RimderExLoveRelationTranspiler)));
                }
                catch (Exception ex)
                {
                    LogUtil.Error($"Error encountered while patching Rimder: Romance Control: {ex}");
                }
            }
        }

        private static void PatchMisc(Harmony harmony)
        {
            if (ModsConfig.IsActive("Neronix17.TweaksGalore"))
            {
                try
                {
                    MethodInfo IsSexualityTrait = AccessTools.DeclaredMethod(Type.GetType("TweaksGalore.Patch_PawnGenerator_GenerateTraits,TweaksGalore"), "IsSexualityTrait");
                    harmony.Patch(IsSexualityTrait, prefix: new(typeof(OtherMod_Patches), nameof(OtherMod_Patches.IsSexualityTraitPrefix)));
                }
                catch (Exception ex)
                {
                    LogUtil.Error($"Error encountered while patching Tweaks Galore: {ex}");
                }
            }
            if (ModsConfig.IsActive("divineDerivative.NonBinaryGender"))
            {
                Settings.NonBinaryActive = true;
            }
        }

        public static void MakeRaceSettingsList()
        {
            foreach (ThingDef race in DefDatabase<ThingDef>.AllDefsListForReading.Where(td => td.race != null && !td.IsCorpse && td.race.Humanlike))
            {
                race.comps.Add(new(typeof(WBR_SettingsComp)));
                Settings.RaceSettingsList.Add(new(race));
            }
        }

        private static void MakeFertilityModList()
        {
            if (ModsConfig.BiotechActive)
            {
                Settings.FertilityMods.Add("ludeon.rimworld.biotech", ModLister.GetActiveModWithIdentifier("ludeon.rimworld.biotech").Name);
            }
            if (ModsConfig.IsActive("dylan.csl"))
            {
                Settings.FertilityMods.Add("dylan.csl", ModLister.GetActiveModWithIdentifier("dylan.csl").Name);
            }
            if (ModsConfig.IsActive("rim.job.world"))
            {
                Settings.FertilityMods.Add("rim.job.world", ModLister.GetActiveModWithIdentifier("rim.job.world").Name);
            }
            if (ModsConfig.IsActive("safe.job.world"))
            {
                Settings.FertilityMods.Add("safe.job.world", ModLister.GetActiveModWithIdentifier("safe.job.world").Name);
            }
            Settings.AutoDetectFertilityMod();
        }
    }

    public static class HelperClasses
    {
        public static MethodInfo RotRFillRomanceBar;
        public static MethodInfo RotRPreceptExplanation;
        public static MethodInfo CSLLoved;
        public static MethodInfo IsConsideredMechanicalAndroid;
        public static MethodInfo IsConsideredMechanicalDrone;
        public static MethodInfo IsConsideredMechanical;
        public static MethodInfo IsHumanlikeAutomaton;
        public static FieldInfo AsimovGrowth;
        public static FieldInfo pawnSettings;
        public static Type PawnDef;
        public static MethodInfo IsHumanlikePM;
        public static MethodInfo CanSire;
        public static MethodInfo CanCarry;
        public static MethodInfo Emotionless;
        public static MethodInfo GetImpregnationPossible;
        public static MethodInfo CanImpregnate;
        public static MethodInfo CanGetPregnant;
    }

    public class MayRequireHARAttribute : MayRequireAttribute
    {
        public MayRequireHARAttribute()
            : base("erdelf.humanoidalienraces") { }
    }

    public class MayRequirePersonalityM2Attribute : MayRequireAttribute
    {
        public MayRequirePersonalityM2Attribute()
            : base("hahkethomemah.simplepersonalities.module2") { }
    }

    public class MayRequireCSLAttribute : MayRequireAttribute
    {
        public MayRequireCSLAttribute()
            : base("dylan.csl") { }
    }

    public class MayRequireRJWAttribute : MayRequireAttribute
    {
        public MayRequireRJWAttribute()
            : base("rim.job.world") { }
    }
}
