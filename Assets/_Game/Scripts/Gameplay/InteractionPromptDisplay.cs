using UnityEngine;
using UnityEngine.UI;

namespace Jamie.Gameplay
{
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    public sealed class InteractionPromptDisplay : MonoBehaviour
    {
        [SerializeField] private WorldInteractor interactor;
        [SerializeField] private Text promptLabel;

        private void Awake()
        {
            if (interactor == null || promptLabel == null)
            {
                Debug.LogError(
                    "Assign Interactor and Prompt Label.", this);
                enabled = false;
            }
        }

        private void LateUpdate()
        {
            if (Time.timeScale <= 0f ||
                !interactor.isActiveAndEnabled)
            {
                promptLabel.text = string.Empty;
                return;
            }

            WorldInteractable target = interactor.CurrentTarget;

            promptLabel.text =
                target != null && target.isActiveAndEnabled
                    ? target.Prompt
                    : string.Empty;
        }

        private void OnDisable()
        {
            if (promptLabel != null)
                promptLabel.text = string.Empty;
        }
    }
}