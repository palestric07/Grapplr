using UnityEngine;
using UnityEngine.InputSystem;

public class GrapplingGun : MonoBehaviour
{
    private LineRenderer lr;
    private Vector3 grapplePoint;
    public LayerMask whatIsGrappleable;
    public Transform cameraTransform;
    public Transform player;
    public float maxDistance = 100f;
    private SpringJoint joint;

    [Header("Dani Spring Settings")]
    public float spring = 4.5f;
    public float damper = 7f;
    public float massScale = 4.5f;

    [Header("Procedural Rope Settings")]
    public int quality = 400;
    public float damperRope = 14f;
    public float strengthRope = 800f;
    public float waveHeight = 1.2f;
    public float waveCount = 2.5f;

    private Vector3 currentGrapplePosition;
    private float springPos = 0f;
    private float springVelocity = 0f;

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
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
            joint = player.gameObject.AddComponent<SpringJoint>();
            joint.autoConfigureConnectedAnchor = false;
            joint.connectedAnchor = grapplePoint;

            float distanceFromPoint = Vector3.Distance(player.position, grapplePoint);

            joint.maxDistance = distanceFromPoint * 0.8f;
            joint.minDistance = distanceFromPoint * 0.25f;

            joint.spring = spring;
            joint.damper = damper;
            joint.massScale = massScale;

            lr.positionCount = quality + 1;
            currentGrapplePosition = cameraTransform.position;
            springPos = 0f;
            springVelocity = 0f;
        }
    }

    void StopGrapple()
    {
        lr.positionCount = 0;
        if (joint != null)
        {
            Destroy(joint);
        }
    }

    void DrawRope()
    {
        if (!joint)
        {
            currentGrapplePosition = cameraTransform.position;
            return;
        }

        float force = -strengthRope * (springPos - 1f) - damperRope * springVelocity;
        springVelocity += force * Time.deltaTime;
        springPos += springVelocity * Time.deltaTime;

        currentGrapplePosition = Vector3.Lerp(currentGrapplePosition, grapplePoint, Time.deltaTime * 12f);

        Vector3 up = Quaternion.LookRotation((grapplePoint - cameraTransform.position).normalized) * Vector3.up;

        for (int i = 0; i <= quality; i++)
        {
            float delta = i / (float)quality;
            Vector3 offset = up * waveHeight * Mathf.Sin(delta * waveCount * Mathf.PI) * (1f - springPos);
            Vector3 target = Vector3.Lerp(cameraTransform.position, currentGrapplePosition, delta) + offset;
            lr.SetPosition(i, target);
        }
    }

    public bool IsGrappling()
    {
        return joint != null;
    }
}