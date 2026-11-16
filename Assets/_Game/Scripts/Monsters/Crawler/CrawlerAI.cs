using System.Collections.Generic;
using System.Collections;
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
    [SerializeField] private FMODUnity.StudioEventEmitter walkEmitter;

    private bool playerInAggroRange = false;
    private bool huntMode = false;
    private bool playerHuntingEnabled = true;
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

    public void SetPlayerHuntingEnabled(bool value)
    {
        playerHuntingEnabled = value;

        StopAllCoroutines();

        huntMode = false;
        playerInAggroRange = false;
        currentDestination = null;
        isWaiting = false;
        timeOutsideWalkableZone = 0f;

        if (agent != null &&
            agent.isActiveAndEnabled &&
            agent.isOnNavMesh)
        {
            agent.ResetPath();
            agent.isStopped = false;
        }
    }

    void Update()
    {
        if (Time.timeScale <= 0f || agent == null || !agent.isActiveAndEnabled || !agent.isOnNavMesh)
    {
        return;
    }

        bool playerDetected =
            playerHuntingEnabled &&
            visionSensor != null &&
            visionSensor.IsPlayerDetected;

        Transform targetPlayer =
            visionSensor != null
                ? visionSensor.DetectedPlayer
                : null;

        if (playerDetected && targetPlayer != null)
        {
            if (!huntMode)
            {
                StopAllCoroutines();

                isWaiting = false;
                currentDestination = null;
                huntMode = true;
                timeOutsideWalkableZone = 0f;
            }
        }

        if (playerHuntingEnabled && huntMode && targetPlayer != null)
        {
            HuntPlayer(targetPlayer);
        }
        else
        {
            Patrol();
        }

        // Handle continuous footstep/walk audio based on movement
        HandleWalkAudio();
    }

    public void Patrol()
    {
        if (agent == null || isWaiting) return;

        if (currentDestination == null)
        {
            currentDestination = PickRandomNode();

            if (currentDestination == null)
            {
                agent.isStopped = true;
                return;
            }

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
        NavMeshQueryFilter filter = new NavMeshQueryFilter
        {
            agentTypeID = agent.agentTypeID,
            areaMask = agent.areaMask
        };

        bool isPlayerOffMesh = !NavMesh.SamplePosition(targetPlayer.position, out _, 1f, filter);
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

    private void HandleWalkAudio()
    {
        if (walkEmitter == null || agent == null) return;

        // Check if the agent is actively moving (not stopped and has physical velocity)
        bool isMoving = !agent.isStopped && agent.velocity.sqrMagnitude > 0.01f;

        if (isMoving)
        {
            if (!walkEmitter.IsPlaying())
            {
                walkEmitter.Play();
            }
        }
        else
        {
            if (walkEmitter.IsPlaying())
            {
                walkEmitter.Stop();
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, aggroRange);
    }
}
