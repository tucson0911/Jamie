using UnityEngine;
using UnityEngine.Rendering;

namespace Jamie
{
    [RequireComponent(typeof(Camera))]
    public sealed class VisibilityMemory : MonoBehaviour
    {
        public enum LayoutVisibilityMode
        {
            VisibleFromStart,
            RememberExplored,
            FadingMemory
        }

        [SerializeField] private Material accumulationMaterial = null;
        [SerializeField] private LayoutVisibilityMode mode =
            LayoutVisibilityMode.VisibleFromStart;
        [SerializeField, Min(0f)] private float memoryHoldTime = 2f;
        [SerializeField, Min(0.1f)] private float memoryFadeTime = 3f;
        [SerializeField, Range(0f, 3f)]
        private float memoryEdgeSoftness = 1.5f;

        [Header("Wall Memory")]
        [SerializeField, Range(0f, 0.03f)]
        private float wallMemoryBrightness = 0.005f;

        [SerializeField, Range(0.5f, 4f)]
        private float wallMemoryWidth = 1.5f;
        [SerializeField, Range(0f, 2f)]
        private float wallMemorySoftness = 1f;

        [SerializeField, Range(0f, 2f)]
        private float wallMemoryWobble = 0.5f;

        [SerializeField, Range(0f, 2f)]
        private float wallMemoryWobbleSpeed = 0.5f;    

        private static readonly int CaptureId =
            Shader.PropertyToID("_JamieMemoryCapture");
        private static readonly int MemoryEnabledId =
            Shader.PropertyToID("_JamieMemoryEnabled");
        private static readonly int ExploredTextureId =
            Shader.PropertyToID("_JamieExploredTexture");
        private static readonly int ExplorationRectId =
            Shader.PropertyToID("_JamieExplorationRect");
        private static readonly int CurrentVisibilityId =
            Shader.PropertyToID("_CurrentVisibility");
        private static readonly int PreviousMemoryId =
            Shader.PropertyToID("_PreviousMemory");
        private static readonly int MemoryTimeId =
            Shader.PropertyToID("_MemoryTime");
        private static readonly int MemoryHoldTimeId =
            Shader.PropertyToID("_MemoryHoldTime");
        private static readonly int MemoryFadeTimeId =
            Shader.PropertyToID("_MemoryFadeTime");
        private static readonly int UseFadingMemoryId =
        Shader.PropertyToID("_JamieUseFadingMemory");
        private static readonly int MemorySoftnessId =
        Shader.PropertyToID("_JamieMemorySoftness");
        private static readonly int MemoryTexelSizeId =
        Shader.PropertyToID("_JamieMemoryTexelSize");
        private static readonly int WallCaptureTextureId =
        Shader.PropertyToID("_JamieWallCaptureTexture");
        private static readonly int WallMemoryBrightnessId =
        Shader.PropertyToID("_JamieWallMemoryBrightness");
        private static readonly int WallMemoryWidthId =
        Shader.PropertyToID("_JamieWallMemoryWidth");
        private static readonly int WallMemorySoftnessId =
        Shader.PropertyToID("_JamieWallMemorySoftness");
        private static readonly int WallMemoryWobbleId =
        Shader.PropertyToID("_JamieWallMemoryWobble");
        private static readonly int WallMemoryWobbleSpeedId =
        Shader.PropertyToID("_JamieWallMemoryWobbleSpeed");
                

        private Camera captureCamera;
        private RenderTexture memoryRead;
        private RenderTexture memoryWrite;
        private MaterialPropertyBlock accumulationProperties;
        private Vector4 explorationRect;

        private void OnEnable()
        {
            captureCamera = GetComponent<Camera>();

            if (accumulationMaterial == null ||
                captureCamera.targetTexture == null ||
                !captureCamera.orthographic)
            {
                Debug.LogError(
                    "Visibility Memory needs an orthographic camera, " +
                    "an Output Texture and an Accumulation Material.", this);
                return;
            }

            RenderTexture source = captureCamera.targetTexture;
            captureCamera.aspect = (float)source.width / source.height;

            float halfHeight = captureCamera.orthographicSize;
            float halfWidth = halfHeight * captureCamera.aspect;
            Vector3 center = captureCamera.transform.position;

            explorationRect = new Vector4(
                center.x - halfWidth, center.y - halfHeight,
                halfWidth * 2f, halfHeight * 2f);

            accumulationProperties = new MaterialPropertyBlock();
            memoryRead = CreateMemoryTexture("Explored memory A");
            memoryWrite = CreateMemoryTexture("Explored memory B");

            RenderPipelineManager.beginCameraRendering += BeginCamera;
            RenderPipelineManager.endCameraRendering += EndCamera;
        }

        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= BeginCamera;
            RenderPipelineManager.endCameraRendering -= EndCamera;

            Shader.SetGlobalFloat(CaptureId, 0f);
            Shader.SetGlobalFloat(MemoryEnabledId, 0f);
            Shader.SetGlobalFloat(UseFadingMemoryId, 0f);
            Shader.SetGlobalTexture(ExploredTextureId, Texture2D.blackTexture);
            Shader.SetGlobalFloat(MemorySoftnessId, 0f);
            Shader.SetGlobalTexture(WallCaptureTextureId, Texture2D.blackTexture);
            Shader.SetGlobalFloat(WallMemoryBrightnessId, 0f);
            Shader.SetGlobalFloat(WallMemorySoftnessId, 0f);
            Shader.SetGlobalFloat(WallMemoryWobbleId, 0f);
            Shader.SetGlobalFloat(WallMemoryWobbleSpeedId, 0f);

            ReleaseMemoryTexture(memoryRead);
            ReleaseMemoryTexture(memoryWrite);

            memoryRead = null;
            memoryWrite = null;
            accumulationProperties = null;
        }

        private void BeginCamera(
            ScriptableRenderContext context, Camera camera)
        {
            CommandBuffer command = CommandBufferPool.Get();

            command.SetGlobalFloat(
                CaptureId, camera == captureCamera ? 1f : 0f);
            command.SetGlobalFloat(
                MemoryEnabledId,
                mode == LayoutVisibilityMode.VisibleFromStart ? 0f : 1f);
            command.SetGlobalFloat(
                UseFadingMemoryId,
                mode == LayoutVisibilityMode.FadingMemory ? 1f : 0f);
            command.SetGlobalTexture(ExploredTextureId, memoryRead);
            command.SetGlobalVector(ExplorationRectId, explorationRect);
            command.SetGlobalFloat(MemorySoftnessId, memoryEdgeSoftness);
            command.SetGlobalVector(
                MemoryTexelSizeId,
                new Vector4(
                    1f / memoryRead.width,
                    1f / memoryRead.height,
                    memoryRead.width,
                    memoryRead.height));
            Texture wallCapture = Texture2D.blackTexture;

            if (camera != captureCamera)
                wallCapture = captureCamera.targetTexture;

            command.SetGlobalTexture(WallCaptureTextureId, wallCapture);
            command.SetGlobalFloat(WallMemoryBrightnessId, wallMemoryBrightness);
            command.SetGlobalFloat(WallMemoryWidthId, wallMemoryWidth);
            command.SetGlobalFloat(WallMemorySoftnessId, wallMemorySoftness);
            command.SetGlobalFloat(WallMemoryWobbleId, wallMemoryWobble);
            command.SetGlobalFloat(WallMemoryWobbleSpeedId, wallMemoryWobbleSpeed);    
            context.ExecuteCommandBuffer(command);
            CommandBufferPool.Release(command);
        }

        private void EndCamera(
            ScriptableRenderContext context, Camera camera)
        {
            if (camera != captureCamera)
                return;

            accumulationProperties.SetTexture(
                CurrentVisibilityId, captureCamera.targetTexture);
            accumulationProperties.SetTexture(PreviousMemoryId, memoryRead);
            accumulationProperties.SetFloat(MemoryTimeId, Time.time);
            accumulationProperties.SetFloat(MemoryHoldTimeId, memoryHoldTime);
            accumulationProperties.SetFloat(MemoryFadeTimeId, memoryFadeTime);

            CommandBuffer command = CommandBufferPool.Get();
            command.SetGlobalFloat(CaptureId, 0f);
            command.SetRenderTarget(memoryWrite);
            command.SetViewport(new Rect(
                0f, 0f, memoryWrite.width, memoryWrite.height));

            CoreUtils.DrawFullScreen(
                command, accumulationMaterial, accumulationProperties);

            context.ExecuteCommandBuffer(command);
            context.Submit();
            CommandBufferPool.Release(command);

            RenderTexture previous = memoryRead;
            memoryRead = memoryWrite;
            memoryWrite = previous;
        }

        [ContextMenu("Clear Explored Memory")]
        private void ClearExploredMemory()
        {
            ClearMemoryTexture(memoryRead);
            ClearMemoryTexture(memoryWrite);
        }

        private RenderTexture CreateMemoryTexture(string textureName)
        {
            RenderTexture source = captureCamera.targetTexture;

            RenderTexture texture = new RenderTexture(
                source.width, source.height, 0,
                RenderTextureFormat.ARGBFloat,
                RenderTextureReadWrite.Linear)
            {
                name = textureName,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                useMipMap = false,
                autoGenerateMips = false
            };

            texture.Create();
            ClearMemoryTexture(texture);
            return texture;
        }

        private static void ClearMemoryTexture(RenderTexture texture)
        {
            if (texture == null)
                return;

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = texture;
            GL.Clear(false, true, Color.clear);
            RenderTexture.active = previous;
        }

        private static void ReleaseMemoryTexture(RenderTexture texture)
        {
            if (texture == null)
                return;

            texture.Release();
            CoreUtils.Destroy(texture);
        }
    }
}