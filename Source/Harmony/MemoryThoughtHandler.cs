﻿using HarmonyLib;
using RimWorld;
using System;
using Verse;

namespace BetterRomance.HarmonyPatches
{
    //Intercepts adding the 'got some lovin'' thought and replaces it with the asexual version if needed
    [HarmonyPatch(typeof(MemoryThoughtHandler), "TryGainMemory", new Type[] { typeof(Thought_Memory), typeof(Pawn) })]
    public static class MemoryThoughtHandler_TryGainMemory
    {
        public static bool Prefix(ref Thought_Memory newThought, Pawn otherPawn, Pawn ___pawn)
        {

            if (newThought.def == ThoughtDefOf.GotSomeLovin)
            {
                if (___pawn.IsAsexual())
                {
                    //Making floor lower so 0.2f, which is the lowest rating that should get this thought, gets the correct modifiers, -8 and -6
                    int moodOffset = (int)GenMath.LerpDouble(0f, 1f, -12f, 8f, ___pawn.AsexualRating());
                    float opinionOffset = GenMath.LerpDouble(0f, 1f, -9f, 6f, ___pawn.AsexualRating());
                    Thought_MemorySocial replacementThought = ThoughtMaker.MakeThought((moodOffset < 0) ? RomanceDefOf.LovinAsexualNegative : RomanceDefOf.LovinAsexualPositive) as Thought_MemorySocial;
                    if (RomanceUtilities.HasLoveEnhancer(___pawn) || RomanceUtilities.HasLoveEnhancer(otherPawn))
                    {
                        replacementThought.moodPowerFactor = 1.5f;
                    }
                    replacementThought.moodOffset = moodOffset;
                    replacementThought.opinionOffset = opinionOffset;
                    newThought = replacementThought;
                    return true;
                }
            }
            //This thought is from 123 Personalities M2
            else if (newThought.def == RomanceDefOf.SP_PassionateLovin)
            {
                if (___pawn.IsAsexual())
                {
                    //Making floor lower so 0.2f, which is the lowest rating that should get this thought, gets the correct modifiers, -10 and -8
                    int moodOffset = (int)GenMath.LerpDouble(0f, 1f, -15f, 10f, ___pawn.AsexualRating());
                    float opinionOffset = GenMath.LerpDouble(0f, 1f, -12f, 8f, ___pawn.AsexualRating());
                    Thought_MemorySocial replacementThought = ThoughtMaker.MakeThought((moodOffset < 0) ? RomanceDefOf.PassionateLovinAsexualNegative : RomanceDefOf.PassionateLovinAsexualPositive) as Thought_MemorySocial;

                    if (RomanceUtilities.HasLoveEnhancer(___pawn) || RomanceUtilities.HasLoveEnhancer(otherPawn))
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