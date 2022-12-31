using HarmonyLib;
using RimWorld;
using System;
using System.Reflection;
using Verse;

namespace BetterRomance.HarmonyPatches
{
    //Intercepts adding the 'got some lovin'' thought and replaces it with the asexual version if needed
    [HarmonyPatch(typeof(MemoryThoughtHandler), "TryGainMemory", new Type[] {typeof(Thought_Memory), typeof(Pawn)})]
    public static class MemoryThoughtHandler_TryGainMemory
    {
        public static bool Prefix(ref Thought_Memory newThought, Pawn otherPawn, Pawn ___pawn)
        {

            if (newThought.def == ThoughtDefOf.GotSomeLovin)
            {
                if (___pawn.IsAsexual())
                {
                    int moodOffset = (int)GenMath.LerpDouble(0f, 1f, -8f, 8f, ___pawn.AsexualRating());
                    float opinionOffset = GenMath.LerpDouble(0f, 1f, -6f, 6f, ___pawn.AsexualRating());
                    Thought_MemorySocial replacementThought = ThoughtMaker.MakeThought((moodOffset < 0) ? RomanceDefOf.LovinAsexualNegative : RomanceDefOf.LovinAsexualPositive) as Thought_MemorySocial;
                    if ((___pawn.health != null && ___pawn.health.hediffSet != null && ___pawn.health.hediffSet.hediffs.Any((Hediff h) => h.def == HediffDefOf.LoveEnhancer)) || (otherPawn.health != null && otherPawn.health.hediffSet != null && otherPawn.health.hediffSet.hediffs.Any((Hediff h) => h.def == HediffDefOf.LoveEnhancer)))
                    {
                        replacementThought.moodPowerFactor = 1.5f;
                    }
                    replacementThought.moodOffset = moodOffset;
                    replacementThought.opinionOffset = opinionOffset;
                    newThought = replacementThought;
                    return true;
                }
            }
            return true;
        }
    }
}