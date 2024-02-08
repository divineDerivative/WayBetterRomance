using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;
using HarmonyLib;

namespace BetterRomance
{
    public static class DateUtility
    {
        public static bool CanInteractWith(Pawn pawn, Thing t)
        {
            if (!pawn.CanReserve(t))
            {
                return false;
            }
            if (t.IsForbidden(pawn))
            {
                return false;
            }
            if (!t.IsSociallyProper(pawn))
            {
                return false;
            }
            if (!t.IsPoliticallyProper(pawn))
            {
                return false;
            }
            CompPowerTrader compPowerTrader = t.TryGetComp<CompPowerTrader>();
            return compPowerTrader == null || compPowerTrader.PowerOn;
        }

        public static bool TryFindBestWatchCellNear(Thing toWatch, Pawn pawn, Building otherChair, bool desireSit, out IntVec3 result, out Building chair)
        {
            List<int> list = (List<int>)AccessTools.Method(typeof(WatchBuildingUtility), "CalculateAllowedDirections").Invoke(null, new object[] { toWatch.def, toWatch.Rotation });
            list.Shuffle();
            IntVec3 intVec = IntVec3.Invalid;
            for (int i = 0; i < list.Count; i++)
            {
                CellRect watchCellRect = (CellRect)AccessTools.Method(typeof(WatchBuildingUtility), "GetWatchCellRect").Invoke(null, new object[] { toWatch.def, toWatch.Position, toWatch.Rotation, list[i] });
                IntVec3 centerCell = watchCellRect.CenterCell;
                int num = watchCellRect.Area * 4;
                for (int j = 0; j < num; j++)
                {
                    IntVec3 intVec2 = centerCell + GenRadial.RadialPattern[j];
                    if (!watchCellRect.Contains(intVec2))
                    {
                        continue;
                    }
                    bool flag = false;
                    Building building = null;
                    if ((bool)AccessTools.Method(typeof(WatchBuildingUtility), "EverPossibleToWatchFrom").Invoke(null, new object[] { intVec2, toWatch.Position, toWatch.Map, false, toWatch.def }) && !intVec2.IsForbidden(pawn) && pawn.CanReserveSittableOrSpot(intVec2) && pawn.Map.pawnDestinationReservationManager.CanReserve(intVec2, pawn))
                    {
                        if (desireSit)
                        {
                            building = intVec2.GetEdifice(pawn.Map);
                            if (building != null && building.def.building.isSittable && building.Position.DistanceTo(otherChair.Position) < 2 && pawn.CanReserve(building))
                            {
                                flag = true;
                            }
                        }
                        else
                        {
                            flag = true;
                        }
                    }
                    if (flag)
                    {
                        if (!desireSit || !(building.Rotation != new Rot4(list[i]).Opposite))
                        {
                            result = intVec2;
                            chair = building;
                            return true;
                        }
                        intVec = intVec2;
                    }
                }
            }
            if (intVec.IsValid)
            {
                result = intVec;
                chair = intVec.GetEdifice(pawn.Map);
                return true;
            }
            result = IntVec3.Invalid;
            chair = null;
            return false;
        }

        public static bool IsPartnerNearGoal(Thing t, Pawn p)
        {
            return t.Position.InHorDistOf(p.Position, 2f);
        }

        /// <summary>
        /// Determines if <paramref name="target"/> accepts a date with <paramref name="asker"/>.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="asker"></param>
        /// <returns>True or false</returns>
        public static bool IsDateAppealing(Pawn target, Pawn asker)
        {
            //Always agree with an existing partner
            if (LovePartnerRelationUtility.LovePartnerRelationExists(target, asker))
            {
                return true;
            }
            if (RomanceUtilities.WillPawnContinue(target, asker, out _))
            {
                //Definitely not cheating, or they decided to cheat
                //Same math as agreeing to a hookup but no asexual check
                float num = 0f;
                num += target.relations.SecondaryRomanceChanceFactor(asker) / 1.5f;
                num *= Mathf.InverseLerp(-100f, 0f, target.relations.OpinionOf(asker));
                return Rand.Range(0.05f, 1f) < num;
            }
            return false;
        }

        public static bool IsHangoutAppealing(Pawn target, Pawn asker)
        {
            //Always agree with an existing partner?
            if (LovePartnerRelationUtility.LovePartnerRelationExists(target, asker))
            {
                return true;
            }
            //Just looking at opinion
            float num = Mathf.InverseLerp(-100f, 0f, target.relations.OpinionOf(asker));
            return Rand.Range(0.05f, 1f) < num;
        }

        public static void DateTickAction(Pawn pawn, bool isDate)
        {
            JoyUtility.JoyTickCheckEnd(pawn, JoyTickFullJoyAction.None);
            if (isDate)
            {
                HelperClasses.RotRFillRomanceBar?.Invoke(null, new object[] { pawn, 0.00002f });
            }
        }

        public static bool FailureCheck(Pawn Partner, JobDef job)
        {
            return !Partner.Spawned || Partner.Dead || Partner.Downed || Partner.CurJob?.def != job;
        }
    }
}
