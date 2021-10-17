using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace BetterRomance
{
    //Intercepts adding the 'got some lovin'' thought and replaces it with the asexual version if needed
    [HarmonyPatch(typeof(MemoryThoughtHandler), "TryGainMemory", new Type[] {typeof(Thought_Memory), typeof(Pawn)})]
    public static class MemoryThoughtHandler_TryGainMemory
    {
        internal static FieldInfo _pawn;

        public static bool Prefix(Thought_Memory newThought, Pawn otherPawn, MemoryThoughtHandler __instance)
        {

            if (newThought.def == ThoughtDefOf.GotSomeLovin)
            {
                Pawn pawn = __instance.GetPawn();
                if (pawn.story.traits.HasTrait(TraitDefOf.Asexual))
                {
                    var replacementThought = (Thought_MemorySocial)ThoughtMaker.MakeThought(RomanceDefOf.GotSomeLovinAsexual);
                    if ((pawn.health != null && pawn.health.hediffSet != null && pawn.health.hediffSet.hediffs.Any((Hediff h) => h.def == HediffDefOf.LoveEnhancer)) || (otherPawn.health != null && otherPawn.health.hediffSet != null && otherPawn.health.hediffSet.hediffs.Any((Hediff h) => h.def == HediffDefOf.LoveEnhancer)))
                    {
                        replacementThought.moodPowerFactor = 1.5f;
                    }
                    replacementThought.moodOffset = (int)GenMath.LerpDouble(0f, 1f, -8f, 8f, pawn.AsexualRating());
                    replacementThought.opinionOffset = GenMath.LerpDouble(0f, 1f, -6f, 6f, pawn.AsexualRating());
                    if (pawn.needs.mood != null)
                    {
                        pawn.needs.mood.thoughts.memories.TryGainMemory(replacementThought, otherPawn);
                    }
                    return false;
                }
            }
            return true;
        }

        private static Pawn GetPawn(this MemoryThoughtHandler _this)
        {
            bool flag = _pawn == null;
            if (!flag)
            {
                return (Pawn)_pawn.GetValue(_this);
            }

            _pawn = typeof(MemoryThoughtHandler).GetField("pawn", BindingFlags.Instance | BindingFlags.Public);
            bool flag2 = _pawn == null;
            if (flag2)
            {
                Log.Error("Unable to reflect MemoryThoughtHandler.pawn!");
            }

            return (Pawn)_pawn?.GetValue(_this);
        }
    }
}