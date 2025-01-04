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

            hetero *= 100f;
            homo *= 100f;
            bi *= 100f;
            none *= 100f;
            enby *= 100f;

            float roundedHetero = Mathf.Round(hetero);
            float roundedHomo = Mathf.Round(homo);
            float roundedBi = Mathf.Round(bi);
            float roundedNone = Mathf.Round(none);
            orientation.enby = Mathf.Round(enby);

            if (roundedHetero + roundedHomo +  roundedBi + roundedNone == 100f)
            {
                orientation.hetero = roundedHetero;
                orientation.homo = roundedHomo;
                orientation.bi = roundedBi;
                orientation.none = roundedNone;
                return orientation;
            }

            //This is to attempt to fix rounding errors when there's two values that end in .5
            List<float> list = [hetero, homo, bi, none];

            for (int i = 0; i < 3; i++)
            {
                float first = list[i];
                if (first % 1 == 0)
                {
                    continue;
                }
                float second = list[i + 1];
                float preSum = Mathf.Round(first + second);
                first = Mathf.Round(first);
                second = Mathf.Round(second);
                float postSum = first + second;
                if (preSum != postSum)
                {
                    second += (preSum - postSum);

                }
                list[i] = first;
                list[i + 1] = second;
            }

            orientation.hetero = list[0];
            orientation.homo = list[1];
            orientation.bi = list[2];
            orientation.none = list[3];

            if (!orientation.TotalCorrect)
            {
                LogUtil.Error($"Conversion from gender chances failed for {gender}. hetero: {orientation.hetero:R}, homo: {orientation.homo:R}, bi: {orientation.bi:R}, none: {orientation.none:R}");
            }

            return orientation;
        }
    }
}
