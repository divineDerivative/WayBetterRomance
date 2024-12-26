using DivineFramework.UI;
using Verse;

namespace BetterRomance
{
    public partial class Settings : ModSettings
    {
        internal TabbedHandler<Settings> complexHandler;
        internal SettingsHandler<Settings> miscHandler;
        internal SettingsHandler<Settings> sexualHandler;
        internal SettingsHandler<Settings> romanticHandler;

        internal void SetUpComplexHandler()
        {
            complexHandler.Clear();
            sexualHandler ??= new(true, SetUpSexualHandler);
            romanticHandler ??= new(true, SetUpRomanticHandler);
            miscHandler ??= new(true, SetUpMiscHandler);
            complexHandler.AddTab(sexualHandler, "WBR.SexualTab".Translate());
            complexHandler.AddTab(romanticHandler, "WBR.RomanticTab".Translate());
            complexHandler.AddTab(miscHandler, "WBR.MiscTab".Translate());
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

        UILabel sexualEquivalentLabel;
        internal void SetUpSexualHandler()
        {
            sexualHandler.Clear();
            //Gender attraction sections
            sexualHandler.RegisterNewRow()
                .AddLabel("WBR.SexualAttractionMen".Translate);
            SetUpGenderChanceSection(sexualHandler.RegisterNewSection(sectionBorder: 6f), Gender.Male, false);

            sexualHandler.RegisterNewRow()
                .AddLabel("WBR.SexualAttractionWomen".Translate);
            SetUpGenderChanceSection(sexualHandler.RegisterNewSection(sectionBorder: 6f), Gender.Female, false);

            if (NonBinaryActive)
            {
                sexualHandler.RegisterNewRow()
                    .AddLabel("WBR.SexualAttractionEnby".Translate);
                SetUpGenderChanceSection(sexualHandler.RegisterNewSection(sectionBorder: 6f), (Gender)3, false);
            }

            sexualHandler.RegisterNewRow()
                .Add(ComplexButton());

            //Orientation equivalence
            sexualEquivalentLabel ??= NewElement.Label("WBR.SexualOrientationEquivalent".Translate)
                .WithTooltip("WBR.SexualOrientationHeadingTip".Translate);

            sexualHandler.RegisterNewRow(newColumn: true)
                .Add(sexualEquivalentLabel);
            SetUpEquivalenceSection(sexualHandler.RegisterNewSection(name: "SexualOrientationMenSection", sectionBorder: 6f), Gender.Male, false);

            sexualHandler.RegisterNewRow()
                .Add(sexualEquivalentLabel);
            SetUpEquivalenceSection(sexualHandler.RegisterNewSection(name: "SexualOrientationWomenSection", sectionBorder: 6f), Gender.Female, false);

            if (NonBinaryActive)
            {
                sexualHandler.RegisterNewRow()
                    .Add(sexualEquivalentLabel);
                SetUpEquivalenceSection(sexualHandler.RegisterNewSection(name: "SexualOrientationEnbySection", sectionBorder: 6f), (Gender)3, false);
            }
        }

        UILabel romanticEquivalentLabel;
        internal void SetUpRomanticHandler()
        {
            romanticHandler.Clear();
            //Gender attraction sections
            romanticHandler.RegisterNewRow()
                .AddLabel("WBR.RomanticAttractionMen".Translate);
            SetUpGenderChanceSection(romanticHandler.RegisterNewSection(sectionBorder: 6f), Gender.Male, true);

            romanticHandler.RegisterNewRow()
                .AddLabel("WBR.RomanticAttractionWomen".Translate);
            SetUpGenderChanceSection(romanticHandler.RegisterNewSection(sectionBorder: 6f), Gender.Female, true);

            if (NonBinaryActive)
            {
                romanticHandler.RegisterNewRow()
                    .AddLabel("WBR.RomanticAttractionEnby".Translate);
                SetUpGenderChanceSection(romanticHandler.RegisterNewSection(sectionBorder: 6f), (Gender)3, true);
            }

            romanticHandler.RegisterNewRow()
                .Add(ComplexButton());

            //Orientation equivalence
            romanticEquivalentLabel ??= NewElement.Label("WBR.RomanticOrientationEquivalent".Translate)
                .WithTooltip("WBR.RomanticOrientationHeadingTip".Translate);

            romanticHandler.RegisterNewRow(newColumn: true)
                .Add(romanticEquivalentLabel);
            SetUpEquivalenceSection(romanticHandler.RegisterNewSection(name: "RomanticOrientationMenSection", sectionBorder: 6f), Gender.Male, true);

            romanticHandler.RegisterNewRow()
                .Add(romanticEquivalentLabel);
            SetUpEquivalenceSection(romanticHandler.RegisterNewSection(name: "RomanticOrientationWomenSection", sectionBorder: 6f), Gender.Female, true);

            if (NonBinaryActive)
            {
                romanticHandler.RegisterNewRow()
                    .Add(romanticEquivalentLabel);
                SetUpEquivalenceSection(romanticHandler.RegisterNewSection(name: "RomanticOrientationEnbySection", sectionBorder: 6f), (Gender)3, true);
            }
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
                .HideWhen(() => cheatChance == 0f)
                .AddLabel("WBR.CheatingOpinionRange".Translate)
                .WithTooltip("WBR.CheatingOpinionRangeTip".Translate);
            //HideWhen is in the wrong spot, it's hiding the element, not the row
            miscHandler.RegisterNewRow()
                .HideWhen(() => cheatChance == 0f)
                .Add(NewElement.Range<IntRange, int>(5)
                .WithReference(this, nameof(cheatingOpinion), cheatingOpinion)
                .MinMax(-100, 100), "CheatOpinionRange");

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
            UIRow guestRow = miscHandler.RegisterNewRow()
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
    }
}