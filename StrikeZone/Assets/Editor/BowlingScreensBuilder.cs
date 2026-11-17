using System.Collections.Generic;
using System.Linq;
using StrikeZone.Core;
using StrikeZone.Gameplay;
using StrikeZone.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace StrikeZone.Editor
{
    /// <summary>Builds lightweight uGUI screens and adds all playable scenes to Build Settings.</summary>
    public static class BowlingScreensBuilder
    {
        private static readonly Color Navy = new Color(.025f, .055f, .105f, .96f);
        private static readonly Color Cyan = new Color(.08f, .78f, .92f);
        private static readonly Color White = new Color(.92f, .96f, 1f);
        private static TMP_FontAsset _font;

        [MenuItem("Strike Zone/Build Remaining Screens")]
        public static void BuildAll()
        {
            _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            if (_font == null)
            {
                if (Application.isBatchMode) Debug.LogError("Import TMP Essential Resources before building the screens.");
                else EditorUtility.DisplayDialog("TextMesh Pro resources missing", "Use Window > TextMeshPro > Import TMP Essential Resources, then run this menu again.", "OK");
                return;
            }

            BuildGameplayHud();
            BuildMenu();
            BuildResults();
            ConfigureBuildScenes();
            EditorSceneManager.OpenScene("Assets/Scenes/BowlingMenu.unity", OpenSceneMode.Single);
            AssetDatabase.SaveAssets();
            if (Application.isBatchMode) Debug.Log("Strike Zone gameplay HUD, menu, results screens, and Build Settings are ready.");
            else EditorUtility.DisplayDialog("Strike Zone screens ready", "Gameplay HUD, main menu, and results screens were built. The three scenes are now in Build Settings.", "OK");
        }

        [MenuItem("Strike Zone/Build Windows Player")]
        private static void BuildWindowsPlayer()
        {
            const string outputDirectory = "Builds/Windows";
            System.IO.Directory.CreateDirectory(outputDirectory);
            var options = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
                locationPathName = outputDirectory + "/StrikeZone.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result == BuildResult.Succeeded)
            {
                var message = $"Windows build created at {options.locationPathName} ({report.summary.totalSize / (1024f * 1024f):0.0} MB).";
                Debug.Log(message);
                if (!Application.isBatchMode) EditorUtility.DisplayDialog("Build complete", message, "OK");
            }
            else
            {
                var message = $"Windows build failed: {report.summary.result}. See the Console for details.";
                Debug.LogError(message);
                if (!Application.isBatchMode) EditorUtility.DisplayDialog("Build failed", message, "OK");
            }
        }

        private static void BuildGameplayHud()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/BowlingGame.unity", OpenSceneMode.Single);
            var existing = GameObject.Find("StrikeZoneHUD");
            if (existing != null) Object.DestroyImmediate(existing);
            var canvas = CreateCanvas("StrikeZoneHUD");
            var manager = canvas.AddComponent<BowlingUIManager>();
            InstallCameraAndAudio();

            Label(canvas.transform, "FrameInfo", "FRAME 1 / 10   ROLL 1", 29, new Vector2(0, 442), new Vector2(650, 54), TextAlignmentOptions.Center);
            Label(canvas.transform, "TotalScore", "TOTAL: 0", 34, new Vector2(-710, 440), new Vector2(300, 55), TextAlignmentOptions.Left);
            Label(canvas.transform, "KeyHints", "A / D  AIM     Q / E  HOOK     SPACE  START POWER", 24, new Vector2(0, -465), new Vector2(1200, 48), TextAlignmentOptions.Center);
            Label(canvas.transform, "ChallengeProgress", "STRIKE CHALLENGE   0 / 3 STRIKES", 22, new Vector2(700, 440), new Vector2(480, 48), TextAlignmentOptions.Right);
            var track = Panel(canvas.transform, "PowerTrack", new Vector2(0, -395), new Vector2(660, 28), new Color(.08f, .15f, .23f, .95f));
            var fill = Panel(track.transform, "PowerFill", new Vector2(5, 0), new Vector2(650, 20), Cyan);
            var fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0, .5f); fillRect.anchorMax = new Vector2(0, .5f); fillRect.pivot = new Vector2(0, .5f);
            fillRect.anchoredPosition = new Vector2(5, 0); fillRect.sizeDelta = new Vector2(650, 20);
            var fillImage = fill.GetComponent<Image>(); fillImage.type = Image.Type.Filled; fillImage.fillMethod = Image.FillMethod.Horizontal; fillImage.fillAmount = 0;
            Label(canvas.transform, "PowerPercent", "0%", 24, new Vector2(370, -395), new Vector2(100, 40), TextAlignmentOptions.Left);

            var board = BuildScoreboard(canvas.transform, "Scoreboard", new Vector2(0, 340));
            Label(canvas.transform, "Banner", "", 38, new Vector2(0, 20), new Vector2(900, 70), TextAlignmentOptions.Center);

            var pause = Panel(canvas.transform, "PausePanel", Vector2.zero, new Vector2(560, 460), Navy);
            Label(pause.transform, "PauseTitle", "PAUSED", 42, new Vector2(0, 150), new Vector2(500, 60), TextAlignmentOptions.Center);
            var resume = MakeButton(pause.transform, "ResumeButton", "RESUME", new Vector2(0, 55));
            var restart = MakeButton(pause.transform, "RestartButton", "RESTART", new Vector2(0, -35));
            var menu = MakeButton(pause.transform, "MainMenuButton", "MAIN MENU", new Vector2(0, -125));

            var over = Panel(canvas.transform, "GameOverPanel", Vector2.zero, new Vector2(620, 500), Navy);
            Label(over.transform, "GameOverTitle", "LANE COMPLETE", 38, new Vector2(0, 170), new Vector2(560, 60), TextAlignmentOptions.Center);
            Label(over.transform, "GameOverScore", "FINAL SCORE: 0", 34, new Vector2(0, 85), new Vector2(560, 60), TextAlignmentOptions.Center);
            var results = MakeButton(over.transform, "ResultsButton", "VIEW RESULTS", new Vector2(0, -40));
            // Attach references explicitly so the HUD does not depend on scene-wide searches.
            var so = new SerializedObject(manager);
            so.FindProperty("_scoreboard").objectReferenceValue = board.GetComponent<ScoreboardUI>();
            so.FindProperty("_challengeProgress").objectReferenceValue = canvas.transform.Find("ChallengeProgress").GetComponent<TMP_Text>();
            so.FindProperty("_powerFill").objectReferenceValue = fillImage;
            so.FindProperty("_pausePanel").objectReferenceValue = pause;
            so.FindProperty("_gameOverPanel").objectReferenceValue = over;
            so.FindProperty("_resumeButton").objectReferenceValue = resume;
            so.FindProperty("_restartButton").objectReferenceValue = restart;
            so.FindProperty("_mainMenuButton").objectReferenceValue = menu;
            so.FindProperty("_resultsButton").objectReferenceValue = results;
            so.ApplyModifiedProperties();
            pause.SetActive(false); over.SetActive(false);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        }

        private static void BuildMenu()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera(); var canvas = CreateCanvas("BowlingMenuUI"); canvas.AddComponent<BowlingMenuManager>();
            Panel(canvas.transform, "MenuBackground", Vector2.zero, new Vector2(1920, 1080), new Color(.018f, .04f, .075f));
            Label(canvas.transform, "Title", "STRIKE ZONE", 78, new Vector2(0, 275), new Vector2(1000, 120), TextAlignmentOptions.Center);
            Label(canvas.transform, "Tagline", "A precision bowling challenge", 28, new Vector2(0, 200), new Vector2(800, 50), TextAlignmentOptions.Center);
            MakeButton(canvas.transform, "PlayButton", "CLASSIC GAME", new Vector2(0, 140));
            MakeButton(canvas.transform, "ChallengeButton", "STRIKE CHALLENGE", new Vector2(0, 52));
            MakeButton(canvas.transform, "TwoPlayerButton", "2 PLAYER LOCAL", new Vector2(0, -36));
            MakeButton(canvas.transform, "RecordsButton", "LOCAL RECORDS", new Vector2(0, -124));
            MakeButton(canvas.transform, "InstructionsButton", "HOW TO PLAY", new Vector2(0, -212));
            MakeButton(canvas.transform, "SettingsButton", "SETTINGS", new Vector2(0, -300));
            MakeButton(canvas.transform, "ExitButton", "EXIT", new Vector2(0, -388));
            var records = Panel(canvas.transform, "RecordsPanel", Vector2.zero, new Vector2(760, 510), new Color(.025f, .055f, .105f, .98f));
            records.AddComponent<Outline>().effectColor = new Color(.08f, .78f, .92f, .9f);
            Label(records.transform, "RecordsTitle", "LOCAL RECORDS", 42, new Vector2(0, 185), new Vector2(680, 60), TextAlignmentOptions.Center);
            Panel(records.transform, "ClassicRecordCard", new Vector2(0, 88), new Vector2(660, 58), new Color(.055f, .095f, .145f, 1));
            Label(records.transform, "RecordsClassic", "CLASSIC HIGH SCORE    0 / 300", 27, new Vector2(0, 88), new Vector2(640, 52), TextAlignmentOptions.Center);
            Panel(records.transform, "ChallengeRecordCard", new Vector2(0, 12), new Vector2(660, 58), new Color(.055f, .095f, .145f, 1));
            Label(records.transform, "RecordsChallenge", "CHALLENGE HIGH SCORE    0 / 300", 27, new Vector2(0, 12), new Vector2(640, 52), TextAlignmentOptions.Center);
            Panel(records.transform, "LocalMatchRecordCard", new Vector2(0, -64), new Vector2(660, 58), new Color(.055f, .095f, .145f, 1));
            Label(records.transform, "RecordsMatches", "LOCAL MATCHES    P1 0  •  P2 0  •  DRAWS 0", 25, new Vector2(0, -64), new Vector2(640, 52), TextAlignmentOptions.Center);
            MakeButton(records.transform, "CloseRecordsButton", "BACK TO MENU", new Vector2(0, -170));
            records.SetActive(false);
            var instructions = Panel(canvas.transform, "InstructionsPanel", Vector2.zero, new Vector2(820, 430), Navy);
            Label(instructions.transform, "InstructionsText", "A / D or LEFT / RIGHT  —  Aim\nQ / E  —  Add left or right hook to your throw\nSPACE  —  Start the meter, then lock near 70% and throw\nESC  —  Pause\n\nStrike Challenge: earn 3 strikes in 10 frames.\n2 Player Local: alternate turns by frame; high score wins.", 27, Vector2.zero, new Vector2(740, 290), TextAlignmentOptions.Center);
            MakeButton(instructions.transform, "CloseInstructionsButton", "GOT IT", new Vector2(0, -165));
            instructions.SetActive(false);
            var settings = Panel(canvas.transform, "SettingsPanel", Vector2.zero, new Vector2(760, 380), Navy);
            Label(settings.transform, "SettingsTitle", "SETTINGS", 38, new Vector2(0, 112), new Vector2(680, 60), TextAlignmentOptions.Center);
            Label(settings.transform, "VolumeLabel", "MASTER VOLUME", 25, new Vector2(-235, 20), new Vector2(250, 44), TextAlignmentOptions.Left);
            Label(settings.transform, "VolumeValue", "100%", 25, new Vector2(285, 20), new Vector2(100, 44), TextAlignmentOptions.Right);
            MakeVolumeSlider(settings.transform);
            MakeButton(settings.transform, "CloseSettingsButton", "BACK", new Vector2(0, -115));
            settings.SetActive(false);
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/BowlingMenu.unity");
        }

        private static void BuildResults()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera(); var canvas = CreateCanvas("BowlingResultsUI"); canvas.AddComponent<BowlingResultsManager>();
            Panel(canvas.transform, "ResultsBackground", Vector2.zero, new Vector2(1920, 1080), new Color(.018f, .04f, .075f));
            Label(canvas.transform, "ResultsTitle", "YOUR GAME", 56, new Vector2(0, 260), new Vector2(900, 80), TextAlignmentOptions.Center);
            Label(canvas.transform, "FinalScore", "FINAL SCORE\n0 / 300", 48, new Vector2(0, 170), new Vector2(800, 140), TextAlignmentOptions.Center);
            Label(canvas.transform, "GameStats", "STRIKES 0   SPARES 0   OPENS 0   BEST FRAME 0", 21, new Vector2(0, 40), new Vector2(1400, 78), TextAlignmentOptions.Center);
            BuildScoreboard(canvas.transform, "ResultsScoreboard", new Vector2(0, -100));
            BuildScoreboard(canvas.transform, "ResultsScoreboardPlayer2", new Vector2(0, -255));
            MakeButton(canvas.transform, "PlayAgainButton", "PLAY AGAIN", new Vector2(0, -380));
            MakeButton(canvas.transform, "ResultsMenuButton", "MAIN MENU", new Vector2(0, -470));
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/BowlingResults.unity");
        }

        private static void InstallCameraAndAudio()
        {
            var prototype = GameObject.Find("StrikeZonePrototype");
            var camera = Camera.main;
            if (prototype == null || camera == null) return;

            var cameraRig = camera.GetComponent<CameraRig>();
            if (cameraRig == null) cameraRig = camera.gameObject.AddComponent<CameraRig>();
            var cameraSerialized = new SerializedObject(cameraRig);
            cameraSerialized.FindProperty("_ball").objectReferenceValue = prototype.transform.Find("Ball");
            cameraSerialized.FindProperty("_gameManager").objectReferenceValue = prototype.GetComponent<BowlingGameManager>();
            cameraSerialized.ApplyModifiedProperties();

            var managerRoot = prototype.transform.Find("Managers");
            if (managerRoot == null) return;
            var source = managerRoot.GetComponent<AudioSource>();
            if (source == null) source = managerRoot.gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            var audio = managerRoot.GetComponent<BowlingAudioManager>();
            if (audio == null) audio = managerRoot.gameObject.AddComponent<BowlingAudioManager>();
            var audioSerialized = new SerializedObject(audio);
            audioSerialized.FindProperty("_gameManager").objectReferenceValue = prototype.GetComponent<BowlingGameManager>();
            audioSerialized.FindProperty("_bowlingController").objectReferenceValue = managerRoot.GetComponent<BowlingController>();
            audioSerialized.FindProperty("_audioSource").objectReferenceValue = source;
            audioSerialized.ApplyModifiedProperties();
        }

        private static GameObject BuildScoreboard(Transform parent, string name, Vector2 position)
        {
            var board = Panel(parent, name, position, new Vector2(1600, 132), new Color(.025f, .05f, .09f, .94f));
            board.AddComponent<ScoreboardUI>();
            for (var i = 0; i < 10; i++)
            {
                var width = i == 9 ? 154f : 151f;
                var cell = Panel(board.transform, $"FrameCell{i + 1}", new Vector2(-720 + i * 160, -3), new Vector2(width, 112), new Color(.055f, .095f, .145f, 1));
                cell.AddComponent<FrameCellUI>();
                cell.AddComponent<Outline>().effectColor = new Color(.05f, .85f, 1f, .95f);
                var r1x = i == 9 ? -42f : 19f; var r2x = i == 9 ? 2f : 58f;
                Label(cell.transform, "FrameNumber", (i + 1).ToString("00"), 24, new Vector2(0, 39), new Vector2(130, 28), TextAlignmentOptions.Center);
                Label(cell.transform, "Roll1", "", 24, new Vector2(r1x, 12), new Vector2(42, 30), TextAlignmentOptions.Center);
                Label(cell.transform, "Roll2", "", 24, new Vector2(r2x, 12), new Vector2(42, 30), TextAlignmentOptions.Center);
                if (i == 9) Label(cell.transform, "Roll3", "", 24, new Vector2(47, 12), new Vector2(42, 30), TextAlignmentOptions.Center);
                Label(cell.transform, "Cumulative", "", 24, new Vector2(0, -26), new Vector2(130, 28), TextAlignmentOptions.Center);
            }

            return board;
        }

        private static GameObject CreateCanvas(string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
            if (Object.FindAnyObjectByType<EventSystem>() == null)
            {
                var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
            return go;
        }

        private static void CreateCamera()
        {
            var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camera.tag = "MainCamera"; camera.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
            camera.GetComponent<Camera>().backgroundColor = new Color(.018f, .04f, .075f);
        }

        private static GameObject Panel(Transform parent, string name, Vector2 position, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.pivot = new Vector2(.5f, .5f); rect.anchoredPosition = position; rect.sizeDelta = size;
            go.GetComponent<Image>().color = color; return go;
        }

        private static TMP_Text Label(Transform parent, string name, string value, int size, Vector2 position, Vector2 dimensions, TextAlignmentOptions alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)); go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.anchoredPosition = position; rect.sizeDelta = dimensions;
            var text = go.GetComponent<TextMeshProUGUI>(); text.font = _font; text.text = value; text.fontSize = size;
            text.color = White; text.alignment = alignment; text.textWrappingMode = TextWrappingModes.Normal;
            return text;
        }

        private static Button MakeButton(Transform parent, string name, string label, Vector2 position)
        {
            var go = Panel(parent, name, position, new Vector2(350, 72), new Color(.03f, .37f, .63f, 1));
            var button = go.AddComponent<Button>(); var colors = button.colors;
            colors.highlightedColor = Cyan; colors.pressedColor = new Color(.03f, .22f, .4f); button.colors = colors;
            Label(go.transform, name + "Label", label, 25, Vector2.zero, new Vector2(330, 60), TextAlignmentOptions.Center);
            return button;
        }

        private static Slider MakeVolumeSlider(Transform parent)
        {
            var root = Panel(parent, "VolumeSlider", new Vector2(0, 20), new Vector2(510, 38), new Color(.08f, .15f, .23f, 1f));
            var slider = root.AddComponent<Slider>();
            slider.minValue = 0f; slider.maxValue = 1f; slider.value = 1f; slider.direction = Slider.Direction.LeftToRight;
            var fillArea = new GameObject("FillArea", typeof(RectTransform)); fillArea.transform.SetParent(root.transform, false);
            var fillAreaRect = fillArea.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0, .25f); fillAreaRect.anchorMax = new Vector2(1, .75f);
            fillAreaRect.offsetMin = new Vector2(16, 0); fillAreaRect.offsetMax = new Vector2(-16, 0);
            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image)); fill.transform.SetParent(fillArea.transform, false);
            var fillRect = fill.GetComponent<RectTransform>(); fillRect.anchorMin = Vector2.zero; fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero; fillRect.offsetMax = Vector2.zero; fill.GetComponent<Image>().color = Cyan;
            var handleArea = new GameObject("HandleSlideArea", typeof(RectTransform)); handleArea.transform.SetParent(root.transform, false);
            var handleAreaRect = handleArea.GetComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero; handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = new Vector2(12, 0); handleAreaRect.offsetMax = new Vector2(-12, 0);
            var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image)); handle.transform.SetParent(handleArea.transform, false);
            var handleRect = handle.GetComponent<RectTransform>(); handleRect.sizeDelta = new Vector2(26, 34);
            handle.GetComponent<Image>().color = White;
            slider.fillRect = fillRect; slider.handleRect = handleRect; slider.targetGraphic = handle.GetComponent<Image>();
            return slider;
        }

        private static void ConfigureBuildScenes()
        {
            var paths = new[] { "Assets/Scenes/BowlingMenu.unity", "Assets/Scenes/BowlingGame.unity", "Assets/Scenes/BowlingResults.unity" };
            var scenes = new List<EditorBuildSettingsScene>();
            foreach (var path in paths) scenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
