using UnityEngine;

public class FloatingPickup : MonoBehaviour
{
    [Header("Float")]
    public float floatAmplitude = 0.25f;
    public float floatFrequency = 1.5f;

    [Header("Rotation")]
    public float rotationSpeed = 90f; // grade/secunda

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        // plutire pe Y cu sin
        float yOffset = Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
        transform.position = startPos + new Vector3(0f, yOffset, 0f);

        // rotatie continua
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
    }
}