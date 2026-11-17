using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using StrikeZone.Core;
using UnityEngine.UI;

namespace StrikeZone.UI
{
    public sealed class BowlingMenuManager : MonoBehaviour
    {
        private void Start()
        {
            EnsureModeButtons();
            EnsureRecordsScreen();
            Bind("PlayButton", StartClassicGame);
            Bind("ChallengeButton", StartStrikeChallenge);
            Bind("TwoPlayerButton", StartTwoPlayerGame);
            Bind("RecordsButton", ShowRecords);
            Bind("CloseRecordsButton", () => SetVisible("RecordsPanel", false));
            Bind("InstructionsButton", () => SetVisible("InstructionsPanel", true));
            Bind("CloseInstructionsButton", () => SetVisible("InstructionsPanel", false));
            Bind("SettingsButton", () => SetVisible("SettingsPanel", true));
            Bind("CloseSettingsButton", () => SetVisible("SettingsPanel", false));
            Bind("ExitButton", () => Application.Quit());

            foreach (var text in FindObjectsByType<TMP_Text>(FindObjectsInactive.Include))
            {
                if (text.name == "InstructionsText")
                {
                    text.text = "A / D or LEFT / RIGHT — Aim\nQ / E — Add left or right hook\nSPACE — Start the meter, then lock near 70% and throw\nESC — Pause\n\nStrike Challenge: earn 3 strikes in 10 frames.\n2 Player Local: alternate turns by frame; high score wins.\nShot rank uses pins hit, power timing, and aim.";
                }
            }

            var slider = FindSlider("VolumeSlider");
            if (slider != null)
            {
                slider.SetValueWithoutNotify(PlayerPrefs.GetFloat("StrikeZone.MasterVolume", 1f));
                slider.onValueChanged.AddListener(SetMasterVolume);
                SetMasterVolume(slider.value);
            }
        }

        private void StartClassicGame()
        {
            BowlingSession.BeginGame(false);
            SceneManager.LoadScene("BowlingGame");
        }

        private void StartStrikeChallenge()
        {
            BowlingSession.BeginGame(true);
            SceneManager.LoadScene("BowlingGame");
        }

        private void StartTwoPlayerGame()
        {
            BowlingSession.BeginTwoPlayerGame();
            SceneManager.LoadScene("BowlingGame");
        }

        private void EnsureModeButtons()
        {
            var buttons = FindObjectsByType<Button>(FindObjectsInactive.Include);
            Button challengeButton = null;
            Button twoPlayerButton = null;
            foreach (var button in buttons)
            {
                if (button.name == "ChallengeButton") challengeButton = button;
                else if (button.name == "PlayButton") SetButton(button, "CLASSIC GAME", new Vector2(0f, 140f));
                else if (button.name == "TwoPlayerButton") twoPlayerButton = button;
                else if (button.name == "RecordsButton") SetButton(button, "LOCAL RECORDS", new Vector2(0f, -124f));
                else if (button.name == "InstructionsButton") SetButton(button, "HOW TO PLAY", new Vector2(0f, -212f));
                else if (button.name == "SettingsButton") SetButton(button, "SETTINGS", new Vector2(0f, -300f));
                else if (button.name == "ExitButton") SetButton(button, "EXIT", new Vector2(0f, -388f));
            }

            if (challengeButton == null)
            {
                var buttonObject = new GameObject("ChallengeButton", typeof(RectTransform), typeof(Image), typeof(Button));
                buttonObject.transform.SetParent(transform, false);
                challengeButton = buttonObject.GetComponent<Button>();
                challengeButton.targetGraphic = buttonObject.GetComponent<Image>();
            }

            SetButton(challengeButton, "STRIKE CHALLENGE", new Vector2(0f, 52f));
            var colors = challengeButton.colors;
            colors.normalColor = new Color(.64f, .32f, .07f, 1f);
            colors.highlightedColor = new Color(1f, .62f, .16f, 1f);
            colors.pressedColor = new Color(.42f, .2f, .04f, 1f);
            challengeButton.colors = colors;

            if (twoPlayerButton == null)
            {
                var buttonObject = new GameObject("TwoPlayerButton", typeof(RectTransform), typeof(Image), typeof(Button));
                buttonObject.transform.SetParent(transform, false);
                twoPlayerButton = buttonObject.GetComponent<Button>();
                twoPlayerButton.targetGraphic = buttonObject.GetComponent<Image>();
            }

            SetButton(twoPlayerButton, "2 PLAYER LOCAL", new Vector2(0f, -36f));
            var twoPlayerColors = twoPlayerButton.colors;
            twoPlayerColors.normalColor = new Color(.1f, .38f, .48f, 1f);
            twoPlayerColors.highlightedColor = new Color(.12f, .7f, .78f, 1f);
            twoPlayerButton.colors = twoPlayerColors;

            EnsureMenuButton("RecordsButton", "LOCAL RECORDS", new Vector2(0f, -124f));
        }

        private void EnsureRecordsScreen()
        {
            if (FindNamedTransform("RecordsPanel") != null) return;

            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;
            var panelObject = new GameObject("RecordsPanel", typeof(RectTransform), typeof(Image), typeof(Outline));
            panelObject.transform.SetParent(canvas.transform, false);
            var panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(.5f, .5f);
            panelRect.sizeDelta = new Vector2(760f, 510f);
            panelObject.GetComponent<Image>().color = new Color(.025f, .055f, .105f, .98f);
            panelObject.GetComponent<Outline>().effectColor = new Color(.08f, .78f, .92f, .9f);

            CreateRecordsLabel(panelObject.transform, "RecordsTitle", "LOCAL RECORDS", 42, new Vector2(0f, 185f), new Vector2(680f, 60f), TextAlignmentOptions.Center);
            CreateRecordsLabel(panelObject.transform, "RecordsClassic", "CLASSIC HIGH SCORE    0 / 300", 27, new Vector2(0f, 88f), new Vector2(660f, 54f), TextAlignmentOptions.Center);
            CreateRecordsLabel(panelObject.transform, "RecordsChallenge", "CHALLENGE HIGH SCORE    0 / 300", 27, new Vector2(0f, 12f), new Vector2(660f, 54f), TextAlignmentOptions.Center);
            CreateRecordsLabel(panelObject.transform, "RecordsMatches", "LOCAL MATCHES    P1 0  •  P2 0  •  DRAWS 0", 25, new Vector2(0f, -64f), new Vector2(680f, 54f), TextAlignmentOptions.Center);

            var back = new GameObject("CloseRecordsButton", typeof(RectTransform), typeof(Image), typeof(Button));
            back.transform.SetParent(panelObject.transform, false);
            var backRect = back.GetComponent<RectTransform>();
            backRect.anchorMin = backRect.anchorMax = new Vector2(.5f, .5f);
            backRect.anchoredPosition = new Vector2(0f, -170f);
            backRect.sizeDelta = new Vector2(260f, 60f);
            back.GetComponent<Image>().color = new Color(.03f, .37f, .63f, 1f);
            var backButton = back.GetComponent<Button>();
            backButton.targetGraphic = back.GetComponent<Image>();
            CreateRecordsLabel(back.transform, "CloseRecordsLabel", "BACK TO MENU", 23, Vector2.zero, new Vector2(250f, 56f), TextAlignmentOptions.Center);
            panelObject.SetActive(false);
        }

        private void ShowRecords()
        {
            SetRecordText("RecordsClassic", $"CLASSIC HIGH SCORE    {BowlingRecords.ClassicBestScore} / 300");
            SetRecordText("RecordsChallenge", $"CHALLENGE HIGH SCORE    {BowlingRecords.ChallengeBestScore} / 300");
            SetRecordText("RecordsMatches", $"LOCAL MATCHES    P1 {BowlingRecords.PlayerOneWins}  •  P2 {BowlingRecords.PlayerTwoWins}  •  DRAWS {BowlingRecords.Draws}");
            SetVisible("RecordsPanel", true);
        }

        private void EnsureMenuButton(string name, string label, Vector2 position)
        {
            foreach (var button in FindObjectsByType<Button>(FindObjectsInactive.Include))
                if (button.name == name) return;

            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(transform, false);
            var createdButton = buttonObject.GetComponent<Button>();
            var graphic = buttonObject.GetComponent<Image>();
            graphic.color = new Color(.03f, .37f, .63f, 1f);
            createdButton.targetGraphic = graphic;
            var colors = createdButton.colors;
            colors.normalColor = new Color(.03f, .37f, .63f, 1f);
            colors.highlightedColor = new Color(.08f, .78f, .92f, 1f);
            colors.pressedColor = new Color(.03f, .22f, .4f, 1f);
            createdButton.colors = colors;
            SetButton(createdButton, label, position);
        }

        private static TMP_Text CreateRecordsLabel(Transform parent, string name, string value, int size, Vector2 position, Vector2 dimensions, TextAlignmentOptions alignment)
        {
            var labelObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(parent, false);
            var rect = labelObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions;
            var text = labelObject.GetComponent<TextMeshProUGUI>();
            var source = FindAnyObjectByType<TMP_Text>();
            if (source != null) text.font = source.font;
            text.text = value;
            text.fontSize = size;
            text.color = Color.white;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.Normal;
            return text;
        }

        private Transform FindNamedTransform(string objectName)
        {
            foreach (var item in FindObjectsByType<Transform>(FindObjectsInactive.Include))
                if (item.name == objectName) return item;
            return null;
        }

        private void SetRecordText(string objectName, string value)
        {
            foreach (var text in FindObjectsByType<TMP_Text>(FindObjectsInactive.Include))
                if (text.name == objectName) text.text = value;
        }

        private void SetButton(Button button, string label, Vector2 position)
        {
            if (button.targetGraphic == null) button.targetGraphic = button.GetComponent<Image>();
            var rect = button.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(350f, 72f);
            var text = button.GetComponentInChildren<TMP_Text>(true);
            if (text == null)
            {
                var source = GetComponentInChildren<TMP_Text>(true);
                var labelObject = new GameObject("ChallengeButtonLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
                labelObject.transform.SetParent(button.transform, false);
                var textRect = labelObject.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2(10f, 4f);
                textRect.offsetMax = new Vector2(-10f, -4f);
                text = labelObject.GetComponent<TextMeshProUGUI>();
                if (source != null) text.font = source.font;
                text.color = Color.white;
                text.alignment = TextAlignmentOptions.Center;
            }

            text.text = label;
            text.fontSize = 25f;
        }

        private void Bind(string buttonName, UnityEngine.Events.UnityAction action)
        {
            foreach (var button in FindObjectsByType<Button>(FindObjectsInactive.Include))
                if (button.name == buttonName) button.onClick.AddListener(action);
        }

        private void SetVisible(string objectName, bool visible)
        {
            var objects = FindObjectsByType<Transform>(FindObjectsInactive.Include);
            foreach (var item in objects)
                if (item.name == objectName) item.gameObject.SetActive(visible);
        }

        private Slider FindSlider(string objectName)
        {
            foreach (var slider in FindObjectsByType<Slider>(FindObjectsInactive.Include))
                if (slider.name == objectName) return slider;
            return null;
        }

        private void SetMasterVolume(float volume)
        {
            AudioListener.volume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat("StrikeZone.MasterVolume", AudioListener.volume);
            PlayerPrefs.Save();
            foreach (var label in FindObjectsByType<TMP_Text>(FindObjectsInactive.Include))
                if (label.name == "VolumeValue") label.text = $"{Mathf.RoundToInt(AudioListener.volume * 100f)}%";
        }
    }
}
