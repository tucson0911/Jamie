using UnityEngine;
using UnityEngine.InputSystem;

public class NoteInteractionHandler : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private LayerMask noteLayer;
    [SerializeField] private float maxInteractionDistance = 3f;

    private Camera topDownCamera;
    private Transform playerTransform;

    private void Start()
    {
        topDownCamera = Camera.main;
        if (topDownCamera == null) Debug.LogError("Diagnostic: No Camera tagged with 'MainCamera' found!");

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
        else
        {
            Debug.LogError("Diagnostic: No GameObject with the 'Player' tag found!");
        }
    }

    private void Update()
    {
        // 1. Check if the New Input System detects the mouse click at all
        if (Mouse.current == null)
        {
            Debug.LogWarning("Diagnostic: Mouse.current is NULL! Input System is not picking up a mouse.");
            return;
        }

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Debug.Log("Diagnostic: Mouse left click registered!");

            if (topDownCamera == null || playerTransform == null)
            {
                Debug.LogWarning("Diagnostic: Camera or Player reference is missing, skipping raycast.");
                return;
            }

            Ray ray = topDownCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            RaycastHit hit;

            // Temporarily removed noteLayer filter to see EVERYTHING the ray hits
            if (Physics.Raycast(ray, out hit, Mathf.Infinity))
            {
                Debug.Log($"Diagnostic: Raycast hit object named '{hit.collider.gameObject.name}' on layer '{LayerMask.LayerToName(hit.collider.gameObject.layer)}'");

                // Check if it has the NoteItem component
                NoteItem clickedNote = hit.collider.GetComponent<NoteItem>();
                if (clickedNote != null)
                {
                    Debug.Log($"Diagnostic: SUCCESS! Hit a NoteItem named '{clickedNote.noteTitle}'");

                    float distanceToPlayer = Vector3.Distance(playerTransform.position, clickedNote.transform.position);
                    Debug.Log($"Diagnostic: Distance to player is {distanceToPlayer:F2} (Max allowed: {maxInteractionDistance})");

                    if (distanceToPlayer <= maxInteractionDistance)
                    {
                        Debug.Log("Diagnostic: Distance check passed. Opening note!");
                        NoteManager.Instance.OpenNote(clickedNote);
                    }
                    else
                    {
                        Debug.Log("Diagnostic: Distance check FAILED - Player is too far away.");
                    }
                }
                else
                {
                    Debug.Log($"Diagnostic: Hit '{hit.collider.gameObject.name}', but it does NOT have a NoteItem component attached.");
                }
            }
            else
            {
                Debug.Log("Diagnostic: Raycast hit NOTHING. It shot out into empty space or missed all colliders.");
            }
        }
    }
}
