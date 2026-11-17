namespace StrikeZone.Core
{
    /// <summary>High-level states used by the bowling game flow and HUD.</summary>
    public enum BowlingGameState
    {
        Initializing,
        WaitingForAim,
        PowerMeterRunning,
        BallRolling,
        PinsSettling,
        ScoreUpdate,
        FrameAdvance,
        ResettingPins,
        GameOver,
        Paused
    }
}
