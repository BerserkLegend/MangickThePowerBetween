using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class SwordWeapon : WeaponBase
{
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
    private bool isParrying = false;
    private bool parrySuccessful = false;

    public override void ExecuteBasicAttack()
    {
        if (isAttacking) return;
        StartCoroutine(SwipeRoutine());
    }

    public override void ExecuteSpecialAttack()
    {
        if (isAttacking) return;
        StartCoroutine(CounterThrustRoutine());
    }

    private IEnumerator SwipeRoutine()
    {
        isAttacking = true;

        AimTowardsMouse();

        yield return new WaitForSeconds(windUpFrames / targetFPS);

        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayer);
        foreach (Collider2D enemy in hitEnemies)
        {
            enemy.GetComponent<EnemyStats>()?.TakeDamage(swipeDamage, 10f);
            
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

        yield return new WaitForSeconds(activeFrames / targetFPS);

        yield return new WaitForSeconds(recoveryFrames / targetFPS);
        
        isAttacking = false;
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