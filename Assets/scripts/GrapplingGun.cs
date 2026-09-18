using UnityEngine;
using UnityEngine.InputSystem;

public class GrapplingGun : MonoBehaviour
{
    [Header("References")]
    public Transform gunTip;
    public Transform cameraTransform;
    public Transform player;
    public LayerMask whatIsGrappleable;
    public Camera playerCam;

    [Header("Spider-Man Swing Physics")]
    public float maxDistance = 120f;
    public float swingThrust = 40f;          // W press karne par swing speed boost
    public float steerForce = 25f;           // A/D se hawa mein turn/revolve hona
    public float pullTowardsAnchor = 15f;    // Hook point ki taraf tight radial pull
    public float releaseLaunchMultiplier = 1.35f; // Release par cube ke upar phenkne wala slingshot force
    public float upwardLaunchBonus = 5f;

    [Header("Rope Animation")]
    public int quality = 300;
    public float damperRope = 12f;
    public float strengthRope = 700f;
    public float waveHeight = 1.2f;
    public float waveCount = 2.5f;

    private LineRenderer lr;
    private Vector3 grapplePoint;
    private float currentRopeLength;
    private bool isGrappling = false;
    private Rigidbody rb;
    private Vector3 currentGrapplePosition;

    private float springPos = 0f;
    private float springVelocity = 0f;

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
        lr.positionCount = 0;
        rb = player.GetComponent<Rigidbody>();
    }

    void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        if (mouse.rightButton.wasPressedThisFrame)
        {
            StartGrapple();
        }
        else if (mouse.rightButton.wasReleasedThisFrame)
        {
            StopGrapple();
        }

        if (playerCam != null)
        {
            float targetFov = isGrappling ? 80f : 60f;
            playerCam.fieldOfView = Mathf.Lerp(playerCam.fieldOfView, targetFov, Time.deltaTime * 6f);
        }
    }

    void FixedUpdate()
    {
        if (!isGrappling || rb == null) return;

        ApplySpiderManSwingPhysics();
    }

    void LateUpdate()
    {
        DrawRope();
    }

    void StartGrapple()
    {
        RaycastHit hit;
        if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out hit, maxDistance, whatIsGrappleable))
        {
            grapplePoint = hit.point;
            currentRopeLength = Vector3.Distance(player.position, grapplePoint);
            isGrappling = true;

            // Centripetal lock: Inward falling velocity ko cancel karke tangential direction mein shift karna
            Vector3 radialVector = (player.position - grapplePoint).normalized;
            Vector3 currentVel = rb.linearVelocity;
            Vector3 tangentialVel = Vector3.ProjectOnPlane(currentVel, radialVector);

            // Initial forward impulse for instant fluid swing
            rb.linearVelocity = tangentialVel + (cameraTransform.forward * 8f);

            lr.positionCount = quality + 1;
            currentGrapplePosition = gunTip.position;
            springPos = 0f;
            springVelocity = 0f;
        }
    }

   void ApplySpiderManSwingPhysics()
    {
        Vector3 toAnchor = grapplePoint - player.position;
        float distance = toAnchor.magnitude;
        Vector3 anchorDir = toAnchor.normalized;

        // 1. Inward Radial Force (Pendulum Tension maintain karna)
        if (distance > currentRopeLength)
        {
            float stretch = distance - currentRopeLength;
            rb.AddForce(anchorDir * (stretch * 45f + pullTowardsAnchor), ForceMode.Acceleration);

            Vector3 velAlongRope = Vector3.Project(rb.linearVelocity, anchorDir);
            if (Vector3.Dot(velAlongRope, anchorDir) < 0f)
            {
                rb.linearVelocity -= velAlongRope * 0.5f;
            }
        }

        // 2. User Input Drives Rotation, Reel-in aur Rope Shortening
        Keyboard kb = Keyboard.current;
        if (kb != null)
        {
            Vector3 swingForward = Vector3.ProjectOnPlane(cameraTransform.forward, anchorDir).normalized;
            Vector3 swingRight = Vector3.ProjectOnPlane(cameraTransform.right, anchorDir).normalized;

            // Sirf 'W' ya 'Up Arrow' dabane se rope choti hogi aur aage ki swing boost milegi
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed)
            {
                // Rope ko chota karna (Reeling in towards cube)
                currentRopeLength = Mathf.Max(2.5f, currentRopeLength - (Time.deltaTime * 12f));

                // Anchor point ki taraf aur aage swing hone wali combined force
                rb.AddForce((swingForward + anchorDir * 0.5f).normalized * swingThrust, ForceMode.Acceleration);
            }

            // 'A' / 'D' ya Left/Right Arrow keys se hawa mein steer karna
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed)
            {
                rb.AddForce(swingRight * steerForce, ForceMode.Acceleration);
            }
            else if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)
            {
                rb.AddForce(-swingRight * steerForce, ForceMode.Acceleration);
            }
        }

        // Upward compensation taake swing karte waqt downward gravity rope ko drop na kare
        if (player.position.y < grapplePoint.y)
        {
            rb.AddForce(Vector3.up * 10f, ForceMode.Acceleration);
        }
    }
    void StopGrapple()
    {
        if (isGrappling)
        {
            isGrappling = false;
            lr.positionCount = 0;

            // Slingshot catapult boost on release (Cube ke upar launch hona)
            Vector3 releaseDir = rb.linearVelocity.normalized;
            float currentSpeed = rb.linearVelocity.magnitude;

            rb.linearVelocity = (releaseDir * currentSpeed * releaseLaunchMultiplier) + (Vector3.up * upwardLaunchBonus);
        }
    }

    void DrawRope()
    {
        if (!isGrappling)
        {
            currentGrapplePosition = gunTip.position;
            return;
        }

        float force = -strengthRope * (springPos - 1f) - damperRope * springVelocity;
        springVelocity += force * Time.deltaTime;
        springPos += springVelocity * Time.deltaTime;

        currentGrapplePosition = Vector3.Lerp(currentGrapplePosition, grapplePoint, Time.deltaTime * 14f);

        Vector3 up = Quaternion.LookRotation((grapplePoint - gunTip.position).normalized) * Vector3.up;

        for (int i = 0; i <= quality; i++)
        {
            float delta = i / (float)quality;
            Vector3 offset = up * waveHeight * Mathf.Sin(delta * waveCount * Mathf.PI) * (1f - springPos);
            Vector3 target = Vector3.Lerp(gunTip.position, currentGrapplePosition, delta) + offset;
            lr.SetPosition(i, target);
        }
    }

    public bool IsGrappling()
    {
        return isGrappling;
    }
}