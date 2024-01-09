using RimWorld;
using Verse;
using UnityEngine;

namespace BetterRomance
{
    public class ITab_Sexuality : ITab
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
            Listing_Standard list = new Listing_Standard();
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
            string text;
            switch (orientation)
            {
                case Orientation.Hetero:
                    text = "This person is heteroromantic";
                    break;
                case Orientation.Homo:
                    text = "This person is homoromantic";
                    break;
                case Orientation.Bi:
                    text = "This person is biromantic";
                    break;
                case Orientation.None:
                default:
                    text = "This person is aromantic";
                    break;
            }
            list.Label(text);
            //Display sex repulsion, describe the effects of their specific rating.
            if (SelPawn.IsAsexual())
            {
                list.Label($"{SelPawn.LabelShort} is asexual. This means she will never initiate lovin', which includes asking someone to hook up.");
                float repulsion = SelPawn.GetStatValue(StatDef.Named("AsexualRating"));
                string repulsionString = StatDef.Named("AsexualRating").ValueToString(repulsion);
                list.Label($"Their sex repulsion is {repulsionString}. The lower their sex repulsion, the more likely they are to agree to lovin' when someone else initiates.");
                if (SexualityUtility.editRepulsion)
                {
                    Comp_SexRepulsion comp = SelPawn.CheckForAsexualComp();
                    comp.rating = 1f - list.Slider(repulsion, 0f, 1f);
                }
                if (repulsion >= 0.8f)
                {
                    list.Label($"At {repulsionString} they will never agree to lovin' no matter who asks them. They will only agree to marry an asexual person.");
                }
                else if (repulsion >= 0.5f)
                {
                    list.Label($"At {repulsionString} they will only agree to lovin' with existing partners. They will have negative thoughts about any lovin'.");
                }
                else if (repulsion >= 0.4f)
                {
                    list.Label($"At {repulsionString} they can agree to lovin' with anyone, but will have slightly negative thoughts about it.");
                }
                else
                {
                    list.Label($"At {repulsionString} they can agree to lovin' with anyone, and will have positive thoughts about it.");
                }
            }
            list.End();
        }

        public ITab_Sexuality()
        {
            size = new Vector2(400f, 300f);
        }

        public bool CanShowSexualityTab()
        {
            return SelPawn.RaceProps.Humanlike;
        }
    }
}
