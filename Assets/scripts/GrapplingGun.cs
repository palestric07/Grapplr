using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(LineRenderer))]
public class GrapplingGun : MonoBehaviour
{
    [Header("Grapple Settings")]
    public float maxDistance = 50f;
    public LayerMask whatIsGrappleable;   
    public Transform muzzlePoint;
    public Transform cameraTransform;
    public Transform playerTransform;

    [Header("Joint Physics (Swing Feel)")]
    public float spring = 4.5f;          
    public float damper = 7f;            
    public float massScale = 4.5f;

    [Header("Visuals & Audio")]
    public LineRenderer lineRenderer;
    public AudioSource audioSource;
    public AudioClip grappleShootSound;

    private Vector3 grapplePoint;
    private SpringJoint joint;
    private Rigidbody playerRb;

    void Start()
    {
        // 1. LineRenderer Setup
        if (lineRenderer == null) 
            lineRenderer = GetComponent<LineRenderer>();

        // Line ko flat ribbon ke bajaye round/smooth curves dena
        lineRenderer.numCapVertices = 8;      // Dono siron ko gol karega
        lineRenderer.numCornerVertices = 8;   // Turns ko round banayega
        lineRenderer.positionCount = 0;

        // Missing/Pink material ka auto-fix agar koi assign na ho
        if (lineRenderer.sharedMaterial == null || lineRenderer.sharedMaterial.name.Contains("Default"))
        {
            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            if (unlitShader != null)
            {
                Material roundRopeMat = new Material(unlitShader);
                roundRopeMat.color = Color.cyan; // Pasandeeda color yahan set kar sakte hain
                lineRenderer.material = roundRopeMat;
            }
        }

        // 2. AudioSource Setup
        if (audioSource == null) 
            audioSource = GetComponent<AudioSource>();

        if (audioSource != null)
            audioSource.playOnAwake = false;

        // 3. Muzzle Point Fail-Safe (Kabhi error nahi aayega)
        if (muzzlePoint == null)
        {
            Transform found = transform.Find("muzzlepoint");
            if (found == null) found = transform.Find("MuzzlePoint");
            muzzlePoint = found != null ? found : transform;
        }

        // 4. Player References Setup
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        if (playerTransform == null && cameraTransform != null)
            playerTransform = cameraTransform.root;

        if (playerTransform != null)
            playerRb = playerTransform.GetComponent<Rigidbody>();
    }

    void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        // Right Mouse Button daba kar rakhne se swing shuru
        if (mouse.rightButton.wasPressedThisFrame)
        {
            StartGrapple();
        }
        // Right Mouse Button chhodte hi rassi release
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
        // Samne aane wali har solid surface par grapple lagega
        if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out hit, maxDistance))
        {
            // Khud apne player body par hook na lage
            if (hit.transform.root == playerTransform) return;

            grapplePoint = hit.point;

            // Player par SpringJoint lagana swing physics ke liye
            joint = playerTransform.gameObject.AddComponent<SpringJoint>();
            joint.autoConfigureConnectedAnchor = false;
            joint.connectedAnchor = grapplePoint;

            float distanceFromPoint = Vector3.Distance(playerTransform.position, grapplePoint);

            // Rassi ke stretch constraints
            joint.maxDistance = distanceFromPoint * 0.8f;
            joint.minDistance = distanceFromPoint * 0.25f;

            joint.spring = spring;
            joint.damper = damper;
            joint.massScale = massScale;

            lineRenderer.positionCount = 2;

            if (audioSource != null && grappleShootSound != null)
                audioSource.PlayOneShot(grappleShootSound);
        }
    }

    void StopGrapple()
    {
        lineRenderer.positionCount = 0;
        if (joint != null)
        {
            Destroy(joint);
        }
    }

    void DrawRope()
    {
        // Agar grapple active na ho toh rassi draw na ho
        if (joint == null) return;

        lineRenderer.SetPosition(0, muzzlePoint.position);
        lineRenderer.SetPosition(1, grapplePoint);
    }
}