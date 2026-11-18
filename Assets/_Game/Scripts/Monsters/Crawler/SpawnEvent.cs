using UnityEngine;

public class EnableObjectOnTrigger2D : MonoBehaviour
{
    [Tooltip("The GameObject you want to enable when the player enters the trigger.")]
    [SerializeField] private GameObject targetObject;

    [Tooltip("If true, this trigger object will disable itself after being activated once.")]
    [SerializeField] private bool disableAfterTrigger = true;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the entering object is tagged as "Player"
        if (other.CompareTag("Player"))
        {
            if (targetObject != null)
            {
                targetObject.SetActive(true);
            }
            else
            {
                Debug.LogWarning("Target Object is not assigned on " + gameObject.name, this);
            }

            if (disableAfterTrigger)
            {
                gameObject.SetActive(false);
            }
        }
    }
}