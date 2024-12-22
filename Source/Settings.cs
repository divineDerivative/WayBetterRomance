using System.Collections.Generic;
using System.Linq;
using DivineFramework;
using DivineFramework.UI;
using LudeonTK;
using RimWorld;
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
            enby = 10f,
        };
        public OrientationChances romanticOrientations = new()
        {
            hetero = 20f,
            homo = 20f,
            bi = 50f,
            none = 10f,
            enby = 10f,
        };
        //Do I need separate objects for the complex chances?

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
        public bool complex = false;
        public float complexChance = 25f;

        //These are not set by the user
        public static bool HARActive = false;
        public static bool RotRActive = false;
        public static bool ATRActive = false;
        public static bool VREHighmateActive = false;
        public static bool VREAndroidActive = false;
        public static bool NonBinaryActive = false;
        public static Dictionary<string, string> FertilityMods = new();
        public static bool debugLogging = false;
        public static bool AsimovActive;
        public static bool PawnmorpherActive;
        public static bool TransActive;
        public static bool AltFertilityActive;
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
            Scribe_Values.Look(ref joyOnGuests, "joyOnGuests", false);
            Scribe_Values.Look(ref debugLogging, "debugLogging", false);
            Scribe_Values.Look(ref complex, "compexOrientations", false);
            Scribe_Values.Look(ref complexChance, "complexChance", 25f);
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

        internal SettingsHandler<Settings> regularHandler = new(true);
        internal TabbedHandler<Settings> complexHandler;
        internal SettingsHandler<Settings> miscHandler;
        internal SettingsHandler<Settings> sexualHandler;
        internal SettingsHandler<Settings> romanticHandler;

        internal void SetUpRegularHandler(Listing_Standard listing)
        {
            regularHandler.width = listing.ColumnWidth;

            //Sexual orientation
            regularHandler.RegisterNewRow("Sexual Orientation Heading row")
                .AddLabel("WBR.OrientationHeading".Translate)
                .WithTooltip("WBR.OrientationHeadingTip".Translate);
            SetUpChanceSection(regularHandler.RegisterNewSection(name: "SexualOrientationSection", sectionBorder: 6f), false);
            regularHandler.AddGap(10f);
            //Romantic orientation
            regularHandler.RegisterNewRow()
                .AddLabel("WBR.AceOrientationHeading".Translate)
                .WithTooltip("WBR.AceOrientationHeadingTip".Translate);
            SetUpChanceSection(regularHandler.RegisterNewSection(name: "RomanceOrientationSection", sectionBorder: 6f), true);
            //Complex button
            regularHandler.RegisterNewRow()
                .Add(NewElement.Button(() => complex = !complex)
                .WithLabel(() => complex ? "Simplify it" : "Let's make it complicated"));
            //Misc section
            regularHandler.RegisterNewRow(newColumn: true).AddLabel("WBR.OtherHeading".Translate);
            SetUpMiscSection(regularHandler.RegisterNewSection(name: "MiscSection", sectionBorder: 6f));
            regularHandler.AddGap();
            //Fertility mod
            UIContainer fertilityRow = regularHandler.RegisterNewRow();
            fertilityRow.AddLabel("WBR.FertilityMod".Translate);
            fertilityRow.Add(NewElement.Button(FertilityModOnClick, relative: 1f / 3f)
                .WithLabel(() => fertilityMod != "None" ? FertilityMods.TryGetValue(fertilityMod) : "None"));
            regularHandler.RegisterNewRow()
                .HideWhen(() => FertilityMods.Count > 0)
                .AddLabel("WBR.NoFertilityMod".Translate);
            //Joy need
            regularHandler.RegisterNewRow()
                .AddLabel("WBR.AddJoyNeed".Translate);
            regularHandler.RegisterNewRow()
                .HideWhen(() => !ModsConfig.IdeologyActive)
                .Add(NewElement.Checkbox()
                .WithReference(this, nameof(joyOnSlaves), joyOnSlaves)
                .WithLabel("SlavesSection".Translate));
            regularHandler.RegisterNewRow()
                .Add(NewElement.Checkbox()
                .WithReference(this, nameof(joyOnPrisoners), joyOnPrisoners)
                .WithLabel("PrisonersSection".Translate));
            UIContainer guestRow = regularHandler.RegisterNewRow()
                .HideWhen(() => !joyOnPrisoners);
            guestRow.AddSpace(relative: 0.02f);
            guestRow.Add(NewElement.Checkbox()
                .WithReference(this, nameof(joyOnGuests), joyOnGuests)
                .WithLabel("WBR.Guests".Translate)
                .WithTooltip("WBR.GuestsTip".Translate));

            //Dev logging
            regularHandler.RegisterNewRow()
                .Add(NewElement.Checkbox()
                .WithReference(this, nameof(debugLogging), debugLogging)
                .WithLabel(() => "Enable dev logging"));

            regularHandler.Initialize();
        }

        internal void SetUpComplexHandler()
        {
            complexHandler.Clear();
            sexualHandler ??= new(true, SetUpSexualHandler);
            romanticHandler ??= new(true, SetUpRomanticHandler);
            miscHandler ??= new(true, SetUpMiscHandler);
            complexHandler.AddTab(sexualHandler, "Sexual");
            complexHandler.AddTab(romanticHandler, "Romantic");
            complexHandler.AddTab(miscHandler, "Misc");
            ////Complex stuff
            //regularHandler.RegisterNewRow("ComplexChanceLabel")
            //    .HideWhen(() => !complex)
            //    .AddLabel(() => $"WBR.ComplexChanceLabel".Translate(complexChance))
            //    .WithTooltip("WBR.ComplexChanceTooltip".Translate);
            //regularHandler.RegisterNewRow("ComplexChanceSlider")
            //    .HideWhen(() => !complex)
            //    .Add(NewElement.Slider<float>()
            //    .WithReference(this, nameof(complexChance), complexChance)
            //    .MinMax(0f, 100f)
            //    .RoundTo(0));
            ////Don't really like how this looks here.
            ////Adjusting the value on one of the sliders makes the location of the slider change, so it only goes down one tick and then you have to grab it again to keep adjusting
            ////Maybe make it a pop up when they try to close settings?
            //regularHandler.RegisterNewRow("ComplexChanceWarning")
            //    .HideWhen(() => !complex || !NeedWarning())
            //    .AddLabel(() => "WBR.ComplexWarning".Translate(sexualOrientations.hetero == 100f ? RomanceDefOf.Straight.DataAtDegree(0).label : TraitDefOf.Gay.DataAtDegree(0).label));

            //bool NeedWarning() => (sexualOrientations.hetero == 100f && romanticOrientations.homo == 100f) || (sexualOrientations.homo == 100f && romanticOrientations.hetero == 100f);
            ////Having one orientation be 100% hetero means that anyone who rolls homo on the other orientation will have to be made hetero, and presumably the other way around too
            ////I'm wondering if having hetero/homo set to 100 should lock homo/hetero in the other to 0?
        }

        internal void SetUpSexualHandler()
        {

        }

        internal void SetUpRomanticHandler()
        {

        }

        internal void SetUpMiscHandler()
        {
            miscHandler.Clear();
            //Date rate
            miscHandler.RegisterNewRow()
                .AddLabel(() => "WBR.DateRate".Translate(dateRate))
                .WithTooltip("WBR.DateRateTip".Translate);
            miscHandler.RegisterNewRow()
                .Add(NewElement.Slider<float>()
                .WithReference(this, nameof(dateRate), dateRate)
                .MinMax(0f, 200f)
                .RoundTo(0));
            //Hook up rate
            miscHandler.RegisterNewRow()
                .AddLabel(() => "WBR.HookupRate".Translate(hookupRate))
                .WithTooltip("WBR.HookupRateTip".Translate);
            miscHandler.RegisterNewRow()
                .Add(NewElement.Slider<float>()
                .WithReference(this, nameof(hookupRate), hookupRate)
                .MinMax(0f, 200f)
                .RoundTo(0));
            //Alien love chance
            miscHandler.RegisterNewRow()
                .AddLabel(() => "WBR.AlienLoveChance".Translate(alienLoveChance))
                .WithTooltip("WBR.AlienLoveChanceTip".Translate);
            miscHandler.RegisterNewRow()
                .Add(NewElement.Slider<float>()
                .WithReference(this, nameof(alienLoveChance), alienLoveChance)
                .MinMax(-100f, 100f)
                .RoundTo(0));
            //Min opinion for romance
            miscHandler.RegisterNewRow()
                .AddLabel(() => "WBR.MinOpinionRomance".Translate(minOpinionRomance))
                .WithTooltip("WBR.MinOpinionRomanceTip".Translate);
            miscHandler.RegisterNewRow()
                .Add(NewElement.Slider<int>()
                .WithReference(this, nameof(minOpinionRomance), minOpinionRomance)
                .MinMax(-100, 100));
            //Min opinion for hook up
            miscHandler.RegisterNewRow()
                .AddLabel(() => "WBR.MinOpinionHookup".Translate(minOpinionHookup))
                .WithTooltip("WBR.MinOpinionHookupTip".Translate);
            miscHandler.RegisterNewRow()
                .Add(NewElement.Slider<int>()
                .WithReference(this, nameof(minOpinionHookup), minOpinionHookup)
                .MinMax(-100, 50));
            //Cheat chance
            miscHandler.RegisterNewRow()
                .AddLabel(() => "WBR.CheatChance".Translate(cheatChance))
                .WithTooltip("WBR.CheatChanceTip".Translate);
            miscHandler.RegisterNewRow()
                .Add(NewElement.Slider<float>()
                .WithReference(this, nameof(cheatChance), cheatChance)
                .MinMax(0f, 200f)
                .RoundTo(0));
            //Cheat opinion range
            miscHandler.RegisterNewRow()
                .AddLabel("WBR.CheatingOpinionRange".Translate)
                .WithTooltip("WBR.CheatingOpinionRangeTip".Translate)
                .HideWhen(() => cheatChance == 0f);
            miscHandler.RegisterNewRow()
                .Add(NewElement.Range<IntRange, int>(5)
                .WithReference(this, nameof(cheatingOpinion), cheatingOpinion)
                .MinMax(-100, 100)
                .HideWhen(() => cheatChance == 0f), "CheatOpinionRange");

            miscHandler.AddGap();
            miscHandler.RegisterNewRow().AddResetButton(regularHandler);
            //Fertility mod
            UIContainer fertilityRow = miscHandler.RegisterNewRow(newColumn: true);
            fertilityRow.AddLabel("WBR.FertilityMod".Translate);
            fertilityRow.Add(NewElement.Button(FertilityModOnClick, relative: 1f / 3f)
                .WithLabel(() => fertilityMod != "None" ? FertilityMods.TryGetValue(fertilityMod) : "None"));
            miscHandler.RegisterNewRow()
                .HideWhen(() => FertilityMods.Count > 0)
                .AddLabel("WBR.NoFertilityMod".Translate);
            //Joy need
            miscHandler.RegisterNewRow()
                .AddLabel("WBR.AddJoyNeed".Translate);
            miscHandler.RegisterNewRow()
                .HideWhen(() => !ModsConfig.IdeologyActive)
                .Add(NewElement.Checkbox()
                .WithReference(this, nameof(joyOnSlaves), joyOnSlaves)
                .WithLabel("SlavesSection".Translate));
            miscHandler.RegisterNewRow()
                .Add(NewElement.Checkbox()
                .WithReference(this, nameof(joyOnPrisoners), joyOnPrisoners)
                .WithLabel("PrisonersSection".Translate));
            var guestRow = miscHandler.RegisterNewRow()
                .HideWhen(() => !joyOnPrisoners);
            guestRow.AddSpace(relative: 0.02f);
            guestRow.Add(NewElement.Checkbox()
                .WithReference(this, nameof(joyOnGuests), joyOnGuests)
                .WithLabel("WBR.Guests".Translate)
                .WithTooltip("WBR.GuestsTip".Translate));

            //Dev logging
            miscHandler.RegisterNewRow()
                .Add(NewElement.Checkbox()
                .WithReference(this, nameof(debugLogging), debugLogging)
                .WithLabel(() => "Enable dev logging"));
        }

        internal void SetUpChanceSection(UISection section, bool romance)
        {
            OrientationChances chances = romance ? romanticOrientations : sexualOrientations;
            //Hetero
            section.AddLabel(HeteroChance, name: romance ? "Heteroromantic chance label" : "Heterosexual chance label")
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
                }), "Hetero slider");
            //Bi
            section.AddLabel(BiChance, name: romance ? "Biromantic chance label" : "Bisexual chance label")
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
                }), "Bi slider");
            //Homo
            section.AddLabel(HomoChance, name: romance ? "Homoromantic chance label" : "Homosexual chance label")
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
                }), "Homo slider");
            //None
            section.AddLabel(NoneChance, name: romance ? "Aromantic chance label" : "Asexual chance label")
                .WithTooltip(NoneChanceTooltip)
                .WithPreDraw(delegate
                {
                    chances.none = 100f - chances.hetero - chances.bi - chances.homo;
                });
            //Buttons
            regularHandler.AddGap(8f);
            UIContainer buttonRow = regularHandler.RegisterNewRow(gap: 0f);
            buttonRow.Add(NewElement.Button(() => chances.CopyFrom(romance ? sexualOrientations : romanticOrientations))
                .WithLabel((romance ? "WBR.MatchAboveButton" : "WBR.MatchBelowButton").Translate));
            buttonRow.Add(NewElement.Button(chances.Reset)
                .WithLabel("RestoreToDefaultSettings".Translate));

            TaggedString HeteroChance() => (romance ? "WBR.HeteroromanticChance" : "WBR.StraightChance").Translate(chances.hetero);
            TaggedString HeteroChanceTooltip() => (romance ? "WBR.HeteroromanticChanceTip" : "WBR.StraightChanceTip").Translate();
            TaggedString BiChance() => (romance ? "WBR.BiromanticChance" : "WBR.BisexualChance").Translate(chances.bi);
            TaggedString BiChanceTooltip() => (romance ? "WBR.BiromanticChanceTip" : "WBR.BisexualChanceTip").Translate();
            TaggedString HomoChance() => (romance ? "WBR.HomoromanticChance" : "WBR.GayChance").Translate(chances.homo);
            TaggedString HomoChanceTooltip() => (romance ? "WBR.HomoromanticChanceTip" : "WBR.GayChanceTip").Translate();
            TaggedString NoneChance() => (romance ? "WBR.AromanticChance" : "WBR.AsexualChance").Translate(chances.none);
            TaggedString NoneChanceTooltip() => (romance ? "WBR.AromanticChanceTip" : "WBR.AsexualChanceTip").Translate();
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
                .RegisterResettable(regularHandler, 100f), "DateRateSlider");
            //Hook up rate
            section.AddLabel(() => "WBR.HookupRate".Translate(hookupRate))
                .WithTooltip("WBR.HookupRateTip".Translate);
            section.Add(NewElement.Slider<float>()
                .WithReference(this, nameof(hookupRate), hookupRate)
                .MinMax(0f, 200f)
                .RoundTo(0)
                .RegisterResettable(regularHandler, 100f), "HookupRateSlider");
            //Alien love chance
            section.AddLabel(() => "WBR.AlienLoveChance".Translate(alienLoveChance))
                .WithTooltip("WBR.AlienLoveChanceTip".Translate);
            section.Add(NewElement.Slider<float>()
                .WithReference(this, nameof(alienLoveChance), alienLoveChance)
                .MinMax(-100f, 100f)
                .RoundTo(0)
                .RegisterResettable(regularHandler, 33f), "AlienChanceSlider");
            //Min opinion for romance
            section.AddLabel(() => "WBR.MinOpinionRomance".Translate(minOpinionRomance))
                .WithTooltip("WBR.MinOpinionRomanceTip".Translate);
            section.Add(NewElement.Slider<int>()
                .WithReference(this, nameof(minOpinionRomance), minOpinionRomance)
                .MinMax(-100, 100)
                .RegisterResettable(regularHandler, 5), "MinOpinionRomanceSlider");
            //Min opinion for hook up
            section.AddLabel(() => "WBR.MinOpinionHookup".Translate(minOpinionHookup))
                .WithTooltip("WBR.MinOpinionHookupTip".Translate);
            section.Add(NewElement.Slider<int>()
                .WithReference(this, nameof(minOpinionHookup), minOpinionHookup)
                .MinMax(-100, 50)
                .RegisterResettable(regularHandler, 0), "MinOpinionHookupSlider");
            //Cheat chance
            section.AddLabel(() => "WBR.CheatChance".Translate(cheatChance))
                .WithTooltip("WBR.CheatChanceTip".Translate);
            section.Add(NewElement.Slider<float>()
                .WithReference(this, nameof(cheatChance), cheatChance)
                .MinMax(0f, 200f)
                .RoundTo(0)
                .RegisterResettable(regularHandler, 100f), "CheatChanceSlider");
            //Cheat opinion range
            section.AddLabel("WBR.CheatingOpinionRange".Translate)
                .WithTooltip("WBR.CheatingOpinionRangeTip".Translate)
                .HideWhen(() => cheatChance == 0f);
            section.Add(NewElement.Range<IntRange, int>(5)
                .WithReference(this, nameof(cheatingOpinion), cheatingOpinion)
                .MinMax(-100, 100)
                .RegisterResettable(regularHandler, new IntRange(-75, 75))
                .HideWhen(() => cheatChance == 0f), "CheatOpinionRange");

            regularHandler.AddGap();
            regularHandler.RegisterNewRow().AddResetButton(regularHandler);
        }

        private void FertilityModOnClick()
        {
            List<FloatMenuOption> options = new();
            foreach (KeyValuePair<string, string> item in FertilityMods)
            {
                options.Add(new(item.Value, delegate { fertilityMod = item.Key; }));
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
            ModManagement.RegisterMod("WBR.WayBetterRomance", new(FrameworkVersionInfo.Version));
        }

        public override string SettingsCategory() => "WBR.WayBetterRomance".Translate();

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
                if (!settings.regularHandler.Initialized)
                {
                    settings.SetUpRegularHandler(list);
                }
                settings.regularHandler.Draw(list);
            }

            scrollViewHeight = list.MaxColumnHeightSeen;
            outerList.End();
        }
    }

    internal class WBRLogger : Logging
    {
        public static readonly WBRLogger LogUtil = new();
        private WBRLogger() : base("<color=#1116e4>[WayBetterRomance]</color>", () => Settings.debugLogging) { }
    }
}
