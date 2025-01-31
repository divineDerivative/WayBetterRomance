using DivineFramework;
using DivineFramework.UI;
using RimWorld;
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
        public IntRange cheatingOpinion = new(-75, 75);

        public static string fertilityMod = "None";
        public bool joyOnSlaves = false;
        public bool joyOnPrisoners = false;
        public bool joyOnGuests = false;

        //These are not set by the user
        public static bool HARActive = false;
        public static bool RotRActive = false;
        public static bool ATRActive = false;
        public static bool VREHighmateActive = false;
        public static bool VREAndroidActive = false;
        public static Dictionary<string, string> FertilityMods = new();
        public static bool debugLogging = false;
        public static bool AsimovActive;
        public static bool PawnmorpherActive;
        public static bool TransActive;
        public static NeedDef JoyNeed;

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
            Scribe_Values.Look(ref cheatingOpinion, "cheatingOpinion", new(-75, 75));

            Scribe_Values.Look(ref fertilityMod, "fertilityMod", "None");
            Scribe_Values.Look(ref joyOnSlaves, "joyOnSlaves", false);
            Scribe_Values.Look(ref joyOnPrisoners, "joyOnPrisoners", false);
            Scribe_Values.Look(ref joyOnGuests, "joyOnGuests", false);
            Scribe_Values.Look(ref debugLogging, "debugLogging", false);
        }

        public static void ApplyJoySettings()
        {
            JoyNeed ??= DefDatabase<NeedDef>.GetNamed("Joy", false);
            JoyNeed.neverOnSlave = !BetterRomanceMod.settings.joyOnSlaves;
            if (BetterRomanceMod.settings.joyOnPrisoners)
            {
                JoyNeed.neverOnPrisoner = false;
                JoyNeed.colonistAndPrisonersOnly = true;
                JoyNeed.colonistsOnly = false;
            }
            else
            {
                JoyNeed.neverOnPrisoner = true;
                JoyNeed.colonistAndPrisonersOnly = false;
                JoyNeed.colonistsOnly = true;
                BetterRomanceMod.settings.joyOnGuests = false;
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

        internal SettingsHandler<Settings> handler = new(true);

        internal void SetUpHandler(Listing_Standard listing)
        {
            handler.width = listing.ColumnWidth;
            handler.RegisterNewRow()
                .AddLabel("WBR.OrientationHeading".Translate)
                .WithTooltip("WBR.OrientationHeadingTip".Translate);
            SetUpChanceSection(handler.RegisterNewSection(name: "SexualOrientationSection", sectionBorder: 6f), false);
            handler.AddGap(10f);

            handler.RegisterNewRow()
                .AddLabel("WBR.AceOrientationHeading".Translate)
                .WithTooltip("WBR.AceOrientationHeadingTip".Translate);
            SetUpChanceSection(handler.RegisterNewSection(name: "RomanceOrientationSection", sectionBorder: 6f), true);

            handler.RegisterNewRow(newColumn: true).AddLabel("WBR.OtherHeading".Translate);
            SetUpMiscSection(handler.RegisterNewSection(name: "MiscSection", sectionBorder: 6f));
            handler.AddGap();
            //Fertility mod
            UIContainer fertilityRow = handler.RegisterNewRow();
            fertilityRow.AddLabel("WBR.FertilityMod".Translate);
            fertilityRow.Add(NewElement.Button(FertilityModOnClick, relative: 1f / 3f)
                .WithLabel(() => fertilityMod != "None" ? FertilityMods.TryGetValue(fertilityMod) : "None"));
            handler.RegisterNewRow()
                .HideWhen(() => FertilityMods.Count > 0)
                .AddLabel("WBR.NoFertilityMod".Translate);
            //Joy need
            handler.RegisterNewRow()
                .AddLabel("WBR.AddJoyNeed".Translate);
            handler.RegisterNewRow()
                .HideWhen(() => !ModsConfig.IdeologyActive)
                .Add(NewElement.Checkbox()
                .WithReference(this, nameof(joyOnSlaves), joyOnSlaves)
                .WithLabel("SlavesSection".Translate));
            handler.RegisterNewRow()
                .Add(NewElement.Checkbox()
                .WithReference(this, nameof(joyOnPrisoners), joyOnPrisoners)
                .WithLabel("PrisonersSection".Translate));
            var guestRow = handler.RegisterNewRow()
                .HideWhen(() => !joyOnPrisoners);
            guestRow.AddSpace(relative: 0.02f);
            guestRow.Add(NewElement.Checkbox()
                .WithReference(this, nameof(joyOnGuests), joyOnGuests)
                .WithLabel("WBR.Guests".Translate)
                .WithTooltip("WBR.GuestsTip".Translate));

            handler.RegisterNewRow()
                .Add(NewElement.Checkbox()
                .WithReference(this, nameof(debugLogging), debugLogging)
                .WithLabel(() => "Enable dev logging"));

            handler.Initialize();
        }

        internal void SetUpChanceSection(UISection section, bool romance)
        {
            OrientationChances chances = romance ? asexualOrientations : sexualOrientations;
            //Hetero
            section.AddLabel(HeteroChance)
                .WithTooltip(HeteroChanceTooltip);
            section.Add(NewElement.Slider<float>()
                .WithReference(chances, nameof(chances.hetero), chances.hetero)
                .MinMax(0f, 100f)
                .RoundTo(0)
                .WithPostDraw(delegate
                {
                    if (chances.hetero > 100f - chances.bi - chances.homo)
                    {
                        chances.hetero = 100f - chances.bi - chances.homo;
                    }
                }), "HeteroSlider");
            //Bi
            section.AddLabel(BiChance)
                .WithTooltip(BiChanceTooltip);
            section.Add(NewElement.Slider<float>()
                .WithReference(chances, nameof(chances.bi), chances.bi)
                .MinMax(0f, 100f)
                .RoundTo(0)
                .WithPostDraw(delegate
                {
                    if (chances.bi > 100f - chances.hetero - chances.homo)
                    {
                        chances.bi = 100f - chances.hetero - chances.homo;
                    }
                }), "BiSlider");
            //Homo
            section.AddLabel(HomoChance)
                .WithTooltip(HomoChanceTooltip);
            section.Add(NewElement.Slider<float>()
                .WithReference(chances, nameof(chances.homo), chances.homo)
                .MinMax(0f, 100f)
                .RoundTo(0)
                .WithPostDraw(delegate
                {
                    if (chances.homo > 100f - chances.hetero - chances.bi)
                    {
                        chances.homo = 100f - chances.hetero - chances.bi;
                    }
                }), "HomoSlider");
            //None
            section.AddLabel(NoneChance)
                .WithTooltip(NoneChanceTooltip)
                .WithPreDraw(delegate
                {
                    chances.none = 100f - chances.hetero - chances.bi - chances.homo;
                });
            //Buttons
            handler.AddGap(8f);
            UIContainer buttonRow = handler.RegisterNewRow(gap: 0f);
            buttonRow.Add(NewElement.Button(() => chances.CopyFrom(romance ? sexualOrientations : asexualOrientations))
                .WithLabel((romance ? "WBR.MatchAboveButton" : "WBR.MatchBelowButton").Translate));
            buttonRow.Add(NewElement.Button(chances.Reset)
                .WithLabel("RestoreToDefaultSettings".Translate));

            TaggedString HeteroChance() => (romance ? "WBR.AceHeteroChance" : "WBR.StraightChance").Translate(chances.hetero);
            TaggedString HeteroChanceTooltip() => (romance ? "WBR.AceHeteroChanceTip" : "WBR.StraightChanceTip").Translate();
            TaggedString BiChance() => (romance ? "WBR.AceBiChance" : "WBR.BisexualChance").Translate(chances.bi);
            TaggedString BiChanceTooltip() => (romance ? "WBR.AceBiChanceTip" : "WBR.BisexualChanceTip").Translate();
            TaggedString HomoChance() => (romance ? "WBR.AceHomoChance" : "WBR.GayChance").Translate(chances.homo);
            TaggedString HomoChanceTooltip() => (romance ? "WBR.AceHomoChanceTip" : "WBR.GayChanceTip").Translate();
            TaggedString NoneChance() => (romance ? "WBR.AceAroChance" : "WBR.AsexualChance").Translate(chances.none);
            TaggedString NoneChanceTooltip() => (romance ? "WBR.AceAroChanceTip" : "WBR.AsexualChanceTip").Translate();
        }

        internal void SetUpMiscSection(UISection section)
        {
            //Date rate
            section.AddLabel(() => "WBR.DateRate".Translate(dateRate))
                .WithTooltip("WBR.DateRateTip".Translate);
            section.Add(NewElement.Slider<float>()
                .WithReference(this, nameof(dateRate), dateRate)
                .MinMax(0f, 200f)
                .RoundTo(0)
                .RegisterResetable(handler, 100f), "DateRateSlider");
            //Hook up rate
            section.AddLabel(() => "WBR.HookupRate".Translate(hookupRate))
                .WithTooltip("WBR.HookupRateTip".Translate);
            section.Add(NewElement.Slider<float>()
                .WithReference(this, nameof(hookupRate), hookupRate)
                .MinMax(0f, 200f)
                .RoundTo(0)
                .RegisterResetable(handler, 100f), "HookupRateSlider");
            //Alien love chance
            section.AddLabel(() => "WBR.AlienLoveChance".Translate(alienLoveChance))
                .WithTooltip("WBR.AlienLoveChanceTip".Translate);
            section.Add(NewElement.Slider<float>()
                .WithReference(this, nameof(alienLoveChance), alienLoveChance)
                .MinMax(-100f, 100f)
                .RoundTo(0)
                .RegisterResetable(handler, 33f), "AlienChanceSlider");
            //Min opinion for romance
            section.AddLabel(() => "WBR.MinOpinionRomance".Translate(minOpinionRomance))
                .WithTooltip("WBR.MinOpinionRomanceTip".Translate);
            section.Add(NewElement.Slider<int>()
                .WithReference(this, nameof(minOpinionRomance), minOpinionRomance)
                .MinMax(-100, 100)
                .RegisterResetable(handler, 5), "MinOpinionRomanceSlider");
            //Min opinion for hook up
            section.AddLabel(() => "WBR.MinOpinionHookup".Translate(minOpinionHookup))
                .WithTooltip("WBR.MinOpinionHookupTip".Translate);
            section.Add(NewElement.Slider<int>()
                .WithReference(this, nameof(minOpinionHookup), minOpinionHookup)
                .MinMax(-100, 50)
                .RegisterResetable(handler, 0), "MinOpinionHookupSlider");
            //Cheat chance
            section.AddLabel(() => "WBR.CheatChance".Translate(cheatChance))
                .WithTooltip("WBR.CheatChanceTip".Translate);
            section.Add(NewElement.Slider<float>()
                .WithReference(this, nameof(cheatChance), cheatChance)
                .MinMax(0f, 200f)
                .RoundTo(0)
                .RegisterResetable(handler, 100f), "CheatChanceSlider");
            //Cheat opinion range
            section.AddLabel("WBR.CheatingOpinionRange".Translate)
                .WithTooltip("WBR.CheatingOpinionRangeTip".Translate)
                .HideWhen(() => cheatChance == 0f);
            section.Add(NewElement.Range<IntRange, int>(5)
                .WithReference(this, nameof(cheatingOpinion), cheatingOpinion)
                .MinMax(-100, 100)
                .RegisterResetable(handler, new IntRange(-75, 75)), "CheatOpinionRange");

            handler.AddGap();
            handler.RegisterNewRow().AddResetButton(handler);
        }

        private void FertilityModOnClick()
        {
            List<FloatMenuOption> options = new();
            foreach (KeyValuePair<string, string> item in FertilityMods)
            {
                options.Add(new FloatMenuOption(item.Value, delegate
                {
                    fertilityMod = item.Key;
                }));
            }
            if (!options.NullOrEmpty())
            {
                Find.WindowStack.Add(new FloatMenu(options));
            }
        }
    }

    internal class BetterRomanceMod : Mod
    {
        public static Settings settings;

        public BetterRomanceMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<Settings>();
            ModManagement.RegisterMod("WBR.WayBetterRomance", typeof(BetterRomanceMod).Assembly.GetName().Name, new("0.8.1.0"), "<color=#1116e4>[WayBetterRomance]</color>", () => Settings.debugLogging);
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

        public override void DoSettingsWindowContents(Rect canvas)
        {
            Listing_ScrollView outerList = new()
            {
                ColumnWidth = (canvas.width / 2f) - 24f
            };
            Listing_Standard list = outerList.BeginScrollView(canvas, scrollViewHeight, ref scrollPos);
            Settings.AutoDetectFertilityMod();

            if (!settings.handler.Initialized)
            {
                settings.SetUpHandler(list);
            }

            settings.handler.Draw(list);

            scrollViewHeight = list.MaxColumnHeightSeen;
            outerList.End();
        }
    }
}