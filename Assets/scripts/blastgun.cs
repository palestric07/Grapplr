using UnityEngine;
using UnityEngine.InputSystem;

public class BlastGun : MonoBehaviour
{
    [Header("Gun Stats (Blast Cannon)")]
    public float damage = 100f;
    public float range = 150f;
    public float fireRate = 0.8f;              

    [Header("Explosive Physics (Environment)")]
    public float explosionForce = 1500f;     
    public float explosionRadius = 7f;       
    public float upwardModifier = 2f;        

    [Header("Self Knockback (Rocket Jump)")]
    public Transform playerTransform;        
    public Rigidbody playerRb;               
    public float selfKnockbackRadius = 6f;   
    public float selfKnockbackForce = 25f;   
    public float selfUpwardBoost = 1.2f;     

    [Header("Heavy Visual Recoil")]
    public float recoilKickBack = 0.22f;     
    public float recoilRotation = 12f;       
    public float recoilReturnSpeed = 8f;     

    [Header("Effects & Audio")]
    public GameObject tracerPrefab;          
    public GameObject bigExplosionPrefab;    
    public ParticleSystem muzzleFlash;       
    public AudioSource gunAudioSource;
    public AudioClip shootSound;             
    public AudioClip explosionSound;         
    [Range(0f, 1f)] public float explosionVolume = 1f;

    [Header("References")]
    public Transform cameraTransform;
    public Transform muzzlePoint;

    private float nextTimeToFire = 0f;
    private Vector3 originalPos;
    private Quaternion originalRot;

    void Start()
    {
        originalPos = transform.localPosition;
        originalRot = transform.localRotation;

        if (gunAudioSource == null)
            gunAudioSource = GetComponent<AudioSource>();

        if (playerTransform == null && cameraTransform != null)
            playerTransform = cameraTransform.root;

        if (playerRb == null && playerTransform != null)
            playerRb = playerTransform.GetComponent<Rigidbody>();
    }

    void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        // Single Tap Firing
        if (mouse.leftButton.wasPressedThisFrame && Time.time >= nextTimeToFire)
        {
            nextTimeToFire = Time.time + (1f / fireRate);
            Shoot();
        }

        transform.localPosition = Vector3.Lerp(transform.localPosition, originalPos, Time.deltaTime * recoilReturnSpeed);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, originalRot, Time.deltaTime * recoilReturnSpeed);
    }

    void Shoot()
    {
        transform.localPosition -= Vector3.forward * recoilKickBack;
        transform.localRotation *= Quaternion.Euler(-recoilRotation, 0f, 0f);

        if (gunAudioSource != null && shootSound != null)
            gunAudioSource.PlayOneShot(shootSound);

        if (muzzleFlash != null)
            muzzleFlash.Play();

        RaycastHit hit;
        Vector3 targetPoint;

        if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out hit, range))
        {
            targetPoint = hit.point;

            if (bigExplosionPrefab != null)
                Instantiate(bigExplosionPrefab, hit.point, Quaternion.LookRotation(hit.normal));

            if (explosionSound != null)
                AudioSource.PlayClipAtPoint(explosionSound, hit.point, explosionVolume);

            // Objects ko udaana
            Collider[] colliders = Physics.OverlapSphere(hit.point, explosionRadius);
            foreach (Collider col in colliders)
            {
                Rigidbody rb = col.GetComponent<Rigidbody>();
                if (rb != null && rb != playerRb)
                {
                    rb.AddExplosionForce(explosionForce, hit.point, explosionRadius, upwardModifier, ForceMode.Impulse);
                }
            }

            // Player Rocket Jump
            ApplySelfKnockback(hit.point);
        }
        else
        {
            targetPoint = cameraTransform.position + (cameraTransform.forward * range);
        }

        if (tracerPrefab != null && muzzlePoint != null)
        {
            GameObject tracer = Instantiate(tracerPrefab, muzzlePoint.position, Quaternion.identity);
            BulletTracer tracerScript = tracer.GetComponent<BulletTracer>();
            if (tracerScript != null)
                tracerScript.Init(targetPoint);
        }
    }

    void ApplySelfKnockback(Vector3 explosionPoint)
    {
        if (playerTransform == null) return;

        float distance = Vector3.Distance(explosionPoint, playerTransform.position);

        if (distance < selfKnockbackRadius)
        {
            Vector3 pushDirection = (playerTransform.position - explosionPoint).normalized;
            pushDirection.y += selfUpwardBoost;
            pushDirection = pushDirection.normalized;

            float proximityMultiplier = 1f - (distance / selfKnockbackRadius);
            float finalForce = selfKnockbackForce * proximityMultiplier;

            if (playerRb != null)
            {
                if (playerRb.linearVelocity.y < 0)
                {
                    Vector3 vel = playerRb.linearVelocity;
                    vel.y = 0f;
                    playerRb.linearVelocity = vel;
                }

                playerRb.AddForce(pushDirection * finalForce, ForceMode.VelocityChange);
            }
        }
    }
}