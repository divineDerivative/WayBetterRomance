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
                Gender.Male => romance ? romanticOrientations.attractionForMen : sexualOrientations.attractionForMen,
                Gender.Female => romance ? romanticOrientations.attractionForWomen : sexualOrientations.attractionForWomen,
                (Gender)3 => romance ? romanticOrientations.attractionForEnby : sexualOrientations.attractionForEnby,
                _ => new()
            };
        }

        internal OrientationChances GenderToOrientation(Gender gender, bool romance)
        {
            return gender switch
            {
                Gender.Male => romance ? romanticOrientations.orientationForMen : sexualOrientations.orientationForMen,
                Gender.Female => romance ? romanticOrientations.orientationForWomen : sexualOrientations.orientationForWomen,
                (Gender)3 => romance ? romanticOrientations.orientationForEnby : sexualOrientations.orientationForEnby,
                _ => new()
            };
        }

        internal void ConvertGenderToOrientation(Gender gender, bool romance)
        {
            OrientationChances orientations = GenderToOrientation(gender, romance);
            GenderToChances(gender, romance).ConvertToOrientation(gender, ref orientations);
        }

        internal void SetUpEquivalenceSection(UISection section, Gender gender, bool romance)
        {
            GenderAttractionChances chances = GenderToChances(gender, romance);
            SettingsHandler<Settings> handler = romance ? romanticHandler : sexualHandler;
            OrientationChances orientations = GenderToOrientation(gender, romance);

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

            //For men and women, adding enby does not change the orientation (except for bi->pan but that's fine) so it's not part of the total 
            float EnbyChance()
            {
                return gender switch
                {
                    //Can't be added to asexual
                    Gender.Male or Gender.Female => (orientations.Hetero + orientations.Bi + orientations.Homo) * chances.enby,
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

            TaggedString HeteroChanceLabel() => (romance ? "WBR.HeteroromanticChance" : "WBR.HeterosexualChance").Translate(orientations.Hetero);
            TaggedString HeteroChanceTooltip() => (romance ? "WBR.HeteroromanticChanceTip" : "WBR.HeterosexualChanceTip").Translate();
            TaggedString BiChanceLabel() => (romance ? "WBR.BiromanticChance" : "WBR.BisexualChance").Translate(orientations.Bi);
            TaggedString BiChanceTooltip() => (romance ? "WBR.BiromanticChanceTip" : "WBR.BisexualComplexChanceTip").Translate();
            TaggedString HomoChanceLabel() => (romance ? "WBR.HomoromanticChance" : "WBR.HomosexualChance").Translate(orientations.Homo);
            TaggedString HomoChanceTooltip() => (romance ? "WBR.HomoromanticChanceTip" : "WBR.HomosexualChanceTip").Translate();
            TaggedString NoneChanceLabel() => (romance ? "WBR.AromanticChance" : "WBR.AsexualChance").Translate(orientations.None);
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