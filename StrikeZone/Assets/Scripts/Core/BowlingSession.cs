using StrikeZone.Logic;

namespace StrikeZone.Core
{
    /// <summary>Holds the finished game's score while Unity loads the results scene.</summary>
    public static class BowlingSession
    {
        public static ScoreSnapshot FinalSnapshot { get; private set; }

        public static void Capture(ScoreSnapshot snapshot)
        {
            FinalSnapshot = snapshot;
        }

        public static void Clear()
        {
            FinalSnapshot = null;
        }
    }
}
