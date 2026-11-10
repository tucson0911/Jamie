using System.Collections.Generic;
using System.Collections;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.AI; // Required for NavMeshAgent

public class CrawlerAI : MonoBehaviour
{
    [SerializeField] private VisionSensor visionSensor;
    [SerializeField] private NavMeshAgent agent;       // Reference to the NavMeshAgent

    [SerializeField] private float rotationSpeed = 5.0f;
    [SerializeField] private float aggroRange = 10.0f;
    [SerializeField] private float lostTargetTimeout = 3.0f; // Time (seconds) allowed outside walkable zone before giving up

    [SerializeField] private List<Transform> patrolNodes;
    [SerializeField] private float timeBeforeIdle = 5.0f;

    private bool playerInAggroRange = false;
    private bool huntMode = false;
    private Transform currentDestination = null;
    private bool isWaiting = false;
    private float timeOutsideWalkableZone = 0f;

    void Awake()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();

        if (agent != null)
        {
            agent.updateRotation = false; 
            agent.updateUpAxis = false;    
        }
    }

    void Update()
    {
        bool playerDetected = visionSensor != null && visionSensor.IsPlayerDetected;
        Transform targetPlayer = visionSensor != null ? visionSensor.DetectedPlayer : null;

        if (playerDetected && targetPlayer != null)
        {
            if (!huntMode)
            {
                StopAllCoroutines();
                isWaiting = false;
                currentDestination = null;
                huntMode = true;
                timeOutsideWalkableZone = 0f; // Reset timer on new detection
            }
        }

        if (huntMode && targetPlayer != null)
        {
            HuntPlayer(targetPlayer);
        }
        else
        {
            Patrol();
        }
    }

    public void Patrol()
    {
        if (agent == null || isWaiting) return;

        if (currentDestination == null)
        {
            currentDestination = PickRandomNode();
            agent.isStopped = false;
            agent.SetDestination(currentDestination.position);
        }

        if (currentDestination != null)
        {
            MoveTo(currentDestination);

            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                StartCoroutine(WaitAndClearDestination());
            }
        }
    }

    private IEnumerator WaitAndClearDestination()
    {
        isWaiting = true;
        agent.isStopped = true; 

        float waitTime = 2f;
        if (currentDestination != null && currentDestination.TryGetComponent<PatrolNode>(out var node))
        {
            waitTime = node.waitTime;
        }

        yield return new WaitForSeconds(waitTime);

        currentDestination = null;
        isWaiting = false;
    }

    public Transform PickRandomNode()
    {
        if (patrolNodes == null || patrolNodes.Count == 0) return null;
        int i = Random.Range(0, patrolNodes.Count);
        return patrolNodes[i];
    }

    public void HuntPlayer(Transform targetPlayer)
    {
        // Check if player is physically off the NavMesh or if the path is entirely invalid
        bool isPlayerOffMesh = !NavMesh.SamplePosition(targetPlayer.position, out _, 1.0f, NavMesh.AllAreas);
        bool isPathInvalid = agent.pathStatus == NavMeshPathStatus.PathInvalid;

        if (isPlayerOffMesh || isPathInvalid || !PlayerInAggro(targetPlayer))
        {
            timeOutsideWalkableZone += Time.deltaTime;

            // If the player stays outside the walkable zone longer than the timeout, give up pursuit
            if (timeOutsideWalkableZone >= lostTargetTimeout)
            {
                huntMode = false;
                playerInAggroRange = false;
                timeOutsideWalkableZone = 0f;
                if (agent != null) agent.isStopped = true;
                return;
            }
        }
        else
        {
            // Reset timer if the player is safely back inside the walkable bounds / reachable
            timeOutsideWalkableZone = 0f;
        }

        // Continue pursuing
        agent.isStopped = false;
        agent.SetDestination(targetPlayer.position);
        MoveTo(targetPlayer);
    }

    public bool PlayerInAggro(Transform targetPlayer)
    {
        if (targetPlayer == null) return false;

        float distance = Vector2.Distance(transform.position, targetPlayer.position);

        if (distance <= aggroRange && !playerInAggroRange)
        {
            playerInAggroRange = true;
            return true;
        }
        else if (distance > aggroRange && playerInAggroRange)
        {
            playerInAggroRange = false;
            return false;
        }

        return playerInAggroRange;
    }

    public void MoveTo(Transform target)
    {
        if (agent == null || target == null) return;

        Vector2 moveDir = agent.velocity.sqrMagnitude > 0.01f ? (Vector2)agent.velocity.normalized : ((Vector2)(target.position - transform.position)).normalized;

        if (moveDir != Vector2.zero)
        {
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