using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Jamie.Tests.Editor
{
    public static class LightingSeparationChecks
    {
        private const string Running = "Jamie.LightingCheck.Running";
        private const string Batch = "Jamie.LightingCheck.Batch";
        private const string ScenePath = "Assets/Scenes/Josh_LightingSeparationTest.unity";
        private const string OutputDirectory = "Library/LightingSeparationCheck";
        private static int stage;
        private static int waitFrames;
        private static int lastFrame = -1;
        private static double deadline;
        private static LightingSeparationTest test;
        private static PlayerController player;
        private static Transform facing;
        private static Camera view;
        private static RenderTexture output;
        private static float darkBrightness;
        private static float torchBrightness;
        private static string runtimeError;

        [InitializeOnLoadMethod]
        private static void ResumeAfterReload()
        {
            if (!SessionState.GetBool(Running, false))
                return;
            stage = 0;
            waitFrames = 0;
            lastFrame = -1;
            runtimeError = null;
            deadline = EditorApplication.timeSinceStartup + 120;
            Application.logMessageReceived += ReceiveLog;
            EditorApplication.update -= Poll;
            EditorApplication.update += Poll;
        }

        // Invoke with -executeMethod Jamie.Tests.Editor.LightingSeparationChecks.RunBatch.
        public static void RunBatch()
        {
            SessionState.SetBool(Batch, true);
            Run();
        }

        [MenuItem("Jamie/Tests/Check Lighting Separation")]
        public static void RunInteractive()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode ||
                !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            SessionState.SetBool(Batch, false);
            Run();
        }

        private static void Run()
        {
            try
            {
                AssetDatabase.Refresh();
                foreach (string path in new[] {
                    "Assets/_Game/Tests/Lighting/ObjectsSeparated.shader",
                    "Assets/_Game/Tests/Lighting/StructureSeparated.shader" })
                {
                    Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
                    Require(shader != null && shader.isSupported, "Shader failed to import: " + path);
                    foreach (var message in ShaderUtil.GetShaderMessages(shader))
                        Require(message.severity != UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error,
                            path + ": " + message.message);
                }
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                SessionState.SetBool(Running, true);
                ResumeAfterReload();
                EditorApplication.EnterPlaymode();
            }
            catch (Exception exception)
            {
                Finish(false, exception.ToString());
            }
        }

        private static void ReceiveLog(string message, string stack, LogType type)
        {
            if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert)
                runtimeError = message + "\n" + stack;
        }

        private static void Poll()
        {
            if (EditorApplication.timeSinceStartup > deadline)
            {
                Finish(false, "Timed out waiting for Unity Play mode.");
                return;
            }
            if (!EditorApplication.isPlaying || EditorApplication.isCompiling)
                return;
            if (Time.frameCount == lastFrame)
                return;
            lastFrame = Time.frameCount;
            try
            {
                Require(string.IsNullOrEmpty(runtimeError), runtimeError);
                if (waitFrames-- > 0)
                    return;
                if (stage == 0)
                {
                    test = UnityEngine.Object.FindFirstObjectByType<LightingSeparationTest>();
                    player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
                    Require(test != null && test.SightMask != null && player != null,
                        "Lighting scene did not initialize.");
                    Require(!test.OwnsFlashlight && !test.FlashlightOn && test.SightMask.enabled,
                        "Initial state must have active sight and no flashlight.");
                    player.CancelInvoke("DecrementOxygen");
                    // Hold a deterministic aim during render measurements rather than
                    // allowing the user's current mouse position to turn the beam.
                    player.enabled = false;
                    facing = test.SightMask.transform.parent;
                    view = UnityEngine.Object.FindFirstObjectByType<CameraFollow>().GetComponent<Camera>();
                    output = new RenderTexture(960, 540, 24, RenderTextureFormat.ARGB32);
                    output.Create();
                    view.targetTexture = output;
                    view.aspect = 960f / 540f;
                    player.RespawnAt(new Vector2(4.25f, 4f));
                    facing.rotation = Quaternion.identity;
                    view.GetComponent<CameraFollow>().SnapToTarget();
                    stage = 1;
                    waitFrames = 12;
                }
                else if (stage == 1)
                {
                    darkBrightness = BrightnessAt(new Vector2(4.25f, 5f));
                    Require(darkBrightness < 0.025f,
                        "The sight mask illuminates the dark hallway: " + darkBrightness);
                    SaveImage("01-dark-hallway.png");
                    test.CollectFlashlight();
                    Require(test.OwnsFlashlight && test.FlashlightOn, "Pickup did not enable flashlight.");
                    stage = 2;
                    waitFrames = 12;
                }
                else if (stage == 2)
                {
                    torchBrightness = BrightnessAt(new Vector2(4.25f, 5f));
                    SaveImage("02-flashlight-on.png");
                    Require(torchBrightness > darkBrightness + 0.05f,
                        "Flashlight failed to illuminate the hallway: " + torchBrightness);
                    test.SetFlashlight(false);
                    stage = 3;
                    waitFrames = 12;
                }
                else if (stage == 3)
                {
                    float offBrightness = BrightnessAt(new Vector2(4.25f, 5f));
                    Require(offBrightness < 0.025f && test.SightMask.enabled,
                        "Switching off should darken the corridor while retaining sight: " + offBrightness);
                    test.ResetFlashlight();
                    player.RespawnAt(new Vector2(-4f, -10.2f));
                    Vector2 direction = new Vector2(-5.2f, -9.7f) - (Vector2)player.transform.position;
                    facing.rotation = Quaternion.Euler(0f, 0f,
                        Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f);
                    view.GetComponent<CameraFollow>().SnapToTarget();
                    stage = 4;
                    waitFrames = 12;
                }
                else if (stage == 4)
                {
                    Require(!test.OwnsFlashlight, "The note test must run without owning a flashlight.");
                    test.SendMessage("TryReadNote", SendMessageOptions.RequireReceiver);
                    Require(NoteManager.Instance != null && NoteManager.Instance.IsOpen,
                        "Could not read the lit prescription without a flashlight.");
                    NoteManager.Instance.CloseNote();
                    Require(Time.timeScale > 0f, "Closing the prescription did not resume play.");
                    Finish(true, "PASS: dark corridor=" + darkBrightness.ToString("F4") +
                        ", flashlight on=" + torchBrightness.ToString("F4") +
                        ". Sight remains active with flashlight off. Lit notes work before pickup.");
                }
            }
            catch (Exception exception)
            {
                Finish(false, exception.ToString());
            }
        }

        private static float BrightnessAt(Vector2 position)
        {
            Vector3 uv = view.WorldToViewportPoint(position);
            Require(uv.x > 0 && uv.x < 1 && uv.y > 0 && uv.y < 1, "Sample is outside camera view.");
            RenderTexture previous = RenderTexture.active;
            Texture2D pixel = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            try
            {
                RenderTexture.active = output;
                pixel.ReadPixels(new Rect((int)(uv.x * output.width),
                    (int)(uv.y * output.height), 1, 1), 0, 0);
                pixel.Apply();
                Color color = pixel.GetPixel(0, 0);
                return Mathf.Max(color.r, Mathf.Max(color.g, color.b));
            }
            finally
            {
                RenderTexture.active = previous;
                UnityEngine.Object.DestroyImmediate(pixel);
            }
        }

        private static void SaveImage(string filename)
        {
            Directory.CreateDirectory(OutputDirectory);
            RenderTexture previous = RenderTexture.active;
            Texture2D image = new Texture2D(output.width, output.height, TextureFormat.RGB24, false);
            try
            {
                RenderTexture.active = output;
                image.ReadPixels(new Rect(0, 0, output.width, output.height), 0, 0);
                image.Apply();
                File.WriteAllBytes(Path.Combine(OutputDirectory, filename), image.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                UnityEngine.Object.DestroyImmediate(image);
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        private static void Finish(bool success, string message)
        {
            SessionState.SetBool(Running, false);
            EditorApplication.update -= Poll;
            Application.logMessageReceived -= ReceiveLog;
            Directory.CreateDirectory(OutputDirectory);
            File.WriteAllText(Path.Combine(OutputDirectory, "result.txt"), message);
            Debug.Log(message);
            if (view != null)
                view.targetTexture = null;
            if (output != null)
            {
                output.Release();
                UnityEngine.Object.DestroyImmediate(output);
            }
            if (SessionState.GetBool(Batch, false))
                EditorApplication.Exit(success ? 0 : 1);
            else
                EditorApplication.ExitPlaymode();
        }
    }
}
