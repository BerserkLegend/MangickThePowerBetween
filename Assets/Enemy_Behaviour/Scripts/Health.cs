using UnityEngine;

namespace EnemyBehaviourPrototype
{
    [DisallowMultipleComponent]
    public sealed class Health : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float maxHealth = 5f;
        [SerializeField] private float currentHealth;

        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public bool IsDead => currentHealth <= 0f;

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
            TakeDamage(damage);
        }

        public void TakeDamage(float damage)
        {
            if (IsDead || damage <= 0f || float.IsNaN(damage) || float.IsInfinity(damage)) return;

            currentHealth = Mathf.Max(0f, currentHealth - damage);
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.white;
                flashUntil = Time.time + 0.12f;
            }

            if (IsDead) Destroy(gameObject);
        }

        private void OnValidate()
        {
            maxHealth = Mathf.Max(1f, maxHealth);
        }
    }
}
