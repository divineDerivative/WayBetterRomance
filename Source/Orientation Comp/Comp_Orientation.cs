using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using System;
using Verse;
#if !v1_4
using LudeonTK;
#endif
using static BetterRomance.WBRLogger;

namespace BetterRomance.Orientation_Comp
{
    public class Comp_Orientation : ThingComp
    {
        public class AttractionVars
        {
            public bool men;
            public bool women;
            public bool enby;
        }

        public AttractionVars sexual;
        public AttractionVars romantic;
        public Pawn Pawn => parent as Pawn;
        public Gender Gender => Pawn.gender;

        public bool SexuallyAttractedToGender(Gender gender)
        {
            switch (gender)
            {
                case Gender.Male:
                    return sexual.men;
                case Gender.Female:
                    return sexual.women;
                case (Gender)3:
                    return sexual.enby;
                default:
                    return false;
            }
        }

        public bool RomanticallyAttractedToGender(Gender gender)
        {
            switch (gender)
            {
                case Gender.Male:
                    return romantic.men;
                case Gender.Female:
                    return romantic.women;
                case (Gender)3:
                    return romantic.enby;
                default:
                    return false;
            }
        }

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            sexual = new AttractionVars();
            romantic = new AttractionVars();
        }

        [DebugAction("WBR", name: "Convert orientation", actionType = DebugActionType.ToolMapForPawns)]
        public static void ConvertOrientation(Pawn pawn)
        {
            Comp_Orientation comp = pawn.TryGetComp<Comp_Orientation>();
            Gender gender = pawn.gender;
            if (comp == null)
            {
                if (pawn.IsAro())
                {
                    comp.SetAttraction(Gender.Male, false, true, true);
                    comp.SetAttraction(Gender.Female, false, true, true);
                    comp.SetAttraction((Gender)3, false, true, true);
                    return;
                }
                if (pawn.IsBi())
                {
                    comp.SetAttraction(Gender.Male, true, true, true);
                    comp.SetAttraction(Gender.Female, true, true, true);
                }
                if (pawn.IsHetero())
                {
                    comp.SetAttraction(gender, false, true, true);
                    comp.SetAttraction(gender.Opposite(), true, true, true);
                }
                if (pawn.IsHomo())
                {
                    comp.SetAttraction(gender, true, true, true);
                    comp.SetAttraction(gender.Opposite(), false, true, true);
                }
                if (pawn.IsAsexual())
                {
                    comp.SetAttraction(Gender.Male, false, true, false);
                    comp.SetAttraction(Gender.Female, false, true, false);
                    comp.SetAttraction((Gender)3, false, true, false);
                }
                if (!Settings.NonBinaryActive)
                {
                    comp.SetAttraction((Gender)3, false, true, true);
                }
            }
            LogUtil.Error($"{pawn.LabelShort} is {comp.GetLabel()}");
            //Remove the orientation trait here probably
        }

        private void SetAttraction(Gender gender, bool result, bool sexual, bool romantic)
        {
            if (sexual)
            {
                switch (gender)
                {
                    case Gender.Male:
                        this.sexual.men = result;
                        break;
                    case Gender.Female:
                        this.sexual.women = result;
                        break;
                    default:
                        this.sexual.enby = result;
                        break;
                }
            }
            if (romantic)
            {
                switch (gender)
                {
                    case Gender.Male:
                        this.romantic.men = result;
                        break;
                    case Gender.Female:
                        this.romantic.women = result;
                        break;
                    default:
                        this.romantic.enby = result;
                        break;
                }
                this.romantic.women = result;
            }
        }

        public string GetLabel()
        {
            //Bisexual, pansexual, straight, gay, asexual
            if (sexual.men && sexual.women && sexual.enby)
            {
                return "Pansexual";
            }
            if (sexual.men && sexual.women)
            {
                return "Bisexual";
            }
            if (!sexual.men && !sexual.women && (!Settings.NonBinaryActive || !sexual.enby))
            {
                return "Asexual";
            }
            return SexuallyAttractedToGender(Gender) ? "Gay" : "Straight";
        }
    }
}
