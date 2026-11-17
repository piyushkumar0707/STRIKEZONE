using StrikeZone.Core;
using StrikeZone.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace StrikeZone.Editor
{
    /// <summary>Installs the game-flow and score managers on the lane prototype.</summary>
    public static class GameFlowInstaller
    {
        [MenuItem("Strike Zone/Install Game Flow")]
        private static void Install()
        {
            var root = GameObject.Find("StrikeZonePrototype");
            if (root == null)
            {
                EditorUtility.DisplayDialog("Game Flow", "Build the lane prototype first.", "OK");
                return;
            }

            var managers = root.transform.Find("Managers");
            if (managers == null)
            {
                var managersObject = new GameObject("Managers");
                Undo.RegisterCreatedObjectUndo(managersObject, "Create bowling managers");
                managersObject.transform.SetParent(root.transform, false);
                managers = managersObject.transform;
            }

            var scoreManager = managers.GetComponent<BowlingScoreManager>();
            if (scoreManager == null)
            {
                scoreManager = Undo.AddComponent<BowlingScoreManager>(managers.gameObject);
            }

            var gameManager = root.GetComponent<BowlingGameManager>();
            if (gameManager == null)
            {
                gameManager = Undo.AddComponent<BowlingGameManager>(root);
            }

            var serializedManager = new SerializedObject(gameManager);
            serializedManager.FindProperty("_pinManager").objectReferenceValue = root.GetComponent<PinManager>();
            serializedManager.FindProperty("_bowlingController").objectReferenceValue = managers.GetComponent<BowlingController>();
            serializedManager.FindProperty("_scoreManager").objectReferenceValue = scoreManager;
            serializedManager.FindProperty("_ball").objectReferenceValue =
                root.transform.Find("Ball")?.GetComponent<BowlingBall>();
            serializedManager.FindProperty("_ballSpawnPoint").objectReferenceValue =
                root.transform.Find("BowlingSetup/BallSpawnPoint");
            serializedManager.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorUtility.DisplayDialog(
                "Game Flow Installed",
                "BowlingGameManager now connects each throw to pin settling, frame progression, and score updates. The current score is logged in the Console. Save the scene with Ctrl+S.",
                "OK");
        }
    }
}
