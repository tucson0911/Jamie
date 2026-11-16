using TMPro;
using UnityEngine;

namespace Jamie.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class TransitionNumberDisplay : MonoBehaviour
    {
        [SerializeField] private PhaseController phases;
        [SerializeField] private TMP_Text numberLabel;

        private void Awake()
        {
            if (phases == null || numberLabel == null)
            {
                Debug.LogError(
                    "Assign Phases and Number Label.", this);
                enabled = false;
                return;
            }

            Hide();
        }

        private void OnEnable()
        {
            if (phases != null)
                phases.PhaseChanged += Refresh;
        }

        private void OnDisable()
        {
            if (phases != null)
                phases.PhaseChanged -= Refresh;

            Hide();
        }

        public void Show()
        {
            if (!isActiveAndEnabled)
                return;

            numberLabel.text = phases.DisplayNumber.ToString();
            numberLabel.color = Color.red;
            numberLabel.gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (numberLabel != null)
                numberLabel.gameObject.SetActive(false);
        }

        private void Refresh(HospitalPhase phase)
        {
            if (numberLabel != null &&
                numberLabel.gameObject.activeSelf)
            {
                numberLabel.text =
                    phases.DisplayNumber.ToString();
            }
        }
    }
}