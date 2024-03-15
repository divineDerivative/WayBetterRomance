using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace BetterRomance
{
    public class Comp_PartnerList : ThingComp
    {
        public Pawn Pawn => (Pawn)parent;
        public CompListVars Date;
        public CompListVars Hookup;
        private const int tickInterval = 120000;
        public int orderedHookupTick = -1;
        private const float distanceLimit = 100f;
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
                    //If p is more than limit cells away, continue
                    if (!p.Position.InHorDistOf(Pawn.Position, distanceLimit))
                    {
                        continue;
                    }
                    //Need to get the path to p and use the length, rather than 'as the crow flies' distance
                    if (p.IsFree(hookup ? RomanticActivityType.CasualHookup : RomanticActivityType.Date, out _) && !p.IsForbidden(Pawn))
                    {
                        //The path goes directly from cell to cell; the next cell is always one of the 8 surrounding the previous
                        //So the number of nodes can be used as a shortcut for the length of the path
                        //Horizontal/vertical moves are 1f in length, diagonal moves are 1.4142f (sqrt of 2) in length, so the actual distance the path covers will always be >= the number of nodes
                        PawnPath path = Pawn.Map.pathFinder.FindPath(Pawn.Position, p.Position, Pawn, PathEndMode.Touch);
                        //If the length of the path is longer than the limit, continue
                        if (path.NodesLeftCount <= distanceLimit)
                        {
                            partner = p;
                            //Since we're not actually using the path for anything, get rid of it
                            path?.Dispose();
                            break;
                        }
                        path?.Dispose();
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
            List<Pawn> result = new();
            //Removed asexual check, it instead goes in the joy givers that generate jobs that need this
            //Put existing partners in the list
            if (LovePartnerRelationUtility.HasAnyLovePartner(pawn))
            {
                foreach (Pawn p in RomanceUtilities.GetAllLoveRelationPawns(pawn, false, true))
                {
                    //Skip pawns they share a bed with except for dates
                    if (hookup && RomanceUtilities.DoWeShareABed(pawn, p))
                    {
                        continue;
                    }
                    result.Add(p);
                }
            }
            //Stop here if non-spouse lovin' is not allowed
            if (!IdeoUtility.DoerWillingToDo(HistoryEventDefOf.GotLovin_NonSpouse, pawn) && hookup)
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
                        if ((memory == null && pawn.relations.OpinionOf(p) >= pawn.MinOpinionForHookup()) || !hookup)
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

    public class CompListVars
    {
        public List<Pawn> list;
        public int ticksSinceMake = 0;
        public bool listMadeEver = false;
    }
}

//This is the patch to apply the comp only to pawns with the joy need, since it's only used for joy activities
//Also adds the asexual comp to asexual pawns
namespace BetterRomance.HarmonyPatches
{
    [HarmonyPatch(typeof(Pawn_NeedsTracker), nameof(Pawn_NeedsTracker.AddOrRemoveNeedsAsAppropriate))]
    public static class Pawn_NeedsTracker_AddOrRemoveNeedsAsAppropriate
    {
        public static void Postfix(Pawn_NeedsTracker __instance, Pawn ___pawn)
        {
            if (__instance.joy != null)
            {
                ___pawn.CheckForComp<Comp_PartnerList>();
            }
            if (___pawn.IsAsexual())
            {
                ___pawn.CheckForComp<Comp_SexRepulsion>();
            }
        }
    }
}