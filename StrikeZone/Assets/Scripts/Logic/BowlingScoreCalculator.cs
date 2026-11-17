using System;
using System.Collections.Generic;

namespace StrikeZone.Logic
{
    /// <summary>Calculates official ten-pin bowling scores from recorded roll values.</summary>
    public static class BowlingScoreCalculator
    {
        /// <summary>
        /// Returns ten cumulative frame scores. A null entry means the frame's score
        /// is still waiting on one or more bonus rolls.
        /// </summary>
        public static int?[] CalculateCumulative(IReadOnlyList<int> rolls)
        {
            if (rolls == null)
            {
                throw new ArgumentNullException(nameof(rolls));
            }

            ValidateRollValues(rolls);

            var result = new int?[10];
            var total = 0;
            var rollIndex = 0;

            for (var frame = 0; frame < 10; frame++)
            {
                if (rollIndex >= rolls.Count)
                {
                    break;
                }

                var firstRoll = rolls[rollIndex];
                if (firstRoll == 10)
                {
                    if (rollIndex + 2 >= rolls.Count)
                    {
                        break;
                    }

                    total += 10 + rolls[rollIndex + 1] + rolls[rollIndex + 2];
                    rollIndex++;
                }
                else
                {
                    if (rollIndex + 1 >= rolls.Count)
                    {
                        break;
                    }

                    var framePins = firstRoll + rolls[rollIndex + 1];
                    if (framePins > 10)
                    {
                        throw new ArgumentException(
                            $"Rolls in frame {frame + 1} knock down more than ten pins.",
                            nameof(rolls));
                    }

                    if (framePins == 10)
                    {
                        if (rollIndex + 2 >= rolls.Count)
                        {
                            break;
                        }

                        total += 10 + rolls[rollIndex + 2];
                    }
                    else
                    {
                        total += framePins;
                    }

                    rollIndex += 2;
                }

                result[frame] = total;
            }

            return result;
        }

        private static void ValidateRollValues(IReadOnlyList<int> rolls)
        {
            for (var i = 0; i < rolls.Count; i++)
            {
                if (rolls[i] < 0 || rolls[i] > 10)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(rolls), rolls[i], "Each roll must knock down between 0 and 10 pins.");
                }
            }
        }
    }
}
