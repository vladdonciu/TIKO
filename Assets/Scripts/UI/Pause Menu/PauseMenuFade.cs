using System.Collections;
using UnityEngine;

public class PauseMenuFade : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float fadeDuration = 0.25f;

    private Coroutine fadeRoutine;

    public void Show()
    {
        gameObject.SetActive(true);
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(Fade(0f, 1f));
    }

    public void Hide()
    {
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(Fade(1f, 0f, true));
    }

    private IEnumerator Fade(float from, float to, bool disableAfter = false)
    {
        float t = 0f;
        canvasGroup.alpha = from;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, t / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = to;

        if (disableAfter) gameObject.SetActive(false);
    }
}