using UnityEngine;

namespace Jamie.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class PhaseObjects : MonoBehaviour
    {
        [SerializeField] private PhaseController phases;
        [SerializeField] private HospitalPhase firstPhase =
            HospitalPhase.Second;
        [SerializeField] private HospitalPhase lastPhase =
            HospitalPhase.Third;
        [SerializeField] private GameObject[] targets =
            new GameObject[0];

        private void OnEnable()
        {
            if (phases == null)
            {
                Debug.LogError(
                    "Assign Phases on PhaseObjects.", this);
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
            bool visible =
                (int)phase >= (int)firstPhase &&
                (int)phase <= (int)lastPhase;

            foreach (GameObject target in targets)
            {
                if (target == null)
                    continue;

                if (transform.IsChildOf(target.transform))
                {
                    Debug.LogError(
                        "A phase target cannot contain its controller.",
                        this);
                    continue;
                }

                target.SetActive(visible);
            }
        }
    }
}