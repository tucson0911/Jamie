using UnityEngine;

[ExecuteInEditMode]
public class VisibilityController : MonoBehaviour
{
    public Transform visionPivot; // Points where the flashlight aims

    void Update()
    {
        if (visionPivot == null) return;

        Vector2 playerPos = transform.position;
        Vector2 coneDir = visionPivot.up; // Change to .right if your sprite faces right by default

        // Push values globally to all shaders using these property names
        Shader.SetGlobalVector("_PlayerPos", new Vector4(playerPos.x, playerPos.y, 0, 0));
        Shader.SetGlobalVector("_ConeDir", new Vector4(coneDir.x, coneDir.y, 0, 0));
    }
}