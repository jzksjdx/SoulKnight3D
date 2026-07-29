using System;
using System.Collections.Generic;
using UnityEngine;

namespace SoulKnight3D
{
    public enum GameRandomStream
    {
        MapTopology,
        RoomContent,
        Merchant,
        Rewards,
        Enemies,
        Gameplay,
        Presentation
    }

    public static class GameRandom
    {
        private static readonly Dictionary<GameRandomStream, System.Random> Streams =
            new Dictionary<GameRandomStream, System.Random>();

        private static bool _isInitialized;

        public static int LevelSeed { get; private set; }

        public static void BeginLevel(int runId, int level)
        {
            long ticks = DateTime.UtcNow.Ticks;
            int seed = Guid.NewGuid().GetHashCode();
            seed = MixSeed(seed, Environment.TickCount);
            seed = MixSeed(seed, (int)ticks);
            seed = MixSeed(seed, (int)(ticks >> 32));
            seed = MixSeed(seed, runId);
            seed = MixSeed(seed, level);

            LevelSeed = seed;
            Streams.Clear();
            Array streamValues = Enum.GetValues(typeof(GameRandomStream));
            for (int i = 0; i < streamValues.Length; i++)
            {
                GameRandomStream stream = (GameRandomStream)streamValues.GetValue(i);
                Streams.Add(stream, new System.Random(
                    MixSeed(LevelSeed, 1009 + (int)stream * 7919)));
            }

            _isInitialized = true;
            Debug.Log($"Game RNG initialized for run {runId}, level {level}, seed {LevelSeed}.");
        }

        public static System.Random GetStream(GameRandomStream stream)
        {
            EnsureInitialized();
            return Streams[stream];
        }

        public static float Value(GameRandomStream stream)
        {
            return (float)GetStream(stream).NextDouble();
        }

        public static bool Chance(GameRandomStream stream, float probability)
        {
            return Value(stream) < Mathf.Clamp01(probability);
        }

        public static int Range(GameRandomStream stream, int minimumInclusive,
            int maximumExclusive)
        {
            if (maximumExclusive <= minimumInclusive)
            {
                return minimumInclusive;
            }

            return GetStream(stream).Next(minimumInclusive, maximumExclusive);
        }

        public static float Range(GameRandomStream stream, float minimumInclusive,
            float maximumInclusive)
        {
            if (maximumInclusive <= minimumInclusive)
            {
                return minimumInclusive;
            }

            return minimumInclusive +
                (maximumInclusive - minimumInclusive) * Value(stream);
        }

        private static void EnsureInitialized()
        {
            if (!_isInitialized)
            {
                BeginLevel(0, 0);
            }
        }

        private static int MixSeed(int seed, int salt)
        {
            unchecked
            {
                uint value = (uint)seed;
                value ^= (uint)salt + 0x9e3779b9u + (value << 6) + (value >> 2);
                value ^= value >> 16;
                value *= 0x7feb352du;
                value ^= value >> 15;
                value *= 0x846ca68bu;
                value ^= value >> 16;
                return (int)value;
            }
        }
    }
}
