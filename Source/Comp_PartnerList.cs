using Verse;
using System.Collections.Generic;
using RimWorld;

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

        public void TryMakeList(bool hookup)
        {
            CompListVars type = hookup ? Hookup : Date;
            if ((type.list.NullOrEmpty() && !type.listMadeThisSession) || type.ticksSinceMake > tickInterval)
            {
                type.list = RomanceUtilities.FindAttractivePawns(Pawn, hookup);
                type.listMadeThisSession = true;
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
        public bool listMadeThisSession = false;
    }
}