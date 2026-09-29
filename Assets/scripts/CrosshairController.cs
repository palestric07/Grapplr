using UnityEngine;
using UnityEngine.UI;

public class CrosshairController : MonoBehaviour
{
    [Header("Raycast Target Settings")]
    public Transform cameraTransform;
    public LayerMask whatIsGrappleable;
    public float maxGrappleDistance = 40f;

    [Header("Visual Feedback")]
    public Image crosshairImage;
    public Color normalColor = new Color(1f, 1f, 1f, 0.4f);   // Normal halat mein halka white
    public Color inRangeColor = new Color(0.2f, 0.8f, 1f, 1f);  // Grapple range mein aane par neon cyan/blue
    
    [Header("Scale Animation")]
    public float normalScale = 1f;
    public float inRangeScale = 1.25f;
    public float transitionSpeed = 12f;

    private float targetScale;
    private Color targetColor;

    void Start()
    {
        if (crosshairImage == null)
            crosshairImage = GetComponent<Image>();

        targetScale = normalScale;
        targetColor = normalColor;
    }

    void Update()
    {
        CheckGrappleTarget();
        AnimateCrosshair();
    }

    void CheckGrappleTarget()
    {
        RaycastHit hit;
        // Camera ke center se samne ray check karna
        if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out hit, maxGrappleDistance, whatIsGrappleable))
        {
            // Grappleable surface target mein hai
            targetColor = inRangeColor;
            targetScale = inRangeScale;
        }
        else
        {
            // Range se bahar
            targetColor = normalColor;
            targetScale = normalScale;
        }
    }

    void AnimateCrosshair()
    {
        // Smoothly color aur size interpolate karna
        crosshairImage.color = Color.Lerp(crosshairImage.color, targetColor, Time.deltaTime * transitionSpeed);
        transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * targetScale, Time.deltaTime * transitionSpeed);
    }
}