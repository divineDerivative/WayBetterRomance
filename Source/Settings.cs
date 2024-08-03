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
        public OrientationChances romanticOrientations = new()
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
        public IntRange cheatingOpinion = new(-75, 75);

        public static string fertilityMod = "None";
        public bool joyOnSlaves = false;
        public bool joyOnPrisoners = false;
        public bool complex = false;

        //These are not set by the user
        public static bool HARActive = false;
        public static bool RotRActive = false;
        public static bool ATRActive = false;
        public static bool VREHighmateActive = false;
        public static bool NonBinaryActive = false;
        public static Dictionary<string, string> FertilityMods = new();
        public static bool debugLogging = false;
        public static bool AsimovActive;
        public static bool PawnmorpherActive;

        public static bool LoveRelationsLoaded => !CustomLoveRelationUtility.LoveRelations.EnumerableNullOrEmpty();
        public static List<RaceSettings> RaceSettingsList = new();

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref sexualOrientations.none, "asexualChance", 10.0f);
            Scribe_Values.Look(ref sexualOrientations.bi, "bisexualChance", 50.0f);
            Scribe_Values.Look(ref sexualOrientations.homo, "gayChance", 20.0f);
            Scribe_Values.Look(ref sexualOrientations.hetero, "straightChance", 20.0f);

            Scribe_Values.Look(ref romanticOrientations.none, "aromanticChance", 10.0f);
            Scribe_Values.Look(ref romanticOrientations.bi, "biromanticChance", 50.0f);
            Scribe_Values.Look(ref romanticOrientations.homo, "homoromanticChance", 20.0f);
            Scribe_Values.Look(ref romanticOrientations.hetero, "heteroromanticChance", 20.0f);

            Scribe_Values.Look(ref dateRate, "dateRate", 100.0f);
            Scribe_Values.Look(ref hookupRate, "hookupRate", 100.0f);
            Scribe_Values.Look(ref alienLoveChance, "alienLoveChance", 33.0f);
            Scribe_Values.Look(ref minOpinionRomance, "minOpinionRomance", 5);
            Scribe_Values.Look(ref cheatChance, "cheatChance", 100.0f);
            Scribe_Values.Look(ref minOpinionHookup, "minOpinionHookup", 0);
            Scribe_Values.Look(ref cheatingOpinion, "cheatingOpinion", new(-75, 75));

            Scribe_Values.Look(ref fertilityMod, "fertilityMod", "None");
            Scribe_Values.Look(ref joyOnSlaves, "joyOnSlaves", false);
            Scribe_Values.Look(ref joyOnPrisoners, "joyOnPrisoners", false);
            Scribe_Values.Look(ref debugLogging, "debugLogging", false);
            Scribe_Values.Look(ref complex, "compexOrientations", false);
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

        //Try to auto set if there's only one choice
        public static void AutoDetectFertilityMod()
        {
            if (FertilityMods.Count == 1 && (fertilityMod == "None" || !FertilityMods.ContainsKey(fertilityMod)))
            {
                fertilityMod = FertilityMods.First().Key;
            }
            else if (!FertilityMods.ContainsKey(fertilityMod))
            {
                fertilityMod = "None";
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

        Vector2 scrollPos;
        const float scrollListPadding = 20f;
        public override void DoSettingsWindowContents(Rect canvas)
        {
            Listing_Standard list = new();
            list.Begin(canvas);

            Rect outRect = new(0f, 0f, canvas.width - 4f, canvas.height);
            float height = (scrollViewHeight == 0f) ? canvas.height * 2 : scrollViewHeight;
            Rect viewRect = new(0f, 0f, canvas.width - scrollListPadding, height);

            Widgets.BeginScrollView(outRect, ref scrollPos, viewRect, true);
            Listing_Standard scrollList = new(outRect, () => scrollPos)
            {
                ColumnWidth = (outRect.width / 2f) - 17f
            };
            scrollList.Begin(viewRect);

            if (settings.complex)
            {
                DrawSexualOrientationChance(scrollList);
                scrollList.Gap();
                TwoButtonText(scrollList, "WBR.MatchBelowButton".Translate(), delegate { settings.sexualOrientations = settings.romanticOrientations.Copy; }, "RestoreToDefaultSettings".Translate(), delegate
                { settings.sexualOrientations.Reset(); });
                scrollList.Gap();

                DrawRomanticOrientationChance(scrollList);
                scrollList.Gap();
                TwoButtonText(scrollList, "WBR.MatchAboveButton".Translate(), delegate { settings.romanticOrientations = settings.sexualOrientations.Copy; }, "RestoreToDefaultSettings".Translate(), delegate
                { settings.romanticOrientations.Reset(); });
                scrollList.Gap();

                DrawExtraStuff(scrollList);
            }
            else
            {
                DrawBaseSexualityChance(scrollList);
                scrollList.Gap();
                TwoButtonText(scrollList, "WBR.MatchBelowButton".Translate(), delegate { settings.sexualOrientations = settings.romanticOrientations.Copy; }, "RestoreToDefaultSettings".Translate(), delegate
                { settings.sexualOrientations.Reset(); });
                scrollList.Gap();

                DrawAceOrientationChance(scrollList);
                scrollList.Gap();
                TwoButtonText(scrollList, "WBR.MatchAboveButton".Translate(), delegate { settings.romanticOrientations = settings.sexualOrientations.Copy; }, "RestoreToDefaultSettings".Translate(), delegate
                { settings.romanticOrientations.Reset(); });
            }
            scrollList.Gap();
            if (scrollList.ButtonText(settings.complex ? "Simplify it" : "Let's make it complicated"))
            {
                settings.complex = !settings.complex;
                scrollPos = Vector2.zero;
                scrollViewHeight = -1f;
            }
            scrollList.NewColumn();

            DrawCustomRight(scrollList);
            scrollList.Gap();
            if (scrollList.ButtonText(Translator.Translate("RestoreToDefaultSettings")))
            {
                settings.dateRate = 100f;
                settings.hookupRate = 100f;
                settings.alienLoveChance = 33f;
                settings.minOpinionRomance = 5;
                settings.cheatChance = 100f;
                settings.minOpinionHookup = 0;
                settings.cheatingOpinion = new(-75, 75);
            }

            scrollList.Gap();
            DrawRightMisc(scrollList);

            if (scrollViewHeight == 0f)
            {
                scrollViewHeight = height;
            }
            else if (scrollViewHeight == -1f)
            {
                scrollViewHeight = 0f;
            }
            else
            {
                scrollViewHeight = scrollList.MaxColumnHeightSeen;
            }
            scrollList.End();
            Widgets.EndScrollView();
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
        private static float sectionHeightComplex = 0f;
        private static float scrollViewHeight = 0f;

        private static Listing_Standard DrawCustomSectionStart(Listing_Standard listing, float height, string label, string tooltip = null)
        {
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
            list.Label("WBR.HeteroromanticChance".Translate() + "  " + settings.romanticOrientations.hetero + "%", tooltip: "WBR.HeteroromanticChanceTip".Translate());
            settings.romanticOrientations.hetero = Mathf.Round(list.Slider(settings.romanticOrientations.hetero, 0f, 100f));
            if (settings.romanticOrientations.hetero > 100f - settings.romanticOrientations.bi - settings.romanticOrientations.homo)
            {
                settings.romanticOrientations.hetero = 100f - settings.romanticOrientations.bi - settings.romanticOrientations.homo;
            }
            list.Label("WBR.BiromanticChance".Translate() + "  " + settings.romanticOrientations.bi + "%", tooltip: "WBR.BiromanticChanceTip".Translate());
            settings.romanticOrientations.bi = Mathf.Round(list.Slider(settings.romanticOrientations.bi, 0f, 100f));
            if (settings.romanticOrientations.bi > 100f - settings.romanticOrientations.hetero - settings.romanticOrientations.homo)
            {
                settings.romanticOrientations.bi = 100f - settings.romanticOrientations.hetero - settings.romanticOrientations.homo;
            }
            list.Label("WBR.HomoromanticChance".Translate() + "  " + settings.romanticOrientations.homo + "%", tooltip: "WBR.HomoromanticChanceTip".Translate());
            settings.romanticOrientations.homo = Mathf.Round(list.Slider(settings.romanticOrientations.homo, 0f, 100f));
            if (settings.romanticOrientations.homo > 100f - settings.romanticOrientations.hetero - settings.romanticOrientations.bi)
            {
                settings.romanticOrientations.homo = 100f - settings.romanticOrientations.hetero - settings.romanticOrientations.bi;
            }
            settings.romanticOrientations.none = 100 - settings.romanticOrientations.hetero - settings.romanticOrientations.bi - settings.romanticOrientations.homo;
            list.Label("WBR.AromanticChance".Translate() + "  " + settings.romanticOrientations.none + "%", tooltip: "WBR.AromanticChanceTip".Translate());
            DrawCustomSectionEnd(listing, list, out sectionHeightOrientation);
        }

        private static void DrawCustomRight(Listing_Standard listing)
        {
            Listing_Standard list = DrawCustomSectionStart(listing, sectionHeightOther, "WBR.OtherHeading".Translate());
            list.Label("WBR.DateRate".Translate() + "  " + (int)settings.dateRate + "%", tooltip: "WBR.DateRateTip".Translate());
            settings.dateRate = Mathf.Round(list.Slider(settings.dateRate, 0f, 200f));
            list.Label("WBR.HookupRate".Translate() + "  " + (int)settings.hookupRate + "%", tooltip: "WBR.HookupRateTip".Translate());
            settings.hookupRate = Mathf.Round(list.Slider(settings.hookupRate, 0f, 200f));
            list.Label("WBR.AlienLoveChance".Translate() + "  " + (int)settings.alienLoveChance + "%", tooltip: "WBR.AlienLoveChanceTip".Translate());
            settings.alienLoveChance = Mathf.Round(list.Slider(settings.alienLoveChance, 0f, 100f));
            list.Label("WBR.MinOpinionRomance".Translate() + " " + settings.minOpinionRomance, tooltip: "WBR.MinOpinionRomanceTip".Translate());
            settings.minOpinionRomance = Mathf.RoundToInt(list.Slider(settings.minOpinionRomance, -100f, 100f));
            list.Label("WBR.MinOpinionHookup".Translate() + " " + settings.minOpinionHookup, tooltip: "WBR.MinOpinionHookupTip".Translate());
            settings.minOpinionHookup = Mathf.RoundToInt(list.Slider(settings.minOpinionHookup, -100f, 50f));
            list.Label("WBR.CheatChance".Translate() + "  " + (int)settings.cheatChance + "%", tooltip: "WBR.CheatChanceTip".Translate());
            settings.cheatChance = Mathf.Round(list.Slider(settings.cheatChance, 0f, 200f));
            if (settings.cheatChance != 0f)
            {
                list.Label("WBR.CheatingOpinionRange".Translate(), tooltip: "WBR.CheatingOpinionRangeTip".Translate());
                IntRangeWithGap(list, ref settings.cheatingOpinion, -100, 100, 5);
            }
            DrawCustomSectionEnd(listing, list, out sectionHeightOther);
        }

        private static Rect IntRangeWithGap(Listing_Standard listing, ref IntRange range, int min, int max, int gap)
        {
            Rect rect = listing.GetRect(32f);
            if (!listing.BoundingRectCached.HasValue || rect.Overlaps(listing.BoundingRectCached.Value))
            {
                Widgets.IntRange(rect, (int)listing.CurHeight, ref range, min, max, minWidth: gap);
            }
            listing.Gap(listing.verticalSpacing);
            return rect;
        }

        private static void DrawRightMisc(Listing_Standard list)
        {
            DrawFertilityMod(list);

            list.Label("WBR.AddJoyNeed".Translate());
            if (ModsConfig.IdeologyActive)
            {
                list.CheckboxLabeled("SlavesSection".Translate(), ref settings.joyOnSlaves);
            }
            list.CheckboxLabeled("PrisonersSection".Translate(), ref settings.joyOnPrisoners);
            if (Prefs.DevMode)
            {
                list.CheckboxLabeled("Enable dev logging", ref Settings.debugLogging);
            }
        }

        private static void DrawSexualOrientationChance(Listing_Standard listing)
        {
            Listing_Standard list = DrawCustomSectionStart(listing, sectionHeightOrientation, "WBR.OrentationHeading".Translate(), tooltip: "WBR.SexualOrentationHeadingTip".Translate());
            list.Label("WBR.HeterosexualChance".Translate() + "  " + (int)settings.sexualOrientations.hetero + "%", tooltip: "WBR.HeterosexualChanceTip".Translate());
            settings.sexualOrientations.hetero = list.Slider(settings.sexualOrientations.hetero, 0f, 100.99f);
            if (settings.sexualOrientations.hetero > 100.99f - settings.sexualOrientations.bi - settings.sexualOrientations.homo)
            {
                settings.sexualOrientations.hetero = 100.99f - settings.sexualOrientations.bi - settings.sexualOrientations.homo;
            }
            list.Label("WBR.BisexualChance".Translate() + "  " + (int)settings.sexualOrientations.bi + "%", tooltip: "WBR.BisexualComplexChanceTip".Translate());
            settings.sexualOrientations.bi = list.Slider(settings.sexualOrientations.bi, 0f, 100.99f);
            if (settings.sexualOrientations.bi > 100.99f - settings.sexualOrientations.hetero - settings.sexualOrientations.homo)
            {
                settings.sexualOrientations.bi = 100.99f - settings.sexualOrientations.hetero - settings.sexualOrientations.homo;
            }
            list.Label("WBR.HomosexualChance".Translate() + "  " + (int)settings.sexualOrientations.homo + "%", tooltip: "WBR.HomosexualChanceTip".Translate());
            settings.sexualOrientations.homo = list.Slider(settings.sexualOrientations.homo, 0f, 100.99f);
            if (settings.sexualOrientations.homo > 100.99f - settings.sexualOrientations.hetero - settings.sexualOrientations.bi)
            {
                settings.sexualOrientations.homo = 100.99f - settings.sexualOrientations.hetero - settings.sexualOrientations.bi;
            }
            settings.sexualOrientations.none = 100 - (int)settings.sexualOrientations.hetero - (int)settings.sexualOrientations.bi - (int)settings.sexualOrientations.homo;
            list.Label("WBR.AsexualChance".Translate() + "  " + settings.sexualOrientations.none + "%", tooltip: "WBR.AsexualChanceTip".Translate());
            DrawCustomSectionEnd(listing, list, out sectionHeightOrientation);
        }

        private static void DrawRomanticOrientationChance(Listing_Standard listing)
        {
            Listing_Standard list = DrawCustomSectionStart(listing, sectionHeightOrientation, "WBR.RomanticOrentationHeading".Translate(), tooltip: "WBR.RomanticOrentationHeadingTip".Translate());
            list.Label("WBR.HeteroromanticChance".Translate() + "  " + (int)settings.romanticOrientations.hetero + "%", tooltip: "WBR.HeteroromanticChanceTip".Translate());
            settings.romanticOrientations.hetero = list.Slider(settings.romanticOrientations.hetero, 0f, 100.99f);
            if (settings.romanticOrientations.hetero > 100.99f - settings.romanticOrientations.bi - settings.romanticOrientations.homo)
            {
                settings.romanticOrientations.hetero = 100.99f - settings.romanticOrientations.bi - settings.romanticOrientations.homo;
            }
            list.Label("WBR.BiromanticChance".Translate() + "  " + (int)settings.romanticOrientations.bi + "%", tooltip: "WBR.BiromanticChanceTip".Translate());
            settings.romanticOrientations.bi = list.Slider(settings.romanticOrientations.bi, 0f, 100.99f);
            if (settings.romanticOrientations.bi > 100.99f - settings.romanticOrientations.hetero - settings.romanticOrientations.homo)
            {
                settings.romanticOrientations.bi = 100.99f - settings.romanticOrientations.hetero - settings.romanticOrientations.homo;
            }
            list.Label("WBR.HomoromanticChance".Translate() + "  " + (int)settings.romanticOrientations.homo + "%", tooltip: "WBR.HomoromanticChanceTip".Translate());
            settings.romanticOrientations.homo = list.Slider(settings.romanticOrientations.homo, 0f, 100.99f);
            if (settings.romanticOrientations.homo > 100.99f - settings.romanticOrientations.hetero - settings.romanticOrientations.bi)
            {
                settings.romanticOrientations.homo = 100.99f - settings.romanticOrientations.hetero - settings.romanticOrientations.bi;
            }
            settings.romanticOrientations.none = 100 - (int)settings.romanticOrientations.hetero - (int)settings.romanticOrientations.bi - (int)settings.romanticOrientations.homo;
            list.Label("WBR.AromanticChance".Translate() + "  " + settings.romanticOrientations.none + "%", tooltip: "WBR.AromanticTip".Translate());
            DrawCustomSectionEnd(listing, list, out sectionHeightOrientation);
        }

        private static void DrawExtraStuff(Listing_Standard listing)
        {
            Listing_Standard list = DrawCustomSectionStart(listing, sectionHeightComplex, "Extra Settings", tooltip: "WBR.RomanticOrentationHeadingTip".Translate());
            list.Label("Extra stuff for length");
            list.Label("Extra stuff for length");
            list.Label("Extra stuff for length");
            list.Label("Extra stuff for length");
            DrawCustomSectionEnd(listing, list, out sectionHeightComplex);
        }

        private static void DrawFertilityMod(Listing_Standard listing)
        {
            Settings.AutoDetectFertilityMod();
            if (listing.ButtonTextLabeled("WBR.FertilityMod".Translate(), Settings.fertilityMod != "None" ? Settings.FertilityMods.TryGetValue(Settings.fertilityMod) : "None"))
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
                listing.Label("No fertility mod detected. If you are using one, please let me know which one so I can add support for it.");
            }
        }
    }
}