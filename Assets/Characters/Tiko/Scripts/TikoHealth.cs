using UnityEngine;
using UnityEngine.Events;

public class TikoHealth : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 100f;
    public float currentHealth { get; private set; }

    [Header("Invincibility")]
    public float invincibilityDuration = 1f;
    private float invincibilityTimer = 0f;

    [Header("Events")]
    public UnityEvent onDamage;
    public UnityEvent onDeath;
    public UnityEvent onHeal;

    [Header("Debug")]
    public bool debugLog = true;

    private bool isDead = false;

    void Start()
    {
        currentHealth = maxHealth;
    }

    void Update()
    {
        if (invincibilityTimer > 0f)
            invincibilityTimer -= Time.deltaTime;
    }

    public void TakeDamage(float amount)
    {
        if (isDead || invincibilityTimer > 0f) return;

        currentHealth = Mathf.Clamp(currentHealth - amount, 0f, maxHealth);
        invincibilityTimer = invincibilityDuration;

        if (debugLog) Debug.Log($"[TIKO] Damage: -{amount} | HP: {currentHealth}/{maxHealth}");

        onDamage?.Invoke();

        if (currentHealth <= 0f) Die();
    }

    public void Heal(float amount)
    {
        if (isDead) return;
        currentHealth = Mathf.Clamp(currentHealth + amount, 0f, maxHealth);
        if (debugLog) Debug.Log($"[TIKO] Heal: +{amount} | HP: {currentHealth}/{maxHealth}");
        onHeal?.Invoke();
    }

    private void Die()
    {
        isDead = true;
        if (debugLog) Debug.Log("[TIKO] Died!");
        onDeath?.Invoke();
    }

    public bool IsDead() => isDead;
    public float GetHealthPercent() => currentHealth / maxHealth;
}