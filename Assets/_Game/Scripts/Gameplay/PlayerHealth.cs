using System;
using UnityEngine;

namespace Jamie.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class PlayerHealth : MonoBehaviour
    {
        [SerializeField, Min(1f)]
        private float maxHealth = 100f;

        [SerializeField, Min(0f)]
        private float minimumHealth = 20f;

        [SerializeField, Min(0f)]
        private float damageCooldownSeconds = 1f;

        private float nextDamageTime;

        public float CurrentHealth { get; private set; }
        public float MaxHealth => maxHealth;

        public float Normalized =>
            Mathf.Clamp01(CurrentHealth / maxHealth);

        public event Action HealthChanged;

        private void Awake()
        {
            maxHealth = Mathf.Max(1f, maxHealth);
            minimumHealth =
                Mathf.Clamp(minimumHealth, 0f, maxHealth);

            CurrentHealth = maxHealth;
        }

        private void Start()
        {
            HealthChanged?.Invoke();
        }

        public void TakeDamage(float amount)
        {
            if (!isActiveAndEnabled ||
                Time.timeScale <= 0f ||
                amount <= 0f ||
                Time.time < nextDamageTime)
            {
                return;
            }

            nextDamageTime =
                Time.time + damageCooldownSeconds;

            CurrentHealth = Mathf.Clamp(
                CurrentHealth - amount,
                minimumHealth,
                maxHealth);

            HealthChanged?.Invoke();
        }

        public void RestoreFull()
        {
            CurrentHealth = maxHealth;

            nextDamageTime =
                Time.time + damageCooldownSeconds;

            HealthChanged?.Invoke();
        }
    }
}