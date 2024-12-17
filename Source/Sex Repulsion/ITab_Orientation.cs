using RimWorld;
using UnityEngine;
using Verse;

namespace BetterRomance
{
    public class ITab_Orientation : ITab
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

        protected override void FillTab()
        {
            Rect rect = new Rect(0f, 0f, size.x, size.y).ContractedBy(10f);
            Listing_Standard list = new();
            list.Begin(rect);
            Text.Font = GameFont.Small;
            if (Prefs.DevMode && SelPawn.IsAsexual())
            {
                string str = "DEV: Edit Repulsion";
                float pct = (Text.CalcSize(str).x + 24f) / size.x;
                list.CheckboxLabeled(str, ref OrientationUtility.editRepulsion, labelPct: pct);
            }
            //This will just display information, maybe a more detailed explanation of their orientation and the gender attraction comp stuff if/when I implement that
            string text;
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
            else
            {
                text = "Error determining orientation";
            }
            list.Label(text.Translate(SelPawn));

            //Orientation comp
            var comp = SelPawn.CheckForComp<Comp_Orientation>();
            //I think I want these to use checkmark/X instead of True/False
            //Using Listing_Standard.CheckboxLabeled would require exposing the private variables, which I don't want to do. I don't want those being set directly, you have to use SetAttraction
            //So I'll probably need to use a settings handler to attach a function to the button
            list.Label("Sexual attraction:");
            list.Label($"     Men: {comp.sexual.Men}");
            list.Label($"     Women: {comp.sexual.Women}");
            list.Label($"     Enby: {comp.sexual.Enby}");
            list.Label("Romantic attraction:");
            list.Label($"     Men: {comp.romantic.Men}");
            list.Label($"     Women: {comp.romantic.Women}");
            list.Label($"     Enby: {comp.romantic.Enby}");

            //Display sex repulsion, describe the effects of their specific rating.
            if (SelPawn.IsAsexual())
            {
                float repulsion = SelPawn.GetStatValue(StatDef.Named("AsexualRating"));
                string repulsionString = StatDef.Named("AsexualRating").ValueToString(repulsion);
                list.Label("WBR.AsexualExplanation".Translate(SelPawn, repulsionString));
                //Insert a slider if edit option is selected
                if (OrientationUtility.editRepulsion)
                {
                    SelPawn.CheckForComp<Comp_SexRepulsion>().rating = 1f - list.Slider(repulsion, 0f, 1f);
                }
                if (repulsion >= 0.8f)
                {
                    list.Label("WBR.Repulsion08".Translate(SelPawn, repulsionString));
                }
                else if (repulsion >= 0.5f)
                {
                    list.Label("WBR.Repulsion05".Translate(SelPawn, repulsionString));
                }
                else if (repulsion >= 0.4f)
                {
                    list.Label("WBR.Repulsion04".Translate(SelPawn, repulsionString));
                }
                else
                {
                    list.Label("WBR.Repulsion00".Translate(SelPawn, repulsionString));
                }
            }
            list.End();
        }

        public ITab_Orientation()
        {
            size = new(400f, 300f);
        }

        public bool CanShowSexualityTab()
        {
#if v1_4
            return SelPawn.TryGetComp<Comp_Orientation>() is not null;
#else
            return SelPawn.HasComp<Comp_Orientation>();
#endif
        }
    }
}
