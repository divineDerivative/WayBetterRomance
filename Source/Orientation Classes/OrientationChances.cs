using DivineFramework;
using System.Diagnostics;
using Verse;

namespace BetterRomance
{
    public class OrientationChances : IExposable
    {
        public float hetero = -999f;
        public float homo = -999f;
        public float bi = -999f;
        public float none = -999f;
        public float enby = 10f;

        //Use these to roll against Rand.Value
        public float Hetero => hetero / 100f;
        public float Homo => homo / 100f;
        public float Bi => bi / 100f;
        public float None => none / 100f;
        public float Enby => enby / 100f;

        public OrientationChances Copy => (OrientationChances)MemberwiseClone();

        public void CopyFrom(OrientationChances other)
        {
            hetero = other.hetero;
            homo = other.homo;
            bi = other.bi;
            none = other.none;
            enby = other.enby;
        }

        public bool AreAnyUnset(out string list, bool asexual)
        {
            //Need to make these strings match the old variable names I think
            list = "";
            bool result = false;
            if (hetero.IsUnset())
            {
                list += asexual ? " heteroromanticChance" : " straightChance";
                result = true;
            }
            if (homo.IsUnset())
            {
                list += asexual ? " homoromanticChance" : " gayChance";
                result = true;
            }
            if (bi.IsUnset())
            {
                list += asexual ? " biromanticChance" : " bisexualChance"; ;
                result = true;
            }
            if (none.IsUnset())
            {
                list += asexual ? " aromanticChance" : " asexualChance"; ;
                result = true;
            }
            return result;
        }

        public bool TotalCorrect
        {
            get
            {
                //Add first because floats can be dumb
                float sum = hetero + homo + bi + none;
                return sum == 100f;
            } 
        }

        public void Reset()
        {
            none = 10f;
            bi = 50f;
            homo = 20f;
            hetero = 20f;
            enby = 10f;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref hetero, "hetero", 20f);
            Scribe_Values.Look(ref homo, "homo", 20f);
            Scribe_Values.Look(ref bi, "bi", 50f);
            Scribe_Values.Look(ref none, "none", 10f);
            Scribe_Values.Look(ref enby, "enby", 10f);
        }
    }
}
