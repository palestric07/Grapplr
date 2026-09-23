using UnityEngine;

public class JumpPad : MonoBehaviour
{
    [Header("Jump Pad Settings")]
    public float verticalForce = 35f;      // Upar uchalne ki force
    public float forwardMultiplier = 1.2f;   // Jo sliding speed peeche se aa rahi hai usko mazeed boost dena

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Rigidbody rb = other.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // Current velocity retain karein aur vertical launch add karein
                Vector3 currentVel = rb.linearVelocity;

                // Y velocity ko direct upward force se override karein
                Vector3 newVelocity = new Vector3(currentVel.x * forwardMultiplier, verticalForce, currentVel.z * forwardMultiplier);
                
                rb.linearVelocity = newVelocity;
            }
        }
    }
}