using System.Collections.Generic;
using UnityEngine;

public class BulletPool : MonoBehaviour
{
    public static BulletPool Instance { get; private set; }

    [Header("Pool Settings")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private int initialPoolSize = 30;

    [Header("Hierarchy Organization")]
    [SerializeField] private Transform availableBulletsParent;
    [SerializeField] private Transform activeBulletsParent;

    private readonly Queue<GameObject> availableBullets = new Queue<GameObject>();
    private readonly HashSet<GameObject> bulletsInPool = new HashSet<GameObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateNewBullet();
        }
    }

    private GameObject CreateNewBullet()
    {
        GameObject bullet = Instantiate(bulletPrefab, availableBulletsParent);
        bullet.name = "PooledBullet";

        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Setăm velocity ÎNAINTE de a face body-ul kinematic.
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            rb.isKinematic = true;
        }

        Collider bulletCollider = bullet.GetComponent<Collider>();
        if (bulletCollider != null)
            bulletCollider.isTrigger = true;

        bullet.SetActive(false);

        availableBullets.Enqueue(bullet);
        bulletsInPool.Add(bullet);

        return bullet;
    }

    public GameObject GetBullet()
    {
        if (availableBullets.Count == 0)
            CreateNewBullet();

        GameObject bullet = availableBullets.Dequeue();
        bulletsInPool.Remove(bullet);

        // Rămâne organizat sub ActiveBullets în Hierarchy.
        bullet.transform.SetParent(activeBulletsParent, true);

        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Rigidbody-ul vine deja din pool cu isKinematic = true
            // (setat la ReturnBullet), deci velocity e deja zero.
            // Nu mai setăm velocity aici cât timp e kinematic.
        }

        bullet.SetActive(true);

        return bullet;
    }

    public void LaunchBullet(
        GameObject bullet,
        Vector3 position,
        Quaternion rotation,
        Vector3 velocity)
    {
        bullet.transform.SetPositionAndRotation(position, rotation);

        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        if (rb == null)
            return;

        rb.position = position;
        rb.rotation = rotation;

        // Devine dinamic ÎNAINTE de a seta velocity.
        rb.isKinematic = false;
        rb.WakeUp();

        rb.linearVelocity = velocity;
        rb.angularVelocity = Vector3.zero;
    }

    public void ReturnBullet(GameObject bullet)
    {
        if (bullet == null || bulletsInPool.Contains(bullet))
            return;

        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Setăm velocity ÎNAINTE de a face body-ul kinematic.
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            rb.isKinematic = true;
            rb.Sleep();
        }

        bullet.SetActive(false);
        bullet.transform.SetParent(availableBulletsParent, false);

        bulletsInPool.Add(bullet);
        availableBullets.Enqueue(bullet);
    }
}