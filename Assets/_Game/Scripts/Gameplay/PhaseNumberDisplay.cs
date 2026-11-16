using UnityEngine;
using UnityEngine.UI;

namespace Jamie.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class PhaseNumberDisplay : MonoBehaviour
    {
        [SerializeField] private PhaseController phases;
        [SerializeField] private Text numberLabel;

        private void OnEnable()
        {
            if (phases == null || numberLabel == null)
            {
                Debug.LogError(
                    "Assign Phases and Number Label.", this);
                enabled = false;
                return;
            }

            phases.PhaseChanged += Refresh;
            Refresh(phases.CurrentPhase);
        }

        private void Start()
        {
            Refresh(phases.CurrentPhase);
        }

        private void OnDisable()
        {
            if (phases != null)
                phases.PhaseChanged -= Refresh;
        }

        private void Refresh(HospitalPhase phase)
        {
            numberLabel.text = phases.DisplayNumber.ToString();
        }
    }
}