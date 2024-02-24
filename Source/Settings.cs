using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace BetterRomance
{
    public class Settings : ModSettings
    {
        public float asexualChance = 10f;
        public float bisexualChance = 50f;
        public float gayChance = 20f;
        public float straightChance = 20f;

        public float aceAroChance = 10f;
        public float aceBiChance = 50f;
        public float aceHomoChance = 20f;
        public float aceHeteroChance = 20f;

        public float dateRate = 100f;
        public float hookupRate = 100f;
        public float cheatChance = 100f;
        public float alienLoveChance = 33f;
        public int minOpinionRomance = 5;
        public int minOpinionHookup = 0;

        public static string fertilityMod = "None";
        public bool joyOnSlaves = false;
        public bool joyOnPrisoners = false;

        //These are not set by the user
        public static bool HARActive = false;
        public static bool RotRActive = false;
        public static bool ATRActive = false;
        public static bool VREHighmateActive = false;
        public static Dictionary<string, string> FertilityMods = new Dictionary<string, string>();
        public static bool debugLogging = false;
        public static bool AsimovActive;

        public static bool LoveRelationsLoaded => !CustomLoveRelationUtility.LoveRelations.EnumerableNullOrEmpty();

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref asexualChance, "asexualChance", 10.0f);
            Scribe_Values.Look(ref bisexualChance, "bisexualChance", 50.0f);
            Scribe_Values.Look(ref gayChance, "gayChance", 20.0f);
            Scribe_Values.Look(ref straightChance, "straightChance", 20.0f);

            Scribe_Values.Look(ref aceAroChance, "aceAroChance", 10.0f);
            Scribe_Values.Look(ref aceBiChance, "aceBiChance", 50.0f);
            Scribe_Values.Look(ref aceHomoChance, "aceHomoChance", 20.0f);
            Scribe_Values.Look(ref aceHeteroChance, "aceHeteroChance", 20.0f);

            Scribe_Values.Look(ref dateRate, "dateRate", 100.0f);
            Scribe_Values.Look(ref hookupRate, "hookupRate", 100.0f);
            Scribe_Values.Look(ref alienLoveChance, "alienLoveChance", 33.0f);
            Scribe_Values.Look(ref minOpinionRomance, "minOpinionRomance", 5);
            Scribe_Values.Look(ref cheatChance, "cheatChance", 100.0f);
            Scribe_Values.Look(ref minOpinionHookup, "minOpinionHookup", 0);

            Scribe_Values.Look(ref fertilityMod, "fertilityMod", "None");
            Scribe_Values.Look(ref joyOnSlaves, "joyOnSlaves", false);
            Scribe_Values.Look(ref joyOnPrisoners, "joyOnPrisoners", false);
            Scribe_Values.Look(ref debugLogging, "debugLogging", false);
        }

        public static void ApplyJoySettings()
        {
            NeedDef def = DefDatabase<NeedDef>.GetNamed("Joy");
            def.neverOnSlave = !BetterRomanceMod.settings.joyOnSlaves;
            if (BetterRomanceMod.settings.joyOnPrisoners)
            {
                def.neverOnPrisoner = false;
                def.colonistAndPrisonersOnly = true;
                def.colonistsOnly = false;
            }
            else
            {
                def.neverOnPrisoner = true;
                def.colonistAndPrisonersOnly = false;
                def.colonistsOnly = true;
            }
        }
    }

    internal class BetterRomanceMod : Mod
    {
        public static Settings settings;

        public BetterRomanceMod(ModContentPack content) : base(content)
        {

            settings = GetSettings<Settings>();
        }

        public override string SettingsCategory()
        {
            return "WBR.WayBetterRomance".Translate();
        }

        public override void WriteSettings()
        {
            base.WriteSettings();
            Settings.ApplyJoySettings();
        }

        public override void DoSettingsWindowContents(Rect canvas)
        {
            Listing_Standard list = new Listing_Standard
            {
                ColumnWidth = (canvas.width / 2f) - 17f
            };
            list.Begin(canvas);
            DrawBaseSexualityChance(list);
            list.Gap();
            if (list.ButtonText(Translator.Translate("RestoreToDefaultSettings")))
            {
                settings.asexualChance = 10f;
                settings.bisexualChance = 50f;
                settings.gayChance = 20f;
                settings.straightChance = 20f;
            }

            DrawAceOrientationChance(list);
            list.Gap();
            if (list.ButtonText(Translator.Translate("RestoreToDefaultSettings")))
            {
                settings.aceAroChance = 10f;
                settings.aceBiChance = 50f;
                settings.aceHomoChance = 20f;
                settings.aceHeteroChance = 20f;
            }

            list.NewColumn();
            DrawCustomRight(list);
            list.Gap();

            if (list.ButtonText(Translator.Translate("RestoreToDefaultSettings")))
            {
                settings.dateRate = 100f;
                settings.hookupRate = 100f;
                settings.alienLoveChance = 33f;
                settings.minOpinionRomance = 5;
                settings.cheatChance = 100f;
                settings.minOpinionHookup = 0;
            }

            list.Gap();
            if (Settings.FertilityMods.Count == 1 && (Settings.fertilityMod == "None" || !Settings.FertilityMods.ContainsKey(Settings.fertilityMod)))
            {
                Settings.fertilityMod = Settings.FertilityMods.First().Key;
            }
            else if (!Settings.FertilityMods.ContainsKey(Settings.fertilityMod))
            {
                Settings.fertilityMod = "None";
            }
            if (list.ButtonTextLabeled("Fertility Mod", Settings.fertilityMod != "None" ? Settings.FertilityMods.TryGetValue(Settings.fertilityMod) : "None"))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                foreach (KeyValuePair<string, string> item in Settings.FertilityMods)
                {
                    options.Add(new FloatMenuOption(item.Value, delegate
                    {
                        Settings.fertilityMod = item.Key;
                    }));
                }
                if (!options.NullOrEmpty())
                {
                    Find.WindowStack.Add(new FloatMenu(options));
                }
            }
            if (Settings.FertilityMods.Count == 0)
            {
                list.Label("No fertility mod detected. If you are using one, please let me know which one so I can add support for it.");
            }
            list.Label("Add joy need (reload save after changing)");
            if (ModsConfig.IdeologyActive)
            {
                list.CheckboxLabeled("Slaves", ref settings.joyOnSlaves);
            }
            list.CheckboxLabeled("Prisoners", ref settings.joyOnPrisoners);
            if (Prefs.DevMode)
            {
                list.CheckboxLabeled("Enable dev logging", ref Settings.debugLogging);
            }
            list.End();
        }

        private static float sectionHeightOrientation = 0f;
        private static float sectionHeightOther = 0f;

        private static Listing_Standard DrawCustomSectionStart(Listing_Standard listing, float height, string label, string tooltip = null)
        {
            listing.Gap();
            listing.Label(label, -1f, tooltip);
            Listing_Standard listing_Standard = listing.BeginSection(height, 8f, 6f);
            listing_Standard.maxOneColumn = true;
            return listing_Standard;
        }

        private static void DrawCustomSectionEnd(Listing_Standard listing, Listing_Standard section, out float height)
        {
            listing.EndSection(section);
            height = section.CurHeight;
        }

        private static void DrawBaseSexualityChance(Listing_Standard listing)
        {
            Listing_Standard list = DrawCustomSectionStart(listing, sectionHeightOrientation, "WBR.OrentationHeading".Translate(), tooltip: "WBR.OrentationHeadingTip".Translate());
            list.Label("WBR.StraightChance".Translate() + "  " + (int)settings.straightChance + "%", tooltip: "WBR.StraightChanceTip".Translate());
            settings.straightChance = list.Slider(settings.straightChance, 0f, 100.99f);
            if (settings.straightChance > 100.99f - settings.bisexualChance - settings.gayChance)
            {
                settings.straightChance = 100.99f - settings.bisexualChance - settings.gayChance;
            }
            list.Label("WBR.BisexualChance".Translate() + "  " + (int)settings.bisexualChance + "%", tooltip: "WBR.BisexualChanceTip".Translate());
            settings.bisexualChance = list.Slider(settings.bisexualChance, 0f, 100.99f);
            if (settings.bisexualChance > 100.99f - settings.straightChance - settings.gayChance)
            {
                settings.bisexualChance = 100.99f - settings.straightChance - settings.gayChance;
            }
            list.Label("WBR.GayChance".Translate() + "  " + (int)settings.gayChance + "%", tooltip: "WBR.GayChanceTip".Translate());
            settings.gayChance = list.Slider(settings.gayChance, 0f, 100.99f);
            if (settings.gayChance > 100.99f - settings.straightChance - settings.bisexualChance)
            {
                settings.gayChance = 100.99f - settings.straightChance - settings.bisexualChance;
            }
            settings.asexualChance = 100 - (int)settings.straightChance - (int)settings.bisexualChance - (int)settings.gayChance;
            list.Label("WBR.AsexualChance".Translate() + "  " + settings.asexualChance + "%", tooltip: "WBR.AsexualChanceTip".Translate());
            DrawCustomSectionEnd(listing, list, out sectionHeightOrientation);
        }

        private static void DrawAceOrientationChance(Listing_Standard listing)
        {
            Listing_Standard list = DrawCustomSectionStart(listing, sectionHeightOrientation, "WBR.AceOrentationHeading".Translate(), tooltip: "WBR.AceOrentationHeadingTip".Translate());
            list.Label("WBR.AceHeteroChance".Translate() + "  " + (int)settings.aceHeteroChance + "%", tooltip: "WBR.AceHeteroChanceTip".Translate());
            settings.aceHeteroChance = list.Slider(settings.aceHeteroChance, 0f, 100.99f);
            if (settings.aceHeteroChance > 100.99f - settings.aceBiChance - settings.aceHomoChance)
            {
                settings.aceHeteroChance = 100.99f - settings.aceBiChance - settings.aceHomoChance;
            }
            list.Label("WBR.AceBiChance".Translate() + "  " + (int)settings.aceBiChance + "%", tooltip: "WBR.AceBiChanceTip".Translate());
            settings.aceBiChance = list.Slider(settings.aceBiChance, 0f, 100.99f);
            if (settings.aceBiChance > 100.99f - settings.aceHeteroChance - settings.aceHomoChance)
            {
                settings.aceBiChance = 100.99f - settings.aceHeteroChance - settings.aceHomoChance;
            }
            list.Label("WBR.AceHomoChance".Translate() + "  " + (int)settings.aceHomoChance + "%", tooltip: "WBR.AceHomoChanceTip".Translate());
            settings.aceHomoChance = list.Slider(settings.aceHomoChance, 0f, 100.99f);
            if (settings.aceHomoChance > 100.99f - settings.aceHeteroChance - settings.aceBiChance)
            {
                settings.aceHomoChance = 100.99f - settings.aceHeteroChance - settings.aceBiChance;
            }
            settings.aceAroChance = 100 - (int)settings.aceHeteroChance - (int)settings.aceBiChance - (int)settings.aceHomoChance;
            list.Label("WBR.AceAroChance".Translate() + "  " + settings.aceAroChance + "%", tooltip: "WBR.AceAroChanceTip".Translate());
            DrawCustomSectionEnd(listing, list, out sectionHeightOrientation);
        }

        private static void DrawCustomRight(Listing_Standard listing)
        {
            Listing_Standard list = DrawCustomSectionStart(listing, sectionHeightOther, "WBR.OtherHeading".Translate());
            list.Label("WBR.DateRate".Translate() + "  " + (int)settings.dateRate + "%", tooltip: "WBR.DateRateTip".Translate());
            settings.dateRate = list.Slider(settings.dateRate, 0f, 200.99f);
            list.Label("WBR.HookupRate".Translate() + "  " + (int)settings.hookupRate + "%", tooltip: "WBR.HookupRateTip".Translate());
            settings.hookupRate = list.Slider(settings.hookupRate, 0f, 200.99f);
            list.Label("WBR.CheatChance".Translate() + "  " + (int)settings.cheatChance + "%", tooltip: "WBR.CheatChanceTip".Translate());
            settings.cheatChance = list.Slider(settings.cheatChance, 0f, 200.99f);
            list.Label("WBR.AlienLoveChance".Translate() + "  " + (int)settings.alienLoveChance + "%", tooltip: "WBR.AlienLoveChanceTip".Translate());
            settings.alienLoveChance = list.Slider(settings.alienLoveChance, 0f, 100.99f);
            list.Label("WBR.MinOpinionRomance".Translate() + " " + settings.minOpinionRomance, tooltip: "WBR.MinOpinionRomanceTip".Translate());
            settings.minOpinionRomance = Mathf.RoundToInt(list.Slider(settings.minOpinionRomance, -100f, 100f));
            list.Label("WBR.MinOpinionHookup".Translate() + " " + settings.minOpinionHookup, tooltip: "WBR.MinOpinionHookupTip".Translate());
            settings.minOpinionHookup = Mathf.RoundToInt(list.Slider(settings.minOpinionHookup, -100f, 50f));
            DrawCustomSectionEnd(listing, list, out sectionHeightOther);
        }
    }
}