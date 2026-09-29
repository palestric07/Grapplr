using UnityEngine;
using UnityEngine.InputSystem;

public class BlasterGun : MonoBehaviour
{
    [Header("Gun Stats")]
    public float damage = 25f;
    public float range = 100f;
    public float fireRate = 5f;
    public float impactForce = 250f;

    [Header("Visual Recoil")]
    public float recoilKickBack = 0.08f;
    public float recoilRotation = 4f;
    public float recoilReturnSpeed = 15f;

    [Header("Visual Effects")]
    public GameObject tracerPrefab;
    public GameObject impactEffectPrefab;
    public ParticleSystem muzzleFlash;

    [Header("Audio Settings")]
    public AudioSource gunAudioSource;      // Gun par laga hua AudioSource
    public AudioClip shootSound;            // Shooting clip
    public AudioClip impactSound;           // Takraane wali clip
    [Range(0f, 1f)] public float impactVolume = 0.8f;

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
    }

    void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        if (mouse.leftButton.isPressed && Time.time >= nextTimeToFire)
        {
            nextTimeToFire = Time.time + (1f / fireRate);
            Shoot();
        }

        transform.localPosition = Vector3.Lerp(transform.localPosition, originalPos, Time.deltaTime * recoilReturnSpeed);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, originalRot, Time.deltaTime * recoilReturnSpeed);
    }

    void Shoot()
    {
        // Recoil
        transform.localPosition -= Vector3.forward * recoilKickBack;
        transform.localRotation *= Quaternion.Euler(-recoilRotation, 0f, 0f);

        // 1. Shooting Sound & Flash Play
        if (gunAudioSource != null && shootSound != null)
        {
            gunAudioSource.PlayOneShot(shootSound);
        }

        if (muzzleFlash != null)
        {
            muzzleFlash.Play();
        }

        // 2. Hitscan Raycast
        RaycastHit hit;
        Vector3 targetPoint;

        if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out hit, range))
        {
            targetPoint = hit.point;

            // Rigidbody Push
            Rigidbody targetRb = hit.collider.GetComponent<Rigidbody>();
            if (targetRb != null)
            {
                targetRb.AddForce(-hit.normal * impactForce, ForceMode.Impulse);
            }

            // Impact Particles Spawn
            if (impactEffectPrefab != null)
            {
                Instantiate(impactEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
            }

            // 3. Impact Audio Play (Takraane ki jagah par)
            if (impactSound != null)
            {
                AudioSource.PlayClipAtPoint(impactSound, hit.point, impactVolume);
            }
        }
        else
        {
            targetPoint = cameraTransform.position + (cameraTransform.forward * range);
        }

        // Bullet Tracer
        if (tracerPrefab != null && muzzlePoint != null)
        {
            GameObject tracer = Instantiate(tracerPrefab, muzzlePoint.position, Quaternion.identity);
            BulletTracer tracerScript = tracer.GetComponent<BulletTracer>();
            if (tracerScript != null)
            {
                tracerScript.Init(targetPoint);
            }
        }
    }
}