using DivineFramework.UI;
using Verse;

namespace BetterRomance
{
    public partial class Settings : ModSettings
    {
        internal SettingsHandler<Settings> regularHandler;

        internal void SetUpRegularHandler()
        {
            //Sexual orientation
            regularHandler.RegisterNewRow()
                .AddLabel("WBR.OrientationHeading".Translate)
                .WithTooltip("WBR.OrientationHeadingTip".Translate);
            SetUpOrientationChanceSection(regularHandler.RegisterNewSection(name: "SexualOrientationSection", sectionBorder: 6f), false, sexualOrientations.standardOrientation);
            //Buttons
            regularHandler.AddGap(8f);
            UIContainer sexualButtonRow = regularHandler.RegisterNewRow(gap: 0f);
            sexualButtonRow.Add(NewElement.Button(() => sexualOrientations.standardOrientation.CopyFrom(romanticOrientations.standardOrientation))
                .WithLabel("WBR.MatchBelowButton".Translate));
            sexualButtonRow.Add(NewElement.Button(sexualOrientations.standardOrientation.Reset)
                .WithLabel("RestoreToDefaultSettings".Translate));
            regularHandler.AddGap(10f);
            //Romantic orientation
            regularHandler.RegisterNewRow()
                .AddLabel("WBR.AceOrientationHeading".Translate)
                .WithTooltip("WBR.AceOrientationHeadingTip".Translate);
            SetUpOrientationChanceSection(regularHandler.RegisterNewSection(name: "RomanceOrientationSection", sectionBorder: 6f), true, romanticOrientations.standardOrientation);
            //Buttons
            regularHandler.AddGap(8f);
            UIContainer romanticButtonRow = regularHandler.RegisterNewRow(gap: 0f);
            romanticButtonRow.Add(NewElement.Button(() => romanticOrientations.standardOrientation.CopyFrom(sexualOrientations.standardOrientation))
                .WithLabel("WBR.MatchBelowButton".Translate));
            romanticButtonRow.Add(NewElement.Button(romanticOrientations.standardOrientation.Reset)
                .WithLabel("RestoreToDefaultSettings".Translate));
            //Complex button
            regularHandler.RegisterNewRow()
                .Add(ComplexButton());
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
        }

        internal void SetUpOrientationChanceSection(UISection section, bool romance, OrientationChances chances)
        {
            //Hetero
            section.AddLabel(HeteroChance)
                .WithTooltip(HeteroChanceTooltip);
            section.Add(NewElement.Slider<float>()
                .WithReference(chances, nameof(chances.hetero), chances.hetero)
                .MinMax(0f, 100f)
                .RoundTo(0), "HeteroSlider");
            //Bi
            section.AddLabel(BiChance)
                .WithTooltip(BiChanceTooltip);
            section.Add(NewElement.Slider<float>()
                .WithReference(chances, nameof(chances.bi), chances.bi)
                .MinMax(0f, 100f)
                .RoundTo(0), "BiSlider");
            //Homo
            section.AddLabel(HomoChance)
                .WithTooltip(HomoChanceTooltip);
            section.Add(NewElement.Slider<float>()
                .WithReference(chances, nameof(chances.homo), chances.homo)
                .MinMax(0f, 100f)
                .RoundTo(0), "HomoSlider");
            //None
            section.AddLabel(NoneChance)
                .WithTooltip(NoneChanceTooltip);

            //The normalizing needs to be done separately for each value, after the slider for that value
            //So I'm putting it in the label for the next slider
            TaggedString HeteroChance() => (romance ? "WBR.HeteroromanticChance" : "WBR.StraightChance").Translate(chances.Hetero);
            TaggedString HeteroChanceTooltip() => (romance ? "WBR.HeteroromanticChanceTip" : "WBR.StraightChanceTip").Translate();
            TaggedString BiChance()
            {
                if (chances.hetero > 100f - chances.bi - chances.homo)
                {
                    chances.hetero = 100f - chances.bi - chances.homo;
                }
                return (romance ? "WBR.BiromanticChance" : "WBR.BisexualChance").Translate(chances.Bi);
            }
            TaggedString BiChanceTooltip() => (romance ? "WBR.BiromanticChanceTip" : "WBR.BisexualChanceTip").Translate();
            TaggedString HomoChance()
            {
                if (chances.bi > 100f - chances.hetero - chances.homo)
                {
                    chances.bi = 100f - chances.hetero - chances.homo;
                }
                return (romance ? "WBR.HomoromanticChance" : "WBR.GayChance").Translate(chances.Homo);
            }
            TaggedString HomoChanceTooltip() => (romance ? "WBR.HomoromanticChanceTip" : "WBR.GayChanceTip").Translate();
            TaggedString NoneChance()
            {
                if (chances.homo > 100f - chances.hetero - chances.bi)
                {
                    chances.homo = 100f - chances.hetero - chances.bi;
                }
                chances.none = 100f - chances.hetero - chances.bi - chances.homo;
                return (romance ? "WBR.AromanticChance" : "WBR.AsexualChance").Translate(chances.None);
            }
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
    }
}