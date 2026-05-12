using UnityEngine;

[CreateAssetMenu(fileName = "CameraZoneSettings",
                 menuName = "TIKO/Camera Zone Settings")]
public class CameraZoneSettings : ScriptableObject
{
    [Header("Follow")]
    public Vector3 offset           = new Vector3(0f, 4f, -6f);
    public float smoothSpeed        = 4f;
    public float fieldOfView        = 52f;

    [Header("Bounds - relativ la centrul zonei")]
    public Vector2 boundsExtentX    = new Vector2(-20f, 20f);
    public Vector2 boundsExtentY    = new Vector2(0f, 0f);

    [Header("Lock Axes")]
    public bool lockX   = false;
    public bool lockY   = false;
    public bool lockZ   = false;
    public float fixedX = 0f;
    public float fixedY = 0f;
    public float fixedZ = -6f;

    [Header("Fixed Position Override")]
    public bool useFixedWorldPosition   = false;
    public Vector3 fixedWorldLookAt     = Vector3.zero;

    [Header("Feel - Little Nightmares Style")]
    public Vector3 cameraRotation   = new Vector3(18f, 0f, 0f);
    public float lookAheadDistance  = 1.8f;
    public float lookAheadSpeed     = 3f;
    public float deadZoneX          = 0.4f;
    public float deadZoneY          = 0.1f;
}