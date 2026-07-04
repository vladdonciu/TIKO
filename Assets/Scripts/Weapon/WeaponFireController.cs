using UnityEngine;

public class WeaponFireController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private WeaponRecoil recoil;
    [SerializeField] private ParticleSystem muzzleFlash;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip fireClip;

    [Header("Fire Settings")]
    [SerializeField] private float bulletSpeed = 55f;
    [SerializeField] private float fireRate = 10f;
    [SerializeField] private KeyCode fireKey = KeyCode.Mouse0;
    [SerializeField] private bool requireWeaponEquipped = true;
    [SerializeField] private GameObject weaponObject;

    [Header("Audio Variation")]
    [SerializeField] private float minPitch = 0.98f;
    [SerializeField] private float maxPitch = 1.02f;

    [Header("Heat Gauge")]
    [SerializeField] private HeatGaugeController heatGauge;

    private float nextFireTime;

    private void Update()
    {
        if (!CanShoot()) return;

        if (Input.GetKey(fireKey) && Time.time >= nextFireTime)
        {
            Fire();
            nextFireTime = Time.time + (1f / fireRate);
        }
    }

    private bool CanShoot()
    {
        if (heatGauge != null && heatGauge.IsOverheated) return false;
        if (!requireWeaponEquipped) return true;
        if (weaponObject == null) return true;
        return weaponObject.activeInHierarchy;
    }

    private void Fire()
    {
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        if (rb != null) rb.linearVelocity = firePoint.forward * bulletSpeed;

        if (recoil != null) recoil.FireKick();
        if (muzzleFlash != null) muzzleFlash.Play();

        if (audioSource != null && fireClip != null)
        {
            audioSource.pitch = Random.Range(minPitch, maxPitch);
            audioSource.PlayOneShot(fireClip);
        }

        if (heatGauge != null) heatGauge.AddHeat();
    }
}