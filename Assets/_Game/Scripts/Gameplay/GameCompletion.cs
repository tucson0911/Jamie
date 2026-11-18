using UnityEngine;

namespace Jamie.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class GameCompletion : MonoBehaviour
    {
        [SerializeField] private PhaseController phases;

        [SerializeField] private HospitalPhase completeAtPhase =
            HospitalPhase.Third;

        [SerializeField] private GameObject endPanel;

        private float previousTimeScale = 1f;

        public bool IsComplete { get; private set; }

        private void Awake()
        {
            if (phases == null || endPanel == null)
            {
                Debug.LogError(
                    "Assign Phases and End Panel.", this);
                enabled = false;
                return;
            }

            endPanel.SetActive(false);
        }

        public void CheckPhaseCompletion()
        {
            if (!Application.isPlaying ||
                !isActiveAndEnabled ||
                IsComplete ||
                phases.CurrentPhase != completeAtPhase)
            {
                return;
            }

            previousTimeScale = Time.timeScale;
            IsComplete = true;
            endPanel.SetActive(true);
            Time.timeScale = 0f;
        }

        private void OnDisable()
        {
            if (!IsComplete)
                return;

            Time.timeScale = previousTimeScale;
            IsComplete = false;

            if (endPanel != null)
                endPanel.SetActive(false);
        }
    }
}