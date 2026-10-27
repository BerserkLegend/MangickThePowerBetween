using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerControl : MonoBehaviour
{
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

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;
    private bool isGrounded;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        stats = GetComponent<PlayerStats>();
    }

    void Update()
    {
        if (isRolling || (stats != null && stats.isStunned)) return; 

        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        if (jumpBufferCounter > 0f)
        {
            jumpBufferCounter -= Time.deltaTime;
        }

        if (jumpBufferCounter > 0f && isGrounded)
        {
            jumpBufferCounter = 0f;
        
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f); 
            rb.AddForce(new Vector2(0, jumpForce), ForceMode2D.Impulse);
        }
        Vector2 screenMousePosition = Mouse.current.position.ReadValue();
        
        Vector3 worldMousePosition = Camera.main.ScreenToWorldPoint(screenMousePosition);

        float facingDirection = worldMousePosition.x > transform.position.x ? 1f : -1f;

        transform.localScale = new Vector3(facingDirection, 1f, 1f);
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
        if (!isRolling) 
        {
            StartCoroutine(RollRoutine());
        }
    }

    private IEnumerator RollRoutine()
    {
        isRolling = true;       
        isInvulnerable = true;  
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
}