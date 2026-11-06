using UnityEngine;

public class VisionSensor : MonoBehaviour
{
    [Header("Vision Parameters")]
    [SerializeField] private float visionRange = 5.0f;
    [SerializeField] private float visionAngle = 90f;
    [SerializeField] private Transform playerTransform; // Optional: Auto-finds by tag "Player"

    [Header("Obstruction Settings")]
    [SerializeField] private LayerMask obstacleLayer; // Assign your "Wall" layer here in the Inspector

    [Header("Debug Visualization")]
    [SerializeField] private bool showGizmos = true;

    // Public properties so other scripts can easily read the state
    public bool IsPlayerDetected { get; private set; } = false;
    public Transform DetectedPlayer => playerTransform;

    void Start()
    {
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) playerTransform = playerObj.transform;
        }
    }

    void Update()
    {
        IsPlayerDetected = CheckVision();
    }

    bool CheckVision()
    {
        if (playerTransform == null) return false;

        Vector2 sensorPos = transform.position;
        Vector2 playerPos = playerTransform.position;

        // 1. Check distance
        float distanceToPlayer = Vector2.Distance(sensorPos, playerPos);
        if (distanceToPlayer > visionRange) return false;

        // 2. Check angle using this object's independent rotation/up axis
        Vector2 sensorForward = transform.up;
        Vector2 dirToPlayer = (playerPos - sensorPos).normalized;

        float angle = Vector2.Angle(sensorForward, dirToPlayer);
        if (angle > visionAngle / 2f) return false; // Player is outside the cone angle

        // 3. Check line of sight (Raycast to ensure no wall is blocking)
        // We cast a ray from the sensor to the player, checking against the obstacle layer
        RaycastHit2D hit = Physics2D.Raycast(sensorPos, dirToPlayer, distanceToPlayer, obstacleLayer);

        // If the ray hits something on the obstacle layer before reaching the player, a wall is blocking view
        if (hit.collider != null)
        {
            return false;
        }

        return true; // Player is inside the cone AND visible (not blocked by a wall)
    }

    // ==========================================
    // EDITOR GIZMOS
    // ==========================================
    void OnDrawGizmos()
    {
        if (!showGizmos) return;

        Vector3 origin = transform.position;
        Vector3 forward = transform.up;

        // Draw vision range circle
        Gizmos.color = IsPlayerDetected ? Color.red : new Color(0f, 1f, 1f, 0.3f);
        Gizmos.DrawWireSphere(origin, visionRange);

        // Draw vision cone lines based on this object's rotation
        Quaternion leftRayRot = Quaternion.Euler(0, 0, visionAngle / 2f);
        Quaternion rightRayRot = Quaternion.Euler(0, 0, -visionAngle / 2f);

        Vector3 leftDir = leftRayRot * forward;
        Vector3 rightDir = rightRayRot * forward;

        Gizmos.color = IsPlayerDetected ? Color.red : Color.yellow;
        Gizmos.DrawRay(origin, leftDir * visionRange);
        Gizmos.DrawRay(origin, rightDir * visionRange);

        // Optional: Draw line to player if assigned to visualize line-of-sight test in editor
        if (playerTransform != null)
        {
            Gizmos.color = IsPlayerDetected ? Color.green : Color.gray;
            Gizmos.DrawLine(origin, playerTransform.position);
        }
    }
}
