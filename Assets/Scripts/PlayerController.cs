using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundLayer;
    
    [Header("Movement Settings")]
    [SerializeField] private float speed = 8f;
    [SerializeField] private float jumpingPower = 16f;
    [SerializeField] private float groundCheckRadius = 0.2f;
    
    [Header("Coyote Time")]
    [SerializeField] private float coyoteTime = 0.2f;
    private float coyoteTimeCounter;
    
    [Header("Jump Buffer")]
    [SerializeField] private float jumpBufferTime = 0.2f;
    private float jumpBufferCounter;
    
    // Private variables
    private float horizontal;
    private bool isFacingRight = true;
    private bool heldJump;
    private bool wasGrounded;
    
    void Start()
    {
        // Get component if not assigned
        if(rb == null)
            rb = GetComponent<Rigidbody2D>();
    }
    
    void Update()
    {
        // Update coyote time
        if(IsGrounded())
        {
            coyoteTimeCounter = coyoteTime;
            wasGrounded = true;
        }
        else
        {
            if(wasGrounded) // Just left ground
            {
                wasGrounded = false;
            }
            coyoteTimeCounter -= Time.deltaTime;
        }
        
        // Update jump buffer
        if(jumpBufferCounter > 0f)
        {
            jumpBufferCounter -= Time.deltaTime;
        }
        
        HandleJump();
        HandleFlip();
    }
    
    void FixedUpdate()
    {
        // Apply horizontal movement in FixedUpdate for consistent physics
        rb.linearVelocity = new Vector2(horizontal * speed, rb.linearVelocity.y);
    }
    
    public void Move(InputAction.CallbackContext context)
    {
        horizontal = context.ReadValue<Vector2>().x;
    }
    
    public void Jump(InputAction.CallbackContext context)
    {
        if(context.performed)
        {
            heldJump = true;
            jumpBufferCounter = jumpBufferTime;
        }
        
        if(context.canceled)
        {
            heldJump = false;
        }
    }
    
    private void HandleJump()
    {
        // Execute jump if buffer and coyote time allow
        if(jumpBufferCounter > 0f && coyoteTimeCounter > 0f)
        {
            DoJump();
            jumpBufferCounter = 0f;
            coyoteTimeCounter = 0f; // Prevent double jumps
        }
        
        // Variable jump height - reduce velocity if jump released
        if(!heldJump && rb.linearVelocity.y > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f);
        }
    }
    
    private void DoJump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpingPower);
    }
    
    private void HandleFlip()
    {
        if(horizontal > 0f && !isFacingRight)
        {
            Flip();
        }
        else if(horizontal < 0f && isFacingRight)
        {
            Flip();
        }
    }
    
    private void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 localScale = transform.localScale;
        localScale.x *= -1f;
        transform.localScale = localScale;
    }
    
    private bool IsGrounded()
    {
        if(groundCheck == null) return false;
        return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }
    
    // Optional: Visual debugging
    void OnDrawGizmosSelected()
    {
        if(groundCheck != null)
        {
            Gizmos.color = IsGrounded() ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}