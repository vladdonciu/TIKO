using System.Collections;
using UnityEngine;

public class TeleportTrigger : MonoBehaviour
{
    [Header("Teleport")]
    public Vector3 teleportDestination;
    public DoorController doorController;

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

        CharacterController cc = player.GetComponent<CharacterController>();
        Camera cam = Camera.main;

        // 1. Fade OUT
        yield return StartCoroutine(ScreenFader.Instance.FadeOut());

        // 2. Ecranul e COMPLET negru — dezactiveaza camera
        if (cam) cam.enabled = false;

        // 3. Teleporteaza (invizibil pentru player)
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

        // 4. Mica pauza sa se randeze noua pozitie
        yield return new WaitForSeconds(delayOnBlack);

        // 5. Reactiveaza camera DUPA ce pozitia e setata
        if (cam) cam.enabled = true;

        // 6. Acum Fade IN — camera vede direct destinatia
        yield return StartCoroutine(ScreenFader.Instance.FadeIn());

        isTriggered = false;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(teleportDestination, 0.4f);
        Gizmos.DrawLine(transform.position, teleportDestination);
    }
}