using UnityEngine;
using System.Collections;

public class TikoFaceController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Renderer faceRenderer;
    [SerializeField] private Camera mainCamera;

    [Header("Emotions")]
    [SerializeField] private RobotEmotion defaultEmotion;
    [SerializeField] private float transitionSpeed = 4f;

    [Header("Mouse Eye Tracking")]
    [SerializeField] private bool enableEyeTracking = true;
    [SerializeField] private float eyeTrackSpeed = 8f;
    [Tooltip("Cat de departe poate fi camera ca sa mai functioneze tracking-ul")]
    [SerializeField] private float maxTrackDistance = 30f;

    [Header("Blink")]
    [SerializeField] private bool enableBlinking = true;

    // ── Private ──
    private Material mat;
    private RobotEmotion currentEmotion;
    private RobotEmotion displayEmotion;
    private Vector2 currentLookOffset;
    private Vector2 targetLookOffset;
    private float nextBlinkTime;
    private bool isBlinking;
    private Coroutine transitionCoroutine;

    // ── Shader IDs ──
    static readonly int id_EyeWidth          = Shader.PropertyToID("_EyeWidth");
    static readonly int id_EyeHeight         = Shader.PropertyToID("_EyeHeight");
    static readonly int id_CornerRadius      = Shader.PropertyToID("_CornerRadius");
    static readonly int id_EyeSpacing        = Shader.PropertyToID("_EyeSpacing");
    static readonly int id_EyeVerticalOffset = Shader.PropertyToID("_EyeVerticalOffset");
    static readonly int id_LookOffset        = Shader.PropertyToID("_LookOffset");
    static readonly int id_LeftEyeRotation   = Shader.PropertyToID("_LeftEyeRotation");
    static readonly int id_RightEyeRotation  = Shader.PropertyToID("_RightEyeRotation");
    static readonly int id_LeftEyeSquash     = Shader.PropertyToID("_LeftEyeSquash");
    static readonly int id_RightEyeSquash    = Shader.PropertyToID("_RightEyeSquash");
    static readonly int id_BlinkAmount       = Shader.PropertyToID("_BlinkAmount");
    static readonly int id_EyeEmissionColor  = Shader.PropertyToID("_EyeEmissionColor");
    static readonly int id_ScreenColor       = Shader.PropertyToID("_ScreenColor");
    static readonly int id_ScreenEmissionColor = Shader.PropertyToID("_ScreenEmissionColor");
    static readonly int id_GlowSoftness      = Shader.PropertyToID("_GlowSoftness");
    static readonly int id_GlowIntensity     = Shader.PropertyToID("_GlowIntensity");
    static readonly int id_ScanlineIntensity = Shader.PropertyToID("_ScanlineIntensity");
    static readonly int id_ScanlineCount     = Shader.PropertyToID("_ScanlineCount");
    static readonly int id_ScanlineSpeed     = Shader.PropertyToID("_ScanlineSpeed");
    static readonly int id_ScreenFlicker     = Shader.PropertyToID("_ScreenFlicker");
    static readonly int id_ScreenNoise       = Shader.PropertyToID("_ScreenNoise");
    static readonly int id_VignetteIntensity = Shader.PropertyToID("_VignetteIntensity");

    void Awake()
    {
        mat = faceRenderer.material;
        currentEmotion = defaultEmotion;
        displayEmotion = CloneEmotion(defaultEmotion);
        nextBlinkTime = Time.time + Random.Range(1f, defaultEmotion.blinkInterval);

        // Daca nu ai setat camera manual, ia camera principala
        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    void Update()
    {
        UpdateMouseTracking();
        UpdateBlinking();
        PushToShader();
    }

    // ══════════════════════════════
    //  MOUSE EYE TRACKING
    // ══════════════════════════════

    void UpdateMouseTracking()
    {
        if (!enableEyeTracking || displayEmotion == null || mainCamera == null)
        {
            targetLookOffset = Vector2.zero;
            currentLookOffset = Vector2.Lerp(currentLookOffset, targetLookOffset,
                                              Time.deltaTime * eyeTrackSpeed);
            return;
        }

        // Verificam daca camera e prea departe
        float camDist = Vector3.Distance(mainCamera.transform.position, faceRenderer.transform.position);
        if (camDist > maxTrackDistance)
        {
            targetLookOffset = Vector2.zero;
            currentLookOffset = Vector2.Lerp(currentLookOffset, targetLookOffset,
                                              Time.deltaTime * eyeTrackSpeed);
            return;
        }

        // Ray din camera prin pozitia mouse-ului
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        // Planul fetei lui Tiko (pozitia si normala ecranului)
        // Normala ecranului = forward-ul FaceScreen-ului
        Transform faceT = faceRenderer.transform;
        Plane facePlane = new Plane(faceT.forward, faceT.position);

        if (facePlane.Raycast(ray, out float enter))
        {
            // Punctul 3D unde mouse-ul "loveste" planul fetei
            Vector3 worldHitPoint = ray.GetPoint(enter);

            // Convertim in spatiul local al ecranului
            Vector3 localHit = faceT.InverseTransformPoint(worldHitPoint);

            // localHit.x = stanga/dreapta, localHit.y = sus/jos
            // Limitam la maxLookOffset din emotia curenta
            float maxOff = displayEmotion.maxLookOffset;
            float inf = displayEmotion.lookInfluence;

            targetLookOffset = new Vector2(
                Mathf.Clamp(localHit.x * inf, -maxOff, maxOff),
                Mathf.Clamp(localHit.y * inf, -maxOff, maxOff)
            );
        }
        else
        {
            // Mouse-ul nu intersecteaza planul (e in spatele ecranului)
            targetLookOffset = Vector2.zero;
        }

        // Smooth follow
        currentLookOffset = Vector2.Lerp(currentLookOffset, targetLookOffset,
                                          Time.deltaTime * eyeTrackSpeed);
    }

    // ══════════════════════════════
    //  BLINKING
    // ══════════════════════════════

    void UpdateBlinking()
    {
        if (!enableBlinking || displayEmotion == null) return;
        if (!isBlinking && Time.time >= nextBlinkTime)
            StartCoroutine(BlinkCo());
    }

    IEnumerator BlinkCo()
    {
        isBlinking = true;
        float half = displayEmotion.blinkDuration * 0.5f;

        float t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            mat.SetFloat(id_BlinkAmount, Mathf.Lerp(0f, 1f, t / half));
            yield return null;
        }

        t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            mat.SetFloat(id_BlinkAmount, Mathf.Lerp(1f, 0f, t / half));
            yield return null;
        }

        mat.SetFloat(id_BlinkAmount, 0f);
        isBlinking = false;
        nextBlinkTime = Time.time + displayEmotion.blinkInterval
                        + Random.Range(-0.5f, 1.5f);
    }

    // ══════════════════════════════
    //  EMOTION MANAGEMENT
    // ══════════════════════════════

    public void SetEmotion(RobotEmotion newEmotion)
    {
        if (newEmotion == null) return;
        if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
        transitionCoroutine = StartCoroutine(TransitionCo(newEmotion));
    }

    public void FlashEmotion(RobotEmotion emotion, float duration = 2f)
    {
        StartCoroutine(FlashCo(emotion, duration));
    }

    IEnumerator FlashCo(RobotEmotion emotion, float dur)
    {
        RobotEmotion prev = currentEmotion;
        SetEmotion(emotion);
        yield return new WaitForSeconds(dur);
        SetEmotion(prev);
    }

    IEnumerator TransitionCo(RobotEmotion target)
    {
        RobotEmotion from = CloneEmotion(displayEmotion);
        currentEmotion = target;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * transitionSpeed;
            float s = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
            LerpEmotion(from, target, s, displayEmotion);
            yield return null;
        }

        CopyEmotion(target, displayEmotion);
        Destroy(from);
        transitionCoroutine = null;
    }

    // ══════════════════════════════
    //  PUSH TO SHADER
    // ══════════════════════════════

    void PushToShader()
    {
        if (displayEmotion == null || mat == null) return;

        var e = displayEmotion;
        mat.SetFloat(id_EyeWidth, e.eyeWidth);
        mat.SetFloat(id_EyeHeight, e.eyeHeight);
        mat.SetFloat(id_CornerRadius, e.cornerRadius);
        mat.SetFloat(id_EyeSpacing, e.eyeSpacing);
        mat.SetFloat(id_EyeVerticalOffset, e.eyeVerticalOffset);
        mat.SetVector(id_LookOffset, new Vector4(currentLookOffset.x, currentLookOffset.y, 0, 0));
        mat.SetFloat(id_LeftEyeRotation, e.leftEyeRotation);
        mat.SetFloat(id_RightEyeRotation, e.rightEyeRotation);
        mat.SetVector(id_LeftEyeSquash, new Vector4(e.leftEyeSquashX, e.leftEyeSquashY, 0, 0));
        mat.SetVector(id_RightEyeSquash, new Vector4(e.rightEyeSquashX, e.rightEyeSquashY, 0, 0));
        mat.SetColor(id_EyeEmissionColor, e.eyeEmissionColor);
        mat.SetColor(id_ScreenColor, e.screenBaseColor);
        mat.SetColor(id_ScreenEmissionColor, e.screenEmissionColor);
        mat.SetFloat(id_GlowSoftness, e.glowSoftness);
        mat.SetFloat(id_GlowIntensity, e.glowIntensity);
        mat.SetFloat(id_ScanlineIntensity, e.scanlineIntensity);
        mat.SetFloat(id_ScanlineCount, e.scanlineCount);
        mat.SetFloat(id_ScanlineSpeed, e.scanlineSpeed);
        mat.SetFloat(id_ScreenFlicker, e.screenFlicker);
        mat.SetFloat(id_ScreenNoise, e.screenNoise);
        mat.SetFloat(id_VignetteIntensity, e.vignetteIntensity);
    }

    // ══════════════════════════════
    //  HELPERS
    // ══════════════════════════════

    RobotEmotion CloneEmotion(RobotEmotion src)
    {
        var c = ScriptableObject.CreateInstance<RobotEmotion>();
        CopyEmotion(src, c);
        return c;
    }

    void CopyEmotion(RobotEmotion src, RobotEmotion dst)
    {
        dst.eyeWidth = src.eyeWidth;
        dst.eyeHeight = src.eyeHeight;
        dst.cornerRadius = src.cornerRadius;
        dst.eyeSpacing = src.eyeSpacing;
        dst.eyeVerticalOffset = src.eyeVerticalOffset;
        dst.leftEyeRotation = src.leftEyeRotation;
        dst.rightEyeRotation = src.rightEyeRotation;
        dst.leftEyeSquashX = src.leftEyeSquashX;
        dst.leftEyeSquashY = src.leftEyeSquashY;
        dst.rightEyeSquashX = src.rightEyeSquashX;
        dst.rightEyeSquashY = src.rightEyeSquashY;
        dst.eyeEmissionColor = src.eyeEmissionColor;
        dst.screenEmissionColor = src.screenEmissionColor;
        dst.screenBaseColor = src.screenBaseColor;
        dst.glowSoftness = src.glowSoftness;
        dst.glowIntensity = src.glowIntensity;
        dst.scanlineIntensity = src.scanlineIntensity;
        dst.scanlineCount = src.scanlineCount;
        dst.scanlineSpeed = src.scanlineSpeed;
        dst.screenFlicker = src.screenFlicker;
        dst.screenNoise = src.screenNoise;
        dst.vignetteIntensity = src.vignetteIntensity;
        dst.lookInfluence = src.lookInfluence;
        dst.maxLookOffset = src.maxLookOffset;
        dst.blinkInterval = src.blinkInterval;
        dst.blinkDuration = src.blinkDuration;
    }

    void LerpEmotion(RobotEmotion a, RobotEmotion b, float t, RobotEmotion dst)
    {
        dst.eyeWidth = Mathf.Lerp(a.eyeWidth, b.eyeWidth, t);
        dst.eyeHeight = Mathf.Lerp(a.eyeHeight, b.eyeHeight, t);
        dst.cornerRadius = Mathf.Lerp(a.cornerRadius, b.cornerRadius, t);
        dst.eyeSpacing = Mathf.Lerp(a.eyeSpacing, b.eyeSpacing, t);
        dst.eyeVerticalOffset = Mathf.Lerp(a.eyeVerticalOffset, b.eyeVerticalOffset, t);
        dst.leftEyeRotation = Mathf.Lerp(a.leftEyeRotation, b.leftEyeRotation, t);
        dst.rightEyeRotation = Mathf.Lerp(a.rightEyeRotation, b.rightEyeRotation, t);
        dst.leftEyeSquashX = Mathf.Lerp(a.leftEyeSquashX, b.leftEyeSquashX, t);
        dst.leftEyeSquashY = Mathf.Lerp(a.leftEyeSquashY, b.leftEyeSquashY, t);
        dst.rightEyeSquashX = Mathf.Lerp(a.rightEyeSquashX, b.rightEyeSquashX, t);
        dst.rightEyeSquashY = Mathf.Lerp(a.rightEyeSquashY, b.rightEyeSquashY, t);
        dst.eyeEmissionColor = Color.Lerp(a.eyeEmissionColor, b.eyeEmissionColor, t);
        dst.screenEmissionColor = Color.Lerp(a.screenEmissionColor, b.screenEmissionColor, t);
        dst.screenBaseColor = Color.Lerp(a.screenBaseColor, b.screenBaseColor, t);
        dst.glowSoftness = Mathf.Lerp(a.glowSoftness, b.glowSoftness, t);
        dst.glowIntensity = Mathf.Lerp(a.glowIntensity, b.glowIntensity, t);
        dst.scanlineIntensity = Mathf.Lerp(a.scanlineIntensity, b.scanlineIntensity, t);
        dst.scanlineCount = Mathf.Lerp(a.scanlineCount, b.scanlineCount, t);
        dst.scanlineSpeed = Mathf.Lerp(a.scanlineSpeed, b.scanlineSpeed, t);
        dst.screenFlicker = Mathf.Lerp(a.screenFlicker, b.screenFlicker, t);
        dst.screenNoise = Mathf.Lerp(a.screenNoise, b.screenNoise, t);
        dst.vignetteIntensity = Mathf.Lerp(a.vignetteIntensity, b.vignetteIntensity, t);
        dst.lookInfluence = Mathf.Lerp(a.lookInfluence, b.lookInfluence, t);
        dst.maxLookOffset = Mathf.Lerp(a.maxLookOffset, b.maxLookOffset, t);
        dst.blinkInterval = Mathf.Lerp(a.blinkInterval, b.blinkInterval, t);
        dst.blinkDuration = Mathf.Lerp(a.blinkDuration, b.blinkDuration, t);
    }

    void OnDestroy()
    {
        if (mat != null) Destroy(mat);
        if (displayEmotion != null) Destroy(displayEmotion);
    }
}