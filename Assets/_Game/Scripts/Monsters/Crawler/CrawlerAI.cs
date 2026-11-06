using UnityEngine;
using UnityEngine.AI; // Required for NavMeshAgent

public class CrawlerAI : MonoBehaviour
{
    [SerializeField] private VisionSensor visionSensor;  
    [SerializeField] private NavMeshAgent agent;       // Reference to the NavMeshAgent

    [SerializeField] private float rotationSpeed = 5.0f;
    [SerializeField] private float aggroRange = 10.0f;

    private bool playerInAggroRange = false;
    private bool huntMode = false;

    void Awake()
    {
        // Automatically grab the NavMeshAgent if not assigned
        if (agent == null) agent = GetComponent<NavMeshAgent>();

        // Crucial for 2D: NavMeshAgent operates in 3D space by default, 
        // so we lock rotation and Z-axis movement if it's a top-down 2D game.
        if (agent != null)
        {
            agent.updateRotation = false; // We handle custom rotation via script
            agent.updateUpAxis = false;    // Keeps it locked to 2D plane
        }
    }

    void Update() 
    {
        bool playerDetected = visionSensor != null && visionSensor.IsPlayerDetected;
        Transform targetPlayer = visionSensor != null ? visionSensor.DetectedPlayer : null;

        if (playerDetected && targetPlayer != null) {
            huntMode = true;
        }

        if (huntMode && targetPlayer != null) {
            HuntPlayer(targetPlayer);
        } else {
            if (agent != null && agent.enabled) agent.isStopped = true;
        }
    }

    public void HuntPlayer(Transform targetPlayer) {
        if (PlayerInAggro(targetPlayer)) {
            FollowPlayer(targetPlayer);
        } else {
            huntMode = false;
            if (agent != null) agent.isStopped = true;
        }
    }

    public bool PlayerInAggro(Transform targetPlayer) {
        if (targetPlayer == null) return false;

        float distance = Vector2.Distance(transform.position, targetPlayer.position);

        if (distance <= aggroRange && !playerInAggroRange) {
            playerInAggroRange = true;
            return true;
        }
        else if (distance > aggroRange && playerInAggroRange) {
            playerInAggroRange = false;
            return false;
        }

        return playerInAggroRange;
    }

    public void FollowPlayer(Transform target) {
        if (agent == null) return;

        // 1. Tell the NavMeshAgent where to go instead of teleporting position directly
        agent.isStopped = false;
        agent.SetDestination(target.position);

        // 2. Rotate smoothly toward the agent's actual *path direction* (or toward the player if close)
        // Using agent.velocity ensures the crawler faces the direction it's actually walking around corners.
        Vector2 moveDir = agent.velocity.sqrMagnitude > 0.01f ? (Vector2)agent.velocity.normalized : ((Vector2)(target.position - transform.position)).normalized;

        if (moveDir != Vector2.zero) {
            // Accounting for default sprite facing down (+90 degrees offset)
            float angle = Mathf.Atan2(moveDir.y, moveDir.x) * Mathf.Rad2Deg + 90f;
            Quaternion targetRotation = Quaternion.Euler(0, 0, angle);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, aggroRange);
    }
}