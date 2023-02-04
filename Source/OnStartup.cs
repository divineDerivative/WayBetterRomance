using HarmonyLib;
using Verse;
using RimWorld;
using System.Linq;

namespace BetterRomance
{
    [StaticConstructorOnStartup]
    public static class OnStartup
    {
        static OnStartup()
        {
            //I can simplify these
            SettingsUtilities.LoveRelations = SettingsUtilities.AdditionalLoveRelations();
            SettingsUtilities.ExLoveRelations = SettingsUtilities.AdditionalExLoveRelations();
            SettingsUtilities.MakeTraitList();

            if (ModsConfig.IsActive("erdelf.humanoidalienraces") || ModsConfig.IsActive("erdelf.humanoidalienraces.dev"))
            {
                Settings.HARActive = true;
                HARPatches.PatchHAR();
            }
            Harmony harmony = new Harmony(id: "rimworld.divineDerivative.romance");
            harmony.PatchAll();
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
            //Try to auto set if there's only one choice
            if (Settings.FertilityMods.Count == 1 && (BetterRomanceMod.settings.fertilityMod == "None" || !Settings.FertilityMods.ContainsKey(BetterRomanceMod.settings.fertilityMod)))
            {
                BetterRomanceMod.settings.fertilityMod = Settings.FertilityMods.First().Key;
            }
            else if (!Settings.FertilityMods.ContainsKey(BetterRomanceMod.settings.fertilityMod))
            {
                BetterRomanceMod.settings.fertilityMod = "None";
            }
        }
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