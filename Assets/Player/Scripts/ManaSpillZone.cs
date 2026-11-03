using UnityEngine;

public class ManaSpillZone : MonoBehaviour
{
    public float manaToGive = 0.3f;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerStats stats = collision.GetComponent<PlayerStats>();
            if (stats != null)
            {
                stats.AddMana(manaToGive);
                Destroy(gameObject);
            }
        }
    }
}