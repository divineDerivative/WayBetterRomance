using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using HarmonyLib;
using UnityEngine;
using static UnityEngine.Scripting.GarbageCollector;

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
            Rect rect2 = new Rect(0f, 0f, size.x, size.y).ContractedBy(10f);
            Listing_Standard list = new Listing_Standard();
            list.Begin(rect2);
            Text.Font = GameFont.Medium;
            list.Label("This is the title");
            Text.Font = GameFont.Small;
            //This will just display information, maybe a more detailed explanation of their orientation and the gender attraction comp stuff if/when I implement that
            var orientation = SelPawn.GetOrientation();
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
                list.Label($"{SelPawn.Label} is asexual. This means she will never initiate lovin', which includes asking someone to hook up.");
                float repulsion = SelPawn.GetStatValue(StatDef.Named("AsexualRating"));
                string repulsionString = StatDef.Named("AsexualRating").ValueToString(repulsion);
                list.Label("Their sex repulsion is " + repulsionString + ". The lower their sex repulsion, the more likely they are to agree to lovin' when someone else initiates.");
                if (repulsion >= 0.8f)
                {
                    list.Label("At " + repulsionString + " they will never agree to lovin' no matter who asks them. They will only agree to marry an asexual person.");
                }
                else if (repulsion >= 0.5f)
                {
                    list.Label("At " + repulsionString + " they will only agree to lovin' with existing partners. They will have negative thoughts about any lovin'.");
                }
                else if (repulsion >= 0.4f)
                {
                    list.Label("At " + repulsionString + " they can agree to lovin' with anyone, but will have slightly negative thoughts about it.");
                }
                else
                {
                    list.Label("At " + repulsionString + " they can agree to lovin' with anyone, and will have positive thoughts about it.");
                }
                //Have something to click on to edit it. Maybe tie this to dev mode ?
                //That will open a dialog box, which is a different thing code wise
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
