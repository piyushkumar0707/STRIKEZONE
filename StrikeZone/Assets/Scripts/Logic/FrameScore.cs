namespace StrikeZone.Logic
{
    /// <summary>Immutable score data for one frame.</summary>
    public sealed class FrameScore
    {
        public int FrameNumber { get; }
        public int? Roll1 { get; }
        public int? Roll2 { get; }
        public int? Roll3 { get; }
        public int? Cumulative { get; }
        public bool IsStrike { get; }
        public bool IsSpare { get; }

        public FrameScore(
            int frameNumber,
            int? roll1,
            int? roll2,
            int? roll3,
            int? cumulative,
            bool isStrike,
            bool isSpare)
        {
            FrameNumber = frameNumber;
            Roll1 = roll1;
            Roll2 = roll2;
            Roll3 = roll3;
            Cumulative = cumulative;
            IsStrike = isStrike;
            IsSpare = isSpare;
        }
    }
}
