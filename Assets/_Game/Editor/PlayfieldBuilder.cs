using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Jamie.Editor
{
    /// <summary>Authors ordinary scene objects; no level generation runs in the player.</summary>
    public static class PlayfieldBuilder
    {
        public const string ScenePath = "Assets/_Game/Scenes/Playfield.unity";
        private const string ArtPath = "Assets/_Game/Art/Placeholders";
        private static Sprite square;
        private static Sprite circle;
        private static Material lit;
        private static Material unlit;
        private static PhysicsMaterial2D frictionless;

        [MenuItem("Jamie/Create placeholder playfield")]
        public static void Create()
        {
            if (File.Exists(ScenePath))
            {
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
                Debug.Log("Playfield already exists. Open it from Assets/_Game/Scenes; existing edits were preserved.");
                return;
            }

            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            Directory.CreateDirectory(ArtPath);
            AssetDatabase.Refresh();
            square = MakeSprite("Square", false);
            circle = MakeSprite("Circle", true);
            lit = MakeMaterial("PlaceholderLit", "Universal Render Pipeline/2D/Sprite-Lit-Default");
            unlit = MakeMaterial("PlaceholderUnlit", "Universal Render Pipeline/2D/Sprite-Unlit-Default");
            frictionless = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(ArtPath + "/Slide.physicsMaterial2D");
            if (frictionless == null)
            {
                frictionless = new PhysicsMaterial2D("Slide") { friction = 0, bounciness = 0 };
                AssetDatabase.CreateAsset(frictionless, ArtPath + "/Slide.physicsMaterial2D");
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildScene();
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("JAMIE_PLAYFIELD_CREATED " + ScenePath);
        }

        private static void BuildScene()
        {
            Transform floors = new GameObject("Floors and markings").transform;
            Transform walls = new GameObject("Walls - colliders and shadows").transform;
            Transform props = new GameObject("Furniture - colliders and shadows").transform;
            Transform labels = new GameObject("Room labels").transform;

            Box("Hospital foundation", Vector2.zero, new Vector2(30, 20), new Color(0.31f, 0.36f, 0.34f), -20, floors);
            Box("Corridor", Vector2.zero, new Vector2(29.4f, 4.2f), new Color(0.43f, 0.46f, 0.39f), -19, floors);
            Box("Reception floor", new Vector2(-10, 6), new Vector2(9.4f, 7.5f), new Color(0.38f, 0.43f, 0.4f), -19, floors);
            Box("Ward floor", new Vector2(0, 6), new Vector2(9.4f, 7.5f), new Color(0.38f, 0.45f, 0.44f), -19, floors);
            Box("Store floor", new Vector2(10, 6), new Vector2(9.4f, 7.5f), new Color(0.42f, 0.41f, 0.34f), -19, floors);
            for (int x = -14; x < 15; x++)
                Box("Tile seam", new Vector2(x, 0), new Vector2(0.018f, 19.4f), new Color(0.25f, 0.3f, 0.28f), -18, floors);
            for (int y = -9; y < 10; y++)
                Box("Tile seam", new Vector2(0, y), new Vector2(29.4f, 0.018f), new Color(0.25f, 0.3f, 0.28f), -18, floors);
            for (int x = -13; x <= 13; x += 2)
                Box("Corridor stripe", new Vector2(x, 0), new Vector2(0.55f, 0.055f), new Color(0.7f, 0.65f, 0.42f), -17, floors);

            Wall("North", 0, 10, 30.6f, 0.6f, walls);
            Wall("South", 0, -10, 30.6f, 0.6f, walls);
            Wall("West", -15, 0, 0.6f, 20, walls);
            Wall("East", 15, 0, 0.6f, 20, walls);
            Wall("Reception north corridor", -13.1f, 2.1f, 3.8f, 0.45f, walls);
            Wall("Ward west corridor", -5, 2.1f, 7.6f, 0.45f, walls);
            Wall("Ward east corridor", 5, 2.1f, 7.6f, 0.45f, walls);
            Wall("Store north corridor", 13.1f, 2.1f, 3.8f, 0.45f, walls);
            Wall("Upper divider west", -5, 6.05f, 0.45f, 7.45f, walls);
            Wall("Upper divider east", 5, 6.05f, 0.45f, 7.45f, walls);
            Wall("Records corridor west", -12.1f, -2.1f, 5.8f, 0.45f, walls);
            Wall("South corridor centre", 0, -2.1f, 13.6f, 0.45f, walls);
            Wall("Treatment corridor east", 12.1f, -2.1f, 5.8f, 0.45f, walls);
            Wall("Lower divider", 0, -6.05f, 0.45f, 7.45f, walls);

            foreach (float x in new[] { -10f, 0f, 10f })
                Box("Open doorway", new Vector2(x, 2.1f), new Vector2(2.4f, 0.18f), new Color(0.62f, 0.61f, 0.44f), -16, floors);
            foreach (float x in new[] { -8f, 8f })
                Box("Open doorway", new Vector2(x, -2.1f), new Vector2(2.4f, 0.18f), new Color(0.62f, 0.61f, 0.44f), -16, floors);

            Furniture("Reception desk", -10, 6, 4.3f, 1.3f, new Color(0.48f, 0.39f, 0.27f), props);
            Furniture("Waiting bench", -13.5f, 4.4f, 0.8f, 2.2f, new Color(0.3f, 0.42f, 0.4f), props);
            Bed(-2.8f, 6.5f, props); Bed(2.8f, 6.5f, props);
            Furniture("Ward cabinet", 0, 8.7f, 2.1f, 0.7f, new Color(0.44f, 0.45f, 0.38f), props);
            Furniture("Shelf A", 7, 7, 1, 3.4f, new Color(0.47f, 0.4f, 0.29f), props);
            Furniture("Shelf B", 12.8f, 7, 1, 3.4f, new Color(0.47f, 0.4f, 0.29f), props);
            Furniture("Storage crate", 11, 4.5f, 1.2f, 1.2f, new Color(0.57f, 0.46f, 0.3f), props);
            Furniture("Records shelf A", -12, -6.8f, 1.4f, 3.4f, new Color(0.45f, 0.4f, 0.32f), props);
            Furniture("Records shelf B", -9.3f, -6.8f, 1.4f, 3.4f, new Color(0.45f, 0.4f, 0.32f), props);
            Furniture("Records desk", -4.3f, -6, 3.5f, 1.5f, new Color(0.48f, 0.39f, 0.27f), props);
            Bed(5, -6.5f, props); Bed(11, -6.5f, props);

            Label("RECEPTION", -10, 8.5f, labels);
            Label("WARD A", 0, 3.5f, labels);
            Label("STORE", 10, 8.8f, labels);
            Label("RECORDS", -5, -8.8f, labels);
            Label("TREATMENT", 8, -8.8f, labels);

            var ambient = new GameObject("Ambient light").AddComponent<Light2D>();
            ambient.lightType = Light2D.LightType.Global;
            ambient.color = new Color(0.76f, 0.85f, 0.8f);
            ambient.intensity = 0.48f;

            GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(UniversalAdditionalCameraData));
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 6.3f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 50;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.025f, 0.033f, 0.03f);
            camera.transform.position = new Vector3(-4, 0, -10);
            cameraObject.GetComponent<UniversalAdditionalCameraData>().renderPostProcessing = false;

            GameObject player = new GameObject("Player", typeof(Rigidbody2D), typeof(CircleCollider2D));
            player.transform.position = new Vector3(-11, 0.7f, 0);
            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
            body.gravityScale = 0;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.sharedMaterial = frictionless;
            player.GetComponent<CircleCollider2D>().radius = 0.3f;
            Transform facing = new GameObject("Facing - mouse aim").transform;
            facing.SetParent(player.transform, false);
            facing.rotation = Quaternion.Euler(0, 0, -90);
            Disc("Player shadow", new Vector2(0.04f, -0.09f), new Vector2(0.75f, 0.64f), new Color(0.02f, 0.03f, 0.025f, 0.65f), 5, player.transform);
            Disc("Body", Vector2.zero, new Vector2(0.62f, 0.62f), new Color(0.77f, 0.76f, 0.62f), 6, facing);
            Disc("Head", new Vector2(0, 0.13f), new Vector2(0.36f, 0.36f), new Color(0.93f, 0.84f, 0.65f), 7, facing);
            Box("Facing marker", new Vector2(0, 0.39f), new Vector2(0.12f, 0.23f), new Color(0.94f, 0.69f, 0.33f), 8, facing, true);
            var flashlight = new GameObject("Flashlight cone").AddComponent<Light2D>();
            flashlight.transform.SetParent(facing, false);
            flashlight.transform.localPosition = new Vector3(0, 0.28f, 0);
            flashlight.lightType = Light2D.LightType.Point;
            flashlight.color = new Color(1, 0.88f, 0.64f);
            flashlight.intensity = 1.25f;
            flashlight.pointLightInnerRadius = 1;
            flashlight.pointLightOuterRadius = 8;
            flashlight.pointLightInnerAngle = 36;
            flashlight.pointLightOuterAngle = 64;
            flashlight.falloffIntensity = 0.65f;
            flashlight.shadowIntensity = 1;
            var nearby = new GameObject("Nearby visibility").AddComponent<Light2D>();
            nearby.transform.SetParent(player.transform, false);
            nearby.lightType = Light2D.LightType.Point;
            nearby.intensity = 0.45f;
            nearby.pointLightInnerRadius = 0.4f;
            nearby.pointLightOuterRadius = 2.4f;
            nearby.shadowIntensity = 1;
            PlayerController controller = player.AddComponent<PlayerController>();
            controller.Configure(camera, facing, flashlight);
            cameraObject.AddComponent<CameraFollow>().Configure(player.transform);
            new GameObject("Controls overlay").AddComponent<PlayfieldHud>().Configure(controller);
        }

        private static GameObject Box(string name, Vector2 position, Vector2 size, Color color, int order, Transform parent, bool fullBright = false)
        {
            GameObject obj = new GameObject(name, typeof(SpriteRenderer));
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position;
            obj.transform.localScale = new Vector3(size.x, size.y, 1);
            SpriteRenderer renderer = obj.GetComponent<SpriteRenderer>();
            renderer.sprite = square;
            renderer.sharedMaterial = fullBright ? unlit : lit;
            renderer.color = color;
            renderer.sortingOrder = order;
            return obj;
        }

        private static void Disc(string name, Vector2 position, Vector2 size, Color color, int order, Transform parent)
        {
            GameObject obj = Box(name, position, size, color, order, parent, true);
            obj.GetComponent<SpriteRenderer>().sprite = circle;
        }

        private static void Wall(string name, float x, float y, float width, float height, Transform parent)
        {
            GameObject obj = Box(name, new Vector2(x, y), new Vector2(width, height), new Color(0.2f, 0.25f, 0.23f), 3, parent);
            obj.AddComponent<BoxCollider2D>().sharedMaterial = frictionless;
            obj.AddComponent<ShadowCaster2D>();
        }

        private static GameObject Furniture(string name, float x, float y, float width, float height, Color color, Transform parent)
        {
            GameObject obj = Box(name, new Vector2(x, y), new Vector2(width, height), color, 2, parent);
            obj.AddComponent<BoxCollider2D>().sharedMaterial = frictionless;
            obj.AddComponent<ShadowCaster2D>();
            return obj;
        }

        private static void Bed(float x, float y, Transform parent)
        {
            GameObject bed = Furniture("Bed", x, y, 1.6f, 2.7f, new Color(0.27f, 0.33f, 0.32f), parent);
            Box("Blanket", new Vector2(0, -0.1f), new Vector2(0.85f, 0.65f), new Color(0.49f, 0.6f, 0.6f), 3, bed.transform);
            Box("Pillow", new Vector2(0, 0.33f), new Vector2(0.76f, 0.18f), new Color(0.76f, 0.77f, 0.67f), 3, bed.transform);
        }

        private static void Label(string text, float x, float y, Transform parent)
        {
            GameObject obj = new GameObject(text, typeof(TextMesh));
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = new Vector3(x, y, -0.1f);
            TextMesh mesh = obj.GetComponent<TextMesh>();
            mesh.text = text;
            mesh.fontSize = 40;
            mesh.characterSize = 0.075f;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.color = new Color(0.59f, 0.66f, 0.59f);
            obj.GetComponent<MeshRenderer>().sortingOrder = -10;
        }

        private static Sprite MakeSprite(string name, bool round)
        {
            string path = ArtPath + "/" + name + ".png";
            Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null)
                return existing;
            Texture2D texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 32; x++)
                    texture.SetPixel(x, y, !round || Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(16, 16)) <= 15.5f ? Color.white : Color.clear);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 32;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Material MakeMaterial(string name, string shader)
        {
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(ArtPath + "/" + name + ".mat");
            if (existing != null)
                return existing;
            Material material = new Material(Shader.Find(shader));
            AssetDatabase.CreateAsset(material, ArtPath + "/" + name + ".mat");
            return material;
        }
    }
}
