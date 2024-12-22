#if !v1_4
using LudeonTK;
#endif
using RimWorld;
using System.Collections.Generic;
using Verse;
using DivineFramework;

namespace BetterRomance.Testing
{
    internal static class Tests
    {
        internal static readonly LogToFile logger = new("OrientationTests", true);

        private static Pawn CreateTestPawn(Gender gender)
        {
            //Letting them generate relationships makes them stick around in WorldPawns, which makes generation take longer
            PawnGenerationRequest request = new(PawnKindDefOf.Colonist, fixedGender: gender, canGeneratePawnRelations: false);
            return PawnGenerator.GeneratePawn(request);
        }

        [DebugAction("Orientation testing", "Test ALL orientations")]
        public static void TestOrientations()
        {
            int batch = 0;
            logger.Log($"********Testing orientations********");

            foreach (Pair<OrientationChances, OrientationChances> pair in IterateOrientations())
            {
                batch++;
                LongEventHandler.QueueLongEvent(() => GeneratePawnBatch(pair.First, pair.Second), $"Generating Batch {batch}/1225", true, null);
            }

            LongEventHandler.QueueLongEvent(() =>
            {
                logger.Close();
            }, "Closing log file", true, null);
            //I'd also like to see how closely the distribution matches the settings, but that's probably very complicated
        }

        [DebugAction("Orientation testing", "Test current settings")]
        public static void TestOrientation()
        {
            List<Pawn> pawns = new();
            for (int i = 0; i < 20; i++)
            {
                pawns.Add(CreateTestPawn(Gender.Male));
            }
            for (int i = 0; i < 20; i++)
            {
                pawns.Add(CreateTestPawn(Gender.Female));
            }
            for (int i = 0; i < 20; i++)
            {
                pawns.Add(CreateTestPawn((Gender)3));
            }
            pawns.Clear();
        }

        private static void GeneratePawnBatch(OrientationChances sexual, OrientationChances romantic)
        {
            if (!BetterRomanceMod.settings.sexualOrientations.Equals(sexual))
            {
                logger.Log("======Starting new sexual orientation======");
            }
            BetterRomanceMod.settings.sexualOrientations = sexual;
            BetterRomanceMod.settings.romanticOrientations = romantic;

            logger.Log($"Sexual orientation: {BetterRomanceMod.settings.sexualOrientations.hetero} hetero, {BetterRomanceMod.settings.sexualOrientations.homo} homo, {BetterRomanceMod.settings.sexualOrientations.bi} bi, {BetterRomanceMod.settings.sexualOrientations.none} none");
            logger.Log($"Romantic orientation: {BetterRomanceMod.settings.romanticOrientations.hetero} hetero, {BetterRomanceMod.settings.romanticOrientations.homo} homo, {BetterRomanceMod.settings.romanticOrientations.bi} bi, {BetterRomanceMod.settings.romanticOrientations.none} none");

            List<Pawn> pawns = new();
            TestOrientation();
        }

        public static IEnumerable<Pair<OrientationChances, OrientationChances>> IterateOrientations()
        {
            int increment = 25;

            foreach (OrientationChances sexual in GenerateCombinations(increment))
            {
                foreach (OrientationChances romantic in GenerateCombinations(increment))
                {
                    yield return new Pair<OrientationChances, OrientationChances>(sexual, romantic);
                }
            }
        }

        private static IEnumerable<OrientationChances> GenerateCombinations(int increment)
        {
            for (int i = 0; i <= 100; i += increment)
            {
                for (int j = 0; j <= 100; j += increment)
                {
                    for (int k = 0; k <= 100; k += increment)
                    {
                        for (int l = 0; l <= 100; l += increment)
                        {
                            if (i + j + k + l == 100)
                            {
                                yield return new OrientationChances
                                {
                                    hetero = i,
                                    homo = j,
                                    bi = k,
                                    none = l
                                };
                            }
                        }
                    }
                }
            }
        }
    }
}
