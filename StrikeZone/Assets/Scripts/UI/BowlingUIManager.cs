using System.Collections;
using StrikeZone.Core;
using StrikeZone.Gameplay;
using StrikeZone.Logic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace StrikeZone.UI
{
    /// <summary>Updates the bowling HUD and controls pause, restart, menu, and results screens.</summary>
    public sealed class BowlingUIManager : MonoBehaviour
    {
        [SerializeField] private BowlingGameManager _gameManager;
        [SerializeField] private BowlingScoreManager _scoreManager;
        [SerializeField] private BowlingScoreManager _secondScoreManager;
        [SerializeField] private BowlingController _bowlingController;
        [SerializeField] private PowerMeter _powerMeter;
        [SerializeField] private ScoreboardUI _scoreboard;
        [SerializeField] private ScoreboardUI _secondScoreboard;
        [SerializeField] private TMP_Text _frameInfo;
        [SerializeField] private TMP_Text _totalScore;
        [SerializeField] private TMP_Text _keyHints;
        [SerializeField] private TMP_Text _powerPercent;
        [SerializeField] private TMP_Text _banner;
        [SerializeField] private TMP_Text _gameOverScore;
        [SerializeField] private TMP_Text _challengeProgress;
        [SerializeField] private TMP_Text _secondTotalScore;
        [SerializeField] private TMP_Text _playerOneLabel;
        [SerializeField] private TMP_Text _playerTwoLabel;
        [SerializeField] private Image _powerFill;
        [SerializeField] private GameObject _pausePanel;
        [SerializeField] private GameObject _gameOverPanel;
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _mainMenuButton;
        [SerializeField] private Button _resultsButton;
        private Coroutine _bannerRoutine;
        private float _lastLockedPower;

        private void Awake()
        {
            ResolveReferences();
            if (_banner != null)
            {
                _banner.gameObject.SetActive(false);
            }
        }

        private void OnEnable()
        {
            if (_gameManager != null)
            {
                _gameManager.OnStateChanged += HandleStateChanged;
                _gameManager.OnPauseRequested += TogglePause;
                _gameManager.OnGameCompleted += HandleGameCompleted;
                _gameManager.OnPlayerRollResolved += HandleRollResolved;
                _gameManager.OnActivePlayerChanged += HandleActivePlayerChanged;
            }

            if (_scoreManager != null)
            {
                _scoreManager.OnScoreUpdated += HandleScoreUpdated;
            }

            if (_secondScoreManager != null)
            {
                _secondScoreManager.OnScoreUpdated += HandleScoreUpdated;
            }

            if (_bowlingController != null)
            {
                _bowlingController.OnPowerLocked += HandlePowerLocked;
            }

            if (_resumeButton != null) _resumeButton.onClick.AddListener(Resume);
            if (_restartButton != null) _restartButton.onClick.AddListener(Restart);
            if (_mainMenuButton != null) _mainMenuButton.onClick.AddListener(GoToMainMenu);
            if (_resultsButton != null) _resultsButton.onClick.AddListener(OpenResults);
        }

        private void Start()
        {
            _pausePanel?.SetActive(false);
            _gameOverPanel?.SetActive(false);
            HandleStateChanged(_gameManager != null ? _gameManager.State : BowlingGameState.WaitingForAim);
            HandleScoreUpdated(_scoreManager != null ? _scoreManager.CurrentSnapshot : null);
            UpdateChallengeProgress();
        }

        private void OnDisable()
        {
            if (_gameManager != null)
            {
                _gameManager.OnStateChanged -= HandleStateChanged;
                _gameManager.OnPauseRequested -= TogglePause;
                _gameManager.OnGameCompleted -= HandleGameCompleted;
                _gameManager.OnPlayerRollResolved -= HandleRollResolved;
                _gameManager.OnActivePlayerChanged -= HandleActivePlayerChanged;
            }

            if (_scoreManager != null)
            {
                _scoreManager.OnScoreUpdated -= HandleScoreUpdated;
            }

            if (_secondScoreManager != null)
            {
                _secondScoreManager.OnScoreUpdated -= HandleScoreUpdated;
            }

            if (_bowlingController != null)
            {
                _bowlingController.OnPowerLocked -= HandlePowerLocked;
            }

            if (_resumeButton != null) _resumeButton.onClick.RemoveListener(Resume);
            if (_restartButton != null) _restartButton.onClick.RemoveListener(Restart);
            if (_mainMenuButton != null) _mainMenuButton.onClick.RemoveListener(GoToMainMenu);
            if (_resultsButton != null) _resultsButton.onClick.RemoveListener(OpenResults);
        }

        private void Update()
        {
            if (_powerMeter == null)
            {
                return;
            }

            var value = _powerMeter.Value;
            if (_powerFill != null)
            {
                _powerFill.fillAmount = value;
                var timingDistance = Mathf.Abs(value - 0.7f);
                _powerFill.color = timingDistance <= 0.06f ? new Color(.15f, .95f, .55f) :
                    timingDistance <= 0.22f ? new Color(1f, .82f, .1f) : new Color(1f, .2f, .25f);
            }

            if (_powerPercent != null)
            {
                _powerPercent.text = $"{Mathf.RoundToInt(value * 100f)}%";
            }
        }

        private void TogglePause()
        {
            if (_gameManager == null)
            {
                return;
            }

            if (_gameManager.State == BowlingGameState.Paused)
            {
                Resume();
            }
            else if (_gameManager.State != BowlingGameState.GameOver)
            {
                _gameManager.PauseGame();
            }
        }

        private void Resume()
        {
            _pausePanel?.SetActive(false);
            _gameManager?.ResumeGame();
        }

        private void Restart()
        {
            Time.timeScale = 1f;
            BowlingSession.RestartCurrentGame();
            SceneManager.LoadScene("BowlingGame");
        }

        private void GoToMainMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("BowlingMenu");
        }

        private void OpenResults()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("BowlingResults");
        }

        private void HandleStateChanged(BowlingGameState state)
        {
            if (_pausePanel != null)
            {
                _pausePanel.SetActive(state == BowlingGameState.Paused);
            }

            if (_frameInfo != null && _gameManager != null)
            {
                _frameInfo.text = _gameManager.IsTwoPlayer
                    ? $"PLAYER {_gameManager.ActivePlayerNumber}  •  FRAME {_gameManager.CurrentFrame}/10   ROLL {_gameManager.CurrentRoll}"
                    : $"FRAME {_gameManager.CurrentFrame}/10   ROLL {_gameManager.CurrentRoll}";
            }

            if (_keyHints != null)
            {
                switch (state)
                {
                    case BowlingGameState.WaitingForAim:
                        _keyHints.text = "A / D  AIM     Q / E  HOOK     SPACE  START POWER";
                        break;
                    case BowlingGameState.PowerMeterRunning:
                        _keyHints.text = "A / D  AIM     Q / E  HOOK     SPACE  LOCK NEAR 70% & THROW";
                        break;
                    case BowlingGameState.BallRolling:
                    case BowlingGameState.PinsSettling:
                        _keyHints.text = "ROLL IN PROGRESS     SPACE  QUICK RETURN";
                        break;
                    case BowlingGameState.Paused:
                        _keyHints.text = "PAUSED";
                        break;
                    default:
                        break;
                }
            }

            if (_scoreboard != null && _scoreManager != null)
            {
                UpdateScoreboards();
            }
        }

        private void HandleScoreUpdated(ScoreSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return;
            }

            UpdateScoreboards();
            if (_gameOverScore != null && snapshot.IsGameComplete)
            {
                _gameOverScore.text = $"FINAL SCORE: {snapshot.TotalSoFar}";
            }
        }

        private void HandleGameCompleted()
        {
            _gameOverPanel?.SetActive(true);
            var title = FindText("GameOverTitle");
            if (title != null)
            {
                title.text = _gameManager != null && _gameManager.IsTwoPlayer
                    ? "MATCH COMPLETE"
                    : BowlingSession.IsStrikeChallenge
                    ? BowlingSession.ChallengeComplete ? "CHALLENGE COMPLETE!" : "CHALLENGE MISSED"
                    : "LANE COMPLETE";
            }
            if (_gameManager != null && _gameManager.IsTwoPlayer)
            {
                var first = _gameManager.FirstPlayerScore != null ? _gameManager.FirstPlayerScore.TotalSoFar : 0;
                var second = _gameManager.SecondPlayerScore != null ? _gameManager.SecondPlayerScore.TotalSoFar : 0;
                var outcome = first == second ? "DRAW" : first > second ? "PLAYER 1 WINS" : "PLAYER 2 WINS";
                if (_gameOverScore != null) _gameOverScore.text = $"P1 {first}  |  P2 {second}  |  {outcome}";
                if (_gameOverScore != null) _gameOverScore.fontSize = 24f;
            }
            else
            {
                _gameOverScore?.SetText("FINAL SCORE: {0}", _scoreManager != null && _scoreManager.CurrentSnapshot != null
                    ? _scoreManager.CurrentSnapshot.TotalSoFar
                    : 0);
            }
            _keyHints?.SetText("GAME COMPLETE");
        }

        private void HandleRollResolved(int playerNumber, int frameNumber, int rollNumber, int pins, ScoreSnapshot snapshot)
        {
            if (snapshot == null || frameNumber < 1 || frameNumber > snapshot.Frames.Length)
            {
                return;
            }

            var pinsBefore = _gameManager != null ? Mathf.Max(1, _gameManager.PinsStandingBeforeLastRoll) : 10;
            var pinAccuracy = Mathf.Clamp01(pins / (float)pinsBefore);
            const float idealPower = 0.7f;
            var powerTiming = 1f - Mathf.Abs(_lastLockedPower - idealPower) / idealPower;
            var aimCentering = _bowlingController != null ? _bowlingController.AimCentering : 1f;
            var quality = Mathf.Clamp01(pinAccuracy * 0.7f + powerTiming * 0.2f + aimCentering * 0.1f);
            var rank = quality >= 0.9f ? "S" : quality >= 0.78f ? "A" : quality >= 0.62f ? "B" : "C";
            BowlingSession.RecordShot(quality, rank);

            var outcome = pins == 10 && pinsBefore == 10 ? "STRIKE" :
                pins == pinsBefore && pinsBefore > 0 ? "SPARE" :
                pins == 0 ? "GUTTER" : $"{pins} PINS";
            var color = rank == "S" ? new Color(1f, .83f, .08f) :
                rank == "A" ? new Color(.08f, .86f, 1f) :
                rank == "B" ? new Color(.45f, 1f, .55f) : new Color(1f, .48f, .35f);
            var playerPrefix = _gameManager != null && _gameManager.IsTwoPlayer ? $"P{playerNumber}  •  " : string.Empty;
            ShowBanner($"{playerPrefix}{rank} RANK   •   {outcome}", color);
            UpdateChallengeProgress();
        }

        private void HandleActivePlayerChanged(int playerNumber)
        {
            UpdateScoreboards();
            if (_frameInfo != null && _gameManager != null)
            {
                _frameInfo.text = $"PLAYER {playerNumber}  •  FRAME {_gameManager.CurrentFrame}/10   ROLL {_gameManager.CurrentRoll}";
            }
        }

        private void HandlePowerLocked(float power)
        {
            _lastLockedPower = power;
        }

        private void UpdateChallengeProgress()
        {
            if (_challengeProgress == null)
            {
                return;
            }

            _challengeProgress.gameObject.SetActive(BowlingSession.IsStrikeChallenge);
            if (BowlingSession.IsStrikeChallenge)
            {
                var progress = Mathf.Min(BowlingSession.ChallengeStrikes, BowlingSession.ChallengeStrikeTarget);
                _challengeProgress.text = BowlingSession.ChallengeComplete
                    ? $"STRIKE CHALLENGE   {progress} / {BowlingSession.ChallengeStrikeTarget}   TARGET HIT!"
                    : $"STRIKE CHALLENGE   {progress} / {BowlingSession.ChallengeStrikeTarget} STRIKES";
            }
        }

        private void ShowBanner(string message, Color color)
        {
            if (_banner == null)
            {
                return;
            }

            if (_bannerRoutine != null)
            {
                StopCoroutine(_bannerRoutine);
            }

            _banner.color = color;
            _banner.text = message;
            _banner.gameObject.SetActive(true);
            _banner.rectTransform.localScale = Vector3.one * .72f;
            var bannerColor = _banner.color;
            bannerColor.a = 0f;
            _banner.color = bannerColor;
            _bannerRoutine = StartCoroutine(HideBannerAfterDelay());
        }

        private IEnumerator HideBannerAfterDelay()
        {
            const float popDuration = .24f;
            var elapsed = 0f;
            while (elapsed < popDuration && _banner != null)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / popDuration);
                var overshoot = Mathf.Sin(t * Mathf.PI * .5f);
                _banner.rectTransform.localScale = Vector3.one * Mathf.Lerp(.72f, 1.12f, overshoot);
                var color = _banner.color;
                color.a = t;
                _banner.color = color;
                yield return null;
            }

            if (_banner != null) _banner.rectTransform.localScale = Vector3.one;
            yield return new WaitForSecondsRealtime(1.05f);
            elapsed = 0f;
            while (elapsed < .3f && _banner != null)
            {
                elapsed += Time.unscaledDeltaTime;
                var color = _banner.color;
                color.a = 1f - Mathf.Clamp01(elapsed / .3f);
                _banner.color = color;
                yield return null;
            }

            if (_banner != null)
            {
                _banner.gameObject.SetActive(false);
            }

            _bannerRoutine = null;
        }

        private void ResolveReferences()
        {
            if (_gameManager == null) _gameManager = FindAnyObjectByType<BowlingGameManager>();
            if (_scoreManager == null) _scoreManager = _gameManager != null
                ? _gameManager.FirstPlayerScoreManager
                : FindAnyObjectByType<BowlingScoreManager>();
            if (_secondScoreManager == null && _gameManager != null)
                _secondScoreManager = _gameManager.SecondPlayerScoreManager;
            if (_bowlingController == null) _bowlingController = FindAnyObjectByType<BowlingController>();
            if (_powerMeter == null) _powerMeter = FindAnyObjectByType<PowerMeter>();
            if (_scoreboard == null) _scoreboard = GetComponentInChildren<ScoreboardUI>(true);
            _frameInfo ??= FindText("FrameInfo");
            _totalScore ??= FindText("TotalScore");
            _keyHints ??= FindText("KeyHints");
            _powerPercent ??= FindText("PowerPercent");
            _banner ??= FindText("Banner");
            _gameOverScore ??= FindText("GameOverScore");
            _challengeProgress ??= FindText("ChallengeProgress");
            _powerFill ??= FindImage("PowerFill");
            _pausePanel ??= FindObject("PausePanel");
            _gameOverPanel ??= FindObject("GameOverPanel");
            _resumeButton ??= FindButton("ResumeButton");
            _restartButton ??= FindButton("RestartButton");
            _mainMenuButton ??= FindButton("MainMenuButton");
            _resultsButton ??= FindButton("ResultsButton");
            if (_gameManager != null && _gameManager.IsTwoPlayer)
            {
                ConfigureTwoPlayerHud();
            }

            if (_challengeProgress == null)
            {
                var sourceText = GetComponentInChildren<TMP_Text>(true);
                var labelObject = new GameObject("ChallengeProgress", typeof(RectTransform), typeof(TextMeshProUGUI));
                labelObject.transform.SetParent(transform, false);
                var rect = labelObject.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(700f, 440f);
                rect.sizeDelta = new Vector2(480f, 48f);
                _challengeProgress = labelObject.GetComponent<TextMeshProUGUI>();
                if (sourceText != null) _challengeProgress.font = sourceText.font;
                _challengeProgress.fontSize = 22f;
                _challengeProgress.color = new Color(.08f, .86f, 1f);
                _challengeProgress.alignment = TextAlignmentOptions.Right;
                _challengeProgress.gameObject.SetActive(false);
            }
        }

        private void ConfigureTwoPlayerHud()
        {
            if (_secondScoreboard == null)
            {
                _secondScoreboard = FindObject("ScoreboardPlayer2")?.GetComponent<ScoreboardUI>();
            }

            if (_secondScoreboard == null && _scoreboard != null)
            {
                var secondBoardObject = Instantiate(_scoreboard.gameObject, _scoreboard.transform.parent, false);
                secondBoardObject.name = "ScoreboardPlayer2";
                _secondScoreboard = secondBoardObject.GetComponent<ScoreboardUI>();
            }

            if (_scoreboard != null)
            {
                CompactScoreboard(_scoreboard, new Vector2(0f, 385f));
            }

            if (_secondScoreboard != null)
            {
                CompactScoreboard(_secondScoreboard, new Vector2(0f, 270f));
            }

            if (_frameInfo != null)
            {
                _frameInfo.rectTransform.anchoredPosition = new Vector2(0f, 500f);
            }

            _playerOneLabel = CreateHudLabel("PlayerOneLabel", new Vector2(0f, 445f));
            _playerOneLabel.rectTransform.anchoredPosition = new Vector2(0f, 445f);
            _playerTwoLabel = CreateHudLabel("PlayerTwoLabel", new Vector2(0f, 330f));
            _playerTwoLabel.rectTransform.anchoredPosition = new Vector2(0f, 330f);
            _secondTotalScore = CreateHudLabel("PlayerTwoTotal", new Vector2(710f, 460f));
            _secondTotalScore.rectTransform.anchoredPosition = new Vector2(710f, 460f);
            if (_totalScore != null)
            {
                _totalScore.rectTransform.anchoredPosition = new Vector2(-710f, 460f);
            }
        }

        private static void CompactScoreboard(ScoreboardUI scoreboard, Vector2 position)
        {
            var boardRect = scoreboard.GetComponent<RectTransform>();
            boardRect.anchoredPosition = position;
            boardRect.sizeDelta = new Vector2(1600f, 82f);

            foreach (var cell in scoreboard.GetComponentsInChildren<FrameCellUI>(true))
            {
                var cellRect = cell.GetComponent<RectTransform>();
                cellRect.sizeDelta = new Vector2(cellRect.sizeDelta.x, 66f);
                SetCompactLabel(cell.transform.Find("FrameNumber"), 18f, new Vector2(0f, 20f), new Vector2(130f, 24f));
                SetCompactLabel(cell.transform.Find("Roll1"), 20f, new Vector2(cell.transform.Find("Roll1").GetComponent<RectTransform>().anchoredPosition.x, 0f), new Vector2(42f, 24f));
                SetCompactLabel(cell.transform.Find("Roll2"), 20f, new Vector2(cell.transform.Find("Roll2").GetComponent<RectTransform>().anchoredPosition.x, 0f), new Vector2(42f, 24f));
                SetCompactLabel(cell.transform.Find("Roll3"), 20f, new Vector2(cell.transform.Find("Roll3") != null ? cell.transform.Find("Roll3").GetComponent<RectTransform>().anchoredPosition.x : 0f, 0f), new Vector2(42f, 24f));
                SetCompactLabel(cell.transform.Find("Cumulative"), 16f, new Vector2(0f, -20f), new Vector2(130f, 22f));
            }
        }

        private static void SetCompactLabel(Transform labelTransform, float fontSize, Vector2 position, Vector2 size)
        {
            if (labelTransform == null) return;
            var label = labelTransform.GetComponent<TMP_Text>();
            if (label == null) return;
            label.fontSize = fontSize;
            var rect = label.rectTransform;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private TMP_Text CreateHudLabel(string objectName, Vector2 position)
        {
            var existing = FindText(objectName);
            if (existing != null) return existing;

            var sourceText = GetComponentInChildren<TMP_Text>(true);
            var labelObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(transform, false);
            var rect = labelObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(420f, 32f);
            var label = labelObject.GetComponent<TextMeshProUGUI>();
            if (sourceText != null) label.font = sourceText.font;
            label.fontSize = 21f;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Center;
            return label;
        }

        private void UpdateScoreboards()
        {
            if (_gameManager == null)
            {
                _scoreboard?.Render(_scoreManager != null ? _scoreManager.CurrentSnapshot : null, 1);
                return;
            }

            if (_gameManager.IsTwoPlayer)
            {
                _scoreboard?.Render(_gameManager.FirstPlayerScore, _gameManager.FrameForPlayer(1));
                _secondScoreboard?.Render(_gameManager.SecondPlayerScore, _gameManager.FrameForPlayer(2));
                if (_totalScore != null)
                    _totalScore.text = $"P1 TOTAL: {_gameManager.FirstPlayerScore?.TotalSoFar ?? 0}";
                if (_secondTotalScore != null)
                    _secondTotalScore.text = $"P2 TOTAL: {_gameManager.SecondPlayerScore?.TotalSoFar ?? 0}";
                if (_playerOneLabel != null)
                    _playerOneLabel.text = _gameManager.ActivePlayerNumber == 1 ? "PLAYER 1  •  UP NEXT" : "PLAYER 1";
                if (_playerTwoLabel != null)
                    _playerTwoLabel.text = _gameManager.ActivePlayerNumber == 2 ? "PLAYER 2  •  UP NEXT" : "PLAYER 2";
                return;
            }

            var snapshot = _gameManager.CurrentScore;
            _scoreboard?.Render(snapshot, _gameManager.CurrentFrame);
            if (_totalScore != null && snapshot != null)
                _totalScore.text = $"TOTAL: {snapshot.TotalSoFar}";
        }

        private TMP_Text FindText(string objectName)
        {
            var texts = GetComponentsInChildren<TMP_Text>(true);
            foreach (var text in texts)
            {
                if (text.name == objectName) return text;
            }
            return null;
        }

        private Image FindImage(string objectName)
        {
            foreach (var image in GetComponentsInChildren<Image>(true))
                if (image.name == objectName) return image;
            return null;
        }

        private GameObject FindObject(string objectName)
        {
            foreach (var child in GetComponentsInChildren<Transform>(true))
                if (child.name == objectName) return child.gameObject;
            return null;
        }

        private Button FindButton(string objectName)
        {
            foreach (var button in GetComponentsInChildren<Button>(true))
                if (button.name == objectName) return button;
            return null;
        }
    }
}
