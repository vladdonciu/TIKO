using UnityEngine;

[RequireComponent(typeof(Collider))]
public class InstantDeathTrigger : MonoBehaviour
{
    [Header("Options")]
    [SerializeField] private bool oneShotPerTiko = false;
    [SerializeField] private float cooldown = 0.5f;

    private float lastTriggerTime = -999f;

    private void Reset()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (Time.time - lastTriggerTime < cooldown)
            return;

        TikoHealth tikoHealth = other.GetComponentInParent<TikoHealth>();

        if (tikoHealth == null || tikoHealth.IsDead)
            return;

        lastTriggerTime = Time.time;
        tikoHealth.Kill();

        Debug.Log($"[INSTANT DEATH] Tiko a murit instant la: {name}");
    }
}