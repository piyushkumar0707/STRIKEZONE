using System;
using System.Collections.Generic;
using StrikeZone.Logic;
using UnityEngine;

namespace StrikeZone.Core
{
    /// <summary>Stores recorded roll values and publishes recalculated bowling scores.</summary>
    public sealed class BowlingScoreManager : MonoBehaviour
    {
        private readonly List<int> _rolls = new List<int>();

        public event Action<ScoreSnapshot> OnScoreUpdated;

        public IReadOnlyList<int> Rolls => _rolls;
        public ScoreSnapshot CurrentSnapshot { get; private set; }

        private void Awake()
        {
            CurrentSnapshot = BuildSnapshot(false);
        }

        public void ResetGame()
        {
            _rolls.Clear();
            PublishSnapshot(false);
        }

        public ScoreSnapshot AddRoll(int pins, bool isGameComplete)
        {
            if (pins < 0 || pins > 10)
            {
                throw new ArgumentOutOfRangeException(nameof(pins), pins, "A roll must knock down 0 to 10 pins.");
            }

            _rolls.Add(pins);
            return PublishSnapshot(isGameComplete);
        }

        private ScoreSnapshot PublishSnapshot(bool isGameComplete)
        {
            CurrentSnapshot = BuildSnapshot(isGameComplete);
            OnScoreUpdated?.Invoke(CurrentSnapshot);
            return CurrentSnapshot;
        }

        private ScoreSnapshot BuildSnapshot(bool isGameComplete)
        {
            var cumulativeScores = BowlingScoreCalculator.CalculateCumulative(_rolls);
            var frames = new FrameScore[10];
            var rollIndex = 0;

            for (var frameIndex = 0; frameIndex < 9; frameIndex++)
            {
                int? roll1 = null;
                int? roll2 = null;
                var isStrike = false;
                var isSpare = false;

                if (rollIndex < _rolls.Count)
                {
                    roll1 = _rolls[rollIndex];
                    if (roll1 == 10)
                    {
                        isStrike = true;
                        rollIndex++;
                    }
                    else if (rollIndex + 1 < _rolls.Count)
                    {
                        roll2 = _rolls[rollIndex + 1];
                        isSpare = roll1 + roll2 == 10;
                        rollIndex += 2;
                    }
                    else
                    {
                        rollIndex++;
                    }
                }

                frames[frameIndex] = new FrameScore(
                    frameIndex + 1, roll1, roll2, null, cumulativeScores[frameIndex], isStrike, isSpare);
            }

            int? tenthRoll1 = rollIndex < _rolls.Count ? _rolls[rollIndex++] : null;
            int? tenthRoll2 = rollIndex < _rolls.Count ? _rolls[rollIndex++] : null;
            int? tenthRoll3 = rollIndex < _rolls.Count ? _rolls[rollIndex] : null;
            var tenthIsStrike = tenthRoll1 == 10;
            var tenthIsSpare = tenthRoll1.HasValue && tenthRoll2.HasValue &&
                               tenthRoll1 < 10 && tenthRoll1 + tenthRoll2 == 10;
            frames[9] = new FrameScore(
                10, tenthRoll1, tenthRoll2, tenthRoll3, cumulativeScores[9], tenthIsStrike, tenthIsSpare);

            var totalSoFar = 0;
            for (var i = cumulativeScores.Length - 1; i >= 0; i--)
            {
                if (cumulativeScores[i].HasValue)
                {
                    totalSoFar = cumulativeScores[i].Value;
                    break;
                }
            }

            return new ScoreSnapshot(frames, totalSoFar, isGameComplete);
        }
    }
}
