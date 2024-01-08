using RimWorld;
using Verse;

namespace BetterRomance
{
    public enum SpouseCountComparison
    {
        Below,
        At,
        Over,
    }

    public static class PreceptUtility
    {
        //These should only be called if the total love relations is at or above the spouse count precept
        public static HistoryEventDef GetHistoryEventLoverCount(this Pawn pawn)
        {
            int count = pawn.GetLoveRelations(includeDead: false).Count - AllowedSpouseCount(pawn.ideo.Ideo, pawn.gender);
            if (count <= 1)
            {
                return RomanceDefOf.TookLover_LoverCount_OneOrFewer;
            }
            if (count <= 2)
            {
                return RomanceDefOf.TookLover_LoverCount_Two;
            }
            if (count <= 3)
            {
                return RomanceDefOf.TookLover_LoverCount_Three;
            }
            if (count <= 4)
            {
                return RomanceDefOf.TookLover_LoverCount_Four;
            }
            return RomanceDefOf.TookLover_LoverCount_FiveOrMore;
        }

        public static HistoryEventDef GetHistoryEventLoverCountPlusOne(this Pawn pawn)
        {
            int count = pawn.GetLoveRelations(includeDead: false).Count - AllowedSpouseCount(pawn.ideo.Ideo, pawn.gender);
            if (count == 0)
            {
                return RomanceDefOf.TookLover_LoverCount_OneOrFewer;
            }
            if (count < 2)
            {
                return RomanceDefOf.TookLover_LoverCount_Two;
            }
            if (count < 3)
            {
                return RomanceDefOf.TookLover_LoverCount_Three;
            }
            if (count < 4)
            {
                return RomanceDefOf.TookLover_LoverCount_Four;
            }
            return RomanceDefOf.TookLover_LoverCount_FiveOrMore;
        }

        public static SpouseCountComparison CompareSpouseAndLoverCount(this Pawn pawn)
        {
            int allowed = AllowedSpouseCount(pawn.ideo.Ideo, pawn.gender);
            if (allowed == 5)
            {
                return SpouseCountComparison.Below;
            }
            int total = pawn.GetLoveRelations(false).Count;
            if (total < allowed)
            {
                return SpouseCountComparison.Below;
            }
            if (total == allowed)
            {
                return SpouseCountComparison.At;
            }
            return SpouseCountComparison.Over;
        }

        /// <summary>
        /// Converts the spouse count precept of an <paramref name="ideo"/> for a given <paramref name="gender"/> into a number to use for comparisons
        /// </summary>
        /// <param name="ideo"></param>
        /// <param name="gender"></param>
        /// <returns></returns>
        public static int AllowedSpouseCount(Ideo ideo, Gender gender)
        {
            switch (gender)
            {
                case Gender.Male:
                    if (ideo.HasPrecept(RomanceDefOf.SpouseCount_Male_MaxOne))
                    {
                        return 1;
                    }
                    else if (ideo.HasPrecept(RomanceDefOf.SpouseCount_Male_MaxTwo))
                    {
                        return 2;
                    }
                    else if (ideo.HasPrecept(RomanceDefOf.SpouseCount_Male_MaxThree))
                    {
                        return 3;
                    }
                    else if (ideo.HasPrecept(RomanceDefOf.SpouseCount_Male_MaxFour))
                    {
                        return 4;
                    }
                    else if (ideo.HasPrecept(RomanceDefOf.SpouseCount_Male_Unlimited))
                    {
                        return 5;
                    }
                    return 0;
                case Gender.Female:
                    if (ideo.HasPrecept(RomanceDefOf.SpouseCount_Female_MaxOne))
                    {
                        return 1;
                    }
                    else if (ideo.HasPrecept(RomanceDefOf.SpouseCount_Female_MaxTwo))
                    {
                        return 2;
                    }
                    else if (ideo.HasPrecept(RomanceDefOf.SpouseCount_Female_MaxThree))
                    {
                        return 3;
                    }
                    else if (ideo.HasPrecept(RomanceDefOf.SpouseCount_Female_MaxFour))
                    {
                        return 4;
                    }
                    else if (ideo.HasPrecept(RomanceDefOf.SpouseCount_Female_Unlimited))
                    {
                        return 5;
                    }
                    return 0;
                default:
                    return -1;
            }
        }
    }
}
