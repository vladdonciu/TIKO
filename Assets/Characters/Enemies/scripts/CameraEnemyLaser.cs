using UnityEngine;

public class CameraEnemyLaser : MonoBehaviour
{
    [Header("References")]
    [Tooltip("LaserOrigin, copil al EyeSocket de pe capul inamicului.")]
    [SerializeField] private Transform eye;

    [Header("Eye Charge Glow")]
    [Tooltip("Mesh Renderer-ul ochiului pe care ai pus M_SentryChargeGlow.")]
    [SerializeField] private Renderer eyeRenderer;

    [Tooltip("Index-ul slotului de material ocupat de M_SentryChargeGlow.")]
    [SerializeField] private int eyeMaterialIndex = 3;
    [SerializeField] private Color idleEyeColor =
        new Color(0.35f, 0f, 0.02f, 1f);

    [SerializeField] private Color chargeEyeColor =
        new Color(1f, 0f, 0.3f, 1f);

    [SerializeField] private float idleGlowIntensity = 0.7f;

    [SerializeField] private float chargeMinGlowIntensity = 2f;

    [SerializeField] private float chargeMaxGlowIntensity = 7f;

    [SerializeField] private float chargePulseSpeed = 9f;

    [Header("Damage")]
    [SerializeField] private float laserDamage = 10f;
    [SerializeField] private float laserRange = 12f;

    [Header("Attack Timing")]
    [SerializeField] private float windUpDuration = 0.7f;
    [SerializeField] private float shotDuration = 0.16f;
    [SerializeField] private float cooldownDuration = 1.2f;

    [Header("Aim")]
    [Tooltip("35 = con frontal de tragere de 70° total.")]
    [SerializeField] private float maxFireAngle = 35f;

    [Header("Beam Visual")]
    [SerializeField] private GameObject laserBeamPrefab;
    [SerializeField] private GameObject laserDotPrefab;
    [SerializeField] private float aimBeamWidth = 0.008f;
    [SerializeField] private float shotBeamWidth = 0.07f;
    [SerializeField] private float dotLerpSpeed = 30f;

    [Header("Charge VFX")]
    [Tooltip("Prefab cu Particle System pentru energia care intră în ochi la charge.")]
    [SerializeField] private ParticleSystem chargeParticlesPrefab;

    [Header("Shot VFX")]
    [Tooltip("Prefab Particle System pentru impactul laserului.")]
    [SerializeField] private ParticleSystem impactParticlesPrefab;

    [Tooltip("Prefab Particle System pentru flash-ul de la ochi.")]
    [SerializeField] private ParticleSystem muzzleFlashPrefab;

    [Tooltip("Distanța cu care impactul este mutat în afara colliderului.")]
    [SerializeField] private float impactSurfaceOffset = 0.06f;

    [Tooltip("Cât de gros devine beam-ul pentru o fracțiune de secundă la foc.")]
    [SerializeField] private float shotPunchMultiplier = 1.7f;

    [Tooltip("Durata pulsului de grosime al laserului.")]
    [SerializeField] private float shotPunchDuration = 0.05f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;

    [Tooltip("Sunet de încărcare, când apare laserul subțire.")]
    [SerializeField] private AudioClip windUpClip;

    [Tooltip("Sunet de foc, exact când laserul gros aplică damage.")]
    [SerializeField] private AudioClip shotClip;

    [Range(0f, 1f)]
    [SerializeField] private float windUpVolume = 0.55f;

    [Range(0f, 1f)]
    [SerializeField] private float shotVolume = 0.8f;

    private enum AttackPhase
    {
        Idle,
        WindUp,
        Shot,
        Cooldown
    }

    private AttackPhase currentPhase = AttackPhase.Idle;

    private float phaseTimer;
    private float shotPunchTimer;

    private Transform player;
    private TikoHealth playerHealth;
    private StealthCloakController playerStealth;

    private GameObject beamInstance;
    private GameObject dotInstance;

    private ParticleSystem chargeParticlesInstance;
    private ParticleSystem impactParticlesInstance;
    private ParticleSystem muzzleFlashInstance;

    private Material eyeMaterialInstance;

    private static readonly int GlowColorId =
        Shader.PropertyToID("_GlowColor");

    private static readonly int PulseSpeedId =
        Shader.PropertyToID("_PulseSpeed");

    private static readonly int MinIntensityId =
        Shader.PropertyToID("_MinIntensity");

    private static readonly int MaxIntensityId =
        Shader.PropertyToID("_MaxIntensity");

    private static readonly int AlphaId =
        Shader.PropertyToID("_Alpha");

    private void Awake()
    {
        if (eye == null)
            eye = transform;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        FindPlayer();

        SetupEyeMaterial();
        SetEyeIdle();

        if (laserBeamPrefab != null)
        {
            beamInstance = Instantiate(laserBeamPrefab);
            beamInstance.SetActive(false);
        }

        if (laserDotPrefab != null)
        {
            dotInstance = Instantiate(laserDotPrefab);
            dotInstance.SetActive(false);
        }

        if (chargeParticlesPrefab != null)
        {
            chargeParticlesInstance = Instantiate(
                chargeParticlesPrefab,
                eye.position,
                eye.rotation,
                eye
            );

            chargeParticlesInstance.transform.localPosition = Vector3.zero;
            chargeParticlesInstance.transform.localRotation =
                Quaternion.identity;

            chargeParticlesInstance.gameObject.SetActive(false);
        }

        if (impactParticlesPrefab != null)
        {
            impactParticlesInstance =
                Instantiate(impactParticlesPrefab);

            impactParticlesInstance.gameObject.SetActive(false);
        }

        if (muzzleFlashPrefab != null)
        {
            muzzleFlashInstance =
                Instantiate(muzzleFlashPrefab);

            muzzleFlashInstance.gameObject.SetActive(false);
        }
    }

    public void TickLaser(bool enemyCanAttack)
    {
        if (player == null)
            FindPlayer();

        bool playerIsStealthed =
            playerStealth != null &&
            playerStealth.IsStealthed;

        if (
            !enemyCanAttack ||
            player == null ||
            eye == null ||
            playerHealth == null ||
            playerHealth.IsDead ||
            playerIsStealthed)
        {
            StopAttack();
            return;
        }

        if (!IsPlayerInFireCone())
        {
            StopAttack();
            return;
        }

        switch (currentPhase)
        {
            case AttackPhase.Idle:
                StartWindUp();
                break;

            case AttackPhase.WindUp:
                UpdateWindUp();
                break;

            case AttackPhase.Shot:
                UpdateShot();
                break;

            case AttackPhase.Cooldown:
                UpdateCooldown();
                break;
        }
    }

    private void FindPlayer()
    {
        GameObject playerObject =
            GameObject.FindGameObjectWithTag("Player");

        if (playerObject == null)
            return;

        player = playerObject.transform;
        playerHealth = playerObject.GetComponent<TikoHealth>();
        playerStealth =
            playerObject.GetComponent<StealthCloakController>();
    }

    private void SetupEyeMaterial()
    {
        if (eyeRenderer == null)
            return;

        Material[] materials = eyeRenderer.materials;

        if (
            eyeMaterialIndex < 0 ||
            eyeMaterialIndex >= materials.Length)
        {
            Debug.LogWarning(
                $"[LASER] Eye Material Index invalid: {eyeMaterialIndex}. " +
                $"Renderer-ul are {materials.Length} material slots."
            );

            return;
        }

        eyeMaterialInstance = materials[eyeMaterialIndex];
    }
    private void SetEyeIdle()
    {
        if (eyeMaterialInstance == null)
            return;

        if (eyeMaterialInstance.HasProperty(GlowColorId))
            eyeMaterialInstance.SetColor(
                GlowColorId,
                idleEyeColor
            );

        if (eyeMaterialInstance.HasProperty(MinIntensityId))
            eyeMaterialInstance.SetFloat(
                MinIntensityId,
                idleGlowIntensity
            );

        if (eyeMaterialInstance.HasProperty(MaxIntensityId))
            eyeMaterialInstance.SetFloat(
                MaxIntensityId,
                idleGlowIntensity
            );

        if (eyeMaterialInstance.HasProperty(PulseSpeedId))
            eyeMaterialInstance.SetFloat(
                PulseSpeedId,
                0f
            );

        if (eyeMaterialInstance.HasProperty(AlphaId))
            eyeMaterialInstance.SetFloat(
                AlphaId,
                1f
            );
    }

    private void SetEyeCharging()
    {
        if (eyeMaterialInstance == null)
            return;

        if (eyeMaterialInstance.HasProperty(GlowColorId))
            eyeMaterialInstance.SetColor(
                GlowColorId,
                chargeEyeColor
            );

        if (eyeMaterialInstance.HasProperty(MinIntensityId))
            eyeMaterialInstance.SetFloat(
                MinIntensityId,
                chargeMinGlowIntensity
            );

        if (eyeMaterialInstance.HasProperty(MaxIntensityId))
            eyeMaterialInstance.SetFloat(
                MaxIntensityId,
                chargeMaxGlowIntensity
            );

        if (eyeMaterialInstance.HasProperty(PulseSpeedId))
            eyeMaterialInstance.SetFloat(
                PulseSpeedId,
                chargePulseSpeed
            );

        if (eyeMaterialInstance.HasProperty(AlphaId))
            eyeMaterialInstance.SetFloat(
                AlphaId,
                1f
            );
    }

    private void SetEyeShotFlash()
    {
        if (eyeMaterialInstance == null)
            return;

        float shotIntensity = chargeMaxGlowIntensity * 1.5f;

        if (eyeMaterialInstance.HasProperty(GlowColorId))
            eyeMaterialInstance.SetColor(
                GlowColorId,
                Color.white
            );

        if (eyeMaterialInstance.HasProperty(MinIntensityId))
            eyeMaterialInstance.SetFloat(
                MinIntensityId,
                shotIntensity
            );

        if (eyeMaterialInstance.HasProperty(MaxIntensityId))
            eyeMaterialInstance.SetFloat(
                MaxIntensityId,
                shotIntensity
            );

        if (eyeMaterialInstance.HasProperty(PulseSpeedId))
            eyeMaterialInstance.SetFloat(
                PulseSpeedId,
                0f
            );

        if (eyeMaterialInstance.HasProperty(AlphaId))
            eyeMaterialInstance.SetFloat(
                AlphaId,
                1f
            );
    }

    private void StartWindUp()
    {
        currentPhase = AttackPhase.WindUp;
        phaseTimer = 0f;

        SetEyeCharging();
        PlayChargeParticles();
        PlaySound(windUpClip, windUpVolume);
    }

    private void UpdateWindUp()
    {
        phaseTimer += Time.deltaTime;

        DrawLaser(aimBeamWidth);

        if (phaseTimer >= windUpDuration)
        {
            SetEyeShotFlash();
            StopChargeParticles();

            currentPhase = AttackPhase.Shot;
            phaseTimer = 0f;
            shotPunchTimer = shotPunchDuration;

            PlaySound(shotClip, shotVolume);
            PlayMuzzleFlash();
            DealShotDamage();
        }
    }

    private void UpdateShot()
    {
        phaseTimer += Time.deltaTime;

        float currentBeamWidth = shotBeamWidth;

        if (shotPunchTimer > 0f)
        {
            shotPunchTimer -= Time.deltaTime;

            float punchProgress = Mathf.Clamp01(
                shotPunchTimer / shotPunchDuration
            );

            currentBeamWidth = Mathf.Lerp(
                shotBeamWidth,
                shotBeamWidth * shotPunchMultiplier,
                punchProgress
            );
        }

        DrawLaser(currentBeamWidth);

        if (phaseTimer >= shotDuration)
        {
            currentPhase = AttackPhase.Cooldown;
            phaseTimer = 0f;

            SetEyeIdle();
            HideVisuals();
        }
    }

    private void UpdateCooldown()
    {
        phaseTimer += Time.deltaTime;

        HideVisuals();

        if (phaseTimer >= cooldownDuration)
        {
            currentPhase = AttackPhase.Idle;
            phaseTimer = 0f;
        }
    }

    private void PlaySound(AudioClip clip, float volume)
    {
        if (audioSource == null || clip == null)
            return;

        audioSource.PlayOneShot(clip, volume);
    }

    private bool IsPlayerInFireCone()
    {
        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;

        if (toPlayer.sqrMagnitude < 0.001f)
            return false;

        float angle = Vector3.Angle(
            transform.forward,
            toPlayer.normalized
        );

        return angle <= maxFireAngle;
    }

    private void DealShotDamage()
    {
        Vector3 origin = eye.position;
        Vector3 targetPosition =
            player.position + Vector3.up * 0.5f;

        Vector3 direction =
            (targetPosition - origin).normalized;

        Vector3 impactPosition =
            origin + direction * laserRange;

        Quaternion impactRotation =
            Quaternion.LookRotation(-direction, Vector3.up);

        if (Physics.Raycast(
            origin,
            direction,
            out RaycastHit hit,
            laserRange,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore))
        {
            impactPosition =
                hit.point + hit.normal * impactSurfaceOffset;

            if (hit.normal.sqrMagnitude > 0.001f)
            {
                impactRotation = Quaternion.LookRotation(
                    hit.normal,
                    Vector3.up
                );
            }

            TikoHealth hitHealth =
                hit.collider.GetComponentInParent<TikoHealth>();

            if (hitHealth != null && !hitHealth.IsDead)
                hitHealth.TakeDamage(laserDamage);
        }

        PlayImpactParticles(impactPosition, impactRotation);
    }

    private void DrawLaser(float width)
    {
        Vector3 origin = eye.position;
        Vector3 targetPosition =
            player.position + Vector3.up * 0.5f;

        Vector3 direction =
            (targetPosition - origin).normalized;

        Vector3 endPoint = origin + direction * laserRange;

        if (Physics.Raycast(
            origin,
            direction,
            out RaycastHit hit,
            laserRange,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore))
        {
            endPoint = hit.point;
        }

        ShowBeam(origin, endPoint, width);
        ShowDot(endPoint);
    }

    private void ShowBeam(Vector3 from, Vector3 to, float width)
    {
        if (beamInstance == null)
            return;

        float length = Vector3.Distance(from, to);

        beamInstance.SetActive(true);
        beamInstance.transform.position = (from + to) * 0.5f;

        // Prefab-ul beam are lungimea locală pe axa Y.
        beamInstance.transform.up = (to - from).normalized;

        beamInstance.transform.localScale = new Vector3(
            width,
            length * 0.5f,
            width
        );
    }

    private void ShowDot(Vector3 position)
    {
        if (dotInstance == null)
            return;

        if (!dotInstance.activeSelf)
            dotInstance.transform.position = position;

        dotInstance.SetActive(true);

        dotInstance.transform.position = Vector3.Lerp(
            dotInstance.transform.position,
            position,
            dotLerpSpeed * Time.deltaTime
        );
    }

    private void PlayChargeParticles()
    {
        if (chargeParticlesInstance == null)
            return;

        chargeParticlesInstance.gameObject.SetActive(true);

        chargeParticlesInstance.Stop(
            true,
            ParticleSystemStopBehavior.StopEmittingAndClear
        );

        chargeParticlesInstance.Play(true);
    }

    private void StopChargeParticles()
    {
        if (chargeParticlesInstance == null)
            return;

        chargeParticlesInstance.Stop(
            true,
            ParticleSystemStopBehavior.StopEmittingAndClear
        );

        chargeParticlesInstance.gameObject.SetActive(false);
    }

    private void PlayMuzzleFlash()
    {
        if (muzzleFlashInstance == null || eye == null)
            return;

        muzzleFlashInstance.gameObject.SetActive(true);

        muzzleFlashInstance.transform.SetPositionAndRotation(
            eye.position,
            eye.rotation
        );

        muzzleFlashInstance.Stop(
            true,
            ParticleSystemStopBehavior.StopEmittingAndClear
        );

        muzzleFlashInstance.Play(true);
    }

    private void PlayImpactParticles(
        Vector3 position,
        Quaternion rotation)
    {
        if (impactParticlesInstance == null)
            return;

        impactParticlesInstance.gameObject.SetActive(true);

        impactParticlesInstance.transform.SetPositionAndRotation(
            position,
            rotation
        );

        impactParticlesInstance.Stop(
            true,
            ParticleSystemStopBehavior.StopEmittingAndClear
        );

        impactParticlesInstance.Play(true);
    }

    private void StopAttack()
    {
        currentPhase = AttackPhase.Idle;
        phaseTimer = 0f;
        shotPunchTimer = 0f;

        StopChargeParticles();
        SetEyeIdle();
        HideVisuals();
    }

    private void HideVisuals()
    {
        if (beamInstance != null)
            beamInstance.SetActive(false);

        if (dotInstance != null)
            dotInstance.SetActive(false);
    }

    private void OnDisable()
    {
        StopAttack();

        if (impactParticlesInstance != null)
        {
            impactParticlesInstance.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear
            );
        }

        if (muzzleFlashInstance != null)
        {
            muzzleFlashInstance.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear
            );
        }
    }

    private void OnDrawGizmosSelected()
    {
        Transform origin = eye != null ? eye : transform;

        Gizmos.color = Color.red;

        Vector3 leftDirection =
            Quaternion.Euler(0f, -maxFireAngle, 0f) * transform.forward;

        Vector3 rightDirection =
            Quaternion.Euler(0f, maxFireAngle, 0f) * transform.forward;

        Gizmos.DrawLine(
            origin.position,
            origin.position + leftDirection * laserRange
        );

        Gizmos.DrawLine(
            origin.position,
            origin.position + rightDirection * laserRange
        );

        Gizmos.DrawLine(
            origin.position,
            origin.position + transform.forward * laserRange
        );
    }
}