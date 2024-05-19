using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Verse;

namespace BetterRomance.HarmonyPatches
{

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos))]
    public static class Pawn_GetGizmos
    {
        //Make growth tier gizmos show up based on actual min age instead of static 13
        [HarmonyPatch(MethodType.Enumerator)]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = instructions.ToList();
            for (int i = 0; i < codes.Count; i++)
            {
                CodeInstruction code = codes[i];
                yield return code;
                if (code.Is(OpCodes.Callvirt, AccessTools.DeclaredPropertyGetter(typeof(Pawn_AgeTracker), nameof(Pawn_AgeTracker.AgeBiologicalYears))))
                {
                    yield return new CodeInstruction(OpCodes.Ldloc_2);
                    yield return CodeInstruction.LoadField(typeof(Pawn), nameof(Pawn.ageTracker));
                    yield return new CodeInstruction(OpCodes.Callvirt, InfoHelper.AdultMinAge);
                    yield return new CodeInstruction(OpCodes.Conv_I4);
                    i++;
                }
            }
        }

        //Add dev gizmo to reset ordered hookup cooldown
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> values, Pawn __instance)
        {
            foreach (Gizmo gizmo in values)
            {
                yield return gizmo;
            }
            if (DebugSettings.ShowDevGizmos)
            {
                Comp_PartnerList comp = __instance.TryGetComp<Comp_PartnerList>();
                if (comp != null)
                {
                    if (comp.IsOrderedHookupOnCooldown)
                    {
                        Command_Action action1 = new()
                        {
                            defaultLabel = "DEV: Reset ordered hookup cooldown",
                            action = delegate
                            {
                                comp.orderedHookupTick = -1;
                            }
                        };
                        yield return action1;
                    }

                    Command_Action action2 = new()
                    {
                        defaultLabel = "DEV: Clear partner lists",
                        action = delegate
                        {
                            comp.Date.list = null;
                            comp.Date.listMadeEver = false;
                            comp.Hookup.list = null;
                            comp.Hookup.listMadeEver = false;
                            comp.Date.ticksSinceMake = 120000;
                            comp.Hookup.ticksSinceMake = 120000;
                            //Test this
                            List<Thought_Memory> memories = __instance.needs.mood.thoughts.memories.Memories.FindAll(delegate (Thought_Memory x)
                            {
                                return x.def == RomanceDefOf.RebuffedMyHookupAttempt || x.def == RomanceDefOf.RebuffedMyDateAttempt;
                            });
                            foreach (Thought_Memory memory in memories)
                            {
                                __instance.needs.mood.thoughts.memories.RemoveMemory(memory);
                                memory.otherPawn.needs.mood.thoughts.memories.RemoveMemoriesOfDefWhereOtherPawnIs(memory.def, __instance);
                            }
                        }
                    };
                    yield return action2;
                }

                if (__instance.RaceProps.Humanlike)
                {
                    Command_Action action3 = new()
                    {
                        defaultLabel = "DEV: Reset lovin' tick",
                        action = delegate
                        {
                            __instance.mindState.canLovinTick = -99999;
                        }
                    };
                    yield return action3;
                }

                if (__instance.TryGetComp<WBR_SettingsComp>() is WBR_SettingsComp settingsComp)
                {
                    Command_Action action4 = new()
                    {
                        defaultLabel = "DEV: Log settings comp",
                        action = delegate
                        {
                            settingsComp.LogToConsole();
                        }
                    };
                    yield return action4;
                }
            }
        }
    }
}
