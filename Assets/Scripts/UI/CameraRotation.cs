using UnityEngine;

public class CameraSwingLoop : MonoBehaviour
{
    [Header("Swing")]
    public float amplitude = 30f;   // grade maxim în fiecare parte
    public float speed     = 0.5f;  // cât de repede oscilează

    private Quaternion initialRotation;

    void Start()
    {
        initialRotation = transform.rotation;
    }

    void Update()
    {
        float angle = Mathf.Sin(Time.time * speed) * amplitude;
        transform.rotation = initialRotation * Quaternion.Euler(0f, angle, 0f);
    }
}