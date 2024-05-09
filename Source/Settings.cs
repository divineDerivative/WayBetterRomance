using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace BetterRomance
{
    public class Settings : ModSettings
    {
        public OrientationChances sexualOrientations = new()
        {
            hetero = 20f,
            homo = 20f,
            bi = 50f,
            none = 10f,
        };
        public OrientationChances asexualOrientations = new()
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
        public static Dictionary<string, string> FertilityMods = new();
        public static bool debugLogging = false;
        public static bool AsimovActive;

        public static bool LoveRelationsLoaded => !CustomLoveRelationUtility.LoveRelations.EnumerableNullOrEmpty();
        public static List<RaceSettings> RaceSettingsList = new();

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
            Listing_Standard list = new()
            {
                ColumnWidth = (canvas.width / 2f) - 17f
            };
            list.Begin(canvas);
            DrawBaseSexualityChance(list);
            list.Gap();
            TwoButtonText(list, "WBR.MatchBelowButton".Translate(), delegate { settings.sexualOrientations = settings.asexualOrientations.Copy; }, "RestoreToDefaultSettings".Translate(), delegate
            { settings.sexualOrientations.Reset(); });

            DrawAceOrientationChance(list);
            list.Gap();
            TwoButtonText(list, "WBR.MatchAboveButton".Translate(), delegate { settings.asexualOrientations = settings.sexualOrientations.Copy; }, "RestoreToDefaultSettings".Translate(), delegate
            { settings.asexualOrientations.Reset(); });

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
            DrawRightMisc(list);

            list.End();
        }

        public void TwoButtonText(Listing_Standard listing, string firstLabel, Action firstAction, string secondLabel, Action secondAction)
        {
            Rect firstRect = listing.GetRect(30f, 0.5f);
            Rect secondRect = new(firstRect);
            secondRect.x += secondRect.width;
            bool firstResult = false;
            if (!listing.BoundingRectCached.HasValue || firstRect.Overlaps(listing.BoundingRectCached.Value))
            {
                firstResult = Widgets.ButtonText(firstRect, firstLabel);
            }
            if (firstResult)
            {
                firstAction.Invoke();
            }
            bool secondResult = false;
            if (!listing.BoundingRectCached.HasValue || secondRect.Overlaps(listing.BoundingRectCached.Value))
            {
                secondResult = Widgets.ButtonText(secondRect, secondLabel);
            }
            if (secondResult)
            {
                secondAction.Invoke();
            }
            listing.Gap(listing.verticalSpacing);
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
        //I wonder if I can combine these into one method that can act on the different orientation objects?
        private static void DrawBaseSexualityChance(Listing_Standard listing)
        {
            Listing_Standard list = DrawCustomSectionStart(listing, sectionHeightOrientation, "WBR.OrentationHeading".Translate(), tooltip: "WBR.OrentationHeadingTip".Translate());
            list.Label("WBR.StraightChance".Translate() + "  " + settings.sexualOrientations.hetero + "%", tooltip: "WBR.StraightChanceTip".Translate());
            settings.sexualOrientations.hetero = Mathf.Round(list.Slider(settings.sexualOrientations.hetero, 0f, 100f));
            if (settings.sexualOrientations.hetero > 100f - settings.sexualOrientations.bi - settings.sexualOrientations.homo)
            {
                settings.sexualOrientations.hetero = 100f - settings.sexualOrientations.bi - settings.sexualOrientations.homo;
            }
            list.Label("WBR.BisexualChance".Translate() + "  " + settings.sexualOrientations.bi + "%", tooltip: "WBR.BisexualChanceTip".Translate());
            settings.sexualOrientations.bi = Mathf.Round(list.Slider(settings.sexualOrientations.bi, 0f, 100f));
            if (settings.sexualOrientations.bi > 100f - settings.sexualOrientations.hetero - settings.sexualOrientations.homo)
            {
                settings.sexualOrientations.bi = 100f - settings.sexualOrientations.hetero - settings.sexualOrientations.homo;
            }
            list.Label("WBR.GayChance".Translate() + "  " + settings.sexualOrientations.homo + "%", tooltip: "WBR.GayChanceTip".Translate());
            settings.sexualOrientations.homo = Mathf.Round(list.Slider(settings.sexualOrientations.homo, 0f, 100f));
            if (settings.sexualOrientations.homo > 100f - settings.sexualOrientations.hetero - settings.sexualOrientations.bi)
            {
                settings.sexualOrientations.homo = 100f - settings.sexualOrientations.hetero - settings.sexualOrientations.bi;
            }
            settings.sexualOrientations.none = 100f - settings.sexualOrientations.hetero - settings.sexualOrientations.bi - settings.sexualOrientations.homo;
            list.Label("WBR.AsexualChance".Translate() + "  " + settings.sexualOrientations.none + "%", tooltip: "WBR.AsexualChanceTip".Translate());
            DrawCustomSectionEnd(listing, list, out sectionHeightOrientation);
        }

        private static void DrawAceOrientationChance(Listing_Standard listing)
        {
            Listing_Standard list = DrawCustomSectionStart(listing, sectionHeightOrientation, "WBR.AceOrentationHeading".Translate(), tooltip: "WBR.AceOrentationHeadingTip".Translate());
            list.Label("WBR.AceHeteroChance".Translate() + "  " + settings.asexualOrientations.hetero + "%", tooltip: "WBR.AceHeteroChanceTip".Translate());
            settings.asexualOrientations.hetero = Mathf.Round(list.Slider(settings.asexualOrientations.hetero, 0f, 100f));
            if (settings.asexualOrientations.hetero > 100f - settings.asexualOrientations.bi - settings.asexualOrientations.homo)
            {
                settings.asexualOrientations.hetero = 100f - settings.asexualOrientations.bi - settings.asexualOrientations.homo;
            }
            list.Label("WBR.AceBiChance".Translate() + "  " + settings.asexualOrientations.bi + "%", tooltip: "WBR.AceBiChanceTip".Translate());
            settings.asexualOrientations.bi = Mathf.Round(list.Slider(settings.asexualOrientations.bi, 0f, 100f));
            if (settings.asexualOrientations.bi > 100f - settings.asexualOrientations.hetero - settings.asexualOrientations.homo)
            {
                settings.asexualOrientations.bi = 100f - settings.asexualOrientations.hetero - settings.asexualOrientations.homo;
            }
            list.Label("WBR.AceHomoChance".Translate() + "  " + settings.asexualOrientations.homo + "%", tooltip: "WBR.AceHomoChanceTip".Translate());
            settings.asexualOrientations.homo = Mathf.Round(list.Slider(settings.asexualOrientations.homo, 0f, 100f));
            if (settings.asexualOrientations.homo > 100f - settings.asexualOrientations.hetero - settings.asexualOrientations.bi)
            {
                settings.asexualOrientations.homo = 100f - settings.asexualOrientations.hetero - settings.asexualOrientations.bi;
            }
            settings.asexualOrientations.none = 100 - settings.asexualOrientations.hetero - settings.asexualOrientations.bi - settings.asexualOrientations.homo;
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
            settings.alienLoveChance = list.Slider(settings.alienLoveChance, 0f, 100f);
            list.Label("WBR.MinOpinionRomance".Translate() + " " + settings.minOpinionRomance, tooltip: "WBR.MinOpinionRomanceTip".Translate());
            settings.minOpinionRomance = Mathf.RoundToInt(list.Slider(settings.minOpinionRomance, -100f, 100f));
            list.Label("WBR.MinOpinionHookup".Translate() + " " + settings.minOpinionHookup, tooltip: "WBR.MinOpinionHookupTip".Translate());
            settings.minOpinionHookup = Mathf.RoundToInt(list.Slider(settings.minOpinionHookup, -100f, 50f));
            DrawCustomSectionEnd(listing, list, out sectionHeightOther);
        }

        private static void DrawRightMisc(Listing_Standard list)
        {
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
                List<FloatMenuOption> options = new();
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
        }
    }
}