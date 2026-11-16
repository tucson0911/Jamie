using UnityEngine;

namespace Jamie.Gameplay
{
    [DefaultExecutionOrder(-200)]
    [DisallowMultipleComponent]
    public sealed class PlayerStartSpawn : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        [SerializeField] private Transform spawnPoint;

        private void Awake()
        {
            if (player == null || spawnPoint == null)
            {
                Debug.LogError(
                    "Assign Player and Spawn Point.", this);
                return;
            }

            player.RespawnAt((Vector2)spawnPoint.position);
        }
    }
}