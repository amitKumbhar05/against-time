using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;



public class PlayerController : MonoBehaviour
{
    public Rigidbody2D rb;
    public Transform groundCheck;
    public LayerMask groundLayer;
    private float horizontal;
    private float speed = 8f;
    private float jumpingPower = 16f;
    private bool isFacingRight = true;


    // cayote time
    private float cayoteTime = 0.2f;
    private float cayoteTimeCounter;

    // jump buffer
    private float jumpBufferTime = 0.2f;
    private float jumpBufferCounter; 
    private bool heldJump;
    void Update()
    {
        if(IsGrounded())
        {
            cayoteTimeCounter = cayoteTime;
        }
        else
        {
            cayoteTimeCounter -= Time.deltaTime;
        }

        rb.linearVelocity = new Vector2(horizontal * speed, rb.linearVelocity.y);

        HandleJump();
        
        if(!isFacingRight && horizontal>0f)
        {
            Flip();
        }
        else if(isFacingRight && horizontal<0f)
        {
            Flip();
        }
    }

    public void Jump(InputAction.CallbackContext context)
    {
        if(context.performed && cayoteTimeCounter > 0f)
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
        if(jumpBufferCounter > 0f)
        {
            jumpBufferCounter -= Time.deltaTime;
        }
        if(cayoteTimeCounter > 0f && jumpBufferCounter > 0f)
        {
            DoJump();
            jumpBufferCounter = 0f;
        }

        if(!heldJump && rb.linearVelocity.y > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f);
            cayoteTimeCounter = 0f;
        }
        
    }

    private void DoJump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpingPower);
    }

    private bool IsGrounded()
    {
        return Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer);
    }

    private void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 localScale = transform.localScale;
        localScale.x *= -1f;
        transform.localScale = localScale;
    }

    public void Move(InputAction.CallbackContext context)
    {
        horizontal = context.ReadValue<Vector2>().x;
    }
}