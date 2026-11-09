using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Jamie.Gamefeel
{
    public enum GamefeelAction { Door, Flashlight, ReceptionKey, Battery, Potion, Inspect, Reveal }

    public sealed class GamefeelInteractable : MonoBehaviour
    {
        public GamefeelDirector director;
        public string identifier;
        public GamefeelAction action;
        [Range(1, 3)] public int unlockAtNumber = 3;
        public bool requiresReceptionKey;
        public string displayName;
        [TextArea(2, 8)] public string description;
        public Transform doorVisual;
        public Collider2D doorCollider;
        public ShadowCaster2D doorShadow;
        public Vector3 openOffset;
        public GameObject revealedContent;
        public bool IsOpen { get; private set; }
        public bool IsRevealed { get; private set; }
        private Vector3 closedPosition;
        private bool initialized;
        private Coroutine slideRoutine;

        private void Awake() { Initialize(); }

        private void Initialize()
        {
            if (initialized) return;
            initialized = true;
            if (doorVisual != null) closedPosition = doorVisual.localPosition;
        }

        public void SetDoorOpen(bool open, bool animate = true)
        {
            Initialize();
            IsOpen = open;
            if (doorCollider != null) doorCollider.enabled = !open;
            if (doorShadow != null) doorShadow.enabled = !open;
            if (slideRoutine != null) StopCoroutine(slideRoutine);
            if (doorVisual == null) return;
            Vector3 destination = closedPosition + (open ? openOffset : Vector3.zero);
            if (Application.isPlaying && animate && isActiveAndEnabled)
                slideRoutine = StartCoroutine(Slide(destination));
            else doorVisual.localPosition = destination;
        }

        private IEnumerator Slide(Vector3 destination)
        {
            Vector3 origin = doorVisual.localPosition;
            float elapsed = 0;
            while (elapsed < 0.22f)
            {
                elapsed += Time.deltaTime;
                doorVisual.localPosition = Vector3.Lerp(origin, destination,
                    Mathf.SmoothStep(0, 1, elapsed / 0.22f));
                yield return null;
            }
            doorVisual.localPosition = destination;
            slideRoutine = null;
        }

        public void ToggleReveal()
        {
            IsRevealed = !IsRevealed;
            if (revealedContent != null) revealedContent.SetActive(IsRevealed);
        }

        public void ResetForRun()
        {
            Initialize();
            if (action == GamefeelAction.Door) SetDoorOpen(false, false);
            IsRevealed = false;
            if (revealedContent != null) revealedContent.SetActive(false);
        }
    }
}
