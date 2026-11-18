using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace Jamie.Gameplay
{
    [RequireComponent(
        typeof(CrawlerAI),
        typeof(NavMeshAgent),
        typeof(MonsterPhasePresence))]
    [DisallowMultipleComponent]
    public sealed class MonsterPeekEncounter : MonoBehaviour
    {
        [SerializeField] private PhaseController phases;
        [SerializeField] private Transform peekPoint;

        [SerializeField]
        private string encounterAreaName = "ScriptedEncounter";

        [SerializeField, Min(0.1f)]
        private float peekSeconds = 1.5f;

        [SerializeField, Min(0.1f)]
        private float placementRadius = 1f;

        private CrawlerAI ai;
        private NavMeshAgent agent;
        private MonsterPhasePresence presence;

        private Coroutine routine;
        private bool ready;
        private bool peeking;
        private bool aiWasEnabled;

        private int encounterAreaBit;
        private int normalAreaMask;
        private Vector3 returnPosition;
        private Quaternion returnRotation;

        public bool HasPlayed { get; private set; }
        public bool IsPlaying => peeking;

        private void Awake()
        {
            ai = GetComponent<CrawlerAI>();
            agent = GetComponent<NavMeshAgent>();
            presence = GetComponent<MonsterPhasePresence>();

            if (phases == null)
                phases = FindFirstObjectByType<PhaseController>();

            int areaIndex =
                NavMesh.GetAreaFromName(encounterAreaName);

            if (phases == null ||
                peekPoint == null ||
                areaIndex < 0)
            {
                Debug.LogError(
                    "Assign Phases and Peek Point, and create " +
                    $"the NavMesh area '{encounterAreaName}'.",
                    this);

                enabled = false;
                return;
            }

            encounterAreaBit = 1 << areaIndex;

            // Reception is excluded outside this encounter.
            normalAreaMask = agent.areaMask & ~encounterAreaBit;
            agent.areaMask = normalAreaMask;

            if (normalAreaMask == 0)
            {
                Debug.LogError(
                    "Enable Walkable in the crawler's Area Mask.",
                    this);

                enabled = false;
                return;
            }

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

            CancelPeek();
        }

        private void OnPhaseChanged(HospitalPhase phase)
        {
            if (phase != HospitalPhase.First)
                CancelPeek();
        }

        public void PlayOnce()
        {
            if (!ready ||
                !isActiveAndEnabled ||
                HasPlayed ||
                peeking ||
                Time.timeScale <= 0f ||
                phases.CurrentPhase != HospitalPhase.First)
            {
                return;
            }

            if (!agent.isActiveAndEnabled ||
                !agent.isOnNavMesh)
            {
                Debug.LogWarning(
                    "The crawler must be on its NavMesh.",
                    this);
                return;
            }

            normalAreaMask =
                agent.areaMask & ~encounterAreaBit;

            NavMeshQueryFilter filter = new NavMeshQueryFilter
            {
                agentTypeID = agent.agentTypeID,
                areaMask = normalAreaMask
            };

            // Allow for the agent's height/base offset when
            // finding its current point on the hallway surface.
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

            filter.areaMask = encounterAreaBit;

            if (!NavMesh.SamplePosition(
                    peekPoint.position,
                    out NavMeshHit peekHit,
                    placementRadius,
                    filter))
            {
                Debug.LogWarning(
                    "Peek Point must be on a baked " +
                    $"'{encounterAreaName}' area.",
                    this);
                return;
            }

            returnPosition = returnHit.position;
            returnRotation = transform.rotation;
            aiWasEnabled = ai.enabled;
            peeking = true;

            ai.SetPlayerHuntingEnabled(false);
            ai.enabled = false;

            agent.ResetPath();
            agent.areaMask =
                normalAreaMask | encounterAreaBit;

            if (!agent.Warp(peekHit.position))
            {
                Debug.LogWarning(
                    "Could not move the crawler to Peek Point.",
                    this);

                CancelPeek();
                return;
            }

            agent.isStopped = true;
            transform.rotation = peekPoint.rotation;

            presence.SetVisible(true);
            HasPlayed = true;

            Debug.Log("[Encounter] Nurse-door glimpse.", this);

            routine = StartCoroutine(Peek());
        }

        private IEnumerator Peek()
        {
            yield return new WaitForSeconds(peekSeconds);

            routine = null;
            CancelPeek();
        }

        public void CancelPeek()
        {
            if (routine != null)
            {
                StopCoroutine(routine);
                routine = null;
            }

            if (!peeking)
                return;

            peeking = false;

            // Hide before returning to the hallway.
            if (presence != null)
                presence.SetVisible(false);

            bool returned = true;

            if (agent != null)
            {
                if (agent.isActiveAndEnabled)
                {
                    if (agent.isOnNavMesh)
                        agent.isStopped = true;

                    // Return while both areas are still permitted.
                    returned = agent.Warp(returnPosition);

                    if (returned)
                        agent.ResetPath();
                }
                else
                {
                    transform.position = returnPosition;
                }

                // Restore the normal restriction afterward.
                agent.areaMask = normalAreaMask;
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

            if (presence != null &&
                presence.isActiveAndEnabled)
            {
                presence.SetVisible(!hiddenPatrol);
            }

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