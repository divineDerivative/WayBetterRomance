using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using HarmonyLib;
using UnityEngine;

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

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            rating = InitialRating();
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref rating, "asexualRating", -1f);
        }
    }

    [HarmonyPatch("<>c__DisplayClass42_2", "<DoLeftSection>b__7")]
    public static class TraitShit
    {
        public static void Postfix(ref Rect r, Trait trait)
        {
            Pawn pawn = trait.pawn;
            if (pawn.IsAsexual() && RomanceUtilities.asexualTraits.Contains(trait.def))
            {
                if (Widgets.ButtonInvisible(r))
                {
                    InspectPaneUtility.OpenTab(typeof(ITab_Sexuality));
                }
            }
        }
    }
}