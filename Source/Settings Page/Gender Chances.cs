using DivineFramework.UI;
using Verse;

namespace BetterRomance
{
    public partial class Settings : ModSettings
    {
        internal GenderAttractionChances GenderToChances(Gender gender, bool romance)
        {
            return gender switch
            {
                Gender.Male => romance ? romanticAttractionForMen : sexualAttractionForMen,
                Gender.Female => romance ? romanticAttrationForWomen : sexualAttrationForWomen,
                (Gender)3 => romance ? romanticAttractionForEnby : sexualAttractionForEnby,
                _ => new()
            };
        }

        internal void SetUpGenderChanceSection(UISection section, Gender gender, bool romance)
        {
            SettingsHandler<Settings> handler = romance ? romanticHandler : sexualHandler;
            GenderAttractionChances chances = GenderToChances(gender, romance);
            section.AddLabel(() => "WBR.Men".Translate(chances.men));
            section.Add(NewElement.Slider<float>()
                .MinMax(0f, 1f)
                .WithReference(chances, nameof(chances.men), chances.men));

            section.AddLabel(() => "WBR.Women".Translate(chances.women));
            section.Add(NewElement.Slider<float>()
                .MinMax(0f, 1f)
                .WithReference(chances, nameof(chances.women), chances.women));

            if (NonBinaryActive)
            {
                section.AddLabel(() => "WBR.NonBinary".Translate(chances.enby));
                section.Add(NewElement.Slider<float>()
                    .MinMax(0f, 1f)
                    .WithReference(chances, nameof(chances.enby), chances.enby));
            }

            //Buttons
            UIRow buttonRow = handler.RegisterNewRow(gap: 0f);
            if (gender != Gender.Male)
            {
                buttonRow.Add(NewElement.Button(() => chances.CopyFrom(GenderToChances(gender - 1, romance)))
                    .WithLabel("WBR.MatchAboveButton".Translate));
            }
            if (gender != (NonBinaryActive ? (Gender)3 : Gender.Female))
            {
                buttonRow.Add(NewElement.Button(() => chances.CopyFrom(GenderToChances(gender + 1, romance)))
                    .WithLabel("WBR.MatchBelowButton".Translate));
            }
            buttonRow.Add(NewElement.Button(chances.Reset)
                .WithLabel("Reset".Translate));
            handler.AddGap();
        }

        internal void SetUpEquivalenceSection(UISection section, Gender gender, bool romance)
        {
            GenderAttractionChances chances = GenderToChances(gender, romance);
            SettingsHandler<Settings> handler = romance ? romanticHandler : sexualHandler;

            //Hetero
            section.AddLabel(HeteroChanceLabel)
                .WithTooltip(HeteroChanceTooltip);
            //Bi
            section.AddLabel(BiChanceLabel)
                .WithTooltip(BiChanceTooltip);
            //Homo
            section.AddLabel(HomoChanceLabel)
                .WithTooltip(HomoChanceTooltip);
            //None
            section.AddLabel(NoneChanceLabel)
                .WithTooltip(NoneChanceTooltip);

            if (NonBinaryActive)
            {
                section.AddLabel(EnbyChanceLabel)
                    .WithTooltip(EnbyChanceTooltip);
                if (gender == (Gender)3)
                {
                    section.AddLabel(QueerChanceLabel)
                        .WithTooltip(QueerChanceTooltip); ;
                }
                handler.AddGap(Heights.Slider);
            }

            handler.AddGap();
            handler.AddGap(Heights.Button);

            //Enby can be added to hetero for men and women without changing the orientation, but not for non-binary
            float HeteroChance()
            {
                return gender switch
                {
                    Gender.Male => chances.notMen * chances.women,
                    Gender.Female => chances.men * chances.notWomen,
                    (Gender)3 => ((chances.men * chances.notWomen) + (chances.notMen * chances.women)) * chances.notEnby,
                    _ => 0f,
                };
            }
            //I think I'll include pan in this for men and women, so enby only matters for non-binary
            float BiChance()
            {
                return gender switch
                {
                    Gender.Male or Gender.Female => chances.men * chances.women,
                    (Gender)3 => chances.men * chances.women * chances.notEnby,
                    _ => 0f,
                };
            }
            //Enby can be added to homo for men and women without changing the orientation
            float HomoChance()
            {
                return gender switch
                {
                    Gender.Male => chances.men * chances.notWomen,
                    Gender.Female => chances.notMen * chances.women,
                    (Gender)3 => chances.notMen * chances.notWomen * chances.enby,
                    _ => 0f,
                };
            }
            //Enby cannot be added to none for men and women, so the enby chance does not matter for them
            float NoneChance()
            {
                return gender switch
                {
                    Gender.Male or Gender.Female => chances.notMen * chances.notWomen,
                    (Gender)3 => chances.notMen * chances.notWomen * (NonBinaryActive ? chances.notEnby : 1f),
                    _ => 0f,
                };
            }
            //For men and women, adding enby does not change the orientation (except for bi->pan but that's fine) so it's not part of the total 
            float EnbyChance()
            {
                return gender switch
                {
                    //Can't be added to asexual
                    Gender.Male or Gender.Female => (HeteroChance() + BiChance() + HomoChance()) * chances.enby,
                    //We're using this to display pan for non-binary, since they don't get enby just added to another orientation
                    (Gender)3 => chances.men * chances.women * chances.enby,
                    _ => 0f,
                };
            }
            //This is the chance for an orientation that doesn't have another explicit label, which so far only happens for non-binary
            float QueerChance()
            {
                return gender switch
                {
                    (Gender)3 => ((chances.men * chances.notWomen) + (chances.notMen * chances.women)) * chances.enby,
                    _ => 0f,
                };
            }

            TaggedString HeteroChanceLabel() => (romance ? "WBR.HeteroromanticChance" : "WBR.HeterosexualChance").Translate(HeteroChance());
            TaggedString HeteroChanceTooltip() => (romance ? "WBR.HeteroromanticChanceTip" : "WBR.HeterosexualChanceTip").Translate();
            TaggedString BiChanceLabel() => (romance ? "WBR.BiromanticChance" : "WBR.BisexualChance").Translate(BiChance());
            TaggedString BiChanceTooltip() => (romance ? "WBR.BiromanticChanceTip" : "WBR.BisexualComplexChanceTip").Translate();
            TaggedString HomoChanceLabel() => (romance ? "WBR.HomoromanticChance" : "WBR.HomosexualChance").Translate(HomoChance());
            TaggedString HomoChanceTooltip() => (romance ? "WBR.HomoromanticChanceTip" : "WBR.HomosexualChanceTip").Translate();
            TaggedString NoneChanceLabel() => (romance ? "WBR.AromanticChance" : "WBR.AsexualChance").Translate(NoneChance());
            TaggedString NoneChanceTooltip() => (romance ? "WBR.AromanticComplexTip" : "WBR.AsexualChanceTip").Translate();
            TaggedString EnbyChanceLabel() => (romance
                ? gender == (Gender)3 ? "WBR.PanromanticChance" : "WBR.EnbyromanticChance"
                : gender == (Gender)3 ? "WBR.PansexualChance" : "WBR.EnbysexualChance").Translate(EnbyChance());
            TaggedString EnbyChanceTooltip() => (romance
                ? gender == (Gender)3 ? "WBR.PanromanticChanceTooltip" : "WBR.EnbyromanticChanceTip"
                : gender == (Gender)3 ? "WBR.PansexualChanceTooltip" : "WBR.EnbysexualChanceTip").Translate();
            TaggedString QueerChanceLabel() => "WBR.QueerChance".Translate(QueerChance());
            TaggedString QueerChanceTooltip() => "WBR.QueerChanceTooltip".Translate();
        }
    }
}