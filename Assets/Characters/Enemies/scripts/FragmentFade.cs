using UnityEngine;
using System.Collections;

public class FragmentFade : MonoBehaviour
{
    private Renderer fragRenderer;
    private Material fragMaterial;
    private Color baseColor;
    private Color emissionColor;
    private Coroutine fadeCoroutine;

    public void StartFade(float minDelay, float maxDelay, float fadeDuration)
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fragRenderer = GetComponent<Renderer>();
        fragMaterial = fragRenderer.material;

        SetSurfaceTransparent();

        float delay = Random.Range(minDelay, maxDelay);
        fadeCoroutine = StartCoroutine(FadeRoutine(delay, fadeDuration));
    }

    public void SetColors(Color newBaseColor, Color newEmissionColor)
    {
        baseColor = newBaseColor;
        emissionColor = newEmissionColor;

        if (fragMaterial == null)
            fragMaterial = GetComponent<Renderer>().material;

        fragMaterial.SetColor("_BaseColor", baseColor);
        fragMaterial.EnableKeyword("_EMISSION");
        fragMaterial.SetColor("_EmissionColor", emissionColor);
    }

    private void SetSurfaceTransparent()
    {
        fragMaterial.SetFloat("_Surface", 1f);
        fragMaterial.SetFloat("_Blend", 0f);
        fragMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        fragMaterial.SetOverrideTag("RenderType", "Transparent");
        fragMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        fragMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        fragMaterial.SetInt("_ZWrite", 0);
        fragMaterial.DisableKeyword("_ALPHATEST_ON");
        fragMaterial.EnableKeyword("_ALPHABLEND_ON");
        fragMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
    }

    private IEnumerator FadeRoutine(float delay, float fadeDuration)
    {
        yield return new WaitForSeconds(delay);

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = 1f - Mathf.Clamp01(elapsed / fadeDuration);

            Color fadedBase = baseColor;
            fadedBase.a = t;
            fragMaterial.SetColor("_BaseColor", fadedBase);

            Color fadedEmission = emissionColor * t;
            fragMaterial.SetColor("_EmissionColor", fadedEmission);

            yield return null;
        }

        ResetFragment();
        FragmentPool.Instance.ReturnFragment(gameObject);
    }

    private void ResetFragment()
    {
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = true;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
}