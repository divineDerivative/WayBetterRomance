using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace BetterRomance.HarmonyPatches
{
    //Adds a float menu option when right clicking on another pawn
    [HarmonyPatch(typeof(FloatMenuMakerMap), "AddHumanlikeOrders")]
    public static class FloatMenuMakerMap_AddHumanlikeOrders
    {
        public static void Postfix(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> opts)
        {
            if (!pawn.Drafted && pawn.ageTracker.AgeBiologicalYearsFloat > pawn.MinAgeForSex() && !pawn.CheckForComp<Comp_PartnerList>().IsOrderedHookupOnCooldown)
            {
                foreach (LocalTargetInfo target in GenUI.TargetsAt(clickPos, TargetingParameters.ForRomance(pawn), thingsOnly: true))
                {
                    Pawn p = (Pawn)target.Thing;
                    if (!p.Drafted && !p.DevelopmentalStage.Baby())
                    {
                        bool optionAvailable = HookupUtility.HookupOption(pawn, p, out FloatMenuOption option, out _);
                        if (option != null)
                        {
                            option.Label = (optionAvailable ? "WBR.CanHookup" : "WBR.CannotHookup").Translate(option.Label);
                            opts.Add(option);
                        }
                    }
                }
            }
        }
    }
}
