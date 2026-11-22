using UnityEngine;
using UnityEngine.UI;

namespace Jamie.Gameplay
{
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    public sealed class InteractionPromptDisplay : MonoBehaviour
    {
        [SerializeField] private WorldInteractor interactor;
        [SerializeField] private Text promptLabel;

        [Header("Popup")]
        [SerializeField] private CanvasGroup popupGroup;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private Vector2 screenOffset =
            new Vector2(32f, 32f);
        [SerializeField, Min(0f)] private float screenPadding = 12f;

        private RectTransform popupRect;
        private RectTransform parentRect;
        private Canvas popupCanvas;

        private void Awake()
        {
            if (interactor == null)
                interactor = FindFirstObjectByType<WorldInteractor>();

            if (worldCamera == null)
                worldCamera = Camera.main;

            if (interactor == null || promptLabel == null)
            {
                Debug.LogError(
                    "Assign Interactor and Prompt Label.", this);
                enabled = false;
                return;
            }

            if (popupGroup != null)
            {
                popupRect = popupGroup.transform as RectTransform;
                parentRect = popupGroup.transform.parent as RectTransform;
                popupCanvas = popupGroup.GetComponentInParent<Canvas>();

                if (popupRect == null ||
                    parentRect == null ||
                    popupCanvas == null ||
                    worldCamera == null)
                {
                    Debug.LogError(
                        "The popup needs a Canvas, RectTransforms, " +
                        "and a World Camera.", this);
                    enabled = false;
                    return;
                }
            }

            Hide();
        }

        private void LateUpdate()
        {
            if (Time.timeScale <= 0f ||
                interactor == null ||
                !interactor.isActiveAndEnabled)
            {
                Hide();
                return;
            }

            WorldInteractable target = interactor.CurrentTarget;

            if (target == null ||
                !target.isActiveAndEnabled ||
                !target.CanInteract(interactor))
            {
                Hide();
                return;
            }

            // Keeps the old text display working in other scenes
            // until their new popup UI is connected.
            if (popupGroup == null)
            {
                promptLabel.text = target.Prompt;
                return;
            }

            Vector3 worldPoint = target.GetInteractionPoint(
                interactor.Player.transform.position);

            Vector3 screenPoint =
                worldCamera.WorldToScreenPoint(worldPoint);

            if (screenPoint.z <= 0f ||
                !worldCamera.pixelRect.Contains(
                    new Vector2(screenPoint.x, screenPoint.y)))
            {
                Hide();
                return;
            }

            Camera uiCamera =
                popupCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                    ? null
                    : popupCanvas.worldCamera;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentRect, screenPoint, uiCamera,
                    out Vector2 position))
            {
                Hide();
                return;
            }

            position += screenOffset;

            Rect parentBounds = parentRect.rect;
            Rect popupBounds = popupRect.rect;

            float minX =
                parentBounds.xMin + screenPadding - popupBounds.xMin;
            float maxX =
                parentBounds.xMax - screenPadding - popupBounds.xMax;
            float minY =
                parentBounds.yMin + screenPadding - popupBounds.yMin;
            float maxY =
                parentBounds.yMax - screenPadding - popupBounds.yMax;

            position.x = Mathf.Clamp(
                position.x, minX, Mathf.Max(minX, maxX));
            position.y = Mathf.Clamp(
                position.y, minY, Mathf.Max(minY, maxY));

            popupRect.localPosition =
                new Vector3(position.x, position.y, 0f);

            promptLabel.text = target.Prompt;
            popupGroup.alpha = 1f;
        }

        private void Hide()
        {
            if (promptLabel != null)
                promptLabel.text = string.Empty;

            if (popupGroup != null)
                popupGroup.alpha = 0f;
        }

        private void OnDisable()
        {
            Hide();
        }
    }
}