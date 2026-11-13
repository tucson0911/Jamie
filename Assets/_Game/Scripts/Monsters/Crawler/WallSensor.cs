using UnityEngine;
using System.Collections.Generic;

public class WallSensor : MonoBehaviour
{
    public enum ForwardAxis { Up, Right, Down, Left }
    public enum LimbPositionType { Front, Back }

    [System.Serializable]
    public class LimbProbe
    {
        public string limbName;
        public Transform shoulderTransform;
        [Tooltip("Local side offset: -1 for Left wall, +1 for Right wall relative to the creature.")]
        public float sideMultiplier = 1f;
        public LimbPositionType positionType = LimbPositionType.Front;
    }

    [Header("Sensor Setup")]
    [SerializeField] private List<LimbProbe> limbs;
    [SerializeField] private ForwardAxis creatureForward = ForwardAxis.Down;
    [SerializeField] private LayerMask wallLayer;

    [Header("Cone Settings")]
    [SerializeField] private float raycastDistance = 4f;
    [SerializeField] private int coneRayCount = 5;
    [SerializeField] private float coneAngleSpan = 60f;
    [SerializeField] private float frontBackStaggerOffset = 1.0f;

    struct BestHitCandidate
    {
        public bool found;
        public Vector3 point;
        public float bestScore;
    }

    public Vector2 GetForwardDir()
    {
        switch (creatureForward)
        {
            case ForwardAxis.Up:    return transform.up;
            case ForwardAxis.Right: return transform.right;
            case ForwardAxis.Down:  return -transform.up;
            case ForwardAxis.Left:  return -transform.right;
            default:                return -transform.up;
        }
    }

    public Vector2 GetLateralDir()
    {
        switch (creatureForward)
        {
            case ForwardAxis.Up:    return transform.right;
            case ForwardAxis.Right: return -transform.up;
            case ForwardAxis.Down:  return -transform.right;
            case ForwardAxis.Left:  return transform.up;
            default:                return -transform.right;
        }
    }

    /// <label>Finds the best valid wall point for a specific limb probe.</label>
    public Vector3 FindStaggeredWallPoint(LimbProbe limb)
    {
        if (limb.shoulderTransform == null) return Vector3.zero;

        Vector2 rayOrigin = (Vector2)limb.shoulderTransform.position;
        Vector2 lateralDir = GetLateralDir();
        Vector2 forwardDir = GetForwardDir();
        Vector2 creatureCenter = (Vector2)transform.position;

        Vector2 baseWallDir = (lateralDir * limb.sideMultiplier).normalized;
        BestHitCandidate bestCandidate = new BestHitCandidate { found = false };

        float angleStep = coneAngleSpan / (coneRayCount - 1);
        float startAngle = -coneAngleSpan / 2f;

        for (int i = 0; i < coneRayCount; i++)
        {
            float currentAngle = startAngle + (angleStep * i);
            Quaternion rotation = Quaternion.Euler(0, 0, currentAngle);
            Vector2 castDir = rotation * baseWallDir;

            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, castDir, raycastDistance, wallLayer);
            if (hit.collider != null)
            {
                Vector2 hitPoint2D = hit.point;
                float hitForwardFromCenter = Vector2.Dot(hitPoint2D - creatureCenter, forwardDir);
                float score = 0f;

                if (limb.positionType == LimbPositionType.Front)
                {
                    score = hitForwardFromCenter;
                }
                else
                {
                    float targetPositionFromCenter = -frontBackStaggerOffset;
                    float distanceToTarget = Mathf.Abs(hitForwardFromCenter - targetPositionFromCenter);
                    score = -distanceToTarget;
                }

                if (!bestCandidate.found || score > bestCandidate.bestScore)
                {
                    bestCandidate.found = true;
                    bestCandidate.point = hit.point;
                    bestCandidate.bestScore = score;
                }
            }
        }

        return bestCandidate.found ? bestCandidate.point : Vector3.zero;
    }

    /// <label>Returns true if at least one limb can successfully find a valid raycast hit on the walls.</label>
    public bool HasValidFooting()
    {
        if (limbs == null || limbs.Count == 0) return false;

        foreach (var limb in limbs)
        {
            Vector3 point = FindStaggeredWallPoint(limb);
            if (point != Vector3.zero)
            {
                return true; // Found at least one viable spot to grab onto
            }
        }
        return false;
    }
}