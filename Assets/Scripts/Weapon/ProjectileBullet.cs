using UnityEngine;

public class ProjectileBullet : MonoBehaviour
{
    [SerializeField] private float lifeTime = 2.5f;
    [SerializeField] private float damage = 10f;
    [SerializeField] private string playerTag = "Player";

    private bool isActiveBullet;
    private bool isReturningToPool;

    private void OnEnable()
    {
        isActiveBullet = true;
        isReturningToPool = false;
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(ReturnToPool));
        isActiveBullet = false;
    }

    public void ResetBullet()
    {
        CancelInvoke(nameof(ReturnToPool));

        isActiveBullet = true;
        isReturningToPool = false;

        Invoke(nameof(ReturnToPool), lifeTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isActiveBullet || isReturningToPool)
            return;

        if (other.CompareTag("CameraZone"))
            return;

        if (other.CompareTag(playerTag))
            return;

        EnemyHealth enemyHealth = other.GetComponentInParent<EnemyHealth>();

        if (enemyHealth != null)
            enemyHealth.TakeDamage(damage);

        ReturnToPool();
    }

    private void ReturnToPool()
    {
        if (!isActiveBullet || isReturningToPool)
            return;

        isReturningToPool = true;
        isActiveBullet = false;

        CancelInvoke(nameof(ReturnToPool));

        if (BulletPool.Instance != null)
            BulletPool.Instance.ReturnBullet(gameObject);
    }
}