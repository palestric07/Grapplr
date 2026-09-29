using UnityEngine;

public class BulletTracer : MonoBehaviour
{
    private Vector3 targetPos;

    [Header("Tracer Speed")]
    public float speed = 120f; // Pehle 250 thi, isko 80 se 120 ke darmiyan rakh kar check karein

    public void Init(Vector3 target)
    {
        targetPos = target;
        Destroy(gameObject, 1f); // Speed kam ki hai toh thora zyada time de dein zinda rehne ke liye
    }

    void Update()
    {
        transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPos) < 0.1f)
        {
            Destroy(gameObject);
        }
    }
}