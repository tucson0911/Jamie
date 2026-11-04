using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CorridorCrawler : MonoBehaviour
{
    public enum ForwardAxis { Up, Right, Down, Left }
    public enum LimbPositionType { Front, Back } 

    [System.Serializable]
    public class Limb
    {
        public string limbName;
        public Transform targetTransform;    
        public Transform shoulderTransform;  
        
        [Tooltip("Local side offset: -1 for Left wall, +1 for Right wall relative to the creature.")]
        public float sideMultiplier = 1f; 

        [Tooltip("Is this a front limb or back limb?")]
        public LimbPositionType positionType = LimbPositionType.Front;
        
        [HideInInspector] public Vector3 currentPos;
        [HideInInspector] public Vector3 targetPos;
        [HideInInspector] public bool isStepping = false;
    }

    [Header("Limb Setup")]
    [SerializeField] private List<Limb> limbs;

    [Header("Gait & Movement Settings")]
    [SerializeField] private ForwardAxis creatureForward = ForwardAxis.Down; 
    [SerializeField] private float stepThreshold = 0.4f;  
    [SerializeField] private float stepDuration = 0.1f;  
    [SerializeField] private LayerMask wallLayer;         

    [Header("Cone & Stagger Settings")]
    [SerializeField] private float raycastDistance = 4f;  
    [SerializeField] private int coneRayCount = 5;        
    [SerializeField] private float coneAngleSpan = 60f;   
    [SerializeField] private float frontBackStaggerOffset = 1.0f; 

    [Header("Gait Rhythm & Concurrency")]
    [Tooltip("How many limbs are allowed to step at the exact same time. Set to 1 for a strict one-by-one crawl.")]
    [SerializeField] private int maxSimultaneousSteps = 1; 

    [Header("Debug Visualization")]
    [SerializeField] private bool showDebugGizmos = true; 

    Vector2 GetForwardDir()
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

    Vector2 GetLateralDir()
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

    void Start()
    {
        foreach (var limb in limbs)
        {
            if (limb.targetTransform != null)
            {
                limb.currentPos = limb.targetTransform.position;
                limb.targetPos = limb.targetTransform.position;
            }
        }
    }

    void Update()
    {
        Vector2 forwardDir = GetForwardDir();

        // 1. Count how many limbs are currently stepping right now to prevent simultaneous bunching
        int activeSteps = 0;
        foreach (var l in limbs)
        {
            if (l.isStepping) activeSteps++;
        }

        foreach (var limb in limbs)
        {
            if (limb.targetTransform == null || limb.shoulderTransform == null) continue;

            Vector3 bestWallPoint = FindStaggeredWallPoint(limb);

            if (bestWallPoint == Vector3.zero) 
            {
                bestWallPoint = limb.targetPos; 
            }

            Vector2 shoulderPos = limb.shoulderTransform.position;
            float forwardOffset = Vector2.Dot((Vector2)limb.targetPos - shoulderPos, forwardDir);
            
            bool hasFallenBehind = (limb.positionType == LimbPositionType.Front && forwardOffset < -0.2f);
            float distance = Vector3.Distance(limb.currentPos, bestWallPoint);

            bool needsStep = (distance > stepThreshold || hasFallenBehind);

            // 2. ONLY start a step if we need one, aren't stepping, and haven't exceeded max simultaneous steps
            if (!limb.isStepping && needsStep && activeSteps < maxSimultaneousSteps)
            {
                StartCoroutine(ExecuteStep(limb, bestWallPoint));
                activeSteps++; // Track locally so subsequent limbs wait their turn this frame
            }

            if (!limb.isStepping)
            {
                limb.targetTransform.position = limb.currentPos;
            }
        }
    }

    Vector3 FindStaggeredWallPoint(Limb limb)
    {
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

    struct BestHitCandidate
    {
        public bool found;
        public Vector3 point;
        public float bestScore;
    }

    IEnumerator ExecuteStep(Limb limb, Vector3 newTargetPos)
    {
        limb.isStepping = true;
        Vector3 startPos = limb.currentPos;
        float elapsed = 0f;

        while (elapsed < stepDuration)
        {
            elapsed += UnityEngine.Time.deltaTime;
            float t = elapsed / stepDuration;

            limb.currentPos = Vector3.Lerp(startPos, newTargetPos, t);
            limb.targetTransform.position = limb.currentPos;

            yield return null;
        }

        limb.currentPos = newTargetPos;
        limb.targetPos = newTargetPos;
        limb.isStepping = false;
    }

    // ==========================================
    // EDITOR GIZMOS (DEBUG VISUALIZATION)
    // ==========================================
    void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;

        Vector2 forwardDir = GetForwardDir();
        Vector2 lateralDir = GetLateralDir();

        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, (Vector3)forwardDir * 1.5f);

        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, (Vector3)lateralDir * 1.5f);

        if (limbs == null) return;

        foreach (var limb in limbs)
        {
            if (limb.shoulderTransform == null) continue;

            Vector3 origin = limb.shoulderTransform.position;
            Vector2 baseWallDir = (lateralDir * limb.sideMultiplier).normalized;

            float angleStep = coneAngleSpan / (coneRayCount - 1);
            float startAngle = -coneAngleSpan / 2f;

            for (int i = 0; i < coneRayCount; i++)
            {
                float currentAngle = startAngle + (angleStep * i);
                Vector3 castDir = Quaternion.Euler(0, 0, currentAngle) * (Vector3)baseWallDir;

                Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.4f); 
                Gizmos.DrawLine(origin, origin + (castDir * raycastDistance));
            }

            if (limb.targetTransform != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(limb.targetTransform.position, 0.2f);
            }
        }
    }
}