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

    [Header("Light Indicators")]
    public GameObject[] redLights;    // ← array
    public GameObject[] greenLights;  // ← array

    private Vector3 leftStart, rightStart;
    private Vector3 leftEnd, rightEnd;
    private bool doorsAreOpen = false;

    void Start()
    {
        // ✅ Salvează poziția CURENTĂ ca start
        leftStart  = leftDoor.position;
        rightStart = rightDoor.position;

        leftEnd  = leftStart  + leftDoor.right  * -slideDistance;
        rightEnd = rightStart + rightDoor.right *  slideDistance;

        SetLights(redLights, true);
        SetLights(greenLights, false);
    }
    public void OpenDoors()
    {
        StartCoroutine(OpenThenClose());
    }

    private IEnumerator OpenThenClose()
    {
        doorsAreOpen = true;

        SetLights(redLights, false);
        SetLights(greenLights, true);

        PlaySound(leftDoorAudio, openSound);
        PlaySound(rightDoorAudio, openSound);
        StartCoroutine(SlideDoors(leftDoor, leftStart, leftEnd));
        StartCoroutine(SlideDoors(rightDoor, rightStart, rightEnd));

        yield return new WaitForSeconds(stayOpenDuration);

        doorsAreOpen = false;

        PlaySound(leftDoorAudio, closeSound);
        PlaySound(rightDoorAudio, closeSound);
        StartCoroutine(SlideDoors(leftDoor, leftEnd, leftStart));
        StartCoroutine(SlideDoors(rightDoor, rightEnd, rightStart));

        yield return new WaitForSeconds(slideDuration);

        SetLights(redLights, true);
        SetLights(greenLights, false);
    }

    private void SetLights(GameObject[] lights, bool state)
    {
        if (lights == null) return;
        foreach (var light in lights)
            if (light) light.SetActive(state);
    }

    public bool AreDoorOpen() => doorsAreOpen;

    private IEnumerator SlideDoors(Transform door, Vector3 from, Vector3 to)
    {
        float elapsed = 0f;
        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / slideDuration;
            t = t * t * (3f - 2f * t);
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