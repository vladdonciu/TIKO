using UnityEngine;

public class StealthCloakController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController25D_Anim playerController;
    [SerializeField] private Renderer[] tikoRenderers;
    [SerializeField] private Renderer faceScreenRenderer;
    [SerializeField] private AudioSource audioSource;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip stealthEndClip;
    [SerializeField] private AudioClip stealthReadyClip;

    [Header("Cloak Settings")]
    [SerializeField] private float transitionSpeed = 4f;
    [SerializeField] private float maxStealthAmount = 1f;

    [Header("Duration & Cooldown")]
    [SerializeField] private float maxStealthDuration = 10f;
    [SerializeField] private float cooldownDuration = 5f;
    [SerializeField] private float warningTimeBeforeEnd = 2.5f;
    [SerializeField] private float blinkFrequency = 8f;

    private static readonly int StealthAmountID = Shader.PropertyToID("_StealthAmount");
    private static readonly int WarningGlitchID = Shader.PropertyToID("_WarningGlitch");

    private MaterialPropertyBlock mpb;
    private float currentStealth;
    private float stealthTimer;
    private float cooldownTimer;
    private bool isOnCooldown;

    public bool IsStealthAvailable => !isOnCooldown;

    private void Awake()
    {
        mpb = new MaterialPropertyBlock();
    }

    private void Update()
    {
        HandleCooldown();

        bool wantsStealth = playerController.IsStealth && !isOnCooldown;

        if (wantsStealth)
        {
            stealthTimer += Time.deltaTime;

            if (stealthTimer >= maxStealthDuration)
            {
                StartCooldown();
                wantsStealth = false;
            }
        }
        else
        {
            stealthTimer = 0f;
        }

        float target = wantsStealth ? maxStealthAmount : 0f;
        currentStealth = Mathf.MoveTowards(currentStealth, target, transitionSpeed * Time.deltaTime);

        float displayedStealth = currentStealth;
        float warningGlitch = 0f;

        float timeRemaining = maxStealthDuration - stealthTimer;
        if (wantsStealth && timeRemaining <= warningTimeBeforeEnd)
        {
            float blink = Mathf.Sin(Time.time * blinkFrequency * Mathf.PI * 2f) * 0.5f + 0.5f;
            displayedStealth = Mathf.Lerp(0f, currentStealth, blink);
            warningGlitch = 1f - blink;
        }

        ApplyStealthToRenderers(displayedStealth);
        ApplyWarningGlitchToFace(warningGlitch);
    }

    private void HandleCooldown()
    {
        if (!isOnCooldown) return;

        cooldownTimer -= Time.deltaTime;
        if (cooldownTimer <= 0f)
        {
            isOnCooldown = false;
            PlayStealthReady(); // NOU: sunet cand poti refolosi abilitatea
        }
    }

    private void StartCooldown()
    {
        isOnCooldown = true;
        cooldownTimer = cooldownDuration;
        stealthTimer = 0f;
        PlayStealthEnd(); // NOU: sunet cand se termina regimul de stealth
    }

    private void PlayStealthEnd()
    {
        if (audioSource != null && stealthEndClip != null)
            audioSource.PlayOneShot(stealthEndClip);
    }

    private void PlayStealthReady()
    {
        if (audioSource != null && stealthReadyClip != null)
            audioSource.PlayOneShot(stealthReadyClip);
    }

    private void ApplyStealthToRenderers(float stealthValue)
    {
        foreach (var rend in tikoRenderers)
        {
            int materialCount = rend.sharedMaterials.Length;
            for (int i = 0; i < materialCount; i++)
            {
                rend.GetPropertyBlock(mpb, i);
                mpb.SetFloat(StealthAmountID, stealthValue);
                rend.SetPropertyBlock(mpb, i);
            }
        }
    }

    private void ApplyWarningGlitchToFace(float glitchValue)
    {
        if (faceScreenRenderer == null) return;

        faceScreenRenderer.GetPropertyBlock(mpb);
        mpb.SetFloat(WarningGlitchID, glitchValue);
        faceScreenRenderer.SetPropertyBlock(mpb);
    }
}