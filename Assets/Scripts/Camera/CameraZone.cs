using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class CameraZone : MonoBehaviour
{
    public CameraZoneSettings settings;
    public bool instantTransition = false;

    private void Awake()
    {
        GetComponent<BoxCollider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (settings == null) return;
        ApplyToCamera(instantTransition);
    }

    // Apelabil si din TeleportTrigger
    public void ApplyToCamera(bool instant)
    {
        if (settings == null) return;

        BoxCollider bc = GetComponent<BoxCollider>();
        Vector3 c = bc != null
            ? transform.TransformPoint(bc.center)
            : transform.position;

        Vector2 bMin = new Vector2(c.x + settings.boundsExtentX.x,
                                    c.y + settings.boundsExtentY.x);
        Vector2 bMax = new Vector2(c.x + settings.boundsExtentX.y,
                                    c.y + settings.boundsExtentY.y);

        float fx = settings.lockX
            ? (settings.useFixedWorldPosition
                ? settings.fixedWorldLookAt.x + settings.offset.x
                : c.x + settings.fixedX)
            : CameraController.Instance.GetFixedX();

        float fy = settings.lockY
            ? (settings.useFixedWorldPosition
                ? settings.fixedWorldLookAt.y + settings.offset.y
                : c.y + settings.fixedY)
            : CameraController.Instance.GetFixedY();

        float fz = settings.lockZ
            ? (settings.useFixedWorldPosition
                ? settings.fixedWorldLookAt.z + settings.offset.z
                : settings.fixedZ)
            : CameraController.Instance.GetFixedZ();

        CameraController.Instance.ApplyZone(settings, instant, bMin, bMax, fx, fy, fz);
    }

    private void OnDrawGizmos()
    {
        BoxCollider bc = GetComponent<BoxCollider>();
        if (bc == null) return;

        Vector3 c    = transform.TransformPoint(bc.center);
        Vector3 size = Vector3.Scale(bc.size, transform.lossyScale);

        Gizmos.color = new Color(0f, 1f, 1f, 0.12f);
        Gizmos.DrawCube(c, size);
        Gizmos.color = new Color(0f, 1f, 1f, 0.9f);
        Gizmos.DrawWireCube(c, size);

        if (settings == null) return;

        float minX = c.x + settings.boundsExtentX.x;
        float maxX = c.x + settings.boundsExtentX.y;
        float minY = c.y + settings.boundsExtentY.x;
        float maxY = c.y + settings.boundsExtentY.y;
        Vector3 bc2 = new Vector3((minX + maxX) / 2f, (minY + maxY) / 2f, c.z);
        Vector3 bs  = new Vector3(maxX - minX, maxY - minY, 0.05f);

        Gizmos.color = new Color(0f, 1f, 0.3f, 0.08f);
        Gizmos.DrawCube(bc2, bs);
        Gizmos.color = new Color(0f, 1f, 0.3f, 0.9f);
        Gizmos.DrawWireCube(bc2, bs);

        Gizmos.color = Color.yellow;
        Vector3 camPos = c + settings.offset;
        Gizmos.DrawSphere(camPos, 0.25f);
        Gizmos.DrawLine(c, camPos);

#if UNITY_EDITOR
        UnityEditor.Handles.Label(
            c + Vector3.up * (size.y / 2f + 0.3f), settings.name,
            new GUIStyle { normal = { textColor = Color.cyan },
                           fontSize = 11, fontStyle = FontStyle.Bold });
#endif
    }
}