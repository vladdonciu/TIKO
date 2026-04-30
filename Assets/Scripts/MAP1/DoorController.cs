using System.Collections;
using UnityEngine;

public class DoorController : MonoBehaviour
{
    [Header("Usi")]
    public Transform leftDoor;
    public Transform rightDoor;

    [Header("Settings")]
    public float slideDistance = 2f;
    public float slideDuration = 1f;
    public float stayOpenDuration = 10f;

    [Header("Audio")]
    public AudioSource leftDoorAudio;
    public AudioSource rightDoorAudio;
    public AudioClip openSound;
    public AudioClip closeSound;

    [Header("Light Indicator")]
    public GameObject redLight;
    public GameObject greenLight;

    private Vector3 leftStart, rightStart;
    private Vector3 leftEnd, rightEnd;
    private bool doorsAreOpen = false;

    void Start()
    {
        leftStart = leftDoor.position;
        rightStart = rightDoor.position;

        // Schimba axa dupa nevoie: forward/back = Z, right/left = X
        leftEnd = leftStart + Vector3.forward * slideDistance;
        rightEnd = rightStart + Vector3.back * slideDistance;
    }

    public void OpenDoors()
    {
        StartCoroutine(OpenThenClose());
    }

    private IEnumerator OpenThenClose()
    {
        // --- DESCHIDERE ---
        doorsAreOpen = true;

        PlaySound(leftDoorAudio, openSound);
        PlaySound(rightDoorAudio, openSound);
        StartCoroutine(SlideDoors(leftDoor, leftStart, leftEnd));
        StartCoroutine(SlideDoors(rightDoor, rightStart, rightEnd));

        // --- ASTEAPTA 10 SEC ---
        yield return new WaitForSeconds(stayOpenDuration);

        // --- INCHIDERE ---
        doorsAreOpen = false;

        PlaySound(leftDoorAudio, closeSound);
        PlaySound(rightDoorAudio, closeSound);
        StartCoroutine(SlideDoors(leftDoor, leftEnd, leftStart));
        StartCoroutine(SlideDoors(rightDoor, rightEnd, rightStart));

        // Asteapta sa termine animatia de inchidere
        yield return new WaitForSeconds(slideDuration);

        // --- REVINE LA BEC ROSU ---
        if (greenLight) greenLight.SetActive(false);
        if (redLight)   redLight.SetActive(true);
    }

    public bool AreDoorOpen()
    {
        return doorsAreOpen;
    }

    private IEnumerator SlideDoors(Transform door, Vector3 from, Vector3 to)
    {
        float elapsed = 0f;
        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / slideDuration;
            t = t * t * (3f - 2f * t); // smoothstep
            door.position = Vector3.Lerp(from, to, t);
            yield return null;
        }
        door.position = to;
    }

    private void PlaySound(AudioSource source, AudioClip clip)
    {
        if (source == null || clip == null) return;
        source.clip = clip;
        source.Play();
    }
}