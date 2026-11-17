namespace StrikeZone.Logic
{
    /// <summary>Describes what the game should do after a roll is recorded.</summary>
    public enum NextAction
    {
        SameFrameNextRoll_KeepPins,
        SameFrameNextRoll_FreshRack,
        NextFrame_FreshRack,
        GameOver
    }
}
