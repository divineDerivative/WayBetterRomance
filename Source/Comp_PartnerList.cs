using Verse;
using System.Collections.Generic;
using RimWorld;
using HarmonyLib;
using System.Reflection;
using System;

namespace BetterRomance
{
    public class Comp_PartnerList : ThingComp
    {
        public CompProperties_PartnerList Props => (CompProperties_PartnerList)props;
        public Pawn Pawn => (Pawn)parent;
        public CompListVars Date;
        public CompListVars Hookup;
        public const float tickInterval = 120000f;

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            Date = new CompListVars();
            Hookup = new CompListVars();
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Collections.Look(ref Date.list, "dateList", LookMode.Reference);
            Scribe_Values.Look(ref Date.ticksSinceMake, "dateTicks", 0f);
            Scribe_Values.Look(ref Date.listMadeEver, "dateListMade", false);
            Scribe_Collections.Look(ref Hookup.list, "hookupList", LookMode.Reference);
            Scribe_Values.Look(ref Hookup.ticksSinceMake, "hookupTicks", 0f);
            Scribe_Values.Look(ref Hookup.listMadeEver, "hookupListMade", false);
        }

        public void TryMakeList(bool hookup)
        {
            CompListVars type = hookup ? Hookup : Date;
            if ((type.list.NullOrEmpty() && !type.listMadeEver) || type.ticksSinceMake > tickInterval)
            {
                type.list = RomanceUtilities.FindAttractivePawns(Pawn, hookup);
                type.listMadeEver = true;
                type.ticksSinceMake = 0f;
            }
        }

        public Pawn GetPartner(bool hookup)
        {
            TryMakeList(hookup);
            Pawn partner = null;
            CompListVars type = hookup ? Hookup : Date;
            if (!type.list.NullOrEmpty())
            {
                foreach (Pawn p in type.list)
                {
                    if (!p.Spawned)
                    {
                        continue;
                    }
                    if (RomanceUtilities.IsPawnFree(p) && ! p.IsForbidden(Pawn))
                    {
                        partner = p;
                        break;
                    }
                }
            }
            return partner;
        }

        public override void CompTick()
        {
            Hookup.ticksSinceMake++;
            Date.ticksSinceMake++;
        }
    }

    public class CompProperties_PartnerList : CompProperties
    {
        public CompProperties_PartnerList()
        {
            compClass = typeof(Comp_PartnerList);
        }
    }

    public class CompListVars
    {
        public List<Pawn> list;
        public float ticksSinceMake = 0f;
        public bool listMadeEver = false;
    }
}

//This is the patch to apply to comp only to pawns with the joy need, since it's only used for joy activities
namespace BetterRomance.HarmonyPatches
{
    [HarmonyPatch(typeof(Pawn_NeedsTracker), "AddOrRemoveNeedsAsAppropriate")]
    public static class Pawn_NeedsTracker_AddOrRemoveNeedsAsAppropriate
    {
        public static void Postfix(Pawn_NeedsTracker __instance, Pawn ___pawn)
        {
            if (__instance.joy != null)
            {
                ___pawn.CheckForPartnerComp();
            }
        }
    }
}