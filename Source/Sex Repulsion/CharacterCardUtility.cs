using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using Verse;

namespace BetterRomance.HarmonyPatches
{
    [HarmonyPatch]
    public static class CharacterCardUtility_DoLeftSection
    {
        //Turns the dynamic trait rect into a button that opens the orientation tab
        public static void Postfix(ref Rect r, Trait trait)
        {
            if (trait.def == RomanceDefOf.DynamicOrientation && Current.ProgramState == ProgramState.Playing)
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
