using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class TikoHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;

    [Header("UI")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TMP_Text healthText;

    [Header("Death")]
    [SerializeField] private Animator animator;
    [SerializeField] private MonoBehaviour playerController;
    [SerializeField] private float deathAnimationDuration = 2.1f;

    [Header("Death Fall")]
    [SerializeField] private float deathFallGravity = -25f;
    [SerializeField] private float deathFallMaxSpeed = -30f;

    [Header("Respawn Safety")]
    [SerializeField] private float respawnInvulnerabilityDuration = 0.5f;

    [Header("Damage Feedback")]
    [SerializeField] private Image damageFlash;
    [SerializeField] private float flashPeakAlpha = 0.20f;
    [SerializeField] private float flashDuration = 0.20f;

    private CharacterController characterController;
    private Coroutine flashRoutine;
    private Transform currentCheckpoint;
    private bool isDead;
    private bool isInvulnerable;

    public bool IsDead => isDead;
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;

    private void Awake()
    {
        currentHealth = maxHealth;
        currentCheckpoint = transform;

        characterController = GetComponent<CharacterController>();

        if (playerController == null)
            playerController = GetComponent<PlayerController25D_Anim>();
    }

    private void Start()
    {
        if (healthSlider != null)
        {
            healthSlider.minValue = 0f;
            healthSlider.maxValue = maxHealth;
        }

        RefreshUI();
    }

    public void TakeDamage(float damage)
    {
        if (isDead || isInvulnerable || damage <= 0f)
            return;

        currentHealth = Mathf.Max(0f, currentHealth - damage);

        Debug.Log($"[TIKO] Damage: {damage} | HP: {currentHealth}/{maxHealth}");

        RefreshUI();
        PlayDamageFeedback();

        if (currentHealth <= 0f)
            Die();
    }

    public void Kill()
    {
        if (isDead || isInvulnerable)
            return;

        currentHealth = 0f;
        RefreshUI();
        Die();
    }

    public void Heal(float amount)
    {
        if (isDead || amount <= 0f)
            return;

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        RefreshUI();
    }

    public void SetCheckpoint(Transform checkpoint)
    {
        if (checkpoint == null)
            return;

        currentCheckpoint = checkpoint;

        Debug.Log($"[TIKO] Checkpoint set: {checkpoint.name}");
    }

    private void Die()
    {
        if (isDead)
            return;

        isDead = true;

        Debug.Log("[TIKO] Death");

        if (playerController != null)
            playerController.enabled = false;

        if (animator != null)
        {
            animator.ResetTrigger("DeathTrigger");
            animator.SetTrigger("DeathTrigger");
        }

        StartCoroutine(DeathRoutine());
    }

    private IEnumerator DeathRoutine()
    {
        float fallVelocity = 0f;
        float timer = 0f;

        while (timer < deathAnimationDuration)
        {
            timer += Time.deltaTime;

            if (characterController != null && characterController.enabled)
            {
                fallVelocity = Mathf.Max(
                    fallVelocity + deathFallGravity * Time.deltaTime,
                    deathFallMaxSpeed
                );

                Vector3 fallMotion = new Vector3(0f, fallVelocity, 0f) * Time.deltaTime;
                characterController.Move(fallMotion);
            }

            yield return null;
        }

        if (characterController != null)
            characterController.enabled = false;

        if (currentCheckpoint != null)
        {
            transform.SetPositionAndRotation(
                currentCheckpoint.position,
                currentCheckpoint.rotation
            );
        }

        Physics.SyncTransforms();

        if (animator != null)
        {
            animator.ResetControllerState(true);
            animator.Update(0f);
        }

        isInvulnerable = true;

        if (characterController != null)
            characterController.enabled = true;

        currentHealth = maxHealth;
        RefreshUI();

        if (playerController != null)
            playerController.enabled = true;

        isDead = false;

        yield return new WaitForSeconds(respawnInvulnerabilityDuration);

        isInvulnerable = false;
    }

    private void PlayDamageFeedback()
    {
        if (damageFlash == null)
            return;

        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        flashRoutine = StartCoroutine(DamageFlashRoutine());
    }

    private IEnumerator DamageFlashRoutine()
    {
        Color color = damageFlash.color;
        color.a = flashPeakAlpha;
        damageFlash.color = color;

        float timer = 0f;

        while (timer < flashDuration)
        {
            timer += Time.deltaTime;

            color.a = Mathf.Lerp(
                flashPeakAlpha,
                0f,
                timer / flashDuration
            );

            damageFlash.color = color;
            yield return null;
        }

        color.a = 0f;
        damageFlash.color = color;
        flashRoutine = null;
    }

    private void RefreshUI()
    {
        if (healthSlider != null)
            healthSlider.value = currentHealth;

        if (healthText != null)
        {
            healthText.text =
                $"HP {Mathf.CeilToInt(currentHealth)} / {Mathf.CeilToInt(maxHealth)}";
        }
    }

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.minusKey.wasPressedThisFrame)
            TakeDamage(10f);

        if (Keyboard.current.numpadPlusKey.wasPressedThisFrame)
            Heal(10f);
    }
}