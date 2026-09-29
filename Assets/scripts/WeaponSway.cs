using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponSway : MonoBehaviour
{
    [Header("Sway Settings")]
    public float smooth = 8f;
    public float swayMultiplier = 1.5f;

    void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        // Mouse delta input (New Input System)
        Vector2 mouseDelta = mouse.delta.ReadValue();
        float mouseX = mouseDelta.x * swayMultiplier;
        float mouseY = mouseDelta.y * swayMultiplier;

        // Rotation angles calculate karna
        Quaternion rotationX = Quaternion.AngleAxis(-mouseY, Vector3.right);
        Quaternion rotationY = Quaternion.AngleAxis(mouseX, Vector3.up);

        Quaternion targetRotation = rotationX * rotationY;

        // Smooth interpolation
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, smooth * Time.deltaTime);
    }
}