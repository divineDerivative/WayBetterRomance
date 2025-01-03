using DivineFramework.UI;
using Verse;

namespace BetterRomance
{
    //I have too many variations of the same thing that each need their own variables and such
    //I think I should make a separate object that can handle those differences
    public partial class Settings : ModSettings
    {
        internal class ComplexOrientationDisplay : UIColumn
        {
            //Two bool values and three gender values covers the six different objects I need
            internal Gender gender;
            internal bool romance;
            //Each variation needs:
            //A label with a button to switch between views
            UIRow headerRow;
            LabelDelegate HeaderLabel;
            //A gender attraction box
            UIColumn genderBox;
            //This includes a section box and buttons
            //An orientation chances box
            UIColumn orientationBox;
            //This includes a section box and buttons
            //A bool to determine if we're looking at gender attraction or orientation chances
            internal bool genderView = true;
            //This is for naming the elements
            string prefix;

            internal ComplexOrientationDisplay(Gender gender, bool romance, Settings settings)
            {
                this.gender = gender;
                this.romance = romance;
                this.relativeWidth = 1f;
                if (gender == Gender.Male)
                {
                    HeaderLabel = () => (genderView ? "WBR.GenderAttractionHeading" : "WBR.OrientationsFor").Translate("WBR.Men".Translate());
                    prefix = romance ? "RomanceMen" : "SexualMen";
                }
                else if (gender == Gender.Female)
                {
                    HeaderLabel = () => (genderView ? "WBR.GenderAttractionHeading" : "WBR.OrientationsFor").Translate("WBR.Women".Translate());
                    prefix = romance ? "RomanceWomen" : "SexualWomen";
                }
                else
                {
                    HeaderLabel = () => (genderView ? "WBR.GenderAttractionHeading" : "WBR.OrientationsFor").Translate("WBR.Enby".Translate());
                    prefix = romance ? "RomanceEnby" : "SexualEnby";
                }
                headerRow = AddRow(name: prefix + "Header");
                headerRow.AddLabel(HeaderLabel, name: prefix + "HeaderLabel");
                headerRow.Add(NewElement.Button(() => genderView = !genderView, buttonHeight: Heights.TextLineSmall)
                    .WithLabel(() => (genderView ? "WBR.SwitchToOrientation" : "WBR.SwitchToGender").Translate()), name: prefix + "HeaderButton");

                MakeGenderBox(settings);
                MakeOrientationBox(settings);
            }

            //The gender box is a column that contains a section and one row
            //The section will be the sliders and the row is the buttons
            void MakeGenderBox(Settings settings)
            {
                genderBox = AddColumn(name: prefix + "GenderBox");
                UISection section = genderBox.AddSection(sectionBorder: 6f, name: prefix + "GenderSectionBox");
                GenderAttractionChances chances = settings.GenderToChances(gender, romance);

                section.AddLabel(() => "WBR.MenPercentage".Translate(chances.men), name: prefix + "MenPercentageLabel");
                section.Add(NewElement.Slider<float>()
                    .MinMax(0f, 1f)
                    .WithReference(chances, nameof(chances.men), chances.men), name: prefix + "MenSlider");

                section.AddLabel(() => "WBR.WomenPercentage".Translate(chances.women), name: prefix + "WomenPercentageLabel");
                section.Add(NewElement.Slider<float>()
                    .MinMax(0f, 1f)
                    .WithReference(chances, nameof(chances.women), chances.women), name: prefix + "WomenSlider");

                if (NonBinaryActive)
                {
                    section.AddLabel(() => "WBR.NonBinaryPercentage".Translate(chances.enby), name: prefix + "EnbyPercentageLabel");
                    section.Add(NewElement.Slider<float>()
                        .MinMax(0f, 1f)
                        .WithReference(chances, nameof(chances.enby), chances.enby), name: prefix + "EnbySlider");
                }
                genderBox.AddSpace(8f);
                UIRow buttonRow = genderBox.AddRow(gap: 0f, name: prefix + "GenderButtonRow");
                if (gender != Gender.Male)
                {
                    buttonRow.Add(NewElement.Button(() => chances.CopyFrom(settings.GenderToChances(gender - 1, romance)))
                        .WithLabel("WBR.MatchAboveButton".Translate), name: prefix + "MatchAboveButton");
                }
                if (gender != (NonBinaryActive ? (Gender)3 : Gender.Female))
                {
                    buttonRow.Add(NewElement.Button(() => chances.CopyFrom(settings.GenderToChances(gender + 1, romance)))
                        .WithLabel("WBR.MatchBelowButton".Translate), name: prefix + "MatchBelowButton");
                }
                buttonRow.Add(NewElement.Button(chances.Reset)
                    .WithLabel("Reset".Translate), name: prefix + "ResetButton");
                genderBox.HideWhen(() => !genderView)
                    .WithPostDraw(() => settings.ConvertGenderAttractionToOrientationChances(gender, romance));
            }

            //The orientation box is a column that contains a section and one row
            //The section will be the sliders and the row is the buttons
            void MakeOrientationBox(Settings settings)
            {
                orientationBox = AddColumn(name: prefix + "OrientationBox");
                UISection section = orientationBox.AddSection(sectionBorder: 6f, name: prefix + "OrientationSectionBox");
                OrientationChances chances = settings.GenderToOrientation(gender, romance);
                settings.SetUpOrientationChanceSection(section, romance, chances);
                orientationBox.AddSpace(8f);
                UIRow buttonRow = orientationBox.AddRow(gap: 0f, name: prefix + "OrientationButtonRow");
                if (gender != Gender.Male)
                {
                    buttonRow.Add(NewElement.Button(() => chances.CopyFrom(settings.GenderToOrientation(gender - 1, romance)))
                        .WithLabel("WBR.MatchAboveButton".Translate));
                }
                if (gender != (NonBinaryActive ? (Gender)3 : Gender.Female))
                {
                    buttonRow.Add(NewElement.Button(() => chances.CopyFrom(settings.GenderToOrientation(gender + 1, romance)))
                        .WithLabel("WBR.MatchBelowButton".Translate));
                }
                buttonRow.Add(NewElement.Button(chances.Reset)
                    .WithLabel("Reset".Translate));
                orientationBox.HideWhen(() => genderView);
            }
        }
    }
}
