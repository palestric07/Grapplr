using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 4500f;
    public float maxSpeed = 20f;
    public float counterMovement = 0.175f;
    public float maxSlopeAngle = 35f;

    [Header("Crouch & Slide")]
    public float slideForce = 400f;
    public float slideCounterMovement = 0.2f;
    private Vector3 playerScale;
    private Vector3 crouchScale = new Vector3(1f, 0.5f, 1f);

    [Header("Slope Slide")]
    public float slopeSlideForce = 2500f;

    [Header("Jumping & Gravity")]
    public float jumpForce = 550f;
    public float normalGravity = 1200f;
    public LayerMask whatIsGround;
    private bool readyToJump = true;
    private float jumpCooldown = 0.2f;

    [Header("Karlson Wallrun & Wall Jump")]
    public LayerMask whatIsWall;
    public float wallCheckDistance = 1.3f;
    public float wallRunGravity = 150f;
    public float wallJumpSideForce = 22f;
    public float wallJumpUpForce = 13f;
    public float wallJumpForwardForce = 12f;

    private Rigidbody rb;
    private float xInput, yInput;
    private bool isGrounded;
    private bool isWallRunning;
    private RaycastHit wallHitLeft;
    private RaycastHit wallHitRight;
    private bool wallLeft;
    private bool wallRight;
    private Vector3 normalVector = Vector3.up;
    private bool crouching;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerScale = transform.localScale;
    }

    void Update()
    {
        ReadInputs();
        CheckWalls();
    }

    void FixedUpdate()
    {
        ApplyMovement();
    }

    void ReadInputs()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        xInput = 0f;
        yInput = 0f;

        if (kb.wKey.isPressed || kb.upArrowKey.isPressed) yInput += 1f;
        if (kb.sKey.isPressed || kb.downArrowKey.isPressed) yInput -= 1f;
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) xInput += 1f;
        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) xInput -= 1f;

        if (kb.spaceKey.wasPressedThisFrame)
        {
            if (isGrounded && readyToJump)
            {
                Jump();
            }
            else if (isWallRunning && readyToJump)
            {
                ExecuteWallJump();
            }
        }

        if (kb.leftCtrlKey.wasPressedThisFrame)
            StartCrouch();
        else if (kb.leftCtrlKey.wasReleasedThisFrame)
            StopCrouch();

        crouching = kb.leftCtrlKey.isPressed;
    }

    void CheckWalls()
    {
        wallLeft = Physics.Raycast(transform.position, -transform.right, out wallHitLeft, wallCheckDistance, whatIsWall);
        wallRight = Physics.Raycast(transform.position, transform.right, out wallHitRight, wallCheckDistance, whatIsWall);

        if ((wallLeft || wallRight) && !isGrounded)
        {
            isWallRunning = true;
        }
        else
        {
            isWallRunning = false;
        }
    }

    void ExecuteWallJump()
    {
        readyToJump = false;

        Vector3 wallNormal = wallRight ? wallHitRight.normal : wallHitLeft.normal;
        rb.linearVelocity = new Vector3(rb.linearVelocity.x * 0.2f, 0f, rb.linearVelocity.z * 0.2f);

        Vector3 jumpDirection = (wallNormal * wallJumpSideForce) + (Vector3.up * wallJumpUpForce) + (transform.forward * wallJumpForwardForce);
        rb.AddForce(jumpDirection, ForceMode.VelocityChange);

        Invoke(nameof(ResetJump), jumpCooldown);
    }

    void ApplyMovement()
    {
        if (isWallRunning)
        {
            rb.AddForce(Vector3.down * Time.deltaTime * wallRunGravity);
        }
        else
        {
            rb.AddForce(Vector3.down * Time.deltaTime * normalGravity);
        }

        Vector2 mag = FindVelRelativeToLook();
        CounterMovement(xInput, yInput, mag);

        float slopeAngle = Vector3.Angle(Vector3.up, normalVector);
        bool onSlope = slopeAngle > 5f && slopeAngle < maxSlopeAngle;

        // Dhalan par massive acceleration
        if (crouching && isGrounded && onSlope)
        {
            Vector3 slopeDirection = Vector3.ProjectOnPlane(Vector3.down, normalVector).normalized;
            rb.AddForce(slopeDirection * slopeSlideForce * Time.deltaTime, ForceMode.Acceleration);
        }

        float multiplier = isGrounded ? 1f : 0.6f;
        if (isGrounded && crouching) multiplier = 0.2f;

        rb.AddForce(transform.forward * yInput * moveSpeed * Time.deltaTime * multiplier);
        rb.AddForce(transform.right * xInput * moveSpeed * Time.deltaTime * multiplier);
    }

    void Jump()
    {
        readyToJump = false;
        rb.AddForce(Vector2.up * jumpForce * 1.5f);
        rb.AddForce(normalVector * jumpForce * 0.5f);

        Vector3 vel = rb.linearVelocity;
        if (rb.linearVelocity.y < 0.5f)
            rb.linearVelocity = new Vector3(vel.x, 0, vel.z);

        Invoke(nameof(ResetJump), jumpCooldown);
    }

    void ResetJump()
    {
        readyToJump = true;
    }

    void StartCrouch()
    {
        transform.localScale = crouchScale;
        transform.position = new Vector3(transform.position.x, transform.position.y - 0.5f, transform.position.z);

        if (rb.linearVelocity.magnitude > 0.5f && isGrounded)
        {
            rb.AddForce(transform.forward * slideForce);
        }
    }

    void StopCrouch()
    {
        transform.localScale = playerScale;
        transform.position = new Vector3(transform.position.x, transform.position.y + 0.5f, transform.position.z);
    }

    void CounterMovement(float x, float y, Vector2 mag)
    {
        if (!isGrounded || isWallRunning) return;

        // Slide ke doran friction kam rakhna aur speed cap na lagana
        if (crouching)
        {
            rb.AddForce(moveSpeed * Time.deltaTime * -rb.linearVelocity.normalized * slideCounterMovement);
            return;
        }

        if (Mathf.Abs(mag.x) > 0.01f && Mathf.Abs(x) < 0.05f || (mag.x < -0.01f && x > 0) || (mag.x > 0.01f && x < 0))
        {
            rb.AddForce(moveSpeed * transform.right * Time.deltaTime * -mag.x * counterMovement);
        }
        if (Mathf.Abs(mag.y) > 0.01f && Mathf.Abs(y) < 0.05f || (mag.y < -0.01f && y > 0) || (mag.y > 0.01f && y < 0))
        {
            rb.AddForce(moveSpeed * transform.forward * Time.deltaTime * -mag.y * counterMovement);
        }

        // Sirf aam chalte waqt maxSpeed limit active hogi
        if (Mathf.Sqrt(Mathf.Pow(rb.linearVelocity.x, 2) + Mathf.Pow(rb.linearVelocity.z, 2)) > maxSpeed)
        {
            float fallspeed = rb.linearVelocity.y;
            Vector3 n = rb.linearVelocity.normalized * maxSpeed;
            rb.linearVelocity = new Vector3(n.x, fallspeed, n.z);
        }
    }

    public Vector2 FindVelRelativeToLook()
    {
        float lookAngle = transform.eulerAngles.y;
        float moveAngle = Mathf.Atan2(rb.linearVelocity.x, rb.linearVelocity.z) * Mathf.Rad2Deg;

        float u = Mathf.DeltaAngle(lookAngle, moveAngle);
        float v = 90 - u;

        float magnitude = rb.linearVelocity.magnitude;
        float yMag = magnitude * Mathf.Cos(u * Mathf.Deg2Rad);
        float xMag = magnitude * Mathf.Cos(v * Mathf.Deg2Rad);

        return new Vector2(xMag, yMag);
    }

    void OnCollisionStay(Collision other)
    {
        int layer = other.gameObject.layer;
        if ((whatIsGround.value & (1 << layer)) == 0) return;

        for (int i = 0; i < other.contactCount; i++)
        {
            Vector3 normal = other.contacts[i].normal;
            if (Vector3.Angle(Vector3.up, normal) < maxSlopeAngle)
            {
                isGrounded = true;
                normalVector = normal;
            }
        }
    }

    void OnCollisionExit(Collision other)
    {
        isGrounded = false;
    }
}