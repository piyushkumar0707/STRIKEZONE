using System;
using System.Collections;
using StrikeZone.Gameplay;
using StrikeZone.Logic;
using UnityEngine;

namespace StrikeZone.Core
{
    /// <summary>Coordinates throws, pin resolution, frame progression, and score updates.</summary>
    [DefaultExecutionOrder(-100)]
    public sealed class BowlingGameManager : MonoBehaviour
    {
        [SerializeField] private PinManager _pinManager;
        [SerializeField] private BowlingController _bowlingController;
        [SerializeField] private BowlingScoreManager _scoreManager;
        [SerializeField] private BowlingScoreManager _secondScoreManager;
        [SerializeField] private BowlingBall _ball;
        [SerializeField] private Transform _ballSpawnPoint;
        [SerializeField, Min(0f)] private float _betweenRollDelay = 0.2f;

        private FrameManager _frameManager;
        private FrameManager _secondFrameManager;
        private int _activePlayerIndex;
        private bool _playerChangedThisTurn;
        private NextAction _nextAction;
        private BowlingGameState _stateBeforePause;
        private bool _inputWasEnabledBeforePause;

        public event Action<BowlingGameState> OnStateChanged;
        public event Action OnGameCompleted;
        public event Action OnPauseRequested;
        public event Action<int, int, int, ScoreSnapshot> OnRollResolved;
        public event Action<int, int, int, int, ScoreSnapshot> OnPlayerRollResolved;
        public event Action<int> OnActivePlayerChanged;

        public BowlingGameState State { get; private set; } = BowlingGameState.Initializing;
        public int CurrentFrame => ActiveFrameManager != null ? ActiveFrameManager.CurrentFrame : 1;
        public int CurrentRoll => ActiveFrameManager != null ? ActiveFrameManager.CurrentRoll : 1;
        public ScoreSnapshot CurrentScore => ActiveScoreManager != null ? ActiveScoreManager.CurrentSnapshot : null;
        public int ActivePlayerNumber => _activePlayerIndex + 1;
        public bool IsTwoPlayer => BowlingSession.IsTwoPlayer;
        public BowlingScoreManager FirstPlayerScoreManager => _scoreManager;
        public BowlingScoreManager SecondPlayerScoreManager => _secondScoreManager;
        public ScoreSnapshot FirstPlayerScore => _scoreManager != null ? _scoreManager.CurrentSnapshot : null;
        public ScoreSnapshot SecondPlayerScore => _secondScoreManager != null ? _secondScoreManager.CurrentSnapshot : null;
        public int FrameForPlayer(int playerNumber) => playerNumber == 2 && _secondFrameManager != null
            ? _secondFrameManager.CurrentFrame
            : _frameManager != null ? _frameManager.CurrentFrame : 1;

        private FrameManager ActiveFrameManager => _activePlayerIndex == 0 ? _frameManager : _secondFrameManager;
        private BowlingScoreManager ActiveScoreManager => _activePlayerIndex == 0 ? _scoreManager : _secondScoreManager;
        public int PinsStandingBeforeLastRoll { get; private set; } = 10;

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

            if (BowlingSession.IsTwoPlayer)
            {
                _secondFrameManager = new FrameManager();
                if (_secondScoreManager == null)
                {
                    var scoreObject = new GameObject("Player2ScoreManager");
                    scoreObject.transform.SetParent(transform, false);
                    _secondScoreManager = scoreObject.AddComponent<BowlingScoreManager>();
                }
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
                _bowlingController.OnQuickReturnRequested += HandleQuickReturnRequested;
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
                _bowlingController.OnQuickReturnRequested -= HandleQuickReturnRequested;
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
            _secondFrameManager?.Reset();
            _activePlayerIndex = 0;
            _playerChangedThisTurn = false;
            _scoreManager.ResetGame();
            _secondScoreManager?.ResetGame();
            _pinManager.RackAll();
            ApplyLanePresentation();
            _bowlingController.ResetForNextRoll(_ballSpawnPoint);
            ChangeState(BowlingGameState.WaitingForAim);
        }

        private void ApplyLanePresentation()
        {
            var lane = transform.Find("Environment/Lane");
            var environment = transform.Find("Environment");
            if (lane == null || environment == null || environment.Find("LaneInlays") != null)
            {
                return;
            }

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null)
            {
                return;
            }

            var inlayMaterial = new Material(shader) { name = "StrikeZone Lane Inlay" };
            inlayMaterial.color = new Color(0.055f, 0.13f, 0.19f);
            if (inlayMaterial.HasProperty("_Smoothness")) inlayMaterial.SetFloat("_Smoothness", 0.72f);

            var inlays = new GameObject("LaneInlays").transform;
            inlays.SetParent(environment, false);
            var meshObject = new GameObject("AimingArrows", typeof(MeshFilter), typeof(MeshRenderer));
            meshObject.transform.SetParent(inlays, false);
            meshObject.transform.localPosition = new Vector3(0f, 0.009f, 5.7f);
            var vertices = new Vector3[15];
            var triangles = new int[15];
            for (var i = 0; i < 5; i++)
            {
                var centerX = (i - 2) * 0.18f;
                var vertex = i * 3;
                vertices[vertex] = new Vector3(centerX - 0.045f, 0f, -0.12f);
                vertices[vertex + 1] = new Vector3(centerX + 0.045f, 0f, -0.12f);
                vertices[vertex + 2] = new Vector3(centerX, 0f, 0.12f);
                triangles[vertex] = vertex;
                triangles[vertex + 1] = vertex + 2;
                triangles[vertex + 2] = vertex + 1;
            }

            var arrowMesh = new Mesh { name = "Bowling Lane Aiming Arrows" };
            arrowMesh.vertices = vertices;
            arrowMesh.triangles = triangles;
            arrowMesh.RecalculateNormals();
            meshObject.GetComponent<MeshFilter>().sharedMesh = arrowMesh;
            meshObject.GetComponent<MeshRenderer>().sharedMaterial = inlayMaterial;

            var stripeMaterial = new Material(shader) { name = "StrikeZone Pin Stripe" };
            stripeMaterial.color = new Color(0.88f, 0.075f, 0.09f);
            if (stripeMaterial.HasProperty("_Smoothness")) stripeMaterial.SetFloat("_Smoothness", 0.82f);
            foreach (var pin in _pinManager.GetComponentsInChildren<BowlingPin>(true))
            {
                var stripe = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                stripe.name = "RedNeckStripe";
                stripe.transform.SetParent(pin.transform, false);
                stripe.transform.localPosition = new Vector3(0f, 0.4f, 0f);
                stripe.transform.localScale = new Vector3(0.93f, 0.037f, 0.93f);
                stripe.GetComponent<Renderer>().sharedMaterial = stripeMaterial;
                stripe.GetComponent<Collider>().enabled = false;
            }
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

            PinsStandingBeforeLastRoll = _pinManager.StandingPinCount;
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
            if (pinsKnockedDown == 10 && PinsStandingBeforeLastRoll == 10)
            {
                BowlingSession.RegisterStrike();
            }

            var frameManager = ActiveFrameManager;
            var scoreManager = ActiveScoreManager;
            _nextAction = frameManager.RecordRoll(pinsKnockedDown);
            var snapshot = scoreManager.AddRoll(pinsKnockedDown, frameManager.IsGameComplete);
            OnPlayerRollResolved?.Invoke(ActivePlayerNumber, resolvedFrame, resolvedRoll, pinsKnockedDown, snapshot);
            OnRollResolved?.Invoke(resolvedFrame, resolvedRoll, pinsKnockedDown, snapshot);
            Debug.Log($"Player {ActivePlayerNumber}, Frame {resolvedFrame}, Roll {resolvedRoll}: {pinsKnockedDown} pins. Score: {snapshot.TotalSoFar}", this);

            ChangeState(BowlingGameState.FrameAdvance);
            var bothPlayersComplete = !IsTwoPlayer ||
                (_frameManager.IsGameComplete && _secondFrameManager != null && _secondFrameManager.IsGameComplete);
            if (_nextAction == NextAction.GameOver && bothPlayersComplete)
            {
                BowlingSession.Capture(_scoreManager.CurrentSnapshot, _secondScoreManager != null
                    ? _secondScoreManager.CurrentSnapshot
                    : null);
                ChangeState(BowlingGameState.GameOver);
                _bowlingController.SetInputEnabled(false);
                OnGameCompleted?.Invoke();
                return;
            }

            _playerChangedThisTurn = IsTwoPlayer &&
                (_nextAction == NextAction.NextFrame_FreshRack || _nextAction == NextAction.GameOver) &&
                SwitchActivePlayer();

            StartCoroutine(PrepareNextRoll(_nextAction));
        }

        private bool SwitchActivePlayer()
        {
            var nextPlayer = 1 - _activePlayerIndex;
            var nextFrameManager = nextPlayer == 0 ? _frameManager : _secondFrameManager;
            if (nextFrameManager == null || nextFrameManager.IsGameComplete)
            {
                return false;
            }

            _activePlayerIndex = nextPlayer;
            OnActivePlayerChanged?.Invoke(ActivePlayerNumber);
            return true;
        }

        private IEnumerator PrepareNextRoll(NextAction nextAction)
        {
            ChangeState(BowlingGameState.ResettingPins);
            _pinManager.SweepKnockedPins();

            if (_playerChangedThisTurn || nextAction == NextAction.SameFrameNextRoll_FreshRack ||
                nextAction == NextAction.NextFrame_FreshRack)
            {
                _pinManager.RackAll();
            }

            _playerChangedThisTurn = false;

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

        private void HandleQuickReturnRequested()
        {
            if (State == BowlingGameState.BallRolling || State == BowlingGameState.PinsSettling)
            {
                _pinManager?.RequestQuickResolve();
            }
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
