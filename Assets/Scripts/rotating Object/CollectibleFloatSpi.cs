using UnityEngine;

public class CollectibleFloatSpin : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform outerShape;
    [SerializeField] private Transform innerShape;

    [Header("Rotation Settings")]
    [SerializeField] private float outerRotationSpeed = 40f;
    [SerializeField] private float innerRotationSpeed = 60f;
    [SerializeField] private Vector3 outerRotationAxis = Vector3.up;
    [SerializeField] private Vector3 innerRotationAxis = Vector3.up;

    [Header("Float Settings")]
    [SerializeField] private float floatHeight = 0.15f;
    [SerializeField] private float floatSpeed = 1.5f;
    [SerializeField] private bool floatAffectsWholeObject = true;

    private Vector3 initialLocalPos;
    private float floatOffset;

    private void Awake()
    {
        initialLocalPos = transform.localPosition;
        floatOffset = Random.Range(0f, Mathf.PI * 2f); // desincronizeaza mai multe collectibles
    }

    private void Update()
    {
        RotateShapes();
        ApplyFloat();
    }

    private void RotateShapes()
    {
        if (outerShape != null)
            outerShape.Rotate(outerRotationAxis.normalized, outerRotationSpeed * Time.deltaTime, Space.Self);

        if (innerShape != null)
            innerShape.Rotate(-innerRotationAxis.normalized, innerRotationSpeed * Time.deltaTime, Space.Self);
    }

    private void ApplyFloat()
    {
        float y = Mathf.Sin((Time.time * floatSpeed) + floatOffset) * floatHeight;

        Transform targetTransform = floatAffectsWholeObject ? transform : outerShape;
        if (targetTransform == null) return;

        Vector3 basePos = floatAffectsWholeObject ? initialLocalPos : outerShape.localPosition;
        targetTransform.localPosition = new Vector3(basePos.x, initialLocalPos.y + y, basePos.z);
    }
}