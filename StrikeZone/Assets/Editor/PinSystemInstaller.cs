using StrikeZone.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace StrikeZone.Editor
{
    /// <summary>Wires the pin scripts into the generated lane prototype scene.</summary>
    public static class PinSystemInstaller
    {
        [MenuItem("Strike Zone/Install Pin System")]
        private static void Install()
        {
            var root = GameObject.Find("StrikeZonePrototype");
            if (root == null)
            {
                EditorUtility.DisplayDialog("Pin System", "Build the lane prototype first.", "OK");
                return;
            }

            var pinObjects = root.GetComponentsInChildren<Transform>(true);
            var pinCount = 0;
            foreach (var child in pinObjects)
            {
                if (!child.name.StartsWith("Pin_"))
                {
                    continue;
                }

                if (child.GetComponent<Collider>() == null)
                {
                    continue;
                }

                if (child.GetComponent<BowlingPin>() == null)
                {
                    Undo.AddComponent<BowlingPin>(child.gameObject);
                }

                pinCount++;
            }

            var pinManager = root.GetComponent<PinManager>();
            if (pinManager == null)
            {
                pinManager = Undo.AddComponent<PinManager>(root);
            }

            var pitTrigger = root.transform.Find("Environment/PitTrigger");
            if (pitTrigger != null)
            {
                var pitReporter = pitTrigger.GetComponent<BowlingPitTrigger>();
                if (pitReporter == null)
                {
                    pitReporter = Undo.AddComponent<BowlingPitTrigger>(pitTrigger.gameObject);
                }

                var serializedReporter = new SerializedObject(pitReporter);
                serializedReporter.FindProperty("_pinManager").objectReferenceValue = pinManager;
                serializedReporter.ApplyModifiedProperties();
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorUtility.DisplayDialog(
                "Pin System Installed",
                $"Added pin behavior to {pinCount} pins, installed PinManager, and connected the pit trigger. Save the scene with Ctrl+S.",
                "OK");
        }
    }
}
