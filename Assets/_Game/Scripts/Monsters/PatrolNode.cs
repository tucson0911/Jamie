using UnityEngine;

public class PatrolNode : MonoBehaviour
{
    [Tooltip("How long the AI should wait at this node before moving on.")]
    public float waitTime = 5f;
    
    // Optional: draw a gizmo in the Scene view so you can see nodes easily
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}