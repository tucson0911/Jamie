using UnityEngine;

namespace Jamie.Gamefeel
{
    // TextMesh signage is unlit, so physical occlusion is applied separately.
    [RequireComponent(typeof(MeshRenderer))]
    public sealed class GamefeelSign : MonoBehaviour
    {
        public GamefeelDirector director;
        private MeshRenderer sign;

        private void Awake() { sign = GetComponent<MeshRenderer>(); }

        private void LateUpdate()
        {
            if (director == null || director.Player == null) return;
            Vector2 origin = director.Player.transform.position;
            sign.enabled = Vector2.Distance(origin, transform.position) < 16f &&
                director.HasLineOfSight(origin, transform);
        }
    }
}
