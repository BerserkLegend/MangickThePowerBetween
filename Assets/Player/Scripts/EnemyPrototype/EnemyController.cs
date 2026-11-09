using System.Collections;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public float moveSpeed = 2f;
    public float attackRange = 1.5f;
    public int attackDamage = 5;
    public float balanceDamage = 20f;
    public float attackCooldown = 2f;

    private Transform player;
    private float lastAttackTime;
    private bool isAttacking = false;

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
    }

    void Update()
    {
        if (player == null || isAttacking) return;

        float distance = Vector2.Distance(transform.position, player.position);

        if (distance > attackRange)
        {
            transform.position = Vector2.MoveTowards(transform.position, player.position, moveSpeed * Time.deltaTime);
        }
        else if (Time.time >= lastAttackTime + attackCooldown)
        {
            StartCoroutine(AttackRoutine());
        }
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;
        
        yield return new WaitForSeconds(0.5f); 

        float distance = Vector2.Distance(transform.position, player.position);
        if (distance <= attackRange) 
        {
            SwordWeapon playerSword = player.GetComponentInChildren<SwordWeapon>();
            
            if (playerSword != null && playerSword.isParrying)
            {
                playerSword.TriggerParry(); 
                Debug.Log("<color=green>ВРАГ: Мою атаку парировали!</color>");
            }
            else
            {
                PlayerStats playerStats = player.GetComponent<PlayerStats>();
                if (playerStats != null)
                {
                    playerStats.TakeDamage(attackDamage, balanceDamage);
                    Debug.Log("<color=red>ВРАГ: Нанес урон игроку!</color>");
                }
            }
        }

        lastAttackTime = Time.time;
        isAttacking = false;
    }
}