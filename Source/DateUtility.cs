using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;

namespace BetterRomance
{
    public static class DateUtility
    {
        //45 mins
        internal const int walkingTicks = 1875;
        //30 min
        internal const int waitingTicks = 1250;
        internal const int distanceLimit = 50;
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
            List<int> list = (List<int>)AccessTools.Method(typeof(WatchBuildingUtility), "CalculateAllowedDirections").Invoke(null, [toWatch.def, toWatch.Rotation]);
            list.Shuffle();
            IntVec3 intVec = IntVec3.Invalid;
            for (int i = 0; i < list.Count; i++)
            {
                CellRect watchCellRect = (CellRect)AccessTools.Method(typeof(WatchBuildingUtility), "GetWatchCellRect").Invoke(null, [toWatch.def, toWatch.Position, toWatch.Rotation, list[i]]);
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
                    if ((bool)AccessTools.Method(typeof(WatchBuildingUtility), "EverPossibleToWatchFrom").Invoke(null, [intVec2, toWatch.Position, toWatch.Map, false, toWatch.def]) && !intVec2.IsForbidden(pawn) && pawn.CanReserveSittableOrSpot(intVec2) && pawn.Map.pawnDestinationReservationManager.CanReserve(intVec2, pawn))
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
                HelperClasses.RotRFillRomanceBar?.Invoke(null, [pawn, 0.00002f]);
            }
        }

        public static bool FailureCheck(Pawn Partner, JobDef job, bool ordered = false)
        {
            if (Settings.debugLogging)
            {
                string activity = job == RomanceDefOf.DoLovinCasual ? "Hook up" : job.defName.Contains("Hangout") ? "Hangout" : "Date";
                if (!Partner.Spawned)
                {
                    LogUtil.Message($"{activity} failed because {Partner.Name.ToStringShort} despawned", true);
                }
                else if (Partner.Dead)
                {
                    LogUtil.Message($"{activity} failed because {Partner.Name.ToStringShort} is dead", true);
                }
                else if (Partner.Downed)
                {
                    LogUtil.Message($"{activity} failed because {Partner.Name.ToStringShort} is downed", true);
                }
                else if (!ordered && PawnUtility.WillSoonHaveBasicNeed(Partner))
                {
                    LogUtil.Message($"{activity} ended early because {Partner.Name.ToStringShort} needs to eat or sleep", true);
                }
                else if (Partner.CurJob?.def != job)
                {
                    LogUtil.Message($"{activity} failed because {Partner.Name.ToStringShort} stopped their job", true);
                }
            }
            return !Partner.Spawned || Partner.Dead || Partner.Downed || (!ordered && PawnUtility.WillSoonHaveBasicNeed(Partner)) || Partner.CurJob?.def != job;
        }

        public static bool DistanceFailure(Pawn pawn, Pawn TargetPawn, ref int waitCount, ref int ticksLeft)
        {
            //If time has run out but they're within 10 tiles, add some extra time
            if (pawn.Position.InHorDistOf(TargetPawn.Position, 10f))
            {
                ticksLeft += 100;
                //But don't add time indefinitely
                waitCount++;
                if (waitCount <= 5 || (pawn.Position.InHorDistOf(TargetPawn.Position, 5f) && waitCount < 8))
                {
                    return false;
                }
            }
            return true;
        }

        public static bool WalkCellValidator(this IntVec3 cell, Map map)
        {
            if (!cell.InBounds(map))
            {
                return false;
            }
            if (!cell.Standable(map))
            {
                return false;
            }
            if (cell.GetTerrain(map).avoidWander)
            {
                return false;
            }
            if (cell.Roofed(map))
            {
                return false;
            }
            return true;
        }

        public static bool WalkCellValidator(this IntVec3 cell, Pawn pawn)
        {
            if (PawnUtility.KnownDangerAt(cell, pawn.Map, pawn))
            {
                return false;
            }
            if (cell.IsForbidden(pawn))
            {
                return false;
            }
            if (!pawn.CanReach(cell, PathEndMode.OnCell, Danger.Some))
            {
                return false;
            }
            return true;
        }
    }
}
