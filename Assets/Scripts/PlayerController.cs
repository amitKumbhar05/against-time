using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CapsuleCollider2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 14f;
    [SerializeField] private int maxJumps = 2;
    [SerializeField] private float jumpCutMultiplier = 0.5f;
    [SerializeField] private float jumpCoyoteTime = 0.2f;
    [SerializeField] private float jumpBufferTime = 0.2f;

    [Header("Gravity")]
    [SerializeField] private float gravityScale = 3f;
    [SerializeField] private float fallGravityMultiplier = 1.5f;
    [SerializeField] private float maxFallSpeed = 20f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    // Components
    private Rigidbody2D rb;
    
    // Input variables
    private Vector2 moveInput;
    private bool jumpPressed = false;
    private bool jumpReleased = true;
    
    // Jump variables
    private int jumpsRemaining;
    private float lastGroundedTime;
    private float lastJumpPressedTime;
    
    // State checks
    private bool isGrounded;
    private bool wasGrounded;

    /* ───── Input-System Callbacks ─────────────────── */
    public void OnMove(InputAction.CallbackContext ctx)
        => moveInput = ctx.ReadValue<Vector2>();

    public void OnJump(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
        {
            jumpPressed = true;
            lastJumpPressedTime = jumpBufferTime;
        }
        
        if (ctx.canceled)
        {
            jumpPressed = false;
            jumpReleased = true;
            OnJumpUp();
        }
    }
    /* ──────────────────────────────────────────────── */

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = gravityScale;
        jumpsRemaining = maxJumps;
    }

    private void Update()
    {
        #region Timers
        if (lastGroundedTime > 0)
            lastGroundedTime -= Time.deltaTime;
            
        if (lastJumpPressedTime > 0)
            lastJumpPressedTime -= Time.deltaTime;
        #endregion

        #region Ground Check - PROPERLY FIXED
        wasGrounded = isGrounded;
        
        // Create contact filter to only detect ground layer
        ContactFilter2D contactFilter = new ContactFilter2D();
        contactFilter.SetLayerMask(groundLayer);
        contactFilter.useLayerMask = true;
        contactFilter.useTriggers = false;
        
        // Get all colliders we're hitting
        Collider2D[] results = new Collider2D[5];
        int hitCount = Physics2D.OverlapCircle(groundCheck.position, groundRadius, contactFilter, results);
        
        // CRITICAL FIX: Only consider grounded if:
        // 1. We're hitting ground objects AND
        // 2. Our velocity is very small (not falling or jumping)
        isGrounded = hitCount > 0 && Mathf.Abs(rb.linearVelocity.y) < 0.5f;
        
        // Enhanced debug info
        // Debug.Log($"IsGrounded: {isGrounded} | HitCount: {hitCount} | VelocityY: {rb.linearVelocity.y:F4} | Position: {transform.position}");
        
        // Show what we're hitting
        if (hitCount > 0)
        {
            string hitNames = "";
            for (int i = 0; i < hitCount && i < results.Length; i++)
            {
                if (results[i] != null)
                {
                    hitNames += results[i].name + " ";
                }
            }
            // Debug.Log($"HIT OBJECTS: {hitNames}");
        }
        #endregion

        #region Ground/Air Transition
        // Just landed
        if (!wasGrounded && isGrounded)
        {
            jumpsRemaining = maxJumps;
            jumpReleased = true;
            lastGroundedTime = 0;
            Debug.Log("✅ LANDED! Jumps reset");
        }

        // Just left ground
        if (wasGrounded && !isGrounded)
        {
            lastGroundedTime = jumpCoyoteTime;
            Debug.Log("🛫 LEFT GROUND! Coyote time: " + jumpCoyoteTime);
        }
        #endregion

        #region Jump Logic
        bool wantsToJump = lastJumpPressedTime > 0;
        bool canGroundJump = (isGrounded || lastGroundedTime > 0) && jumpsRemaining > 0;
        bool canAirJump = !isGrounded && jumpsRemaining > 0 && jumpReleased;
        
        if (wantsToJump && jumpReleased && (canGroundJump || canAirJump))
        {
            Jump();
            jumpReleased = false;
            lastJumpPressedTime = 0;
        }
        #endregion

        #region Gravity
        if (rb.linearVelocity.y < 0)
        {
            rb.gravityScale = gravityScale * fallGravityMultiplier;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Max(rb.linearVelocity.y, -maxFallSpeed));
        }
        else if (rb.linearVelocity.y > 0 && !jumpPressed)
        {
            rb.gravityScale = gravityScale * fallGravityMultiplier;
        }
        else
        {
            rb.gravityScale = gravityScale;
        }
        #endregion
    }

    private void FixedUpdate()
    {
        // Simple direct movement
        rb.linearVelocity = new Vector2(moveInput.x * moveSpeed, rb.linearVelocity.y);
    }

    private void Jump()
    {
        lastGroundedTime = 0;
        
        // Reset Y velocity before jumping
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
        
        // Apply jump force
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        
        // Use a jump
        jumpsRemaining--;
        
        Debug.Log($"🚀 JUMPED! Jumps remaining: {jumpsRemaining}");
    }

    private void OnJumpUp()
    {
        if (rb.linearVelocity.y > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
        }
    }

    private void OnDrawGizmos()
{
    if (!groundCheck) return;

    // Draw ground check sphere in scene even when not selected
    Gizmos.color = isGrounded ? new Color(0, 1, 0, 0.5f) : new Color(1, 0, 0, 0.5f);
    Gizmos.DrawSphere(groundCheck.position, groundRadius);

    // Draw line to show where it's positioned relative to player
    Gizmos.color = Color.yellow;
    Gizmos.DrawLine(transform.position, groundCheck.position);

    // Label for real-time debugging
    if (Application.isPlaying)
    {
        Vector3 labelPos = transform.position + Vector3.up * 1.5f;
        string debugText = $"Jumps: {jumpsRemaining}/{maxJumps}\n" +
                          $"Grounded: {isGrounded}\n" +
                          $"VelY: {rb.linearVelocity.y:F2}\n" +
                          $"GC Pos: {groundCheck.localPosition}";
        UnityEditor.Handles.Label(labelPos, debugText);
    }
}
}