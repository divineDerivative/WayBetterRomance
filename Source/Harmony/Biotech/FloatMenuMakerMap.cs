using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace BetterRomance.HarmonyPatches
{
    //Adds a float menu option when right-clicking on another pawn  
#if !v1_6
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
#else
    public class FloatMenuOptionProvider_Hookup : FloatMenuOptionProvider
    {
        protected override bool Drafted => false;
        protected override bool Undrafted => true;
        protected override bool Multiselect => false;

        protected override FloatMenuOption GetSingleOptionFor(Pawn clickedPawn, FloatMenuContext context)
        {
            if (!clickedPawn.IsHumanlike() || clickedPawn.DroneCheck() || clickedPawn.Drafted || clickedPawn.DevelopmentalStage.Baby())
            {
                return null;
            }

            bool optionAvailable = HookupUtility.HookupOption(context.FirstSelectedPawn, clickedPawn, out FloatMenuOption option, out _);
            if (option != null)
            {
                option.Label = (optionAvailable ? "WBR.CanHookup" : "WBR.CannotHookup").Translate(option.Label);
            }
            return option;
        }

        protected override bool AppliesInt(FloatMenuContext context)
        {
            Pawn pawn = context.FirstSelectedPawn;
            if (!HookupUtility.CanDrawTryHookup(pawn))
            {
                return false;
            }
            Comp_PartnerList comp = pawn.TryGetComp<Comp_PartnerList>();
            if (comp is null || comp.IsOrderedHookupOnCooldown)
            {
                return false;
            }
            return base.AppliesInt(context);
        }
        public override bool Applies(FloatMenuContext context)
        {
            return base.Applies(context);
        }
    }

#endif
}
