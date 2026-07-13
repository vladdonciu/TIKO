using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PushPullBox : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2.1f;

    [Header("Rules")]
    [SerializeField] private bool canPush = true;
    [SerializeField] private bool canPull = true;

    private Rigidbody rb;
    private bool isGrabbed;
    private Vector3 targetPosition;

    public bool IsGrabbed => isGrabbed;
    public bool CanPush => canPush;
    public bool CanPull => canPull;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        targetPosition = rb.position;
    }

    public bool Grab()
    {
        if (isGrabbed)
            return false;

        isGrabbed = true;
        targetPosition = rb.position;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        return true;
    }

    public void Release()
    {
        isGrabbed = false;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    public void MoveTo(Vector3 newPosition)
    {
        if (!isGrabbed)
            return;

        newPosition.y = rb.position.y;
        targetPosition = newPosition;
    }

    private void FixedUpdate()
    {
        if (!isGrabbed)
            return;

        Vector3 nextPosition = Vector3.MoveTowards(
            rb.position,
            targetPosition,
            moveSpeed * Time.fixedDeltaTime
        );

        rb.MovePosition(nextPosition);
    }
}