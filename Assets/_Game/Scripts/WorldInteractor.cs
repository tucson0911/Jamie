using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Jamie
{
    [DisallowMultipleComponent]
    public sealed class WorldInteractor : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        [SerializeField] private PlayerFlashlight flashlight;

        [Header("Interaction")]
        [SerializeField, Min(0.1f)]
        private float interactionDistance = 1.6f;

        [SerializeField, Range(30f, 360f)]
        private float interactionAngle = 160f;

        [SerializeField]
        private LayerMask targetLayers =
            (1 << 0) | (1 << 3) | (1 << 6);

        [SerializeField]
        private LayerMask obstacleLayers =
            (1 << 0) | (1 << 6) | (1 << 7);

        private readonly List<Collider2D> nearby = new();
        private readonly List<RaycastHit2D> hits = new();

        public PlayerController Player => player;
        public bool HasFlashlight =>
            flashlight != null && flashlight.HasFlashlight;

        public bool FlashlightOn =>
            flashlight != null && flashlight.IsOn;

        public WorldInteractable CurrentTarget { get; private set; }

        private void Awake()
        {
            if (player == null)
                player = GetComponent<PlayerController>();

            if (flashlight == null)
                flashlight = GetComponent<PlayerFlashlight>();

            if (player == null || flashlight == null)
            {
                Debug.LogError(
                    "Assign Player and Flashlight on WorldInteractor.",
                    this);

                enabled = false;
            }
        }

        private void LateUpdate()
        {
            CurrentTarget = null;

            if (Time.timeScale <= 0f ||
                !player.isActiveAndEnabled)
            {
                return;
            }

            ContactFilter2D filter = new();
            filter.SetLayerMask(targetLayers);
            filter.useTriggers = true;

            nearby.Clear();

            Physics2D.OverlapCircle(
                player.transform.position,
                interactionDistance,
                filter,
                nearby);

            float bestDistance =
                interactionDistance * interactionDistance;

            foreach (Collider2D collider in nearby)
            {
                WorldInteractable target =
                    collider.GetComponentInParent<WorldInteractable>();

                if (target == null ||
                    !target.isActiveAndEnabled ||
                    !target.CanInteract(this))
                {
                    continue;
                }

                if (target.RequiresFlashlight && !FlashlightOn)
                    continue;

                Vector2 offset =
                    target.transform.position - player.transform.position;

                if (offset.sqrMagnitude > bestDistance)
                    continue;

                if (Vector2.Angle(player.FacingDirection, offset) >
                    interactionAngle * 0.5f)
                {
                    continue;
                }

                if (!HasClearPath(target))
                    continue;

                CurrentTarget = target;
                bestDistance = offset.sqrMagnitude;
            }

            Keyboard keyboard = Keyboard.current;

            if (CurrentTarget != null &&
                keyboard != null &&
                keyboard.eKey.wasPressedThisFrame)
            {
                CurrentTarget.Interact(this);
            }
        }

        private bool HasClearPath(WorldInteractable target)
        {
            ContactFilter2D filter = new();
            filter.SetLayerMask(obstacleLayers);
            filter.useTriggers = false;

            hits.Clear();

            Physics2D.Linecast(
                player.transform.position,
                target.transform.position,
                filter,
                hits);

            foreach (RaycastHit2D hit in hits)
            {
                Transform obstacle = hit.transform;

                if (obstacle == player.transform ||
                    obstacle.IsChildOf(player.transform) ||
                    obstacle == target.transform ||
                    obstacle.IsChildOf(target.transform) ||
                    target.transform.IsChildOf(obstacle))
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        public void CollectFlashlight()
        {
            flashlight.Collect();
        }

        private void OnDisable()
        {
            CurrentTarget = null;
        }
    }
}