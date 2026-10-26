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
            RememberExplored
        }

        [SerializeField] private Material accumulationMaterial = null;
        [SerializeField] private LayoutVisibilityMode mode =
            LayoutVisibilityMode.VisibleFromStart;

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
            Shader.SetGlobalTexture(ExploredTextureId, Texture2D.blackTexture);

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
                mode == LayoutVisibilityMode.RememberExplored ? 1f : 0f);
            command.SetGlobalTexture(ExploredTextureId, memoryRead);
            command.SetGlobalVector(ExplorationRectId, explorationRect);

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
                RenderTextureFormat.ARGB32,
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
            GL.Clear(false, true, Color.black);
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