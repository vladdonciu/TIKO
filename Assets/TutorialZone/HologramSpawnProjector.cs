using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class HologramSpawnProjector : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Copilul care conține panelul și toate tastele hologramei")]
    [SerializeField] private RectTransform visualRoot;

    [Header("Spawn Projection")]
    [SerializeField] private float spawnDuration = 0.6f;

    [Range(0.01f, 1f)]
    [SerializeField] private float spawnHeightScale = 0.04f;

    [SerializeField] private AnimationCurve spawnCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Despawn")]
    [Tooltip("La despawn folosim numai fade-out, fără scale")]
    [SerializeField] private float fadeOutDuration = 0.45f;

    [SerializeField] private AnimationCurve fadeOutCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private CanvasGroup canvasGroup;
    private Vector3 originalScale;
    private Coroutine currentAnimation;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();

        if (visualRoot == null)
        {
            Debug.LogError(
                "Lipsește Visual Root în HologramSpawnProjector.",
                this
            );
            return;
        }

        originalScale = visualRoot.localScale;
    }

    public void StartSpawnEffect()
    {
        if (visualRoot == null)
            return;

        StopAnimation();
        currentAnimation = StartCoroutine(SpawnRoutine());
    }

    public void StartDespawnEffect(Action onComplete = null)
    {
        StopAnimation();
        currentAnimation = StartCoroutine(FadeOutRoutine(onComplete));
    }

    private IEnumerator SpawnRoutine()
    {
        Vector3 startScale = originalScale;
        startScale.y = originalScale.y * spawnHeightScale;

        visualRoot.localScale = startScale;
        canvasGroup.alpha = 0f;

        float elapsed = 0f;

        while (elapsed < spawnDuration)
        {
            elapsed += Time.deltaTime;

            float progress = Mathf.Clamp01(elapsed / spawnDuration);
            float eased = spawnCurve.Evaluate(progress);

            visualRoot.localScale = Vector3.Lerp(
                startScale,
                originalScale,
                eased
            );

            canvasGroup.alpha = eased;

            yield return null;
        }

        visualRoot.localScale = originalScale;
        canvasGroup.alpha = 1f;
        currentAnimation = null;
    }

    private IEnumerator FadeOutRoutine(Action onComplete)
    {
        // Nu schimbăm scale-ul la despawn.
        // Asta elimină complet impresia de spawn invers/sacadare.
        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;

            float progress = Mathf.Clamp01(elapsed / fadeOutDuration);
            float eased = fadeOutCurve.Evaluate(progress);

            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, eased);

            yield return null;
        }

        canvasGroup.alpha = 0f;
        currentAnimation = null;

        onComplete?.Invoke();
    }

    private void StopAnimation()
    {
        if (currentAnimation == null)
            return;

        StopCoroutine(currentAnimation);
        currentAnimation = null;
    }

    private void OnDisable()
    {
        StopAnimation();

        // Pregătește holograma pentru următoarea activare.
        if (visualRoot != null)
            visualRoot.localScale = originalScale;
    }
}