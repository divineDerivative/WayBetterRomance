using System.Collections.Generic;
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
        public float minOpinionRomance = 5f;
        public float minOpinionHookup = 5f;

        //These are not set by the user
        public static bool HARActive = false;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref asexualChance, "asexualChance", 10.0f);
            Scribe_Values.Look(ref bisexualChance, "bisexualChance", 50.0f);
            Scribe_Values.Look(ref gayChance, "gayChance", 20.0f);
            Scribe_Values.Look(ref straightChance, "straightChance", 20.0f);

            Scribe_Values.Look(ref asexualChance, "aceAroChance", 10.0f);
            Scribe_Values.Look(ref bisexualChance, "acebiChance", 50.0f);
            Scribe_Values.Look(ref gayChance, "aceHomoChance", 20.0f);
            Scribe_Values.Look(ref straightChance, "aceHeteroChance", 20.0f);

            Scribe_Values.Look(ref dateRate, "dateRate", 100.0f);
            Scribe_Values.Look(ref hookupRate, "hookupRate", 100.0f);
            Scribe_Values.Look(ref alienLoveChance, "alienLoveChance", 33.0f);
            Scribe_Values.Look(ref minOpinionRomance, "minOpinionRomance", 5.0f);
            Scribe_Values.Look(ref cheatChance, "cheatChance", 100.0f);
            Scribe_Values.Look(ref minOpinionHookup, "minOpinionHookup", 0f);
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
        }

        public override void DoSettingsWindowContents(Rect canvas)
        {
            Listing_Standard list = new Listing_Standard
            {
                ColumnWidth = canvas.width / 2f - 17f
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
                settings.minOpinionRomance = 5f;
                settings.cheatChance = 100f;
                settings.minOpinionHookup = 0f;
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
            list.Label("WBR.MinOpinionRomance".Translate() + " " + (int)settings.minOpinionRomance, tooltip: "WBR.MinOpinionRomanceTip".Translate());
            settings.minOpinionRomance = list.Slider(settings.minOpinionRomance, -100.99f, 100.99f);
            list.Label("WBR.MinOpinionHookup".Translate() + " " + (int)settings.minOpinionHookup, tooltip: "WBR.MinOpinionHookupTip".Translate());
            settings.minOpinionHookup = list.Slider(settings.minOpinionHookup, -100.99f, 50.99f);
            DrawCustomSectionEnd(listing, list, out sectionHeightOther);
        }
    }
}