using UnityEngine;

public class AmbienceNode : MonoBehaviour
{
    [Range(0.0f, 1.0f)]
    public float weight = 1.0f;

    [Header("Radii")]
    [Tooltip("The radius where the effect starts ramping up.")]
    public float influenceRadius = 15f;

    [Tooltip("The inner radius where the effect reaches maximum intensity immediately. Must be smaller than Influence Radius.")]
    public float innerRadius = 5f;

    private void OnDrawGizmos()
    {
        // Outer radius (yellow/red based on weight)
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, influenceRadius);

        // Inner radius (solid red for max intensity zone)
        Gizmos.color = new Color(1f, 0f, 0f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, innerRadius);
    }
}
