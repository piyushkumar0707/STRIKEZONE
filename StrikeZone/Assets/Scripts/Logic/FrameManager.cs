using System;

namespace StrikeZone.Logic
{
    /// <summary>Tracks frame and roll progression for a single ten-pin bowling game.</summary>
    public sealed class FrameManager
    {
        private int _firstRollPins = -1;
        private int _secondRollPins = -1;

        public int CurrentFrame { get; private set; } = 1;
        public int CurrentRoll { get; private set; } = 1;
        public bool IsGameComplete { get; private set; }

        /// <summary>Records pins knocked down and returns the required rack/frame action.</summary>
        public NextAction RecordRoll(int pins)
        {
            if (IsGameComplete)
            {
                throw new InvalidOperationException("The game is complete; no more rolls can be recorded.");
            }

            if (pins < 0 || pins > 10)
            {
                throw new ArgumentOutOfRangeException(nameof(pins), pins, "A roll must knock down 0 to 10 pins.");
            }

            if (CurrentFrame < 10)
            {
                return RecordFramesOneToNine(pins);
            }

            return RecordTenthFrame(pins);
        }

        /// <summary>Restores the progression state to the first roll of a new game.</summary>
        public void Reset()
        {
            CurrentFrame = 1;
            CurrentRoll = 1;
            IsGameComplete = false;
            _firstRollPins = -1;
            _secondRollPins = -1;
        }

        private NextAction RecordFramesOneToNine(int pins)
        {
            if (CurrentRoll == 1)
            {
                if (pins == 10)
                {
                    AdvanceToNextFrame();
                    return NextAction.NextFrame_FreshRack;
                }

                _firstRollPins = pins;
                CurrentRoll = 2;
                return NextAction.SameFrameNextRoll_KeepPins;
            }

            if (_firstRollPins + pins > 10)
            {
                throw new ArgumentException("The two rolls in a frame cannot knock down more than ten pins.", nameof(pins));
            }

            AdvanceToNextFrame();
            return NextAction.NextFrame_FreshRack;
        }

        private NextAction RecordTenthFrame(int pins)
        {
            if (CurrentRoll == 1)
            {
                _firstRollPins = pins;
                CurrentRoll = 2;
                return pins == 10
                    ? NextAction.SameFrameNextRoll_FreshRack
                    : NextAction.SameFrameNextRoll_KeepPins;
            }

            if (CurrentRoll == 2)
            {
                if (_firstRollPins < 10 && _firstRollPins + pins > 10)
                {
                    throw new ArgumentException("The first two rolls of the tenth frame exceed ten pins.", nameof(pins));
                }

                _secondRollPins = pins;

                if (_firstRollPins == 10)
                {
                    CurrentRoll = 3;
                    return pins == 10
                        ? NextAction.SameFrameNextRoll_FreshRack
                        : NextAction.SameFrameNextRoll_KeepPins;
                }

                if (_firstRollPins + _secondRollPins == 10)
                {
                    CurrentRoll = 3;
                    return NextAction.SameFrameNextRoll_FreshRack;
                }

                CompleteGame();
                return NextAction.GameOver;
            }

            if (_firstRollPins == 10 && _secondRollPins < 10 && _secondRollPins + pins > 10)
            {
                throw new ArgumentException("The second and third rolls exceed ten pins without a fresh rack.", nameof(pins));
            }

            CompleteGame();
            return NextAction.GameOver;
        }

        private void AdvanceToNextFrame()
        {
            CurrentFrame++;
            CurrentRoll = 1;
            _firstRollPins = -1;
            _secondRollPins = -1;
        }

        private void CompleteGame()
        {
            IsGameComplete = true;
            CurrentRoll = 3;
        }
    }
}
