using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance;

    [Header("References")]
    [SerializeField] private Image fadePanel;

    [Header("Settings")]
    [SerializeField] private float fadeDuration = 0.8f;

    private void Awake()
    {
        // Singleton - exista doar un ScreenFader
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (!fadePanel) fadePanel = GetComponentInChildren<Image>();
        SetAlpha(0f); // transparent la start
    }

    public IEnumerator FadeOut() // transparent → negru
    {
        yield return StartCoroutine(Fade(0f, 1f));
    }

    public IEnumerator FadeIn() // negru → transparent
    {
        yield return StartCoroutine(Fade(1f, 0f));
    }

    private IEnumerator Fade(float from, float to)
    {
        float elapsed = 0f;
        SetAlpha(from);

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            // Smoothstep pentru fade mai natural
            t = t * t * (3f - 2f * t);
            SetAlpha(Mathf.Lerp(from, to, t));
            yield return null;
        }

        SetAlpha(to);
    }

    private void SetAlpha(float a)
    {
        if (!fadePanel) return;
        Color c = fadePanel.color;
        c.a = a;
        fadePanel.color = c;
    }
}