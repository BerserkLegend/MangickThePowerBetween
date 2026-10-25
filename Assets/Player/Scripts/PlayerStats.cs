using System.Collections;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("HP")]
    [Tooltip("HP In One Sector")]
    public int hpPerSector = 5; 
    [Tooltip("Max HP Sectors Unlocked")]
    public int unlockedHpSectors = 3; 
    
    public int currentHealth;

    [Header("Mana")]
    [Tooltip("Mana In One Sector")]
    public float manaPerSector = 3f; 
    [Tooltip("Max Mana Sectors Unlocked")]
    public int unlockedManaSectors = 1; 
    
    public float currentMana; 

    [Header("Balance")]
    public float maxBalance = 100f;
    public float currentBalance;
    public float balanceRegenDelay = 3f; 
    public float balanceRegenRate = 25f; 
    
    [Tooltip("Stun Damage Multiplier")]
    public int stunDamageMultiplier = 2; 
    public float stunDuration = 2.5f;

    public bool isStunned { get; private set; } 
    
    private float lastBalanceDamageTime;
    private PlayerControl playerMovement;

    void Start()
    {
        currentHealth = unlockedHpSectors * hpPerSector; 
        
        currentMana = unlockedManaSectors * manaPerSector; 
        
        currentBalance = maxBalance;
        playerMovement = GetComponent<PlayerControl>();
    }

    void Update()
    {
        if (!isStunned && currentBalance < maxBalance)
        {
            if (Time.time >= lastBalanceDamageTime + balanceRegenDelay)
            {
                currentBalance += balanceRegenRate * Time.deltaTime;
                if (currentBalance > maxBalance) 
                {
                    currentBalance = maxBalance;
                }
            }
        }
    }

    public void TakeDamage(int hpDamage, float balanceDamage)
    {
        if (playerMovement != null && playerMovement.isInvulnerable) return;

        int finalHpDamage = isStunned ? hpDamage * stunDamageMultiplier : hpDamage;
        
        currentHealth -= finalHpDamage;
        Debug.Log($"Получено {finalHpDamage} урона! Осталось HP: {currentHealth} / {unlockedHpSectors * hpPerSector}");

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        if (!isStunned)
        {
            currentBalance -= balanceDamage;
            lastBalanceDamageTime = Time.time;

            if (currentBalance <= 0)
            {
                StartCoroutine(StunRoutine());
            }
        }
    }

    public bool TryConsumeMana(float amount)
    {
        if (currentMana >= amount)
        {
            currentMana -= amount;
            Debug.Log($"Трата маны: -{amount}. Остаток: {currentMana}");
            return true; 
        }
        
        return false; 
    }

    public void Heal(int amount)
    {
        int maxPossibleHp = unlockedHpSectors * hpPerSector;
        currentHealth += amount;
        
        if (currentHealth > maxPossibleHp)
        {
            currentHealth = maxPossibleHp;
        }
    }

    private IEnumerator StunRoutine()
    {
        isStunned = true;
        currentBalance = 0;
        Debug.Log("ИГРОК ОПРОКИНУТ! (Баланс = 0)");

        yield return new WaitForSeconds(stunDuration);

        isStunned = false;
        currentBalance = maxBalance;
        Debug.Log("Игрок снова на ногах.");
    }

    private void Die()
    {
        Debug.Log("ИГРОК УМЕР.");
    }
}