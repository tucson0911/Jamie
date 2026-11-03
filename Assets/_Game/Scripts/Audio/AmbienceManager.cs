using UnityEngine;
using FMODUnity;

public class AmbienceManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to the FMOD Studio Event Emitter playing the ambience.")]
    public StudioEventEmitter fmodEmitter;
    [Tooltip("Transform of the player.")]
    public Transform playerTransform;

    [Header("Settings")]
    [Tooltip("Name of the FMOD parameter.")]
    public string parameterName = "scary";

    private AmbienceNode[] allNodes;

    void Start()
    {
        allNodes = FindObjectsOfType<AmbienceNode>();

        if (playerTransform == null)
        {
            var playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null) playerTransform = playerObj.transform;
        }
    }

    void Update()
    {
        if (playerTransform == null || fmodEmitter == null || allNodes.Length == 0) return;

        float finalScaryValue = 0f;

        foreach (var node in allNodes)
        {
            float distance = Vector3.Distance(playerTransform.position, node.transform.position);

            // Safety check to prevent division by zero or inverted radii
            float outerRadius = node.influenceRadius;
            float innerRadius = Mathf.Min(node.innerRadius, outerRadius);

            if (distance <= outerRadius)
            {
                float proximityFactor = 0f;

                if (distance <= innerRadius)
                {
                    // Player is inside the inner cap zone -> full effect
                    proximityFactor = 1f;
                }
                else
                {
                    // Player is between inner and outer radius -> ramp down smoothly
                    float rangeSpan = outerRadius - innerRadius;
                    if (rangeSpan > 0f)
                    {
                        float distanceBeyondInner = distance - innerRadius;
                        proximityFactor = 1f - (distanceBeyondInner / rangeSpan);
                    }
                    else
                    {
                        proximityFactor = 1f;
                    }
                }

                // Apply node weight and accumulate
                float nodeEffect = proximityFactor * node.weight;
                finalScaryValue += nodeEffect;
            }
        }

        // Clamp final value between 0.0 and 1.0
        finalScaryValue = Mathf.Clamp01(finalScaryValue);

        // Send to FMOD
        fmodEmitter.SetParameter(parameterName, finalScaryValue);
    }
}
