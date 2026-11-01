using System.Collections.Generic;
using Jamie;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

public class NoteInteractionHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController player = null;
    [SerializeField] private Light2D flashlight = null;

    [Header("Interaction")]
    [SerializeField] private LayerMask noteLayer = 1 << 3;
    [SerializeField] private LayerMask obstacleLayers = (1 << 0) | (1 << 6);
    [SerializeField, Min(0.1f)] private float maxInteractionDistance = 1.8f;

    private readonly List<Collider2D> nearbyNotes = new List<Collider2D>(8);
    private readonly List<RaycastHit2D> obstacleHits = new List<RaycastHit2D>(8);

    public NoteItem CurrentTarget { get; private set; }

    private void Start()
    {
        if (player == null || flashlight == null)
            Debug.LogError("Assign Player and Flashlight on NoteInteraction.", this);
    }

    private void LateUpdate()
    {
        CurrentTarget = null;

        if (player == null || flashlight == null ||
            !player.isActiveAndEnabled || !flashlight.isActiveAndEnabled ||
            flashlight.intensity <= 0f || Time.timeScale <= 0f ||
            NoteManager.Instance == null)
            return;

        CurrentTarget = FindTarget();

        Keyboard keyboard = Keyboard.current;
        if (CurrentTarget != null && keyboard != null &&
            keyboard.eKey.wasPressedThisFrame)
        {
            NoteManager.Instance.OpenNote(CurrentTarget);
        }
    }

    private NoteItem FindTarget()
    {
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(noteLayer);
        filter.useTriggers = true;

        nearbyNotes.Clear();
        Physics2D.OverlapCircle(
            (Vector2)player.transform.position,
            maxInteractionDistance, filter, nearbyNotes);

        NoteItem bestNote = null;
        float bestDistanceSquared =
            maxInteractionDistance * maxInteractionDistance;

        foreach (Collider2D noteCollider in nearbyNotes)
        {
            NoteItem note = noteCollider.GetComponentInParent<NoteItem>();
            if (note == null || !note.isActiveAndEnabled)
                continue;

            Vector2 target = note.transform.position;
            float distanceSquared =
                (target - (Vector2)player.transform.position).sqrMagnitude;

            if (distanceSquared > bestDistanceSquared ||
                !IsInsideFlashlight(target) || !HasClearPath(note, target))
                continue;

            bestNote = note;
            bestDistanceSquared = distanceSquared;
        }

        return bestNote;
    }

    private bool IsInsideFlashlight(Vector2 target)
    {
        Vector2 direction = target - (Vector2)flashlight.transform.position;
        float radius = flashlight.pointLightOuterRadius;

        if (direction.sqrMagnitude > radius * radius)
            return false;

        if (direction.sqrMagnitude < 0.0001f)
            return true;

        return Vector2.Angle(
            (Vector2)flashlight.transform.up, direction)
            <= flashlight.pointLightOuterAngle * 0.5f;
    }

    private bool HasClearPath(NoteItem note, Vector2 target)
    {
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(obstacleLayers);
        filter.useTriggers = false;

        obstacleHits.Clear();
        Physics2D.Linecast(
            (Vector2)flashlight.transform.position,
            target, filter, obstacleHits);

        foreach (RaycastHit2D hit in obstacleHits)
        {
            Transform hitTransform = hit.collider.transform;

            if (hitTransform == player.transform ||
                hitTransform.IsChildOf(player.transform))
                continue;

            if (hitTransform == note.transform ||
                hitTransform.IsChildOf(note.transform))
                continue;

            return false;
        }

        return true;
    }

    private void OnDisable()
    {
        CurrentTarget = null;
    }
}

