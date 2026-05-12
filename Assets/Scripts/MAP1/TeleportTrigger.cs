using System.Collections;
using UnityEngine;

public class TeleportTrigger : MonoBehaviour
{
    [Header("Teleport")]
    public Vector3 teleportDestination;
    public DoorController doorController;

    [Header("Camera la destinatie")]
    public CameraZone destinationCameraZone;

    [Header("Settings")]
    public float delayOnBlack = 0.1f;

    private bool isTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (isTriggered) return;
        if (!other.CompareTag("Player")) return;
        if (!doorController.AreDoorOpen()) return;
        StartCoroutine(DoTeleport(other.transform));
    }

    private IEnumerator DoTeleport(Transform player)
    {
        isTriggered = true;
        Debug.Log("[TELEPORT] Start");

        CharacterController cc         = player.GetComponent<CharacterController>();
        CameraController camController = CameraController.Instance;
        Camera cam = camController != null
            ? camController.GetComponent<Camera>()
            : Camera.main;

        // 1. Fade OUT
        yield return StartCoroutine(ScreenFader.Instance.FadeOut());
        Debug.Log("[TELEPORT] Fade OUT done");

        // 2. Dezactiveaza camera
        if (cam) cam.enabled = false;

        // 3. Teleporteaza playerul
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
        Debug.Log($"[TELEPORT] Player la {player.position}");

        // 4. Aplica zona de camera de la destinatie — un singur apel, totul atomic
        if (destinationCameraZone != null)
        {
            destinationCameraZone.ApplyToCamera(true);
            Debug.Log($"[TELEPORT] Camera zone aplicata: {destinationCameraZone.settings?.name}");
        }
        else
        {
            Debug.LogWarning("[TELEPORT] destinationCameraZone e NULL! " +
                             "Asigneaza CameraZone in Inspector.");
            camController.SetBounds(
                new Vector2(-999f, -999f),
                new Vector2( 999f,  999f)
            );
        }

        // 5. Snap camera instant la player cu noile setari
        camController.SnapNow();
        Debug.Log($"[TELEPORT] Camera snap la {camController.transform.position}");

        // 6. Pauza
        yield return new WaitForSeconds(delayOnBlack);

        // 7. Reactiveaza camera
        if (cam) cam.enabled = true;

        // 8. Fade IN
        yield return StartCoroutine(ScreenFader.Instance.FadeIn());
        Debug.Log("[TELEPORT] Complet!");

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
            Gizmos.DrawLine(teleportDestination,
                destinationCameraZone.transform.position);
            Gizmos.DrawWireSphere(
                destinationCameraZone.transform.position, 0.3f);
        }
    }
}