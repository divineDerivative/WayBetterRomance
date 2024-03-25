using RimWorld;
using Verse;
using UnityEngine;

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
                return SelPawn.RaceProps.Humanlike;
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
                list.CheckboxLabeled(str, ref SexualityUtility.editRepulsion, labelPct: pct);
            }
            //This will just display information, maybe a more detailed explanation of their orientation and the gender attraction comp stuff if/when I implement that
            Orientation orientation = SelPawn.GetOrientation();
            string text = orientation switch
            {
                Orientation.Hetero => "WBR.Heteroromantic",
                Orientation.Homo => "WBR.Homoromantic",
                Orientation.Bi => "WBR.Biromantic",
                _ => "WBR.Aromantic",
            };
            list.Label(text.Translate(SelPawn));
            //Display sex repulsion, describe the effects of their specific rating.
            if (SelPawn.IsAsexual())
            {
                float repulsion = SelPawn.GetStatValue(StatDef.Named("AsexualRating"));
                string repulsionString = StatDef.Named("AsexualRating").ValueToString(repulsion);
                list.Label("WBR.AsexualExplanation".Translate(SelPawn, repulsionString));
                //Insert a slider if edit option is selected
                if (SexualityUtility.editRepulsion)
                {
                    Comp_SexRepulsion comp = SelPawn.CheckForComp<Comp_SexRepulsion>();
                    comp.rating = 1f - list.Slider(repulsion, 0f, 1f);
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
            size = new Vector2(400f, 300f);
        }

        public bool CanShowSexualityTab()
        {
            return SelPawn.RaceProps.Humanlike;
        }
    }
}
