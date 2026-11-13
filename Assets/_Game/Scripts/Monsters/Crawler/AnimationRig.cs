using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CorridorCrawler : MonoBehaviour
{
    public enum LimbPositionType { Front, Back }
    [Header("Front Limb Settings")]
    [SerializeField] private float frontLookaheadDistance = 0.8f;

    [System.Serializable]
    public class Limb
    {
        public string limbName;
        public Transform targetTransform;    
        public Transform shoulderTransform;  
        
        [Tooltip("Local side offset: -1 for Left wall, +1 for Right wall relative to the creature.")]
        public float sideMultiplier = 1f; 
        public LimbPositionType positionType = LimbPositionType.Front;
        
        [HideInInspector] public Vector3 currentPos;
        [HideInInspector] public Vector3 targetPos;
        [HideInInspector] public bool isStepping = false;
        
    }

    [Header("References")]
    [SerializeField] private WallSensor wallSensor;

    [Header("Limb Setup")]
    [SerializeField] private List<Limb> limbs;

    [Header("Gait & Movement Settings")]
    [SerializeField] private float stepThreshold = 0.4f;  
    [SerializeField] private float stepDuration = 0.1f;  

    [Header("Gait Rhythm & Concurrency")]
    [Tooltip("How many limbs are allowed to step at the exact same time.")]
    [SerializeField] private int maxSimultaneousSteps = 1; 

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
        if (wallSensor == null) return;

        Vector2 forwardDir = wallSensor.GetForwardDir();

        int activeSteps = 0;
        foreach (var l in limbs)
        {
            if (l.isStepping) activeSteps++;
        }

        foreach (var limb in limbs)
        {
            if (limb.targetTransform == null || limb.shoulderTransform == null) continue;

            // Map internal Limb structure to WallSensor's LimbProbe structure
            WallSensor.LimbProbe probe = new WallSensor.LimbProbe
            {
                shoulderTransform = limb.shoulderTransform,
                sideMultiplier = limb.sideMultiplier,
                positionType = (WallSensor.LimbPositionType)limb.positionType
            };

            Vector3 bestWallPoint = wallSensor.FindStaggeredWallPoint(probe);

            if (bestWallPoint == Vector3.zero) 
            {
                bestWallPoint = limb.targetPos; 
            } 
            else if (limb.positionType == LimbPositionType.Front)
            {
                // Project the front limb's target further up the corridor
                bestWallPoint += (Vector3)forwardDir * frontLookaheadDistance;
            }

            Vector2 shoulderPos = limb.shoulderTransform.position;
            float forwardOffset = Vector2.Dot((Vector2)limb.targetPos - shoulderPos, forwardDir);
            
            bool hasFallenBehind = (limb.positionType == LimbPositionType.Front && forwardOffset < -0.2f);
            float distance = Vector3.Distance(limb.currentPos, bestWallPoint);

            bool needsStep = (distance > stepThreshold || hasFallenBehind);

            if (!limb.isStepping && needsStep && activeSteps < maxSimultaneousSteps)
            {
                StartCoroutine(ExecuteStep(limb, bestWallPoint));
                activeSteps++;
            }

            if (!limb.isStepping)
            {
                limb.targetTransform.position = limb.currentPos;
            }
        }
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
}