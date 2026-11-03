using UnityEngine;

public class EnemyStats : MonoBehaviour
{
    public float maxHealth = 50f;
    private float currentHealth;

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float hpDamage, float balanceDamage)
    {
        currentHealth -= hpDamage;
        Debug.Log($"{gameObject.name} получил {hpDamage} урона! Осталось HP: {currentHealth}");


        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log($"{gameObject.name} убит!");
        Destroy(gameObject);
    }
}