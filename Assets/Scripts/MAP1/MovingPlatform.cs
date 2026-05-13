using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    [Header("Movement")]
    public float moveDistance = 5f;
    public float speed = 2f;

    [Header("Behavior")]
    public bool startMovingRight = true;
    public bool pauseAtEnds = false;
    public float pauseDuration = 0.5f;

    private Vector3 startPos;
    private Vector3 lastPos;
    private float pauseTimer = 0f;
    private bool isPaused = false;
    private float direction;

    public Vector3 Velocity { get; private set; }

    void Awake()
    {
        startPos = transform.position;
        lastPos = transform.position;
        direction = startMovingRight ? 1f : -1f;
    }

    void Update()
    {
        if (isPaused)
        {
            pauseTimer -= Time.deltaTime;
            if (pauseTimer <= 0f) isPaused = false;
            Velocity = Vector3.zero;
            lastPos = transform.position;
            return;
        }

        Vector3 oldPos = transform.position;

        transform.position += Vector3.right * direction * speed * Time.deltaTime;

        float offset = transform.position.x - startPos.x;

        if (offset >= moveDistance)
        {
            transform.position = new Vector3(startPos.x + moveDistance, startPos.y, startPos.z);
            direction = -1f;
            TriggerPause();
        }
        else if (offset <= -moveDistance)
        {
            transform.position = new Vector3(startPos.x - moveDistance, startPos.y, startPos.z);
            direction = 1f;
            TriggerPause();
        }

        Velocity = (transform.position - oldPos) / Time.deltaTime;
        lastPos = transform.position;
    }

    void TriggerPause()
    {
        if (pauseAtEnds)
        {
            isPaused = true;
            pauseTimer = pauseDuration;
        }
    }
}