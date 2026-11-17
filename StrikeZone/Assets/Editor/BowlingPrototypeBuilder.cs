using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace StrikeZone.Editor
{
    /// <summary>Creates the first primitive lane and physics test setup in the active scene.</summary>
    public static class BowlingPrototypeBuilder
    {
        private const string RootName = "StrikeZonePrototype";
        private const string MaterialsPath = "Assets/Materials";
        private const string PhysicsMaterialsPath = "Assets/Materials/PhysicsMaterials";

        [MenuItem("Strike Zone/Build Lane Prototype")]
        private static void BuildLanePrototype()
        {
            if (EditorSceneManager.GetActiveScene().name != "BowlingGame")
            {
                var proceed = EditorUtility.DisplayDialog(
                    "Build Lane Prototype",
                    "The active scene is not named BowlingGame. Build the prototype in this scene anyway?",
                    "Build Here",
                    "Cancel");
                if (!proceed)
                {
                    return;
                }
            }

            if (GameObject.Find(RootName) != null)
            {
                EditorUtility.DisplayDialog(
                    "Prototype Already Exists",
                    "A StrikeZonePrototype is already in this scene. The builder stopped to avoid creating duplicate objects.",
                    "OK");
                return;
            }

            EnsureFolder(MaterialsPath);
            EnsureFolder(PhysicsMaterialsPath);

            var lanePhysics = GetOrCreatePhysicsMaterial(
                "Assets/Materials/PhysicsMaterials/Lane.physicMaterial", 0.05f, 0f);
            var ballPinPhysics = GetOrCreatePhysicsMaterial(
                "Assets/Materials/PhysicsMaterials/BallPin.physicMaterial", 0.3f, 0.2f);

            var laneVisual = GetOrCreateMaterial("Assets/Materials/Lane.mat", new Color(0.43f, 0.24f, 0.11f));
            var gutterVisual = GetOrCreateMaterial("Assets/Materials/Gutter.mat", new Color(0.035f, 0.045f, 0.065f));
            var pinVisual = GetOrCreateMaterial("Assets/Materials/Pin.mat", new Color(0.92f, 0.92f, 0.86f));
            var ballVisual = GetOrCreateMaterial("Assets/Materials/Ball.mat", new Color(0.02f, 0.75f, 0.88f));

            var root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Build bowling lane prototype");
            var environment = CreateGroup("Environment", root.transform);
            var setup = CreateGroup("BowlingSetup", root.transform);

            CreateBox("Lane", environment.transform, new Vector3(0f, -0.05f, 6f),
                new Vector3(1.05f, 0.1f, 12f), laneVisual, lanePhysics);
            CreateBox("LeftGutter", environment.transform, new Vector3(-0.64f, -0.085f, 6f),
                new Vector3(0.17f, 0.07f, 12f), gutterVisual, lanePhysics);
            CreateBox("RightGutter", environment.transform, new Vector3(0.64f, -0.085f, 6f),
                new Vector3(0.17f, 0.07f, 12f), gutterVisual, lanePhysics);
            CreateBox("LeftLaneDivider", environment.transform, new Vector3(-0.55f, 0.035f, 6f),
                new Vector3(0.03f, 0.07f, 12f), gutterVisual, lanePhysics);
            CreateBox("RightLaneDivider", environment.transform, new Vector3(0.55f, 0.035f, 6f),
                new Vector3(0.03f, 0.07f, 12f), gutterVisual, lanePhysics);
            CreateBox("LeftOuterRail", environment.transform, new Vector3(-0.77f, 0.05f, 6f),
                new Vector3(0.04f, 0.2f, 12f), gutterVisual, lanePhysics);
            CreateBox("RightOuterRail", environment.transform, new Vector3(0.77f, 0.05f, 6f),
                new Vector3(0.04f, 0.2f, 12f), gutterVisual, lanePhysics);
            CreateBox("PitBackstop", environment.transform, new Vector3(0f, 0.45f, 12.45f),
                new Vector3(1.7f, 1f, 0.2f), gutterVisual, lanePhysics);

            var pitTrigger = CreateBox("PitTrigger", environment.transform, new Vector3(0f, 0.45f, 12.15f),
                new Vector3(1.7f, 1f, 0.2f), gutterVisual, lanePhysics);
            pitTrigger.GetComponent<BoxCollider>().isTrigger = true;
            pitTrigger.GetComponent<Renderer>().enabled = false;

            var spawnPoint = new GameObject("BallSpawnPoint");
            spawnPoint.transform.SetParent(setup.transform, false);
            spawnPoint.transform.position = new Vector3(0f, 0.11f, 0.8f);

            var aimDirection = new GameObject("AimDirection");
            aimDirection.transform.SetParent(setup.transform, false);
            aimDirection.transform.position = spawnPoint.transform.position;
            aimDirection.transform.forward = Vector3.forward;

            var pinSpawnPoints = CreateGroup("PinSpawnPoints", setup.transform);
            var pins = CreateGroup("Pins", setup.transform);
            for (var row = 0; row < 4; row++)
            {
                var pinsInRow = row + 1;
                for (var column = 0; column < pinsInRow; column++)
                {
                    var x = (column - (pinsInRow - 1) * 0.5f) * 0.24f;
                    var position = new Vector3(x, 0.19f, 9.85f + row * 0.28f);
                    var pinPoint = new GameObject($"PinSpawn_{row + 1}_{column + 1}");
                    pinPoint.transform.SetParent(pinSpawnPoints.transform, false);
                    pinPoint.transform.position = position;

                    var pin = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    pin.name = $"Pin_{row + 1}_{column + 1}";
                    pin.transform.position = position;
                    pin.transform.localScale = new Vector3(0.15f, 0.19f, 0.15f);
                    pin.transform.SetParent(pins.transform, true);
                    pin.GetComponent<Renderer>().sharedMaterial = pinVisual;
                    pin.GetComponent<CapsuleCollider>().sharedMaterial = ballPinPhysics;

                    var pinBody = pin.AddComponent<Rigidbody>();
                    pinBody.mass = 1.5f;
                    pinBody.linearDamping = 0.05f;
                    pinBody.angularDamping = 0.1f;
                    pinBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                }
            }

            var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.name = "Ball";
            ball.transform.position = spawnPoint.transform.position;
            ball.transform.localScale = Vector3.one * 0.22f;
            ball.transform.SetParent(root.transform, true);
            ball.GetComponent<Renderer>().sharedMaterial = ballVisual;
            ball.GetComponent<SphereCollider>().sharedMaterial = ballPinPhysics;

            var ballBody = ball.AddComponent<Rigidbody>();
            ballBody.mass = 6f;
            ballBody.linearDamping = 0f;
            ballBody.angularDamping = 0.05f;
            ballBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            var camera = Camera.main;
            if (camera != null)
            {
                Undo.RecordObject(camera.transform, "Position bowling camera");
                camera.transform.position = new Vector3(0f, 2.1f, -3.5f);
                camera.transform.rotation = Quaternion.Euler(8f, 0f, 0f);
                camera.fieldOfView = 58f;
            }

            Selection.activeGameObject = root;
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorUtility.DisplayDialog(
                "Lane Prototype Created",
                "The lane, gutters, pit, ball, pin rack, spawn points, and starter physics materials are ready. Save the scene with Ctrl+S.",
                "OK");
        }

        private static GameObject CreateGroup(string name, Transform parent)
        {
            var group = new GameObject(name);
            group.transform.SetParent(parent, false);
            return group;
        }

        private static GameObject CreateBox(
            string name,
            Transform parent,
            Vector3 position,
            Vector3 scale,
            Material visual,
            PhysicsMaterial physics)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.position = position;
            box.transform.localScale = scale;
            box.transform.SetParent(parent, true);
            box.GetComponent<Renderer>().sharedMaterial = visual;
            box.GetComponent<BoxCollider>().sharedMaterial = physics;
            return box;
        }

        private static PhysicsMaterial GetOrCreatePhysicsMaterial(string path, float friction, float bounciness)
        {
            var physicsMaterial = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
            if (physicsMaterial != null)
            {
                return physicsMaterial;
            }

            physicsMaterial = new PhysicsMaterial(System.IO.Path.GetFileNameWithoutExtension(path))
            {
                staticFriction = friction,
                dynamicFriction = friction,
                bounciness = bounciness,
                frictionCombine = PhysicsMaterialCombine.Average,
                bounceCombine = PhysicsMaterialCombine.Average
            };
            AssetDatabase.CreateAsset(physicsMaterial, path);
            return physicsMaterial;
        }

        private static Material GetOrCreateMaterial(string path, Color color)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                return material;
            }

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            material = new Material(shader)
            {
                name = System.IO.Path.GetFileNameWithoutExtension(path),
                color = color
            };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void EnsureFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath))
            {
                return;
            }

            var parentPath = System.IO.Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            var folderName = System.IO.Path.GetFileName(assetPath);
            if (!string.IsNullOrEmpty(parentPath) && AssetDatabase.IsValidFolder(parentPath))
            {
                AssetDatabase.CreateFolder(parentPath, folderName);
            }
        }
    }
}
