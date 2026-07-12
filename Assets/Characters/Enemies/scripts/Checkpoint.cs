using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Checkpoint : MonoBehaviour
{
    [SerializeField] private Transform spawnPoint;
    private bool activated;

    private void Reset()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        TikoHealth tikoHealth = other.GetComponentInParent<TikoHealth>();

        if (tikoHealth == null || activated)
            return;

        Transform point = spawnPoint != null ? spawnPoint : transform;

        tikoHealth.SetCheckpoint(point);

        activated = true;

        Debug.Log($"[CHECKPOINT] Activated: {name}");
    }
}