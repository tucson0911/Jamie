using UnityEngine;

public class CrawlerAI : MonoBehaviour
{
    [SerializeField] private VisionSensor visionSensor;  // Drag your external VisionCone object here
    [SerializeField] private float moveSpeed = 3.0f;
    [SerializeField] private float rotationSpeed = 5.0f;
    [SerializeField] private float aggroRange = 10.0f;

    private bool playerInAggroRange = false;
    private bool huntMode = false;

    void Update() {
        bool playerDetected = visionSensor != null && visionSensor.IsPlayerDetected;
        Transform targetPlayer = visionSensor != null ? visionSensor.DetectedPlayer : null;

        if (playerDetected && targetPlayer != null) {
            huntMode = true;
        }

        if (huntMode) {
            HuntPlayer(targetPlayer);
        }
    }


    public void HuntPlayer(Transform targetPlayer) {
        if (PlayerInAggro(targetPlayer)) {
            FollowPlayer(targetPlayer);
        } else {
            huntMode = false;
        }
    }

    public bool PlayerInAggro(Transform targetPlayer) {
        if (targetPlayer == null) return false;

        float distance = Vector2.Distance(transform.position, targetPlayer.position);

        if (distance <= aggroRange && !playerInAggroRange){
            return true;

        }

        else if (distance > aggroRange && playerInAggroRange){
            return false;
        }

        return false;
    }

    public void FollowPlayer(Transform target) {
        // 1. Move toward the player
        transform.position = Vector2.MoveTowards(transform.position, target.position, moveSpeed * Time.deltaTime);

        // 2. Rotate to face the player (accounting for default facing down)
        Vector2 direction = (target.position - transform.position).normalized;

        // Calculate angle and add 90 degrees because the default sprite faces down
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + 90f;

        Quaternion targetRotation = Quaternion.Euler(new Vector3(0, 0, angle));
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, aggroRange);
    }
}
