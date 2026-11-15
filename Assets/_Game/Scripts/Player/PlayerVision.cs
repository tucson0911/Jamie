using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Jamie
{
    [DisallowMultipleComponent]
    public sealed class PlayerVision : MonoBehaviour
    {
        [SerializeField] private Light2D visionCone;

        [SerializeField]
        private LayerMask obstacleLayers =
            (1 << 0) | (1 << 6) | (1 << 7);

        private readonly List<RaycastHit2D> hits = new();

        public Vector2 Origin =>
            visionCone != null
                ? (Vector2)visionCone.transform.position
                : (Vector2)transform.position;

        public Vector2 Forward =>
            visionCone != null
                ? (Vector2)visionCone.transform.up
                : (Vector2)transform.up;

        private void Awake()
        {
            if (visionCone == null)
            {
                Debug.LogError(
                    "Assign VisionCone on PlayerVision.",
                    this);

                enabled = false;
            }
        }

        public bool CanSeePoint(Vector2 point)
        {
            if (!isActiveAndEnabled ||
                visionCone == null ||
                !visionCone.isActiveAndEnabled)
            {
                return false;
            }

            Vector2 direction = point - Origin;
            float range = visionCone.pointLightOuterRadius;

            if (direction.sqrMagnitude > range * range)
                return false;

            if (direction.sqrMagnitude > 0.0001f &&
                Vector2.Angle(Forward, direction) >
                visionCone.pointLightOuterAngle * 0.5f)
            {
                return false;
            }

            ContactFilter2D filter = new();
            filter.SetLayerMask(obstacleLayers);
            filter.useTriggers = false;

            hits.Clear();

            Physics2D.Linecast(
                Origin,
                point,
                filter,
                hits);

            foreach (RaycastHit2D hit in hits)
            {
                Transform obstacle = hit.transform;

                if (obstacle == transform ||
                    obstacle.IsChildOf(transform))
                {
                    continue;
                }

                return false;
            }

            return true;
        }
    }
}