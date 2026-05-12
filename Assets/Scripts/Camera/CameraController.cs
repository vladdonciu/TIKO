using System.Collections;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance;

    [Header("Target")]
    public Transform player;

    [Header("Follow Settings")]
    public float smoothSpeed        = 4f;
    public Vector3 offset           = new Vector3(0f, 4f, -6f);

    [Header("Bounds")]
    public Vector2 minBounds        = new Vector2(-999f, -999f);
    public Vector2 maxBounds        = new Vector2( 999f,  999f);

    [Header("Lock Axes")]
    public bool lockX   = false;
    public bool lockY   = false;
    public bool lockZ   = false;
    public float fixedX = 0f;
    public float fixedY = 4f;
    public float fixedZ = -6f;

    [Header("Feel")]
    public float lookAheadDistance  = 1.8f;
    public float lookAheadSpeed     = 3f;
    public float deadZoneX          = 0.4f;
    public float deadZoneY          = 0.1f;
    public Vector3 cameraRotation   = new Vector3(18f, 0f, 0f);

    [Header("Transition")]
    public float transitionDuration = 1f;

    private Vector3 lookAheadOffset;
    private Vector3 lastPlayerPos;
    private bool isTransitioning    = false;
    private Camera mainCamera;

    private void Awake()
    {
        Instance   = this;
        mainCamera = GetComponent<Camera>();
        if (!mainCamera) mainCamera = Camera.main;
        if (!player)
            player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    private void Start()
    {
        lastPlayerPos = player.position;
        transform.rotation = Quaternion.Euler(cameraRotation);
    }

    private void LateUpdate()
    {
        if (isTransitioning) return;

        // Full lock
        if (lockX && lockY && lockZ)
        {
            transform.position = new Vector3(fixedX, fixedY, fixedZ);
            return;
        }

        // Look Ahead
        Vector3 velocity = (player.position - lastPlayerPos) / Time.deltaTime;
        lastPlayerPos = player.position;

        lookAheadOffset = Vector3.Lerp(
            lookAheadOffset,
            new Vector3(Mathf.Clamp(velocity.x * lookAheadDistance * 0.1f,
                -lookAheadDistance, lookAheadDistance), 0f, 0f),
            lookAheadSpeed * Time.deltaTime
        );

        Vector3 desired = player.position + offset + lookAheadOffset;

        // Dead Zone
        Vector3 diff = desired - transform.position;
        if (Mathf.Abs(diff.x) < deadZoneX) desired.x = transform.position.x;
        if (Mathf.Abs(diff.y) < deadZoneY) desired.y = transform.position.y;

        // Lock individuale
        if (lockX) desired.x = fixedX;
        if (lockY) desired.y = fixedY;
        if (lockZ) desired.z = fixedZ;

        // Clamp
        if (!lockX) desired.x = Mathf.Clamp(desired.x, minBounds.x, maxBounds.x);
        if (!lockY) desired.y = Mathf.Clamp(desired.y, minBounds.y, maxBounds.y);

        transform.position = Vector3.Lerp(transform.position, desired,
            smoothSpeed * Time.deltaTime);

        // Failsafe
        Vector3 p = transform.position;
        if (!lockX) p.x = Mathf.Clamp(p.x, minBounds.x, maxBounds.x);
        if (!lockY) p.y = Mathf.Clamp(p.y, minBounds.y, maxBounds.y);
        transform.position = p;
    }

    // ─── API PUBLIC ───────────────────────────────────────────

    public void ApplyZone(CameraZoneSettings s, bool instant,
                           Vector2 bMin, Vector2 bMax,
                           float fx, float fy, float fz)
    {
        StopAllCoroutines();
        isTransitioning = false;

        // Seteaza TOTUL inainte de orice altceva
        minBounds = bMin;
        maxBounds = bMax;
        fixedX    = fx;
        fixedY    = fy;
        fixedZ    = fz;

        if (instant)
        {
            ApplySettings(s);
            SnapNow();
        }
        else
        {
            StartCoroutine(DoTransition(s));
        }
    }

    public void SnapNow()
    {
        isTransitioning = false;
        lookAheadOffset = Vector3.zero;
        lastPlayerPos   = player.position;

        if (lockX && lockY && lockZ)
        {
            transform.position = new Vector3(fixedX, fixedY, fixedZ);
            return;
        }

        Vector3 pos = player.position + offset;
        if (lockX) pos.x = fixedX;
        if (lockY) pos.y = fixedY;
        if (lockZ) pos.z = fixedZ;
        pos.x = Mathf.Clamp(pos.x, minBounds.x, maxBounds.x);
        pos.y = Mathf.Clamp(pos.y, minBounds.y, maxBounds.y);
        transform.position = pos;
    }

    public void SetFixedX(float x) { fixedX = x; }
    public void SetFixedY(float y) { fixedY = y; }
    public void SetFixedZ(float z) { fixedZ = z; }
    public float GetFixedX() => fixedX;
    public float GetFixedY() => fixedY;
    public float GetFixedZ() => fixedZ;
    public void SetBounds(Vector2 min, Vector2 max) { minBounds = min; maxBounds = max; }

    // ─── TRANZITIE ────────────────────────────────────────────

    private IEnumerator DoTransition(CameraZoneSettings s)
    {
        isTransitioning = true;

        float elapsed       = 0f;
        Vector3 startPos    = transform.position;
        float startFOV      = mainCamera ? mainCamera.fieldOfView : 60f;
        Quaternion startRot = transform.rotation;
        Quaternion endRot   = Quaternion.Euler(s.cameraRotation);

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / transitionDuration);
            t = t * t * (3f - 2f * t); // smoothstep

            // Target calculat cu valorile DEJA setate (fixedX/Y/Z corecte)
            Vector3 endPos = GetTargetPosition(s);

            transform.position = Vector3.Lerp(startPos, endPos, t);
            transform.rotation = Quaternion.Slerp(startRot, endRot, t);
            if (mainCamera) mainCamera.fieldOfView = Mathf.Lerp(startFOV, s.fieldOfView, t);

            yield return null;
        }

        ApplySettings(s);
        isTransitioning = false;
    }

    private Vector3 GetTargetPosition(CameraZoneSettings s)
    {
        if (lockX && lockY && lockZ)
            return new Vector3(fixedX, fixedY, fixedZ);

        Vector3 pos = player.position + s.offset;
        if (lockX) pos.x = fixedX;
        if (lockY) pos.y = fixedY;
        if (lockZ) pos.z = fixedZ;
        pos.x = Mathf.Clamp(pos.x, minBounds.x, maxBounds.x);
        pos.y = Mathf.Clamp(pos.y, minBounds.y, maxBounds.y);
        return pos;
    }

    private void ApplySettings(CameraZoneSettings s)
    {
        if (s == null) return;
        offset            = s.offset;
        smoothSpeed       = s.smoothSpeed;
        lockX             = s.lockX;
        lockY             = s.lockY;
        lockZ             = s.lockZ;
        cameraRotation    = s.cameraRotation;
        lookAheadDistance = s.lookAheadDistance;
        lookAheadSpeed    = s.lookAheadSpeed;
        deadZoneX         = s.deadZoneX;
        deadZoneY         = s.deadZoneY;
        lookAheadOffset   = Vector3.zero;
        lastPlayerPos     = player.position;
        transform.rotation = Quaternion.Euler(s.cameraRotation);
        if (mainCamera) mainCamera.fieldOfView = s.fieldOfView;
        // fixedX/Y/Z NU le resetam — sunt deja corecte din ApplyZone()
    }
}