using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace BetterRomance
{
    public class Settings : ModSettings
    {
        public OrientationChances sexualOrientations = new OrientationChances
        {
            hetero = 20f,
            homo = 20f,
            bi = 50f,
            none = 10f,
        };
        public OrientationChances asexualOrientations = new OrientationChances
        {
            hetero = 20f,
            homo = 20f,
            bi = 50f,
            none = 10f,
        };

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
        public static List<RaceSettings> RaceSettingsList = new List<RaceSettings>();

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref sexualOrientations.none, "asexualChance", 10.0f);
            Scribe_Values.Look(ref sexualOrientations.bi, "bisexualChance", 50.0f);
            Scribe_Values.Look(ref sexualOrientations.homo, "gayChance", 20.0f);
            Scribe_Values.Look(ref sexualOrientations.hetero, "straightChance", 20.0f);

            Scribe_Values.Look(ref asexualOrientations.none, "aceAroChance", 10.0f);
            Scribe_Values.Look(ref asexualOrientations.bi, "aceBiChance", 50.0f);
            Scribe_Values.Look(ref asexualOrientations.homo, "aceHomoChance", 20.0f);
            Scribe_Values.Look(ref asexualOrientations.hetero, "aceHeteroChance", 20.0f);

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
                settings.sexualOrientations.none = 10f;
                settings.sexualOrientations.bi = 50f;
                settings.sexualOrientations.homo = 20f;
                settings.sexualOrientations.hetero = 20f;
            }

            DrawAceOrientationChance(list);
            list.Gap();
            if (list.ButtonText(Translator.Translate("RestoreToDefaultSettings")))
            {
                settings.asexualOrientations.none = 10f;
                settings.asexualOrientations.bi = 50f;
                settings.asexualOrientations.homo = 20f;
                settings.asexualOrientations.hetero = 20f;
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
            list.Label("WBR.StraightChance".Translate() + "  " + (int)settings.sexualOrientations.hetero + "%", tooltip: "WBR.StraightChanceTip".Translate());
            settings.sexualOrientations.hetero = list.Slider(settings.sexualOrientations.hetero, 0f, 100.99f);
            if (settings.sexualOrientations.hetero > 100.99f - settings.sexualOrientations.bi - settings.sexualOrientations.homo)
            {
                settings.sexualOrientations.hetero = 100.99f - settings.sexualOrientations.bi - settings.sexualOrientations.homo;
            }
            list.Label("WBR.BisexualChance".Translate() + "  " + (int)settings.sexualOrientations.bi + "%", tooltip: "WBR.BisexualChanceTip".Translate());
            settings.sexualOrientations.bi = list.Slider(settings.sexualOrientations.bi, 0f, 100.99f);
            if (settings.sexualOrientations.bi > 100.99f - settings.sexualOrientations.hetero - settings.sexualOrientations.homo)
            {
                settings.sexualOrientations.bi = 100.99f - settings.sexualOrientations.hetero - settings.sexualOrientations.homo;
            }
            list.Label("WBR.GayChance".Translate() + "  " + (int)settings.sexualOrientations.homo + "%", tooltip: "WBR.GayChanceTip".Translate());
            settings.sexualOrientations.homo = list.Slider(settings.sexualOrientations.homo, 0f, 100.99f);
            if (settings.sexualOrientations.homo > 100.99f - settings.sexualOrientations.hetero - settings.sexualOrientations.bi)
            {
                settings.sexualOrientations.homo = 100.99f - settings.sexualOrientations.hetero - settings.sexualOrientations.bi;
            }
            settings.sexualOrientations.none = 100 - (int)settings.sexualOrientations.hetero - (int)settings.sexualOrientations.bi - (int)settings.sexualOrientations.homo;
            list.Label("WBR.AsexualChance".Translate() + "  " + settings.sexualOrientations.none + "%", tooltip: "WBR.AsexualChanceTip".Translate());
            DrawCustomSectionEnd(listing, list, out sectionHeightOrientation);
        }

        private static void DrawAceOrientationChance(Listing_Standard listing)
        {
            Listing_Standard list = DrawCustomSectionStart(listing, sectionHeightOrientation, "WBR.AceOrentationHeading".Translate(), tooltip: "WBR.AceOrentationHeadingTip".Translate());
            list.Label("WBR.AceHeteroChance".Translate() + "  " + (int)settings.asexualOrientations.hetero + "%", tooltip: "WBR.AceHeteroChanceTip".Translate());
            settings.asexualOrientations.hetero = list.Slider(settings.asexualOrientations.hetero, 0f, 100.99f);
            if (settings.asexualOrientations.hetero > 100.99f - settings.asexualOrientations.bi - settings.asexualOrientations.homo)
            {
                settings.asexualOrientations.hetero = 100.99f - settings.asexualOrientations.bi - settings.asexualOrientations.homo;
            }
            list.Label("WBR.AceBiChance".Translate() + "  " + (int)settings.asexualOrientations.bi + "%", tooltip: "WBR.AceBiChanceTip".Translate());
            settings.asexualOrientations.bi = list.Slider(settings.asexualOrientations.bi, 0f, 100.99f);
            if (settings.asexualOrientations.bi > 100.99f - settings.asexualOrientations.hetero - settings.asexualOrientations.homo)
            {
                settings.asexualOrientations.bi = 100.99f - settings.asexualOrientations.hetero - settings.asexualOrientations.homo;
            }
            list.Label("WBR.AceHomoChance".Translate() + "  " + (int)settings.asexualOrientations.homo + "%", tooltip: "WBR.AceHomoChanceTip".Translate());
            settings.asexualOrientations.homo = list.Slider(settings.asexualOrientations.homo, 0f, 100.99f);
            if (settings.asexualOrientations.homo > 100.99f - settings.asexualOrientations.hetero - settings.asexualOrientations.bi)
            {
                settings.asexualOrientations.homo = 100.99f - settings.asexualOrientations.hetero - settings.asexualOrientations.bi;
            }
            settings.asexualOrientations.none = 100 - (int)settings.asexualOrientations.hetero - (int)settings.asexualOrientations.bi - (int)settings.asexualOrientations.homo;
            list.Label("WBR.AceAroChance".Translate() + "  " + settings.asexualOrientations.none + "%", tooltip: "WBR.AceAroChanceTip".Translate());
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