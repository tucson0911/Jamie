using UnityEngine;
using UnityEngine.UI;

namespace Jamie.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class RoomNameDisplay : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        [SerializeField] private Text roomLabel;
        [SerializeField] private CanvasGroup labelGroup;

        [Header("Timing")]
        [SerializeField, Min(0f)] private float fadeInSeconds = 0.4f;
        [SerializeField, Min(0f)] private float holdSeconds = 1.8f;
        [SerializeField, Min(0f)] private float fadeOutSeconds = 0.8f;
        [SerializeField, Min(0f)] private float entryDelay = 0.12f;

        private RoomNameArea[] areas;
        private RoomNameArea currentArea;
        private RoomNameArea pendingArea;
        private float pendingTime;
        private float displayTime;
        private bool showing;

        private void Awake()
        {
            if (roomLabel == null || labelGroup == null)
            {
                Debug.LogError("Assign Room Label and Label Group on RoomNameDisplay.", this);
                enabled = false;
                return;
            }

            roomLabel.raycastTarget = false;
            labelGroup.interactable = false;
            labelGroup.blocksRaycasts = false;
            labelGroup.alpha = 0f;
        }

        private void Start()
        {
            if (player == null)
                player = FindFirstObjectByType<PlayerController>();

            areas = FindObjectsByType<RoomNameArea>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
        }

        private void LateUpdate()
        {
            if (player == null || areas == null || Time.timeScale <= 0f)
                return;

            RoomNameArea nextArea = null;

            foreach (RoomNameArea area in areas)
            {
                if (area == null || !area.Contains(player.transform.position))
                    continue;

                if (nextArea == null || area.Priority > nextArea.Priority ||
                    (area.Priority == nextArea.Priority && area == currentArea))
                {
                    nextArea = area;
                }
            }

            // Briefly confirm entry so crossing a shared edge cannot flash labels.
            if (nextArea != pendingArea)
            {
                pendingArea = nextArea;
                pendingTime = 0f;
            }

            pendingTime += Time.deltaTime;

            if (pendingArea != currentArea && pendingTime >= entryDelay)
            {
                currentArea = pendingArea;

                if (currentArea != null && !string.IsNullOrWhiteSpace(currentArea.RoomName))
                {
                    roomLabel.text = currentArea.RoomName;
                    displayTime = 0f;
                    labelGroup.alpha = 0f;
                    showing = true;
                }
            }

            if (!showing)
                return;

            displayTime += Time.deltaTime;

            if (displayTime < fadeInSeconds)
                labelGroup.alpha = Mathf.SmoothStep(0f, 1f, displayTime / fadeInSeconds);
            else if (displayTime < fadeInSeconds + holdSeconds)
                labelGroup.alpha = 1f;
            else if (displayTime < fadeInSeconds + holdSeconds + fadeOutSeconds)
                labelGroup.alpha = Mathf.SmoothStep(1f, 0f,
                    (displayTime - fadeInSeconds - holdSeconds) / fadeOutSeconds);
            else
            {
                labelGroup.alpha = 0f;
                showing = false;
            }
        }

        private void OnDisable()
        {
            showing = false;
            currentArea = null;
            pendingArea = null;
            pendingTime = 0f;

            if (labelGroup != null)
                labelGroup.alpha = 0f;
        }
    }
}
