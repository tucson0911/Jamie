using UnityEngine;

public class SkullWanderer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform skullTarget;       // Drag your head_solver/target here
    [SerializeField] private Transform neutralAnchor;    // Drag upper_torso or root bone
    [SerializeField] private VisionSensor visionSensor;  // Drag your external VisionCone object here

    [Header("Wander Settings")]
    [SerializeField] private float wanderRadius = 0.4f;  
    [SerializeField] private float wanderSpeed = 1.5f;   

    [Header("Aggro / Tracking Settings")]
    [SerializeField] private float maxReachDistance = 2.2f; 
    [SerializeField] private float trackSpeed = 8.0f;    

    private float seedX;
    private float seedY;

    void Start()
    {
        seedX = Random.Range(0f, 100f);
        seedY = Random.Range(100f, 200f);

        if (neutralAnchor == null) neutralAnchor = transform;
    }

    void Update()
    {
        if (skullTarget == null || neutralAnchor == null) return;

        // Check state from the external vision sensor
        bool playerDetected = visionSensor != null && visionSensor.IsPlayerDetected;
        Transform targetPlayer = visionSensor != null ? visionSensor.DetectedPlayer : null;

        Vector3 targetPosition;

        if (playerDetected && targetPlayer != null)
        {
            // AGGRO STATE: Reach toward player position
            Vector3 directionToPlayer = targetPlayer.position - neutralAnchor.position;
            
            if (directionToPlayer.magnitude > maxReachDistance)
            {
                directionToPlayer = directionToPlayer.normalized * maxReachDistance;
            }

            targetPosition = neutralAnchor.position + directionToPlayer;
            skullTarget.position = Vector3.Lerp(skullTarget.position, targetPosition, Time.deltaTime * trackSpeed);
        }
        else
        {
            // WANDER STATE: Smooth organic Perlin noise drift
            float noiseX = Mathf.PerlinNoise((Time.time * wanderSpeed) + seedX, 0f) - 0.5f; 
            float noiseY = Mathf.PerlinNoise(0f, (Time.time * wanderSpeed) + seedY) - 0.5f; 

            Vector2 wanderOffset = new Vector2(noiseX, noiseY) * (wanderRadius * 2f);
            targetPosition = neutralAnchor.position + (Vector3)wanderOffset;

            skullTarget.position = Vector3.Lerp(skullTarget.position, targetPosition, Time.deltaTime * 5f);
        }
    }
}