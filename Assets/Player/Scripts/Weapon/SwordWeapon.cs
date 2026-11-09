using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class SwordWeapon : WeaponBase
{   [SerializeField] private Animator anim;

    [Header("Swipe (Basic)")]
    public float swipeDamage = 10f;
    public float windUpFrames = 6f;
    public float activeFrames = 4f;
    public float recoveryFrames = 10f;
    public Transform attackPoint; 
    public float attackRange = 1.5f;
    public LayerMask enemyLayer;
    public LayerMask foliageLayer;
    public GameObject manaSpillPrefab;

    [Header("Counter Thrust (Special)")]
    public float thrustDamage = 20f;
    public float parryFrames = 8f;
    public float thrustActiveFrames = 5f;
    
    private bool isAttacking = false;
    public bool isParrying { get; private set; }
    private bool parrySuccessful = false;

    private int comboStep = 0;
    private bool continueCombo = false;

    public override void ExecuteBasicAttack()
    {
        if (isAttacking)
        {
            if (comboStep == 1) 
            {
                continueCombo = true;
            }
            return;
        }
        
        StartCoroutine(ComboSwipeRoutine());
    }

    public override void ExecuteSpecialAttack()
    {
        if (isAttacking) return;
        StartCoroutine(CounterThrustRoutine());
    }

    private IEnumerator ComboSwipeRoutine()
    {
        isAttacking = true;
        comboStep = 1;
        continueCombo = false;

        anim.SetTrigger("Attack1");
        AimTowardsMouse();
        
        yield return new WaitForSeconds(windUpFrames / targetFPS);
        
        DealDamage(swipeDamage); 
        yield return new WaitForSeconds(activeFrames / targetFPS);

        float recoveryTimer = 0;
        float maxRecovery = recoveryFrames / targetFPS;

        while (recoveryTimer < maxRecovery)
        {
            if (continueCombo)
            {
                break;
            }
            recoveryTimer += Time.deltaTime;
            yield return null;
        }

        if (continueCombo)
        {
            comboStep = 2;
            continueCombo = false;
            
            anim.SetTrigger("Attack2");
            AimTowardsMouse();
            
            yield return new WaitForSeconds(windUpFrames / targetFPS); 
            
            DealDamage(swipeDamage * 1.5f); 
            
            yield return new WaitForSeconds(activeFrames / targetFPS);
            yield return new WaitForSeconds(recoveryFrames / targetFPS);
        }

        isAttacking = false;
        comboStep = 0;
    }

    private void DealDamage(float currentDamage)
    {
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayer);

        if (hitEnemies.Length > 0)
        {
            stats.MarkCombatActivity();
        }

        foreach (Collider2D enemy in hitEnemies)
        {
            enemy.GetComponent<EnemyStats>()?.TakeDamage(currentDamage, 10f);
            
            if (manaSpillPrefab != null)
            {
                Instantiate(manaSpillPrefab, enemy.transform.position, Quaternion.identity);
            }
        }

        Collider2D[] hitFoliage = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, foliageLayer);
        foreach (Collider2D foliage in hitFoliage)
        {
            Destroy(foliage.gameObject); 
        }
    }

    private IEnumerator CounterThrustRoutine()
    {
        isAttacking = true;
        parrySuccessful = false;
        isParrying = true;

        bool isEmpowered = stats.TryConsumeMana(1f);
        float currentThrustDamage = isEmpowered ? thrustDamage : thrustDamage * 0.5f;

        yield return new WaitForSeconds(parryFrames / targetFPS);
        isParrying = false;

        if (parrySuccessful)
        {
            currentThrustDamage *= 2f;
            Debug.Log("Успешное парирование! Урон удвоен!");
        }

        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayer);
        foreach (Collider2D enemy in hitEnemies)
        {
            enemy.GetComponent<EnemyStats>()?.TakeDamage(currentThrustDamage, 30f);
        }

        yield return new WaitForSeconds(thrustActiveFrames / targetFPS);

        yield return new WaitForSeconds(recoveryFrames / targetFPS);
        
        isAttacking = false;
    }

    public void TriggerParry()
    {
        if (isParrying)
        {
            parrySuccessful = true;
        }
    }

    private void AimTowardsMouse()
    {
        if (Camera.main != null && Mouse.current != null)
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            Vector2 direction = (mousePos - (Vector2)transform.position).normalized;
        }
    }
}