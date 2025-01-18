using RimWorld;
using System.Collections.Generic;
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
            CheckIdeo(pawn.ideo?.Ideo);
            int count = pawn.GetLoveRelations(includeDead: false).Count - AllowedSpouseCount(pawn.ideo?.Ideo, pawn.gender);
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
            CheckIdeo(pawn.ideo?.Ideo);
            int count = pawn.GetLoveRelations(includeDead: false).Count - AllowedSpouseCount(pawn.ideo?.Ideo, pawn.gender);
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
            int allowed = AllowedSpouseCount(pawn.ideo?.Ideo, pawn.gender);
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
                    if (ideo == null || ideo.HasPrecept(RomanceDefOf.SpouseCount_Male_MaxOne))
                    {
                        return 1;
                    }
                    else if (ideo.HasPrecept(RomanceDefOf.SpouseCount_Male_None))
                    {
                        return 0;
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
                    return 5;
                case Gender.Female:
                    if (ideo == null || ideo.HasPrecept(RomanceDefOf.SpouseCount_Female_MaxOne))
                    {
                        return 1;
                    }
                    else if (ideo.HasPrecept(RomanceDefOf.SpouseCount_Female_None))
                    {
                        return 0;
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

                    return 5;
                default:
                    return -1;
            }
        }

        private static HashSet<Ideo> IdeosChecked = new();

        /// <summary>
        /// Checks if an <paramref name="ideo"/> has lover count precepts, and if not, adds them
        /// </summary>
        /// <param name="ideo"></param>
        public static void CheckIdeo(Ideo ideo)
        {
            if (ideo != null && !IdeosChecked.Contains(ideo))
            {
                if (!ideo.HasMaxPreceptsForIssue(DefDatabase<IssueDef>.GetNamed("LoverCount_Male")))
                {
                    ideo.AddPrecept(PreceptMaker.MakePrecept(RomanceDefOf.LoverCount_Male_None));
                }
                if (!ideo.HasMaxPreceptsForIssue(DefDatabase<IssueDef>.GetNamed("LoverCount_Female")))
                {
                    ideo.AddPrecept(PreceptMaker.MakePrecept(RomanceDefOf.LoverCount_Female_None));
                }
                IdeosChecked.Add(ideo);
            }
        }

        /// <summary>Finds a given <paramref name="ideo"/>'s precept for the specified <paramref name="issue"/>.</summary>
        /// <remarks>Only use for single precept issues.</remarks>
        public static Precept GetPreceptForIssue(this Ideo ideo, IssueDef issue)
        {
            foreach (Precept precept in ideo.PreceptsListForReading)
            {
                if (precept.def.issue == issue)
                {
                    return precept;
                }
            }
            return null;
        }

        public static float NonSpouseLovinWillDoChance(Ideo ideo)
        {
            float fromComp = 0f;
            if (ideo is not null)
            {
                PreceptDef precept = ideo.GetLovinPreceptDef();
                foreach (PreceptComp comp in precept.comps)
                {
                    if (comp is PreceptComp_UnwillingToDo_Chance unwillingComp && unwillingComp.eventDef == HistoryEventDefOf.GotLovin_NonSpouse)
                    {
                        fromComp = unwillingComp.chance;
                        break;
                    }
                }
            }
            return 1f - fromComp;
        }

        public static PreceptDef GetLovinPreceptDef(this Ideo ideo)
        {
            Precept precept = ideo.GetPreceptForIssue(RomanceDefOf.Lovin);
            if ( precept is null)
            {
                LogUtil.Error($"Unable to find lovin' precept for {ideo.name}");
            }
            return precept.def;
        }
    }
}
