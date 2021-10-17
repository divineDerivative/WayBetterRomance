using System;
using System.Reflection;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Verse;
using RimWorld;

namespace BetterRomance
{
    [StaticConstructorOnStartup]
    public static class OnStartup
    {
        static OnStartup()
        {
            SettingsUtilities.LoveRelations = SettingsUtilities.AdditionalLoveRelations();
            SettingsUtilities.ExLoveRelations = SettingsUtilities.AdditionalExLoveRelations();
            if (ModsConfig.IsActive("erdelf.humanoidalienraces"))
            {
                Settings.HARActive = true;
            }
            Harmony harmony = new Harmony(id: "rimworld.divineDerivative.romance");
            harmony.PatchAll();
        }
    }

    public class MayRequireHARAttribute : MayRequireAttribute
    {
        public MayRequireHARAttribute()
            : base("erdelf.humanoidalienraces")
        { }
    }
}