using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using static BetterRomance.WBRLogger;

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

        public OrientationChances ConvertToOrientation(Gender gender, ref OrientationChances orientation)
        {
            float hetero, homo, bi, none, enby;

            switch (gender)
            {
                case Gender.Male:
                    hetero = notMen * women;
                    homo = men * notWomen;
                    bi = men * women;
                    none = notMen * notWomen;
                    enby = this.enby;
                    break;
                case Gender.Female:
                    hetero = men * notWomen;
                    homo = notMen * women;
                    bi = men * women;
                    none = notMen * notWomen;
                    enby = this.enby;

                    break;
                //Not actually sure if this can be converted to a valid set of chances
                case (Gender)3:
                    hetero = (men * notWomen + notMen * women) * notEnby;
                    homo = notMen * notWomen * this.enby;
                    bi = men * women * notEnby;
                    none = notMen * notWomen * notEnby;
                    enby = this.enby;
                    break;
                default:
                    throw new ArgumentException($"Invalid gender: {gender}");
            }

            //Assign separately to avoid precision errors
            orientation.hetero = Mathf.Round(hetero * 100f);
            orientation.homo = Mathf.Round(homo * 100f);
            orientation.bi = Mathf.Round(bi * 100f);
            orientation.none = Mathf.Round(none * 100f);
            orientation.enby = Mathf.Round(enby * 100f);

            if (!orientation.TotalCorrect)
            {
                LogUtil.Error($"Conversion from gender chances failed for {gender}. hetero: {orientation.hetero:R}, homo: {orientation.homo:R}, bi: {orientation.bi:R}, none: {orientation.none:R}");
            }

            return orientation;
        }
    }
}
