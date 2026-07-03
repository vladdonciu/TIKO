using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TeleportTrigger : MonoBehaviour
{
    [Header("Teleport")]
    public Vector3 teleportDestination;
    public DoorController doorController;

    [Header("Camera la destinatie")]
    public CameraZone destinationCameraZone;

    [Header("Fade")]
    public ScreenFader fader;

    [Header("Settings")]
    public float delayOnBlack = 0.1f;

    private bool isTriggered = false;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isTriggered) return;
        if (!other.CompareTag("Player")) return;
        if (doorController != null && !doorController.AreDoorOpen()) return;

        StartCoroutine(DoTeleport(other.transform));
    }

    private IEnumerator DoTeleport(Transform player)
    {
        isTriggered = true;

        CharacterController cc = player.GetComponent<CharacterController>();
        CameraController camController = CameraController.Instance;
        Camera cam = camController != null ? camController.GetComponent<Camera>() : Camera.main;

        // 1) Fade OUT
        if (fader != null)
            yield return StartCoroutine(fader.FadeOut());

        // 2) Dezactiveaza camera
        if (cam) cam.enabled = false;

        // 3) Teleport
        if (cc != null)
        {
            cc.enabled = false;
            player.position = teleportDestination;
            cc.enabled = true;
        }
        else
        {
            player.position = teleportDestination;
        }

        // 4) Aplica camera zone
        if (destinationCameraZone != null)
        {
            destinationCameraZone.ApplyToCamera(true);
        }
        else if (camController != null)
        {
            camController.SetBounds(new Vector2(-999f, -999f), new Vector2(999f, 999f));
        }

        if (camController != null)
            camController.SnapNow();

        // 5) Pauza
        yield return new WaitForSeconds(delayOnBlack);

        // 6) Reactiveaza camera
        if (cam) cam.enabled = true;

        // 7) Fade IN
        if (fader != null)
            yield return StartCoroutine(fader.FadeIn());

        isTriggered = false;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(teleportDestination, 0.4f);
        Gizmos.DrawLine(transform.position, teleportDestination);

        if (destinationCameraZone != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(teleportDestination, destinationCameraZone.transform.position);
            Gizmos.DrawWireSphere(destinationCameraZone.transform.position, 0.3f);
        }
    }
}