using FMODUnity;
using UnityEngine;
using UnityEngine.AI;

namespace Jamie.Gameplay
{
    [RequireComponent(typeof(NavMeshAgent))]
    [DisallowMultipleComponent]
    public sealed class MonsterMovementAudio : MonoBehaviour
    {
        [SerializeField] private NavMeshAgent agent;
        [SerializeField] private StudioEventEmitter walkEmitter;
        [SerializeField, Min(0f)] private float minimumSpeed = 0.1f;

        private void Awake()
        {
            if (agent == null)
                agent = GetComponent<NavMeshAgent>();

            if (walkEmitter == null)
            {
                Debug.LogError("Assign the Walk emitter.", this);
                enabled = false;
            }
        }

        private void LateUpdate()
        {
            bool moving =
                Time.timeScale > 0f &&
                agent.isActiveAndEnabled &&
                agent.isOnNavMesh &&
                !agent.isStopped &&
                walkEmitter.isActiveAndEnabled &&
                agent.velocity.sqrMagnitude >
                    minimumSpeed * minimumSpeed;

            if (moving == walkEmitter.IsActive)
                return;

            if (moving)
                walkEmitter.Play();
            else
                walkEmitter.Stop();
        }

        private void OnDisable()
        {
            if (walkEmitter != null)
                walkEmitter.Stop();
        }
    }
}