using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerControl : MonoBehaviour
{
    private Vector3 originalScale;
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float runSpeed = 10f;
    
    [Header("Roll Frame Data")]
    [SerializeField] private float rollForce = 20f;
    [SerializeField] private float activeFrames = 8f;   
    [SerializeField] private float recoveryFrames = 15f; 
    [SerializeField] private float targetFPS = 60f;      

    public bool isInvulnerable;   
    private Rigidbody2D rb;
    private float horizontalInput;
    private bool isRunning;
    private bool isRolling; 
    private PlayerStats stats;

    [Header("Jump & Buffer Settings")]
    [SerializeField] private float jumpForce = 12f;
    [SerializeField] private float jumpBufferTime = 0.2f;
    private float jumpBufferCounter;
    [SerializeField] private float coyoteTime = 0.15f;
    private float coyoteTimeCounter;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;
    private bool isGrounded;

    [Header("Weapons")]
    public WeaponBase primaryWeapon;
    public WeaponBase secondaryWeapon;

    private Animator anim;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        stats = GetComponent<PlayerStats>();
        originalScale = transform.localScale;
        anim = GetComponent<Animator>();
        if (primaryWeapon != null)
        {
            primaryWeapon.Initialize(stats, this);
        }
        if (secondaryWeapon != null)
        {
            secondaryWeapon.Initialize(stats, this);
        }
    }

    void Update()
    {
        if (isRolling || (stats != null && stats.isStunned)) return; 

        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        if (jumpBufferCounter > 0f)
        {
            jumpBufferCounter -= Time.deltaTime;
        }

        if (jumpBufferCounter > 0f && coyoteTimeCounter > 0f)
        {
            jumpBufferCounter = 0f;
            coyoteTimeCounter = 0f; 
        
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f); 
            rb.AddForce(new Vector2(0, jumpForce), ForceMode2D.Impulse);
        }

        anim.SetFloat("Speed", Mathf.Abs(horizontalInput));
        anim.SetBool("isGrounded", isGrounded);
        anim.SetFloat("yVelocity", rb.linearVelocity.y);
        
        if (Camera.main != null && Mouse.current != null)
        {
            Vector2 screenMousePosition = Mouse.current.position.ReadValue();
            Vector3 worldMousePosition = Camera.main.ScreenToWorldPoint(screenMousePosition);
    
            float facingDirection = worldMousePosition.x > transform.position.x ? 1f : -1f;
    
            transform.localScale = new Vector3(facingDirection * Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
        }
    }

    void OnMove(InputValue value)
    {
        horizontalInput = value.Get<float>();
    }

    void OnRun(InputValue value)
    {
        isRunning = value.isPressed;
    }

    void OnJump(InputValue value)
    {
    if (value.isPressed)
        {
            jumpBufferCounter = jumpBufferTime;
        }
    }

    void OnRoll(InputValue value)
    {
        if (!isRolling && stats != null && !stats.isStunned)
        {
            if (stats.TryConsumeStamina())
            {
                StartCoroutine(RollRoutine());
            }
        }
    }

    private IEnumerator RollRoutine()
    {
        isRolling = true;       
        isInvulnerable = true;
        anim.SetTrigger("Roll");
        Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Enemy"), true);

        float rollDirection;
        if (horizontalInput != 0) 
        {
            rollDirection = Mathf.Sign(horizontalInput);
        }
        else 
        {
            rollDirection = -Mathf.Sign(transform.localScale.x);
        }

        rb.linearVelocity = new Vector2(rollDirection * rollForce, rb.linearVelocity.y);

        yield return new WaitForSeconds(activeFrames / targetFPS);

        isInvulnerable = false; 
        Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Enemy"), false);
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

        yield return new WaitForSeconds(recoveryFrames / targetFPS);
        isRolling = false; 
    }

    void FixedUpdate()
    {
        if (isRolling) return; 

        float currentSpeed = isRunning ? runSpeed : walkSpeed;
        rb.linearVelocity = new Vector2(horizontalInput * currentSpeed, rb.linearVelocity.y);
    }
    void OnPrimaryAttack(InputValue value)
    {
        if (value.isPressed && primaryWeapon != null)
        {
            primaryWeapon.ExecuteBasicAttack();
        }
    }
    void OnSecondaryAttack(InputValue value)
    {
        if (value.isPressed && primaryWeapon != null)
        {
            primaryWeapon.ExecuteSpecialAttack();
        }
    }
}