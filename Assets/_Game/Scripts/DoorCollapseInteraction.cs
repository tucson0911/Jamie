using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Jamie
{
    public sealed class DoorCollapseInteraction : WorldInteractable
    {
        [Header("Scene References")]
        [SerializeField] private Transform doorVisual = null;
        [SerializeField] private Collider2D doorCollider = null;
        [SerializeField] private BoxCollider2D collapseZone = null;
        [SerializeField] private SpriteRenderer ceilingDebris = null;
        [SerializeField] private DeathLoopController deathLoop = null;

        [Header("Door Mashing")]
        [SerializeField, Min(1)] private int pressesToOpen = 6;
        [SerializeField, Min(0.1f)] private float mashResetDelay = 1.5f;
        [SerializeField, Min(0f)] private float openingSeconds = 0.35f;
        [SerializeField] private Vector3 slideOffset = new Vector3(2.2f, 0f, 0f);

        [Header("Ceiling Collapse")]
        [SerializeField, Min(0f)] private float warningSeconds = 0.8f;
        [SerializeField, Min(0f)] private float fallSeconds = 0.25f;
        [SerializeField, Min(0f)] private float fallDistance = 1.5f;

        private enum DoorState { Closed, Opening, Warning, Falling, WaitingForRespawn, Finished }
        private DoorState state;
        private int presses;
        private float lastPress;
        private Vector3 closedDoorPosition;
        private Vector3 debrisRestPosition;
        private Color debrisColor;
        private ShadowCaster2D doorShadow;
        private bool originalShadowEnabled;
        private bool ready;
        private Coroutine sequence;

        public override string Prompt => "Mash E to force door (" + presses + "/" + pressesToOpen + ")";
        public override string ScreenMessage => state == DoorState.Warning || state == DoorState.Falling
            ? "The ceiling is cracking! Move away from the doorway!" : string.Empty;

        private void Awake()
        {
            ready = doorVisual != null && doorCollider != null && collapseZone != null &&
                ceilingDebris != null && deathLoop != null;
            if (!ready)
            {
                Debug.LogError("Assign Door Visual, Door Collider, Collapse Zone, Ceiling Debris " +
                    "and Death Loop on DoorCollapseInteraction.", this);
                enabled = false;
                return;
            }
            closedDoorPosition = doorVisual.localPosition;
            debrisRestPosition = ceilingDebris.transform.position;
            debrisColor = ceilingDebris.color;
            doorShadow = doorCollider.GetComponent<ShadowCaster2D>();
            originalShadowEnabled = doorShadow != null && doorShadow.enabled;
            ResetDoor();
        }

        private void Update()
        {
            if (state == DoorState.Closed && presses > 0 &&
                Time.time - lastPress > mashResetDelay)
                presses = 0;
        }

        public override bool CanInteract(WorldInteractor interactor)
        {
            return ready && state == DoorState.Closed && deathLoop.isActiveAndEnabled &&
                !deathLoop.IsTransitioning;
        }

        public override void Interact(WorldInteractor interactor)
        {
            if (!CanInteract(interactor))
                return;
            if (Time.time - lastPress > mashResetDelay)
                presses = 0;
            lastPress = Time.time;
            presses++;
            if (presses >= pressesToOpen)
            {
                state = DoorState.Opening;
                sequence = StartCoroutine(OpenAndCollapse(interactor));
            }
        }

        private IEnumerator OpenAndCollapse(WorldInteractor interactor)
        {
            Vector3 openPosition = closedDoorPosition + slideOffset;
            float elapsed = 0f;
            while (elapsed < openingSeconds)
            {
                elapsed += Time.deltaTime;
                doorVisual.localPosition = Vector3.Lerp(closedDoorPosition, openPosition,
                    Mathf.SmoothStep(0f, 1f, elapsed / Mathf.Max(openingSeconds, 0.001f)));
                yield return null;
            }
            doorVisual.localPosition = openPosition;
            doorCollider.enabled = false;
            if (doorShadow != null)
                doorShadow.enabled = false;

            state = DoorState.Warning;
            yield return new WaitForSeconds(warningSeconds);
            state = DoorState.Falling;
            ceilingDebris.gameObject.SetActive(true);
            Vector3 startPosition = debrisRestPosition + Vector3.up * fallDistance;
            ceilingDebris.transform.position = startPosition;
            elapsed = 0f;
            while (elapsed < fallSeconds)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / Mathf.Max(fallSeconds, 0.001f));
                ceilingDebris.transform.position = Vector3.Lerp(startPosition, debrisRestPosition,
                    progress * progress);
                Color tint = debrisColor;
                tint.a *= progress;
                ceilingDebris.color = tint;
                yield return null;
            }
            ceilingDebris.transform.position = debrisRestPosition;
            ceilingDebris.color = debrisColor;
            Physics2D.SyncTransforms();

            Collider2D playerCollider = interactor.Player.GetComponent<Collider2D>();
            bool hitPlayer = playerCollider != null &&
                Physics2D.Distance(collapseZone, playerCollider).isOverlapped;
            if (!hitPlayer)
            {
                state = DoorState.Finished;
                sequence = null;
                yield break;
            }

            state = DoorState.WaitingForRespawn;
            int previousDeaths = deathLoop.DeathCount;
            deathLoop.BeginDeath();
            if (!deathLoop.IsTransitioning)
            {
                ResetDoor();
                sequence = null;
                yield break;
            }

            // DeathLoop increments the counter after teleporting while the screen
            // is black. Reset the encounter at that point, before fading back in.
            while (deathLoop.IsTransitioning && deathLoop.DeathCount == previousDeaths)
                yield return null;
            ResetDoor();
            while (deathLoop.IsTransitioning)
                yield return null;
            sequence = null;
        }

        private void ResetDoor()
        {
            presses = 0;
            lastPress = float.NegativeInfinity;
            state = DoorState.Closed;
            doorVisual.localPosition = closedDoorPosition;
            doorCollider.enabled = true;
            if (doorShadow != null)
                doorShadow.enabled = originalShadowEnabled;
            ceilingDebris.transform.position = debrisRestPosition;
            ceilingDebris.color = debrisColor;
            ceilingDebris.gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            if (sequence != null)
            {
                StopCoroutine(sequence);
                sequence = null;
                if (ready)
                    ResetDoor();
            }
        }
    }
}
