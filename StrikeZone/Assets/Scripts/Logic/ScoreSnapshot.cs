namespace StrikeZone.Logic
{
    /// <summary>Immutable scoreboard view published after each recorded roll.</summary>
    public sealed class ScoreSnapshot
    {
        public FrameScore[] Frames { get; }
        public int TotalSoFar { get; }
        public bool IsGameComplete { get; }

        public ScoreSnapshot(FrameScore[] frames, int totalSoFar, bool isGameComplete)
        {
            Frames = frames;
            TotalSoFar = totalSoFar;
            IsGameComplete = isGameComplete;
        }
    }
}
