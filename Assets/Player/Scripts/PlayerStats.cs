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

    [Header("Adrenaline (Sword)")]
    public int currentAdrenaline = 0;
    public int maxAdrenaline = 15; 
    public LayerMask enemyLayer;
    private float adrenalineTimer = 0f;
    private float combatTimer = 0f;
    private Animator anim;

    [Header("Stamina For Roll")]
    public float maxStamina = 100f;
    public float currentStamina;
    [Tooltip("Cost For Roll")]
    public float rollCost = 34f; 
    public float staminaRegenRate = 40f; 
    public float staminaRegenDelay = 0.5f; 
    private float lastStaminaUseTime;

    void Start()
    {
        currentHealth = unlockedHpSectors * hpPerSector; 
        
        currentMana = unlockedManaSectors * manaPerSector; 

        currentStamina = maxStamina;
        
        currentBalance = maxBalance;
        playerMovement = GetComponent<PlayerControl>();
        anim = GetComponent<Animator>();
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

        if (combatTimer > 0)
        {
            combatTimer -= Time.deltaTime;
    
            bool isNearEnemy = Physics2D.OverlapCircle(transform.position, 3f, enemyLayer);
    
            if (isNearEnemy)
            {
                adrenalineTimer += Time.deltaTime;
                if (adrenalineTimer >= 2.5f)
                {
                    adrenalineTimer = 0f;
                    currentAdrenaline++;
                    if (currentAdrenaline > maxAdrenaline) currentAdrenaline = maxAdrenaline;
                    UpdateAnimationSpeed();
                }
            }
            else
            {
                adrenalineTimer = 0f;
            }
        }
        if (currentStamina < maxStamina && Time.time >= lastStaminaUseTime + staminaRegenDelay)
        {
            currentStamina += staminaRegenRate * Time.deltaTime;
            if (currentStamina > maxStamina)
            {
                currentStamina = maxStamina;
            }
        }
    }

    public void TakeDamage(int hpDamage, float balanceDamage)
    {
        if (playerMovement != null && playerMovement.isInvulnerable) return;

        currentAdrenaline = 0; 
        UpdateAnimationSpeed();

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

    public void AddMana(float amount)
    {
        float maxPossibleMana = unlockedManaSectors * manaPerSector;
        currentMana += amount;
    
        if (currentMana > maxPossibleMana)
        {
            currentMana = maxPossibleMana;
        }
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

    public void MarkCombatActivity()
    {
        combatTimer = 3f;
    }

    public void UpdateAnimationSpeed()
    {
        if (anim == null) return;
    
        int boosts = currentAdrenaline / 3;
        float speedMultiplier = 1f + (boosts * 0.05f); 
    
        anim.speed = speedMultiplier;
    }

    public bool TryConsumeStamina()
    {
        if (currentStamina >= rollCost)
        {
            currentStamina -= rollCost;
            lastStaminaUseTime = Time.time;
            return true;
        }
    
        Debug.Log("Не хватает выносливости для переката!");
        return false;
    }
}