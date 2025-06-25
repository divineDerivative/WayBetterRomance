using DivineFramework;
using DivineFramework.UI;
using RimWorld;
using System;
using UnityEngine;
using Verse;

namespace BetterRomance
{
    public class ITab_Orientation : ITab_Handler
    {
        public override bool Hidden => true;

        public override bool IsVisible => CanShowSexualityTab();
        protected override bool StillValid
        {
            get
            {
                if (!base.StillValid)
                {
                    return false;
                }
                return CanShowSexualityTab();
            }
        }

        public Comp_Orientation SelPawnComp => SelPawn?.TryGetComp<Comp_Orientation>();
        private const float checkboxWidth = 0.3f;
        internal void SetUpHandler()
        {
            //Edit toggle
            //Move the checkbox to be closer to the label
            handler.RegisterNewRow("EditToggle")
                .HideWhen(() => !Prefs.DevMode)
                .Add(NewElement.Checkbox()
                .WithGetter(() => OrientationUtility.editOrientation)
                .WithSetter((bool value) => OrientationUtility.editOrientation = value)
                .WithLabel(() => "Edit Orientations"));
            //Description of sexual orientation
            handler.RegisterNewRow("SexualDescription")
                .AddLabel(() => OrientationDescription(false));
            //Men
            handler.RegisterNewRow("SexualMenRow").Add(NewElement.Checkbox(relative: checkboxWidth)
                .WithGetter(() => SelPawnComp.sexual.Men)
                .WithSetter((bool value) => SelPawnComp.SetSexualAttraction(Gender.Male, value))
                .WithLabel(() => "Men")
                .DisableWhen(DisableWhen(Gender.Male, false)));
            //Women
            handler.RegisterNewRow("SexualWomenRow").Add(NewElement.Checkbox(relative: checkboxWidth)
                .WithGetter(() => SelPawnComp.sexual.Women)
                .WithSetter((bool value) => SelPawnComp.SetSexualAttraction(Gender.Female, value))
                .WithLabel(() => "Women")
                .DisableWhen(DisableWhen(Gender.Female, false)));
            //Enby
            handler.RegisterNewRow("SexualEnbyRow").Add(NewElement.Checkbox(relative: checkboxWidth)
                .WithGetter(() => SelPawnComp.sexual.Enby)
                .WithSetter((bool value) => SelPawnComp.SetSexualAttraction((Gender)3, value))
                .WithLabel(() => "Non-binary")
                .DisableWhen(DisableWhen((Gender)3, false)));

            //Description of romantic orientation
            handler.RegisterNewRow("RomanticDescription")
                .AddLabel(() => OrientationDescription(true));
            //Men
            handler.RegisterNewRow("RomanticMenRow").Add(NewElement.Checkbox(relative: checkboxWidth)
                .WithGetter(() => SelPawnComp.romantic.Men)
                .WithSetter((bool value) => SelPawnComp.SetRomanticAttraction(Gender.Male, value))
                .WithLabel(() => "Men")
                .DisableWhen(DisableWhen(Gender.Male, true)));
            //Women
            handler.RegisterNewRow("RomanticWomenRow").Add(NewElement.Checkbox(relative: checkboxWidth)
                .WithGetter(() => SelPawnComp.romantic.Women)
                .WithSetter((bool value) => SelPawnComp.SetRomanticAttraction(Gender.Female, value))
                .WithLabel(() => "Women")
                .DisableWhen(DisableWhen(Gender.Female, true)));
            //Enby
            handler.RegisterNewRow("RomanticEnbyRow").Add(NewElement.Checkbox(relative: checkboxWidth)
                .WithGetter(() => SelPawnComp.romantic.Enby)
                .WithSetter((bool value) => SelPawnComp.SetRomanticAttraction((Gender)3, value))
                .WithLabel(() => "Non-binary")
                .DisableWhen(DisableWhen((Gender)3, true)));
        }

        string OrientationDescription(bool romance)
        {
            string text = string.Empty;
            if (romance)
            {
                if (SelPawn.IsAromantic())
                {
                    text = "WBR.Aromantic";
                }
                else if (SelPawn.IsBi(true))
                {
                    text = "WBR.Biromantic";
                }
                else if (SelPawn.IsGay(true))
                {
                    text = "WBR.Homoromantic";
                }
                else if (SelPawn.IsStraight(true))
                {
                    text = "WBR.Heteroromantic";
                }
            }
            else
            {
                if (SelPawn.IsAsexual())
                {
                    float repulsion = SelPawn.GetStatValue(StatDef.Named("AsexualRating"));
                    string repulsionString = StatDef.Named("AsexualRating").ValueToString(repulsion);
                    return "WBR.AsexualExplanation".Translate(SelPawn, repulsionString);
                }
                else if (SelPawn.IsBi(false))
                {
                    text = "WBR.Bisexual";
                }
                else if (SelPawn.IsGay(false))
                {
                    text = "WBR.Homosexual";
                }
                else if (SelPawn.IsStraight(false))
                {
                    text = "WBR.Heterosexual";
                }
            }
            if (text == string.Empty)
            {
                text = "Error determining orientation";
            }
            return text.Translate(SelPawn);
        }

        //protected override void FillTab()
        //{
        //    Rect rect = new Rect(0f, 0f, size.x, size.y).ContractedBy(10f);
        //    Listing_Standard list = new();
        //    list.Begin(rect);
        //    Text.Font = GameFont.Small;
        //    if (Prefs.DevMode && SelPawn.IsAsexual())
        //    {
        //        string str = "DEV: Edit Repulsion";
        //        float pct = (Text.CalcSize(str).x + 24f) / size.x;
        //        list.CheckboxLabeled(str, ref OrientationUtility.editRepulsion, labelPct: pct);
        //    }
        //    //Orientation comp
        //    var comp = SelPawn.CheckForComp<Comp_Orientation>();

        //    //Display sex repulsion, describe the effects of their specific rating.
        //    if (SelPawn.IsAsexual())
        //    {
        //        float repulsion = SelPawn.GetStatValue(StatDef.Named("AsexualRating"));
        //        string repulsionString = StatDef.Named("AsexualRating").ValueToString(repulsion);
        //        list.Label();
        //        //Insert a slider if edit option is selected
        //        if (OrientationUtility.editRepulsion)
        //        {
        //            SelPawn.CheckForComp<Comp_SexRepulsion>().rating = 1f - list.Slider(repulsion, 0f, 1f);
        //        }
        //        if (repulsion >= 0.8f)
        //        {
        //            list.Label("WBR.Repulsion08".Translate(SelPawn, repulsionString));
        //        }
        //        else if (repulsion >= 0.5f)
        //        {
        //            list.Label("WBR.Repulsion05".Translate(SelPawn, repulsionString));
        //        }
        //        else if (repulsion >= 0.4f)
        //        {
        //            list.Label("WBR.Repulsion04".Translate(SelPawn, repulsionString));
        //        }
        //        else
        //        {
        //            list.Label("WBR.Repulsion00".Translate(SelPawn, repulsionString));
        //        }
        //    }
        //    list.End();
        //}

        public ITab_Orientation() : base()
        {
            LogUtil.Message($"Instantiating ITab_Orientation");
            handler ??= new(true, SetUpHandler);
            labelKey = "Orientation";
            size = new Vector2(430f, 500f);
        }

        public bool CanShowSexualityTab()
        {
#if v1_4
            return SelPawn.TryGetComp<Comp_Orientation>() is not null;
#else
            return SelPawn.HasComp<Comp_Orientation>();
#endif
        }

        protected override void UpdateSize()
        {
            size.y = handler.height + (margin * 2);
        }

        //Consider a tooltip for explaining why a gender cannot be toggled
        public bool CanDisable(Gender gender, bool romance)
        {
            Comp_Orientation.AttractionVars editComp = romance ? SelPawnComp.romantic : SelPawnComp.sexual;
            Comp_Orientation.AttractionVars otherComp = romance ? SelPawnComp.sexual : SelPawnComp.romantic;
            //Do not allow removing a gender if it would result in a binary person having only non-binary attraction
            //This would only be the case when trying to remove a binary gender, and the current attraction was men and enby or women and enby
            if (!SelPawn.IsEnby() && gender != (Gender)3 && editComp.Enby)
            {
                if ((editComp.Men && !editComp.Women) || (editComp.Women && !editComp.Men))
                {
                    return false;
                }
            }
            //If the other set is none, we can remove whatever
            if (otherComp.None)
            {
                return true;
            }
            switch (gender)
            {
                case Gender.Male:
                    //Disabling a gender is always valid if it will result in aromantic
                    if (!editComp.Women && !editComp.Enby)
                    {
                        return true;
                    }
                    //If they have overlapping attraction to another gender, we can disable this one
                    if (otherComp.Women && editComp.Women)
                    {
                        return true;
                    }
                    if (otherComp.Enby && editComp.Enby)
                    {
                        return true;
                    }
                    return false;
                case Gender.Female:
                    //Disabling a gender is always valid if it will result in aromantic
                    if (!editComp.Men && !editComp.Enby)
                    {
                        return true;
                    }
                    //If they have overlapping attraction to another gender, we can disable this one
                    if (otherComp.Men && editComp.Men)
                    {
                        return true;
                    }
                    if (otherComp.Enby && editComp.Enby)
                    {
                        return true;
                    }
                    return false;
                case (Gender)3:
                    //Disabling a gender is always valid if it will result in aromantic
                    if (!editComp.Men && !editComp.Women)
                    {
                        return true;
                    }
                    //If they have overlapping attraction to another gender, we can disable this one
                    if (otherComp.Men && editComp.Men)
                    {
                        return true;
                    }
                    if (otherComp.Women && editComp.Women)
                    {
                        return true;
                    }
                    return false;
                default:
                    throw new ArgumentException($"Invalid gender: {gender}");
            }
        }

        public bool CanEnable(Gender gender, bool romance)
        {
            Comp_Orientation.AttractionVars editComp = romance ? SelPawnComp.romantic : SelPawnComp.sexual;
            Comp_Orientation.AttractionVars otherComp = romance ? SelPawnComp.sexual : SelPawnComp.romantic;
            //If they are asexual or aromantic, non-binary can only be added if they are non-binary themselves
            if (gender == (Gender)3 && editComp.None)
            {
                return SelPawn.IsEnby();
            }
            //If the other set is none, we can do whatever for the binary genders
            if (otherComp.None)
            {
                return true;
            }
            switch (gender)
            {
                case Gender.Male:
                    //Enabling a gender is always valid if they're already sexually attracted to it
                    if (otherComp.Men)
                    {
                        return true;
                    }
                    //If they have overlapping attraction to another gender, we can enable this one without having to check their sexual attraction
                    if (editComp.Women && otherComp.Women)
                    {
                        return true;
                    }
                    if (editComp.Enby && otherComp.Enby)
                    {
                        return true;
                    }
                    return false;
                case Gender.Female:
                    //Enabling a gender is always valid if they're already sexually attracted to it
                    if (otherComp.Women)
                    {
                        return true;
                    }
                    //If they have overlapping attraction to another gender, we can enable this one without having to check their sexual attraction
                    if (editComp.Men && otherComp.Men)
                    {
                        return true;
                    }
                    if (editComp.Enby && otherComp.Enby)
                    {
                        return true;
                    }
                    return false;
                case (Gender)3:
                    //Enabling a gender is always valid if they're already sexually attracted to it
                    if (otherComp.Enby)
                    {
                        return true;
                    }
                    //If they have overlapping attraction to another gender, we can enable this one without having to check their sexual attraction
                    if (editComp.Men && otherComp.Men)
                    {
                        return true;
                    }
                    if (editComp.Women && otherComp.Women)
                    {
                        return true;
                    }
                    return false;
                default:
                    return false;
            }
        }

        internal HideDelegate DisableWhen(Gender gender, bool romance)
        {
            return () =>
            {
                if (SelPawnComp.AttractedTo(gender, romance))
                {
                    // If currently attracted, disable if CanDisable returns false
                    return !CanDisable(gender, romance);
                }
                else
                {
                    // If not currently attracted, disable if CanEnable returns false
                    return !CanEnable(gender, romance);
                }
            };
        }
    }
}
