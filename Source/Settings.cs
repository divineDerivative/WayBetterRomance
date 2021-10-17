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
        public float dateRate = 100f;
        public float hookupRate = 100f;
        public float alienLoveChance = 33f;
        public float minOpinionRomance = 5f;

        //These are not set by the user
        public static bool HARActive = false;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref asexualChance, "asexualChance", 10.0f);
            Scribe_Values.Look(ref bisexualChance, "bisexualChance", 50.0f);
            Scribe_Values.Look(ref gayChance, "gayChance", 20.0f);
            Scribe_Values.Look(ref straightChance, "straightChance", 20.0f);
            Scribe_Values.Look(ref dateRate, "dateRate", 100.0f);
            Scribe_Values.Look(ref hookupRate, "hookupRate", 100.0f);
            Scribe_Values.Look(ref alienLoveChance, "alienLoveChance", 33.0f);
            Scribe_Values.Look(ref minOpinionRomance, "minOpinionRomance", 5.0f);
            
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
                ColumnWidth = canvas.width
            };
            list.Begin(canvas);
            list.Gap();
            Text.Font = GameFont.Tiny;
            list.Label("WBR.Overview".Translate());
            Text.Font = GameFont.Small;
            list.Gap();
            list.Label("WBR.StraightChance".Translate() + "  " + (int)settings.straightChance + "%");
            settings.straightChance = list.Slider(settings.straightChance, 0f, 100.99f);
            if (settings.straightChance > 100.99f - settings.bisexualChance - settings.gayChance)
            {
                settings.straightChance = 100.99f - settings.bisexualChance - settings.gayChance;
            }
            list.Gap();
            list.Label("WBR.BisexualChance".Translate() + "  " + (int)settings.bisexualChance + "%");
            settings.bisexualChance = list.Slider(settings.bisexualChance, 0f, 100.99f);
            if (settings.bisexualChance > 100.99f - settings.straightChance - settings.gayChance)
            {
                settings.bisexualChance = 100.99f - settings.straightChance - settings.gayChance;
            }
            list.Gap();
            list.Label("WBR.GayChance".Translate() + "  " + (int)settings.gayChance + "%");
            settings.gayChance = list.Slider(settings.gayChance, 0f, 100.99f);
            if (settings.gayChance > 100.99f - settings.straightChance - settings.bisexualChance)
            {
                settings.gayChance = 100.99f - settings.straightChance - settings.bisexualChance;
            }

            list.Gap();
            settings.asexualChance = 100 - (int)settings.straightChance - (int)settings.bisexualChance - (int)settings.gayChance;
            list.Label("WBR.AsexualChance".Translate() + "  " + settings.asexualChance + "%");
            list.Gap(24);
            list.Label("WBR.DateRate".Translate() + "  " + (int)settings.dateRate + "%");
            settings.dateRate = list.Slider(settings.dateRate, 0f, 1000.99f);
            list.Gap();
            list.Label("WBR.HookupRate".Translate() + "  " + (int)settings.hookupRate + "%");
            settings.hookupRate = list.Slider(settings.hookupRate, 0f, 1000.99f);
            list.Gap();
            list.Label("WBR.AlienLoveChance".Translate() + "  " + (int)settings.alienLoveChance + "%", tooltip: "WBR.AlienLoveChanceTip".Translate());
            settings.alienLoveChance = list.Slider(settings.alienLoveChance, 0f, 100.99f);
            list.Gap();
            list.Label("WBR.MinOpinionRomance".Translate() + " " + (int)settings.minOpinionRomance, tooltip: "WBR.MinOpinionRomanceTip".Translate());
            settings.minOpinionRomance = list.Slider(settings.minOpinionRomance, -100.99f, 100.99f);

            if (list.ButtonText(Translator.Translate("RestoreToDefaultSettings")))
            {
                settings.asexualChance = 10f;
                settings.bisexualChance = 50f;
                settings.gayChance = 20f;
                settings.straightChance = 20f;
                settings.dateRate = 100f;
                settings.hookupRate = 100f;
                settings.alienLoveChance = 33f;
                settings.minOpinionRomance = 5f;
            }
            list.End();
        }
    }
}