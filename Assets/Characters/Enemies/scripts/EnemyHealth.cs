using UnityEngine;
using UnityEngine.UI;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 20f;

    [Header("Health Bar UI")]
    [SerializeField] private Slider healthBarSlider;
    [SerializeField] private GameObject healthBarRoot;
    [SerializeField] private bool hideWhenFull = true;

    [Header("Death FX")]
    [SerializeField] private ParticleSystem deathParticles;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip hitClip;
    [SerializeField] private AudioClip deathClip;
    [SerializeField] private float destroyDelay = 0.05f;

    [Header("Optional Refs")]
    [SerializeField] private CameraEnemyAI enemyAI;
    [SerializeField] private EnemyShatter enemyShatter;

    private float currentHealth;
    private bool isDead;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDead => isDead;

    private void Awake()
    {
        currentHealth = maxHealth;

        if (enemyAI == null)
            enemyAI = GetComponent<CameraEnemyAI>();

        if (enemyShatter == null)
            enemyShatter = GetComponent<EnemyShatter>();

        if (healthBarSlider != null)
            healthBarSlider.maxValue = maxHealth;

        UpdateHealthBar();
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHealth = Mathf.Max(0f, currentHealth - amount);

        if (audioSource != null && hitClip != null)
            audioSource.PlayOneShot(hitClip);

        UpdateHealthBar();

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void UpdateHealthBar()
    {
        if (healthBarSlider != null)
            healthBarSlider.value = currentHealth;

        if (healthBarRoot != null && hideWhenFull)
            healthBarRoot.SetActive(currentHealth < maxHealth);
    }

    private void Die()
    {
        isDead = true;

        if (enemyShatter != null)
            enemyShatter.Shatter();

        if (deathParticles != null)
        {
            deathParticles.transform.parent = null;
            deathParticles.Play();
        }

        if (audioSource != null && deathClip != null)
            AudioSource.PlayClipAtPoint(deathClip, transform.position);

        if (enemyAI != null)
            enemyAI.enabled = false;

        if (healthBarRoot != null)
            healthBarRoot.SetActive(false);

        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
            r.enabled = false;

        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider c in colliders)
            c.enabled = false;

        Destroy(gameObject, destroyDelay);
    }
}