using StrikeZone.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace StrikeZone.Editor
{
    /// <summary>Attaches and connects the throw system to the generated lane scene.</summary>
    public static class ThrowSystemInstaller
    {
        private const string InputActionsPath = "Assets/Input/StrikeZoneControls.inputactions";

        [MenuItem("Strike Zone/Install Throw System")]
        private static void Install()
        {
            var root = GameObject.Find("StrikeZonePrototype");
            if (root == null)
            {
                EditorUtility.DisplayDialog("Throw System", "Build the lane prototype first.", "OK");
                return;
            }

            AssetDatabase.Refresh();
            var inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            if (inputActions == null)
            {
                EditorUtility.DisplayDialog("Throw System", "Unity has not imported StrikeZoneControls.inputactions yet. Wait a moment and try again.", "OK");
                return;
            }

            var ballObject = root.transform.Find("Ball")?.gameObject;
            var aimObject = root.transform.Find("BowlingSetup/AimDirection")?.gameObject;
            if (ballObject == null || aimObject == null)
            {
                EditorUtility.DisplayDialog("Throw System", "The Ball or AimDirection object is missing from the lane prototype.", "OK");
                return;
            }

            var ball = ballObject.GetComponent<BowlingBall>();
            if (ball == null)
            {
                ball = Undo.AddComponent<BowlingBall>(ballObject);
            }

            var aim = aimObject.GetComponent<AimController>();
            if (aim == null)
            {
                aim = Undo.AddComponent<AimController>(aimObject);
            }

            var line = aimObject.GetComponent<LineRenderer>();
            if (line == null)
            {
                line = Undo.AddComponent<LineRenderer>(aimObject);
            }

            ConfigureAimLine(line);

            var managers = root.transform.Find("Managers");
            if (managers == null)
            {
                var managersObject = new GameObject("Managers");
                Undo.RegisterCreatedObjectUndo(managersObject, "Create bowling managers");
                managersObject.transform.SetParent(root.transform, false);
                managers = managersObject.transform;
            }

            var powerMeter = managers.GetComponent<PowerMeter>();
            if (powerMeter == null)
            {
                powerMeter = Undo.AddComponent<PowerMeter>(managers.gameObject);
            }

            var controller = managers.GetComponent<BowlingController>();
            if (controller == null)
            {
                controller = Undo.AddComponent<BowlingController>(managers.gameObject);
            }

            var serializedController = new SerializedObject(controller);
            serializedController.FindProperty("_inputActionsAsset").objectReferenceValue = inputActions;
            serializedController.FindProperty("_aimController").objectReferenceValue = aim;
            serializedController.FindProperty("_powerMeter").objectReferenceValue = powerMeter;
            serializedController.FindProperty("_ball").objectReferenceValue = ball;
            serializedController.FindProperty("_pinManager").objectReferenceValue = root.GetComponent<PinManager>();
            serializedController.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorUtility.DisplayDialog(
                "Throw System Installed",
                "A/D or Left/Right aims; Space starts and locks the power meter; Escape raises the pause event. Save the scene with Ctrl+S.",
                "OK");
        }

        private static void ConfigureAimLine(LineRenderer line)
        {
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.startWidth = 0.025f;
            line.endWidth = 0.015f;
            line.startColor = new Color(0f, 0.9f, 1f, 0.9f);
            line.endColor = new Color(1f, 0.18f, 0.58f, 0.5f);
            line.numCapVertices = 4;

            if (line.sharedMaterial == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null)
                {
                    shader = Shader.Find("Sprites/Default");
                }

                line.sharedMaterial = new Material(shader) { color = Color.white };
            }
        }
    }
}
