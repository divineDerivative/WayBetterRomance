using HarmonyLib;
using Verse;
using RimWorld;
using System;
using System.Linq;
using System.Reflection;
using BetterRomance.HarmonyPatches;

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
            Harmony harmony = new Harmony(id: "rimworld.divineDerivative.romance");
            harmony.PatchAll();

            if (ModsConfig.IsActive("erdelf.humanoidalienraces") || ModsConfig.IsActive("erdelf.humanoidalienraces.dev"))
            {
                Settings.HARActive = true;
                harmony.PatchHAR();
            }
            if (ModsConfig.IsActive("telardo.romanceontherim"))
            {
                Settings.RotRActive = true;
                HelperClasses.RotRFillRomanceBar = AccessTools.DeclaredMethod(Type.GetType("RomanceOnTheRim.RomanceUtility,RomanceOnTheRim"), "TryAffectRomanceNeedLevelForPawn");
                HelperClasses.RotRPreceptExplanation = AccessTools.Method(Type.GetType("RomanceOnTheRim.HarmonyPatch_SocialCardUtility_RomanceExplanation,RomanceOnTheRim"), "PreceptExplanation");
                harmony.PatchRotR();
            }
            if (ModsConfig.IsActive("dylan.csl"))
            {
                HelperClasses.CSLLoved = AccessTools.DeclaredMethod(Type.GetType("Children.MorePawnUtilities,ChildrenHelperClasses"), "Loved", new Type[] { typeof(Pawn), typeof(Pawn), typeof(bool) });
            }
            if (ModsConfig.IsActive("Killathon.AndroidTiersReforged"))
            {
                Settings.ATRActive = true;
                HelperClasses.IsConsideredMechanicalAndroid = AccessTools.DeclaredMethod(Type.GetType("ATReforged.Utils,Android Tiers Reforged"), "IsConsideredMechanicalAndroid", new Type[] { typeof(Pawn) });
                HelperClasses.IsConsideredMechanicalDrone = AccessTools.DeclaredMethod(Type.GetType("ATReforged.Utils,Android Tiers Reforged"), "IsConsideredMechanicalDrone", new Type[] { typeof(Pawn) });
                HelperClasses.IsConsideredMechanical = AccessTools.DeclaredMethod(Type.GetType("ATReforged.Utils,Android Tiers Reforged"), "IsConsideredMechanical", new Type[] { typeof(Pawn) });
            }
            else if (ModsConfig.IsActive("Killathon.MechHumanlikes.AndroidTiersCore"))
            {
                Settings.ATRActive = true;
                HelperClasses.IsConsideredMechanicalAndroid = AccessTools.DeclaredMethod(Type.GetType("MechHumanlikes.MHC_Utils,Mechanical Humanlikes Core"), "IsConsideredMechanicalAndroid", new Type[] { typeof(Pawn) });
                HelperClasses.IsConsideredMechanicalDrone = AccessTools.DeclaredMethod(Type.GetType("MechHumanlikes.MHC_Utils,Mechanical Humanlikes Core"), "IsConsideredMechanicalDrone", new Type[] { typeof(Pawn) });
                HelperClasses.IsConsideredMechanical = AccessTools.DeclaredMethod(Type.GetType("MechHumanlikes.MHC_Utils,Mechanical Humanlikes Core"), "IsConsideredMechanical", new Type[] { typeof(Pawn) });
            }
            if (ModsConfig.IsActive("Neronix17.TweaksGalore"))
            {
                MethodInfo IsSexualityTrait = AccessTools.DeclaredMethod(Type.GetType("TweaksGalore.Patch_PawnGenerator_GenerateTraits,TweaksGalore"), "IsSexualityTrait");
                harmony.Patch(IsSexualityTrait, prefix: new HarmonyMethod(typeof(OtherMod_Patches), nameof(OtherMod_Patches.IsSexualityTraitPrefix)));
            }
            if (ModsConfig.IsActive("vanillaracesexpanded.highmate"))
            {
                Settings.VREHighmateActive = true;
                harmony.PatchVRE();
            }
            if (ModsConfig.IsActive("VanillaExpanded.VanillaSocialInteractionsExpanded"))
            {
                MethodInfo GetSpouseOrLoverOrFiance = Type.GetType("VanillaSocialInteractionsExpanded.VSIE_Utils, VanillaSocialInteractionsExpanded").GetMethod("GetSpouseOrLoverOrFiance");
                MethodInfo prefixDelegate = null;
                foreach (Type t in Type.GetType("VanillaSocialInteractionsExpanded.AddDirectRelation_Patch, VanillaSocialInteractionsExpanded").GetNestedTypes(AccessTools.all).Where((Type t) => t.GetCustomAttribute<System.Runtime.CompilerServices.CompilerGeneratedAttribute>() != null))
                {
                    foreach (MethodInfo method in t.GetMethods(AccessTools.all))
                    {
                        if (method.ReturnType == typeof(void) && method.Name != "Finalize")
                        {
                            if (prefixDelegate != null)
                            {
                                LogUtil.Error($"Multiple matching methods found: {prefixDelegate.Name} and {method.Name}");
                            }
                            prefixDelegate = method;
                            VSIEPatches.CompilerType = t;
                        }
                    }
                }
                harmony.Patch(GetSpouseOrLoverOrFiance, postfix: new HarmonyMethod(typeof(VSIEPatches), nameof(VSIEPatches.GetSpouseOrLoverOrFiancePostfix)));
                harmony.Patch(prefixDelegate, transpiler: new HarmonyMethod(typeof(VSIEPatches), nameof(VSIEPatches.AddDirectRelation_PrefixTranspiler)));
            }
            if (ModsConfig.IsActive("rim.job.world") || ModsConfig.IsActive("safe.job.world"))
            {
                harmony.Patch(AccessTools.DeclaredMethod(Type.GetType("rjw.xxx,RJW"), "is_asexual"), postfix: new HarmonyMethod(typeof(OtherMod_Patches), nameof(OtherMod_Patches.RJWAsexualPostfix)));
                harmony.Patch(AccessTools.DeclaredMethod(Type.GetType("rjw.CompRJW,RJW"), "VanillaTraitCheck"), transpiler: new HarmonyMethod(typeof(OtherMod_Patches), nameof(OtherMod_Patches.VanillaTraitCheckTranspiler)));
            }
            if (ModsConfig.IsActive("Neronix17.Asimov"))
            {
                Settings.AsimovActive = true;
                HelperClasses.IsHumanlikeAutomaton = AccessTools.DeclaredMethod(Type.GetType("Asimov.AutomatonUtil,Asimov"), "IsHumanlikeAutomaton");
                Type PawnDef = Type.GetType("Asimov.PawnDef,Asimov");
                HelperClasses.pawnSettings = AccessTools.Field(PawnDef, "pawnSettings");
                HelperClasses.AsimovGrowth = AccessTools.Field(Type.GetType("Asimov.PawnSettings,Asimov"), "hasGrowthMoments");
            }
            if (ModsConfig.IsActive("RforReload.EdgesOfAcceptence"))
            {
                harmony.PatchForceLoveHate();
            }

            MakeFertilityModList();
            Settings.ApplyJoySettings();
        }

        private static void MakeFertilityModList()
        {
            if (ModsConfig.BiotechActive)
            {
                Settings.FertilityMods.Add("ludeon.rimworld.biotech", "Biotech");
            }
            if (ModsConfig.IsActive("dylan.csl"))
            {
                Settings.FertilityMods.Add("dylan.csl", "Children, school and learning");
            }
            if (ModsConfig.IsActive("rim.job.world"))
            {
                Settings.FertilityMods.Add("rim.job.world", "RJW");
            }
            if (ModsConfig.IsActive("safe.job.world"))
            {
                Settings.FertilityMods.Add("safe.job.world", "SJW");
            }
            //Try to auto set if there's only one choice
            if (Settings.FertilityMods.Count == 1 && (Settings.fertilityMod == "None" || !Settings.FertilityMods.ContainsKey(Settings.fertilityMod)))
            {
                Settings.fertilityMod = Settings.FertilityMods.First().Key;
            }
            else if (!Settings.FertilityMods.ContainsKey(Settings.fertilityMod))
            {
                Settings.fertilityMod = "None";
            }
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
    }

    public class MayRequireHARAttribute : MayRequireAttribute
    {
        public MayRequireHARAttribute()
            : base("erdelf.humanoidalienraces")
        { }
    }

    public class MayRequirePersonalityM2Attribute : MayRequireAttribute
    {
        public MayRequirePersonalityM2Attribute()
            : base("hahkethomemah.simplepersonalities.module2")
        { }
    }

    public class MayRequireCSLAttribute : MayRequireAttribute
    {
        public MayRequireCSLAttribute()
            : base("dylan.csl")
        { }
    }

    public class MayRequireRJWAttribute : MayRequireAttribute
    {
        public MayRequireRJWAttribute()
            : base("rim.job.world")
        { }
    }
}