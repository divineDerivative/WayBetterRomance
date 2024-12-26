using DivineFramework;
using DivineFramework.UI;
using LudeonTK;
using RimWorld;
using UnityEngine;
using Verse;

namespace BetterRomance
{
    internal class BetterRomanceMod : Mod
    {
        public static Settings settings;

        public BetterRomanceMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<Settings>();
            ModManagement.RegisterMod("WBR.WayBetterRomance", new(FrameworkVersionInfo.Version));
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

        private Vector2 scrollPos;
        private static float scrollViewHeight = 0f;
        [TweakValue("WBR", 0f, 100f)]
        static float padding = 4f;

        public override void DoSettingsWindowContents(Rect canvas)
        {
            Listing_ScrollView outerList = new();
            Listing_Standard list = new();
            Settings.AutoDetectFertilityMod();
            settings.regularHandler ??= new(true, settings.SetUpRegularHandler);

            if (!settings.regularHandler.Initialized)
            {
                settings.regularHandler.SetUp();
            }
            if (settings.complex)
            {
                //Need to figure out where to display the complex chance slider and warning
                settings.complexHandler ??= new(settings.SetUpComplexHandler);
                settings.complexHandler.DrawTabs(canvas, out Rect innerRect);
                list = outerList.BeginScrollView(innerRect, scrollViewHeight, ref settings.complexHandler.CurTab.scrollPos);
                list.ColumnWidth = (innerRect.width / 2f) - GenUI.ScrollBarWidth - padding;
                settings.complexHandler.DrawContents(list);
            }
            else
            {
                list = outerList.BeginScrollView(canvas, scrollViewHeight, ref scrollPos);
                list.ColumnWidth = (canvas.width / 2f) - GenUI.ScrollBarWidth - padding;
                settings.regularHandler.Draw(list);
            }

            scrollViewHeight = list.MaxColumnHeightSeen;
            outerList.End();
        }
    }
    
    internal class WBRLogger : Logging
    {
        public static readonly WBRLogger LogUtil = new WBRLogger();
        private WBRLogger() : base("<color=#1116e4>[WayBetterRomance]</color>", () => Settings.debugLogging) { }
    }
}