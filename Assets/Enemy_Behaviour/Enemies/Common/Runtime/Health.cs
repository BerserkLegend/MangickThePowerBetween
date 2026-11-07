using System;
using UnityEngine;

namespace EnemyBehaviourPrototype
{
    [DisallowMultipleComponent]
    public sealed class Health : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float maxHealth = 5f;
        [SerializeField] private float currentHealth;
        [SerializeField, Min(0f), Tooltip("Seconds to flash after damage.")]
        private float damageFlashDuration = 0.12f;

        public event Action<float, GameObject> OnDamaged;
        public event Action<float, float> OnHealthChanged;

        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public bool IsDead => currentHealth <= 0f;
        public float Fraction => maxHealth > 0f ? Mathf.Clamp01(currentHealth / maxHealth) : 0f;

        private SpriteRenderer spriteRenderer;
        private Color originalColor;
        private float flashUntil;

        private void Awake()
        {
            currentHealth = maxHealth;
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null) originalColor = spriteRenderer.color;
        }

        private void Update()
        {
            if (spriteRenderer != null && Time.time >= flashUntil)
                spriteRenderer.color = originalColor;
        }

        public void OnPrototypeHit(float damage)
        {
            TakeDamage(damage, PrototypeDamageContext.Attacker);
        }

        public void TakeDamage(float damage)
        {
            TakeDamage(damage, null);
        }

        public void TakeDamage(float damage, GameObject attacker)
        {
            if (IsDead || damage <= 0f || float.IsNaN(damage) || float.IsInfinity(damage)) return;

            float appliedDamage = Mathf.Min(currentHealth, damage);
            currentHealth -= appliedDamage;
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.white;
                flashUntil = Time.time + damageFlashDuration;
            }

            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            OnDamaged?.Invoke(appliedDamage, attacker);

            if (IsDead) Destroy(gameObject);
        }

        public float Heal(float amount)
        {
            if (IsDead || amount <= 0f || float.IsNaN(amount) || float.IsInfinity(amount)) return 0f;
            float applied = Mathf.Min(amount, Mathf.Max(0f, maxHealth - currentHealth));
            if (applied <= 0f) return 0f;
            currentHealth += applied;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            return applied;
        }

        public void ResetHealth()
        {
            currentHealth = maxHealth;
            flashUntil = 0f;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        private void OnValidate()
        {
            maxHealth = Mathf.Max(1f, maxHealth);
        }
    }
}
