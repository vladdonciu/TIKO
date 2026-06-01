using UnityEngine;

public class ProximityMute : MonoBehaviour
{
    public Transform listener;       // de obicei Camera.main.transform
    public AudioSource audioSource;
    public float unmuteDistance = 12f;  // sub asta se aude
    public float fadeSpeed = 6f;

    void Reset()
    {
        audioSource = GetComponent<AudioSource>();
    }

    void Start()
    {
        if (listener == null && Camera.main != null)
            listener = Camera.main.transform;
    }

    void Update()
    {
        if (listener == null || audioSource == null) return;

        float d = Vector3.Distance(listener.position, transform.position);
        float target = (d <= unmuteDistance) ? 1f : 0f;

        audioSource.volume = Mathf.Lerp(audioSource.volume, target, Time.deltaTime * fadeSpeed);
    }
}