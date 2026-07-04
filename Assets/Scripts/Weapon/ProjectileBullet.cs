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
        // TODO: damage system
        // var health = other.GetComponent<Health>();
        // if (health) health.TakeDamage(damage);

        DisableSelf();
    }

    private void DisableSelf()
    {
        gameObject.SetActive(false);
    }
}