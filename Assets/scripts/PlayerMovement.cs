using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 4500f;
    public float maxSpeed = 20f;
    public float counterMovement = 0.175f;
    private float threshold = 0.01f;
    public float maxSlopeAngle = 35f;

    [Header("Crouch & Slide")]
    public float slideForce = 400f;
    public float slideCounterMovement = 0.2f;
    private Vector3 playerScale;
    private Vector3 crouchScale = new Vector3(1f, 0.5f, 1f);

    [Header("Jumping & Gravity")]
    public float jumpForce = 550f;
    public float extraGravity = 2500f;
    public LayerMask whatIsGround;
    private bool readyToJump = true;
    private float jumpCooldown = 0.25f;

    // References & State
    private Rigidbody rb;
    private float xInput, yInput;
    private bool jumping, crouching;
    private bool isGrounded;
    private Vector3 normalVector = Vector3.up;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerScale = transform.localScale;
    }

    void Update()
    {
        ReadNewInputs();
    }

    void FixedUpdate()
    {
        Movement();
    }

   void ReadNewInputs()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        // WASD + Arrow Keys Movement (New Input System)
        xInput = 0f;
        yInput = 0f;

        // Forward (W ya Up Arrow)
        if (kb.wKey.isPressed || kb.upArrowKey.isPressed) yInput += 1f;
        // Backward (S ya Down Arrow)
        if (kb.sKey.isPressed || kb.downArrowKey.isPressed) yInput -= 1f;
        // Right (D ya Right Arrow)
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) xInput += 1f;
        // Left (A ya Left Arrow)
        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) xInput -= 1f;

        // Jump (Space key)
        jumping = kb.spaceKey.isPressed;

        // Slide / Crouch (Left Ctrl)
        if (kb.leftCtrlKey.wasPressedThisFrame)
        {
            StartCrouch();
        }
        else if (kb.leftCtrlKey.wasReleasedThisFrame)
        {
            StopCrouch();
        }

        crouching = kb.leftCtrlKey.isPressed;
    }
    void StartCrouch()
    {
        transform.localScale = crouchScale;
        transform.position = new Vector3(transform.position.x, transform.position.y - 0.5f, transform.position.z);

        // Slide boost forward kick
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

    void Movement()
    {
        // Karlson downward snappy gravity
        rb.AddForce(Vector3.down * Time.deltaTime * extraGravity);

        Vector2 mag = FindVelRelativeToLook();
        CounterMovement(xInput, yInput, mag);

        if (readyToJump && jumping && isGrounded)
        {
            Jump();
        }

        float multiplier = 1f;
        float multiplierV = 1f;

        if (!isGrounded)
        {
            multiplier = 0.5f;
            multiplierV = 0.5f;
        }
        if (isGrounded && crouching)
        {
            multiplierV = 0f;
            multiplier = 0.2f;
        }

        rb.AddForce(transform.forward * yInput * moveSpeed * Time.deltaTime * multiplier * multiplierV);
        rb.AddForce(transform.right * xInput * moveSpeed * Time.deltaTime * multiplier);
    }

    void Jump()
    {
        if (isGrounded && readyToJump)
        {
            readyToJump = false;
            rb.AddForce(Vector2.up * jumpForce * 1.5f);
            rb.AddForce(normalVector * jumpForce * 0.5f);

            Vector3 vel = rb.linearVelocity;
            if (rb.linearVelocity.y < 0.5f)
                rb.linearVelocity = new Vector3(vel.x, 0, vel.z);
            else if (rb.linearVelocity.y > 0)
                rb.linearVelocity = new Vector3(vel.x, vel.y / 2, vel.z);

            Invoke(nameof(ResetJump), jumpCooldown);
        }
    }

    void ResetJump()
    {
        readyToJump = true;
    }

    void CounterMovement(float x, float y, Vector2 mag)
    {
        if (!isGrounded || jumping) return;

        if (crouching)
        {
            rb.AddForce(moveSpeed * Time.deltaTime * -rb.linearVelocity.normalized * slideCounterMovement);
            return;
        }

        if (Mathf.Abs(mag.x) > threshold && Mathf.Abs(x) < 0.05f || (mag.x < -threshold && x > 0) || (mag.x > threshold && x < 0))
        {
            rb.AddForce(moveSpeed * transform.right * Time.deltaTime * -mag.x * counterMovement);
        }
        if (Mathf.Abs(mag.y) > threshold && Mathf.Abs(y) < 0.05f || (mag.y < -threshold && y > 0) || (mag.y > threshold && y < 0))
        {
            rb.AddForce(moveSpeed * transform.forward * Time.deltaTime * -mag.y * counterMovement);
        }

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
            if (IsFloor(normal))
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

    bool IsFloor(Vector3 v)
    {
        float angle = Vector3.Angle(Vector3.up, v);
        return angle < maxSlopeAngle;
    }
}