using UnityEngine;
using UnityEngine.AI;

namespace Jamie.Gameplay
{
    [RequireComponent(
        typeof(CrawlerAI),
        typeof(NavMeshAgent),
        typeof(MonsterPhasePresence))]
    [DisallowMultipleComponent]
    public sealed class MonsterScriptedChase : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PhaseController phases;
        [SerializeField] private PlayerController player;
        [SerializeField] private DeathLoopController deathLoop;
        [SerializeField] private Transform chaseStartPoint;

        [Header("Navigation")]
        [SerializeField]
        private string chaseAreaName = "ScriptedChase";

        [SerializeField, Min(0.1f)]
        private float placementRadius = 1f;

        [Header("Timing")]
        [SerializeField, Min(0f)]
        private float revealSeconds = 1.5f;

        [SerializeField, Min(0.1f)]
        private float maxChaseSeconds = 18f;

        [Header("Movement")]
        [SerializeField, Min(0.1f)]
        private float startSpeed = 3.5f;

        [SerializeField, Min(0.1f)]
        private float endSpeed = 7.5f;

        [SerializeField, Min(0.1f)]
        private float speedRampSeconds = 6f;

        private CrawlerAI ai;
        private NavMeshAgent agent;
        private MonsterPhasePresence presence;
        private MonsterPeekEncounter peek;

        private bool ready;
        private bool hasPlayed;
        private bool caught;
        private bool aiWasEnabled;

        private int chaseAreaBit;
        private int normalAreaMask;

        private float elapsed;
        private float normalSpeed;
        private float normalAcceleration;
        private float normalStoppingDistance;

        private Vector3 returnPosition;
        private Quaternion returnRotation;

        public bool IsRunning { get; private set; }

        private void Awake()
        {
            ai = GetComponent<CrawlerAI>();
            agent = GetComponent<NavMeshAgent>();
            presence = GetComponent<MonsterPhasePresence>();
            peek = GetComponent<MonsterPeekEncounter>();

            if (phases == null)
                phases = FindFirstObjectByType<PhaseController>();

            int areaIndex = NavMesh.GetAreaFromName(chaseAreaName);

            if (phases == null ||
                player == null ||
                deathLoop == null ||
                chaseStartPoint == null ||
                areaIndex < 0)
            {
                Debug.LogError(
                    "Assign the chase references and create " +
                    $"the NavMesh area '{chaseAreaName}'.", this);

                enabled = false;
                return;
            }

            chaseAreaBit = 1 << areaIndex;
            ready = true;
        }

        private void OnEnable()
        {
            if (ready)
                phases.PhaseChanged += OnPhaseChanged;
        }

        private void OnDisable()
        {
            if (phases != null)
                phases.PhaseChanged -= OnPhaseChanged;

            CancelChase();
        }

        private void OnPhaseChanged(HospitalPhase phase)
        {
            if (phase != HospitalPhase.First)
                CancelChase();
        }

        public void BeginChase()
        {
            if (!ready ||
                !isActiveAndEnabled ||
                IsRunning ||
                hasPlayed ||
                Time.timeScale <= 0f ||
                deathLoop.IsTransitioning ||
                !deathLoop.isActiveAndEnabled ||
                phases.CurrentPhase != HospitalPhase.First)
            {
                return;
            }

            if (peek != null && peek.IsPlaying)
                return;

            if (!agent.isActiveAndEnabled || !agent.isOnNavMesh)
            {
                Debug.LogWarning(
                    "The crawler must be on its NavMesh.", this);
                return;
            }

            normalAreaMask = agent.areaMask & ~chaseAreaBit;

            NavMeshQueryFilter filter = new NavMeshQueryFilter
            {
                agentTypeID = agent.agentTypeID,
                areaMask = normalAreaMask
            };

            float returnSearchRadius = Mathf.Max(
                placementRadius,
                agent.height + Mathf.Abs(agent.baseOffset));

            if (!NavMesh.SamplePosition(
                    transform.position,
                    out NavMeshHit returnHit,
                    returnSearchRadius,
                    filter))
            {
                Debug.LogWarning(
                    "Could not find a hallway return position.",
                    this);
                return;
            }

            filter.areaMask = chaseAreaBit;

            if (!NavMesh.SamplePosition(
                    chaseStartPoint.position,
                    out NavMeshHit startHit,
                    placementRadius,
                    filter))
            {
                Debug.LogWarning(
                    "Chase Start Point must be on the baked " +
                    $"'{chaseAreaName}' area.", this);
                return;
            }

            returnPosition = returnHit.position;
            returnRotation = transform.rotation;
            aiWasEnabled = ai.enabled;

            normalSpeed = agent.speed;
            normalAcceleration = agent.acceleration;
            normalStoppingDistance = agent.stoppingDistance;

            IsRunning = true;
            caught = false;
            elapsed = 0f;

            ai.SetPlayerHuntingEnabled(false);
            ai.enabled = false;

            agent.ResetPath();
            agent.areaMask = normalAreaMask | chaseAreaBit;

            if (!agent.Warp(startHit.position))
            {
                Debug.LogWarning(
                    "Could not place the crawler for the chase.",
                    this);

                CancelChase();
                return;
            }

            agent.speed = startSpeed;
            agent.acceleration = Mathf.Max(normalAcceleration, 20f);
            agent.stoppingDistance = 0f;
            agent.isStopped = true;

            transform.rotation = chaseStartPoint.rotation;
            presence.SetVisible(true);

            hasPlayed = true;
            Debug.Log("[Chase] Generator chase started.", this);
        }

        private void Update()
        {
            if (!IsRunning ||
                caught ||
                Time.timeScale <= 0f ||
                deathLoop.IsTransitioning)
            {
                return;
            }

            elapsed += Time.deltaTime;

            if (elapsed < revealSeconds)
                return;

            float chaseTime = elapsed - revealSeconds;

            if (chaseTime >= maxChaseSeconds)
            {
                CatchPlayer();
                return;
            }

            if (!agent.isActiveAndEnabled || !agent.isOnNavMesh)
                return;

            float progress = Mathf.Clamp01(
                chaseTime / Mathf.Max(0.1f, speedRampSeconds));

            agent.speed = Mathf.Lerp(startSpeed, endSpeed, progress);
            agent.isStopped = false;

            NavMeshQueryFilter filter = new NavMeshQueryFilter
            {
                agentTypeID = agent.agentTypeID,
                areaMask = agent.areaMask
            };

            if (NavMesh.SamplePosition(
                    player.transform.position,
                    out NavMeshHit playerHit,
                    1f,
                    filter))
            {
                agent.SetDestination(playerHit.position);
            }

            // Reuse Mikkel's facing behaviour while his
            // normal patrol/hunting update is paused.
            ai.MoveTo(player.transform);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (!isActiveAndEnabled ||
                !IsRunning ||
                caught ||
                elapsed < revealSeconds ||
                Time.timeScale <= 0f ||
                other.isTrigger)
            {
                return;
            }

            if (other.GetComponentInParent<PlayerController>() == player)
                CatchPlayer();
        }

        private void CatchPlayer()
        {
            if (!IsRunning ||
                caught ||
                Time.timeScale <= 0f ||
                phases.CurrentPhase != HospitalPhase.First)
            {
                return;
            }

            deathLoop.BeginDeath();

            if (!deathLoop.IsTransitioning)
                return;

            caught = true;

            if (agent.isActiveAndEnabled && agent.isOnNavMesh)
            {
                agent.ResetPath();
                agent.isStopped = true;
            }

            Debug.Log("[Chase] Scripted death triggered.", this);
        }

        public void CancelChase()
        {
            if (!IsRunning)
                return;

            IsRunning = false;
            caught = false;

            if (presence != null)
                presence.SetVisible(false);

            bool returned = true;

            if (agent != null)
            {
                if (agent.isActiveAndEnabled)
                {
                    if (agent.isOnNavMesh)
                        agent.isStopped = true;

                    returned = agent.Warp(returnPosition);

                    if (returned)
                        agent.ResetPath();
                }
                else
                {
                    transform.position = returnPosition;
                }

                agent.areaMask = normalAreaMask;
                agent.speed = normalSpeed;
                agent.acceleration = normalAcceleration;
                agent.stoppingDistance = normalStoppingDistance;
            }

            transform.rotation = returnRotation;

            if (!returned)
            {
                if (ai != null)
                    ai.enabled = false;

                Debug.LogError(
                    "Could not return the crawler to the hallway. " +
                    "Its AI has been stopped.", this);
                return;
            }

            bool hiddenPatrol =
                phases != null &&
                phases.CurrentPhase == HospitalPhase.First;

            if (presence != null && presence.isActiveAndEnabled)
                presence.SetVisible(!hiddenPatrol);

            if (ai != null)
            {
                ai.SetPlayerHuntingEnabled(!hiddenPatrol);
                ai.enabled = aiWasEnabled;
            }

            if (agent != null &&
                agent.isActiveAndEnabled &&
                agent.isOnNavMesh)
            {
                agent.isStopped = false;
            }
        }
    }
}