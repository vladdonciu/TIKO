using UnityEngine;

public class WeaponPickup : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private TikoWeaponFormController weaponFormController;
    [SerializeField] private string playerTag = "Player";

    [Header("Child Visuals (nu le mișcăm, doar le dezactivăm)")]
    [SerializeField] private GameObject gunVisual;
    [SerializeField] private GameObject collectableEffect;

    [Header("Pickup FX")]
    [SerializeField] private ParticleSystem pickupBurstFX;
    [SerializeField] private AudioSource pickupAudioSource;
    [SerializeField] private AudioClip pickupSound;

    private bool collected = false;

    private void OnTriggerEnter(Collider other)
    {
        if (collected) return;

        if (!other.CompareTag(playerTag)) return;

        collected = true;
        Collect();
    }

    private void Collect()
    {
        if (pickupAudioSource != null && pickupSound != null)
            pickupAudioSource.PlayOneShot(pickupSound);

        if (pickupBurstFX != null)
            pickupBurstFX.Play();

        if (gunVisual != null)
            gunVisual.SetActive(false);

        if (collectableEffect != null)
            collectableEffect.SetActive(false);

        if (weaponFormController != null)
            weaponFormController.EquipWeapon();

        Destroy(gameObject, 1.5f);
    }
}