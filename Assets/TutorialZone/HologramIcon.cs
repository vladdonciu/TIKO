using UnityEngine;
using UnityEngine.UI;

public class HologramIcon : MonoBehaviour
{
    [Header("Refs")]
    public CanvasGroup canvasGroup;      // CanvasGroup de pe hologramă
    public Image glowImage;             // Panel_Glow
    public RectTransform iconRoot;      // un parent pentru taste

    [Header("Pulse Settings")]
    public float pulseSpeed = 2f;
    public float minAlpha = 0.4f;
    public float maxAlpha = 0.9f;

    [Header("Hover Settings")]
    public float hoverAmplitude = 5f;
    public float hoverSpeed = 1f;

    [Header("Flicker Settings")]
    public float flickerIntensity = 0.1f;
    public float flickerSpeed = 25f;

    private Vector3 startPos;

    void Start()
    {
        if (iconRoot != null)
            startPos = iconRoot.localPosition;
    }

    void Update()
    {
        float t = Time.time;

        // Pulse pe alpha (respirația hologramei)
        if (canvasGroup != null)
        {
            float pulse = Mathf.Lerp(
                minAlpha,
                maxAlpha,
                (Mathf.Sin(t * pulseSpeed) + 1f) * 0.5f
            );
            canvasGroup.alpha = pulse;
        }

        // Hover ușor sus-jos
        if (iconRoot != null)
        {
            float hover = Mathf.Sin(t * hoverSpeed) * hoverAmplitude;
            iconRoot.localPosition = startPos + new Vector3(0f, hover, 0f);
        }

        // Flicker mic pe glow (ca un glitch digital)
        if (glowImage != null && canvasGroup != null)
        {
            Color c = glowImage.color;
            float noise = (Mathf.PerlinNoise(t * flickerSpeed, 0f) - 0.5f)
                          * 2f * flickerIntensity;
            c.a = Mathf.Clamp01(canvasGroup.alpha + noise);
            glowImage.color = c;
        }
    }
}