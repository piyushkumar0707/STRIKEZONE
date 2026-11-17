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
        [SerializeField] private BowlingController _bowlingController;
        [SerializeField] private PowerMeter _powerMeter;
        [SerializeField] private ScoreboardUI _scoreboard;
        [SerializeField] private TMP_Text _frameInfo;
        [SerializeField] private TMP_Text _totalScore;
        [SerializeField] private TMP_Text _keyHints;
        [SerializeField] private TMP_Text _powerPercent;
        [SerializeField] private TMP_Text _banner;
        [SerializeField] private TMP_Text _gameOverScore;
        [SerializeField] private Image _powerFill;
        [SerializeField] private GameObject _pausePanel;
        [SerializeField] private GameObject _gameOverPanel;
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _mainMenuButton;
        [SerializeField] private Button _resultsButton;
        private Coroutine _bannerRoutine;

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
                _gameManager.OnRollResolved += HandleRollResolved;
            }

            if (_scoreManager != null)
            {
                _scoreManager.OnScoreUpdated += HandleScoreUpdated;
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
        }

        private void OnDisable()
        {
            if (_gameManager != null)
            {
                _gameManager.OnStateChanged -= HandleStateChanged;
                _gameManager.OnPauseRequested -= TogglePause;
                _gameManager.OnGameCompleted -= HandleGameCompleted;
                _gameManager.OnRollResolved -= HandleRollResolved;
            }

            if (_scoreManager != null)
            {
                _scoreManager.OnScoreUpdated -= HandleScoreUpdated;
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
                _powerFill.color = value < 0.4f ? new Color(0.15f, 0.9f, 0.55f) :
                    value < 0.75f ? new Color(1f, 0.82f, 0.1f) : new Color(1f, 0.2f, 0.25f);
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
            BowlingSession.Clear();
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
                _frameInfo.text = $"FRAME {_gameManager.CurrentFrame}/10   ROLL {_gameManager.CurrentRoll}";
            }

            if (_keyHints != null)
            {
                switch (state)
                {
                    case BowlingGameState.WaitingForAim:
                        _keyHints.text = "A / D  AIM     SPACE  START POWER";
                        break;
                    case BowlingGameState.PowerMeterRunning:
                        _keyHints.text = "A / D  AIM     SPACE  LOCK POWER & THROW";
                        break;
                    case BowlingGameState.BallRolling:
                    case BowlingGameState.PinsSettling:
                        _keyHints.text = "ROLL IN PROGRESS";
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
                _scoreboard.Render(_scoreManager.CurrentSnapshot, _gameManager != null ? _gameManager.CurrentFrame : 1);
            }
        }

        private void HandleScoreUpdated(ScoreSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return;
            }

            if (_totalScore != null)
            {
                _totalScore.text = $"TOTAL: {snapshot.TotalSoFar}";
            }

            _scoreboard?.Render(snapshot, _gameManager != null ? _gameManager.CurrentFrame : 1);
            if (_gameOverScore != null && snapshot.IsGameComplete)
            {
                _gameOverScore.text = $"FINAL SCORE: {snapshot.TotalSoFar}";
            }
        }

        private void HandleGameCompleted()
        {
            _gameOverPanel?.SetActive(true);
            _gameOverScore?.SetText("FINAL SCORE: {0}", _scoreManager != null && _scoreManager.CurrentSnapshot != null
                ? _scoreManager.CurrentSnapshot.TotalSoFar
                : 0);
            _keyHints?.SetText("GAME COMPLETE");
        }

        private void HandleRollResolved(int frameNumber, int rollNumber, int pins, ScoreSnapshot snapshot)
        {
            if (snapshot == null || frameNumber < 1 || frameNumber > snapshot.Frames.Length)
            {
                return;
            }

            var frame = snapshot.Frames[frameNumber - 1];
            if (frame.IsStrike && rollNumber == 1)
            {
                ShowBanner("STRIKE!", new Color(1f, .83f, .08f));
            }
            else if (frame.IsSpare && rollNumber == 2)
            {
                ShowBanner("SPARE!", new Color(.08f, .86f, 1f));
            }
            else if (pins == 0)
            {
                ShowBanner("GUTTER", new Color(1f, .28f, .5f));
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
            _bannerRoutine = StartCoroutine(HideBannerAfterDelay());
        }

        private IEnumerator HideBannerAfterDelay()
        {
            yield return new WaitForSecondsRealtime(1.5f);
            if (_banner != null)
            {
                _banner.gameObject.SetActive(false);
            }

            _bannerRoutine = null;
        }

        private void ResolveReferences()
        {
            if (_gameManager == null) _gameManager = FindAnyObjectByType<BowlingGameManager>();
            if (_scoreManager == null) _scoreManager = FindAnyObjectByType<BowlingScoreManager>();
            if (_bowlingController == null) _bowlingController = FindAnyObjectByType<BowlingController>();
            if (_powerMeter == null) _powerMeter = FindAnyObjectByType<PowerMeter>();
            if (_scoreboard == null) _scoreboard = GetComponentInChildren<ScoreboardUI>(true);
            _frameInfo ??= FindText("FrameInfo");
            _totalScore ??= FindText("TotalScore");
            _keyHints ??= FindText("KeyHints");
            _powerPercent ??= FindText("PowerPercent");
            _banner ??= FindText("Banner");
            _gameOverScore ??= FindText("GameOverScore");
            _powerFill ??= FindImage("PowerFill");
            _pausePanel ??= FindObject("PausePanel");
            _gameOverPanel ??= FindObject("GameOverPanel");
            _resumeButton ??= FindButton("ResumeButton");
            _restartButton ??= FindButton("RestartButton");
            _mainMenuButton ??= FindButton("MainMenuButton");
            _resultsButton ??= FindButton("ResultsButton");
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
