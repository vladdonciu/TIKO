using UnityEngine;

public class ZoneSmokeTrigger : MonoBehaviour
{
    [Header("Smoke")]
    [SerializeField] private ParticleSystem[] smokeSystems;

    [Header("Trigger")]
    [SerializeField] private string playerTag = "Player";

    private void Awake()
    {
        // La start, fumul e oprit — pornește doar când Tiko intră în zonă.
        StopAllSmoke();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag))
            return;

        PlayAllSmoke();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag))
            return;

        StopAllSmoke();
    }

    private void PlayAllSmoke()
    {
        foreach (var ps in smokeSystems)
        {
            if (ps != null && !ps.isPlaying)
                ps.Play();
        }
    }

    private void StopAllSmoke()
    {
        foreach (var ps in smokeSystems)
        {
            if (ps != null)
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}