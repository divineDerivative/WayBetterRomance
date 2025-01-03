using DivineFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace BetterRomance
{
    public class GenderAttractionChances : IExposable
    {
        public float men = 0.5f;
        public float women = 0.5f;
        public float enby = 0.25f;

        public float notMen => 1f - men;
        public float notWomen => 1f - women;
        public float notEnby => 1f - enby;

        public void CopyFrom(GenderAttractionChances other)
        {
            men = other.men;
            women = other.women;
            enby = other.enby;
        }

        public void Reset()
        {
            men = 0.5f;
            women = 0.5f;
            enby = 0.25f;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref men, "men", 0.5f);
            Scribe_Values.Look(ref women, "women", 0.5f);
            Scribe_Values.Look(ref enby, "enby", 0.25f);
        }
    }
}
