using System.Collections.Generic;
using RimWorld;
using Verse;
using HarmonyLib;
using UnityEngine;

namespace BetterRomance.HarmonyPatches
{
    //Adds a float menu option when right clicking on another pawn
    [HarmonyPatch(typeof(FloatMenuMakerMap), "AddHumanlikeOrders")]
    public static class FloatMenuMakerMap_AddHumanlikeOrders
    {

        public static void Postfix(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> opts)
        {
            if (!pawn.Drafted && pawn.ageTracker.AgeBiologicalYearsFloat > pawn.MinAgeForSex() && !pawn.CheckForPartnerComp().IsOrderedHookupOnCooldown)
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
