using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using HarmonyLib;

namespace BetterRomance
{
    public class Comp_SexRepulsion : ThingComp
    {
        public Pawn Pawn => (Pawn)parent;
        public float rating;

        float InitialRating()
        {
            Rand.PushState();
            Rand.Seed = Pawn.thingIDNumber;
            float rating = Rand.Range(0f, 1f);
            Rand.PopState();
            return rating;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref rating, "asexualRating", InitialRating());
        }
    }
}