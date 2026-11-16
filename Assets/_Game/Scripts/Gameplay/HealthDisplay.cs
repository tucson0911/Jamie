using UnityEngine;
using UnityEngine.UI;

namespace Jamie.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class HealthDisplay : MonoBehaviour
    {
        [SerializeField] private PlayerHealth health;
        [SerializeField] private Image healthFill;
        [SerializeField] private Text healthLabel;

        private void OnEnable()
        {
            if (health == null ||
                healthFill == null ||
                healthLabel == null)
            {
                Debug.LogError(
                    "Assign Health, Health Fill and Health Label.",
                    this);
                enabled = false;
                return;
            }

            health.HealthChanged += Refresh;
            Refresh();
        }

        private void Start()
        {
            Refresh();
        }

        private void OnDisable()
        {
            if (health != null)
                health.HealthChanged -= Refresh;
        }

        private void Refresh()
        {
            healthFill.fillAmount = health.Normalized;

            healthFill.color = Color.Lerp(
                new Color(0.65f, 0.12f, 0.08f),
                new Color(0.55f, 0.66f, 0.48f),
                health.Normalized);

            healthLabel.text =
                $"HEALTH  {Mathf.RoundToInt(health.CurrentHealth)}";
        }
    }
}