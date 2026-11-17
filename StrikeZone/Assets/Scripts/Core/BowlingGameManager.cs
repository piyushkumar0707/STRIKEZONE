using System;
using System.Collections;
using StrikeZone.Gameplay;
using StrikeZone.Logic;
using UnityEngine;

namespace StrikeZone.Core
{
    /// <summary>Coordinates throws, pin resolution, frame progression, and score updates.</summary>
    public sealed class BowlingGameManager : MonoBehaviour
    {
        [SerializeField] private PinManager _pinManager;
        [SerializeField] private BowlingController _bowlingController;
        [SerializeField] private BowlingScoreManager _scoreManager;
        [SerializeField] private BowlingBall _ball;
        [SerializeField] private Transform _ballSpawnPoint;
        [SerializeField, Min(0f)] private float _betweenRollDelay = 0.2f;

        private FrameManager _frameManager;
        private NextAction _nextAction;
        private BowlingGameState _stateBeforePause;
        private bool _inputWasEnabledBeforePause;

        public event Action<BowlingGameState> OnStateChanged;
        public event Action OnGameCompleted;
        public event Action OnPauseRequested;
        public event Action<int, int, int, ScoreSnapshot> OnRollResolved;

        public BowlingGameState State { get; private set; } = BowlingGameState.Initializing;
        public int CurrentFrame => _frameManager != null ? _frameManager.CurrentFrame : 1;
        public int CurrentRoll => _frameManager != null ? _frameManager.CurrentRoll : 1;
        public ScoreSnapshot CurrentScore => _scoreManager != null ? _scoreManager.CurrentSnapshot : null;

        private void Awake()
        {
            _frameManager = new FrameManager();

            if (_pinManager == null)
            {
                _pinManager = GetComponent<PinManager>();
            }

            if (_bowlingController == null)
            {
                _bowlingController = GetComponentInChildren<BowlingController>(true);
            }

            if (_scoreManager == null)
            {
                _scoreManager = GetComponentInChildren<BowlingScoreManager>(true);
            }

            if (_ball == null)
            {
                var ballTransform = transform.Find("Ball");
                if (ballTransform != null)
                {
                    _ball = ballTransform.GetComponent<BowlingBall>();
                }
            }

            if (_ballSpawnPoint == null)
            {
                _ballSpawnPoint = transform.Find("BowlingSetup/BallSpawnPoint");
            }
        }

        private void OnEnable()
        {
            if (_pinManager != null)
            {
                _pinManager.OnRollResolved += HandleRollResolved;
            }

            if (_bowlingController != null)
            {
                _bowlingController.OnBallReleased += HandleBallReleased;
                _bowlingController.OnPauseRequested += HandlePauseRequested;
                _bowlingController.OnPowerMeterStarted += HandlePowerMeterStarted;
                _bowlingController.OnPowerLocked += HandlePowerLocked;
            }
        }

        private void OnDisable()
        {
            if (_pinManager != null)
            {
                _pinManager.OnRollResolved -= HandleRollResolved;
            }

            if (_bowlingController != null)
            {
                _bowlingController.OnBallReleased -= HandleBallReleased;
                _bowlingController.OnPauseRequested -= HandlePauseRequested;
                _bowlingController.OnPowerMeterStarted -= HandlePowerMeterStarted;
                _bowlingController.OnPowerLocked -= HandlePowerLocked;
            }
        }

        private void Start()
        {
            if (_pinManager == null || _bowlingController == null || _scoreManager == null ||
                _ball == null || _ballSpawnPoint == null)
            {
                Debug.LogError("BowlingGameManager is missing a required game component or spawn point.", this);
                enabled = false;
                return;
            }

            _frameManager.Reset();
            _scoreManager.ResetGame();
            _pinManager.RackAll();
            _bowlingController.ResetForNextRoll(_ballSpawnPoint);
            ChangeState(BowlingGameState.WaitingForAim);
        }

        private void HandlePowerMeterStarted()
        {
            if (State == BowlingGameState.WaitingForAim)
            {
                ChangeState(BowlingGameState.PowerMeterRunning);
            }
        }

        private void HandlePowerLocked(float power)
        {
            if (State == BowlingGameState.PowerMeterRunning)
            {
                ChangeState(BowlingGameState.WaitingForAim);
            }
        }

        private void HandleBallReleased()
        {
            if (State != BowlingGameState.WaitingForAim)
            {
                return;
            }

            ChangeState(BowlingGameState.BallRolling);
            StartCoroutine(AdvanceToPinSettleView());
            StartCoroutine(_pinManager.WaitForRollToSettle());
        }

        private IEnumerator AdvanceToPinSettleView()
        {
            var remaining = 6f;
            while (_ball != null && _ball.transform.position.z < 7.5f && _ball.Speed > 0.05f && remaining > 0f)
            {
                remaining -= Time.deltaTime;
                yield return null;
            }

            if (State == BowlingGameState.BallRolling)
            {
                ChangeState(BowlingGameState.PinsSettling);
            }
        }

        private void HandleRollResolved(int pinsKnockedDown)
        {
            if (State != BowlingGameState.PinsSettling)
            {
                return;
            }

            ChangeState(BowlingGameState.ScoreUpdate);
            var resolvedFrame = CurrentFrame;
            var resolvedRoll = CurrentRoll;
            _nextAction = _frameManager.RecordRoll(pinsKnockedDown);
            var snapshot = _scoreManager.AddRoll(pinsKnockedDown, _frameManager.IsGameComplete);
            OnRollResolved?.Invoke(resolvedFrame, resolvedRoll, pinsKnockedDown, snapshot);
            Debug.Log($"Frame {resolvedFrame}, Roll {resolvedRoll}: {pinsKnockedDown} pins. Score: {snapshot.TotalSoFar}", this);

            ChangeState(BowlingGameState.FrameAdvance);
            if (_nextAction == NextAction.GameOver)
            {
                BowlingSession.Capture(snapshot);
                ChangeState(BowlingGameState.GameOver);
                _bowlingController.SetInputEnabled(false);
                OnGameCompleted?.Invoke();
                return;
            }

            StartCoroutine(PrepareNextRoll(_nextAction));
        }

        private IEnumerator PrepareNextRoll(NextAction nextAction)
        {
            ChangeState(BowlingGameState.ResettingPins);
            _pinManager.SweepKnockedPins();

            if (nextAction == NextAction.SameFrameNextRoll_FreshRack ||
                nextAction == NextAction.NextFrame_FreshRack)
            {
                _pinManager.RackAll();
            }

            _bowlingController.ResetForNextRoll(_ballSpawnPoint);
            if (_betweenRollDelay > 0f)
            {
                yield return new WaitForSeconds(_betweenRollDelay);
            }

            ChangeState(BowlingGameState.WaitingForAim);
        }

        private void HandlePauseRequested()
        {
            OnPauseRequested?.Invoke();
        }

        public void PauseGame()
        {
            if (State == BowlingGameState.Paused || State == BowlingGameState.GameOver ||
                State == BowlingGameState.Initializing)
            {
                return;
            }

            _stateBeforePause = State;
            _inputWasEnabledBeforePause = _bowlingController != null && _bowlingController.IsInputEnabled;
            _bowlingController?.SetInputEnabled(false);
            Time.timeScale = 0f;
            ChangeState(BowlingGameState.Paused);
        }

        public void ResumeGame()
        {
            if (State != BowlingGameState.Paused)
            {
                return;
            }

            Time.timeScale = 1f;
            ChangeState(_stateBeforePause);
            _bowlingController?.SetInputEnabled(_inputWasEnabledBeforePause);
        }

        private void ChangeState(BowlingGameState newState)
        {
            if (State == newState)
            {
                return;
            }

            State = newState;
            OnStateChanged?.Invoke(State);
        }
    }
}
