using UnityEngine;

public class ProjectileBullet : MonoBehaviour
{
    [SerializeField] private float lifeTime = 2.5f;
    [SerializeField] private float damage = 10f;

    private void OnEnable()
    {
        CancelInvoke(nameof(DisableSelf));
        Invoke(nameof(DisableSelf), lifeTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("CameraZone")) return; // ignora zonele non-gameplay

        Debug.Log("Bullet hit: " + other.name + " at time: " + Time.time);
        // TODO: damage system
        DisableSelf();
    }

    private void DisableSelf()
    {
        gameObject.SetActive(false);
    }
}