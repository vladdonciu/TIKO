using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BackgroundBlur : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private RawImage blurTargetImage;
    [SerializeField] private Material blurMaterial;
    [SerializeField] private int blurIterations = 3;
    [SerializeField] private float fadeDuration = 0.3f;

    private Coroutine fadeRoutine;

    public void CaptureAndBlur()
    {
        int w = Screen.width;
        int h = Screen.height;

        RenderTexture rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
        rt.Create();

        mainCamera.targetTexture = rt;
        mainCamera.Render();
        mainCamera.targetTexture = null;

        RenderTexture current = rt;
        for (int i = 0; i < blurIterations; i++)
        {
            RenderTexture pass1 = RenderTexture.GetTemporary(w, h, 0);
            Graphics.Blit(current, pass1, blurMaterial, 0);
            if (current != rt) RenderTexture.ReleaseTemporary(current);

            RenderTexture pass2 = RenderTexture.GetTemporary(w, h, 0);
            Graphics.Blit(pass1, pass2, blurMaterial, 1);
            RenderTexture.ReleaseTemporary(pass1);

            current = pass2;
        }

        blurTargetImage.texture = current;

        blurTargetImage.texture = current;
        Debug.Log("Blur texture assigned: " + (current != null) + " | Size: " + current.width + "x" + current.height);

        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(Fade(0f, 1f));
    }

    public void ClearBlur()
    {
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(Fade(1f, 0f, true));
    }

    private IEnumerator Fade(float from, float to, bool clearAfter = false)
    {
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Lerp(from, to, t / fadeDuration);
            blurTargetImage.color = new Color(1f, 1f, 1f, a);
            yield return null;
        }
        blurTargetImage.color = new Color(1f, 1f, 1f, to);

        if (clearAfter) blurTargetImage.texture = null;
    }
}