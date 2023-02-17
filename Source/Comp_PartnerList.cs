using Verse;
using System.Collections.Generic;
using RimWorld;
using HarmonyLib;

namespace BetterRomance
{
    public class Comp_PartnerList : ThingComp
    {
        public CompProperties_PartnerList Props => (CompProperties_PartnerList)props;
        public Pawn Pawn => (Pawn)parent;
        public CompListVars Date;
        public CompListVars Hookup;
        private const int tickInterval = 120000;
        public int orderedHookupTick = -1;
        public bool IsOrderedHookupOnCooldown => orderedHookupTick > Find.TickManager.TicksGame;

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
            Scribe_Values.Look(ref Date.ticksSinceMake, "dateTicks", 0);
            Scribe_Values.Look(ref Date.listMadeEver, "dateListMade", false);
            Scribe_Collections.Look(ref Hookup.list, "hookupList", LookMode.Reference);
            Scribe_Values.Look(ref Hookup.ticksSinceMake, "hookupTicks", 0);
            Scribe_Values.Look(ref Hookup.listMadeEver, "hookupListMade", false);
            Scribe_Values.Look(ref orderedHookupTick, "orderedHookupTick", 0);
        }

        public void TryMakeList(bool hookup)
        {
            CompListVars type = hookup ? Hookup : Date;
            if ((type.list.NullOrEmpty() && !type.listMadeEver) || type.ticksSinceMake > tickInterval)
            {
                type.list = FindAttractivePawns(Pawn, hookup);
                type.listMadeEver = true;
                type.ticksSinceMake = 0;
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
                    if (RomanceUtilities.IsPawnFree(p) && !p.IsForbidden(Pawn))
                    {
                        partner = p;
                        break;
                    }
                }
            }
            return partner;
        }

        /// <summary>
        /// Builds a list of up to five other pawns that <paramref name="pawn"/> finds suitable for the given activity. Looks at romance chance factor for hookups and opinion for dates.
        /// </summary>
        /// <param name="pawn">The pawn who is looking</param>
        /// <param name="hookup">Whether this list is for a hookup or a date</param>
        /// <returns>A list of pawns, with love relations first, then descending order by the secondary factor.</returns>
        private static List<Pawn> FindAttractivePawns(Pawn pawn, bool hookup = true)
        {
            List<Pawn> result = new List<Pawn>();
            //Removed asexual check, it instead goes in the joy givers that generate jobs that need this
            //Put existing partners in the list
            if (LovePartnerRelationUtility.HasAnyLovePartner(pawn))
            {
                foreach (Pawn p in RomanceUtilities.GetAllLoveRelationPawns(pawn, false, true))
                {
                    //Skip pawns they share a bed with except for dates
                    if (!RomanceUtilities.DoWeShareABed(pawn, p) || !hookup)
                    {
                        result.Add(p);
                    }
                }
            }
            //Stop here if non-spouse lovin' is not allowed
            if (!new HistoryEvent(HistoryEventDefOf.GotLovin_NonSpouse, pawn.Named(HistoryEventArgsNames.Doer)).DoerWillingToDo() && hookup)
            {
                return result;
            }
            //Then grab some attractive pawns
            while (result.Count < 5)
            {
                //For a hookup we need to start with a non-zero number, or pawns that they actually don't find attractive end up on the list
                //For a date we can start at zero since we're looking at opinion instead of romance factor
                float num = hookup ? 0.15f : 0f;
                Pawn tempPawn = null;
                foreach (Pawn p in RomanceUtilities.GetAllSpawnedHumanlikesOnMap(pawn.Map))
                {
                    //Skip them if they're already in the list, or they share a bed if it's a hookup
                    if (result.Contains(p) || pawn == p || (RomanceUtilities.DoWeShareABed(pawn, p) && hookup))
                    {
                        continue;
                    }
                    //Also skip if slave status is not the same for both pawns for hookups only
                    else if (pawn.IsSlave != p.IsSlave && hookup)
                    {
                        continue;
                    }
                    //Skip if prisoner status is not the same
                    else if (pawn.IsPrisoner != p.IsPrisoner)
                    {
                        continue;
                    }
                    //For hookup check romance factor, for date check opinion
                    else if ((pawn.relations.SecondaryRomanceChanceFactor(p) > num && hookup) || (pawn.relations.OpinionOf(p) > num && !hookup))
                    {
                        //This will skip people who recently turned them down
                        Thought_Memory memory = pawn.needs.mood.thoughts.memories.Memories.Find(delegate (Thought_Memory x)
                        {
                            return x.def == (hookup ? RomanceDefOf.RebuffedMyHookupAttempt : RomanceDefOf.RebuffedMyDateAttempt) && x.otherPawn == p;
                        });
                        //Need to also check opinion against setting for a hookup
                        if ((memory == null && pawn.relations.OpinionOf(p) > pawn.MinOpinionForHookup()) || !hookup)
                        {
                            //romance factor for hookup, opinion for date
                            num = hookup ? pawn.relations.SecondaryRomanceChanceFactor(p) : pawn.relations.OpinionOf(p);
                            tempPawn = p;
                        }
                    }
                }
                if (tempPawn == null)
                {
                    break;
                }
                result.Add(tempPawn);
            }

            return result;
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
        public int ticksSinceMake = 0;
        public bool listMadeEver = false;
    }
}

//This is the patch to apply the comp only to pawns with the joy need, since it's only used for joy activities
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