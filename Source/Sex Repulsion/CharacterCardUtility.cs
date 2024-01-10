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
        public static void Postfix(ref Rect r, Trait trait)
        {
            Pawn pawn = trait.pawn;
            if (pawn.IsAsexual() && SexualityUtility.asexualTraits.Contains(trait.def))
            {
                if (Widgets.ButtonInvisible(r))
                {
                    InspectPaneUtility.OpenTab(typeof(ITab_Sexuality));
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
                        ParameterInfo[] adds = method.GetParameters();
                        if (adds.Length == 2 && adds[0].ParameterType == typeof(Rect) && adds[1].ParameterType == typeof(Trait))
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