using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using RimWorld;
using Verse;
using HarmonyLib;
using UnityEngine;
using System.Reflection;

namespace BetterRomance.HarmonyPatches
{
    [HarmonyPatch]
    public static class CharacterCardUtility_DoLeftSection
    {
        //Turns the asexual trait rect into a button that opens the sexuality tab
        public static void Postfix(ref Rect r, Trait trait)
        {
            if (SexualityUtility.asexualTraits.Contains(trait.def))
            {
                if (Widgets.ButtonInvisible(r))
                {
                    InspectPaneUtility.OpenTab(typeof(ITab_Orientation));
                }
            }
        }

        public static MethodBase TargetMethod()
        {
            //[HarmonyPatch("<>c__DisplayClass42_2", "<DoLeftSection>b__7")]
            IEnumerable<Type> innerTypes = typeof(CharacterCardUtility).GetNestedTypes(AccessTools.all).Where((Type t) => t.GetCustomAttribute<CompilerGeneratedAttribute>() != null);
            foreach (Type innerType in innerTypes)
            {
                IEnumerable<MethodInfo> methods = innerType.GetMethods(AccessTools.allDeclared).Where((MethodInfo m) => m.Name.Contains("DoLeftSection") && m.ReturnType == typeof(void));
                if (methods.Count() == 1)
                {
                    foreach (MethodInfo method in methods)
                    {
                        ParameterInfo[] parameters = method.GetParameters();
                        if (parameters.Length == 2 && parameters[0].ParameterType == typeof(Rect) && parameters[1].ParameterType == typeof(Trait))
                        {
                            return method;
                        }
                    }
                }
            }
            return null;
        }
    }
}