using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class GlitchEffect : MonoBehaviour
{
    [SerializeField] private RawImage glitchImage;
    [SerializeField] private Material glitchMaterial;
    [SerializeField] private float burstDuration = 0.25f;

    public void PlayBurst()
    {
        StopAllCoroutines();
        StartCoroutine(Burst());
    }

    private IEnumerator Burst()
    {
        glitchImage.gameObject.SetActive(true);
        glitchImage.texture = Texture2D.whiteTexture;
        glitchImage.material = glitchMaterial;

        float t = 0f;
        while (t < burstDuration)
        {
            t += Time.unscaledDeltaTime;
            float amount = Mathf.Lerp(1f, 0f, t / burstDuration);
            glitchMaterial.SetFloat("_GlitchAmount", amount);
            glitchMaterial.SetFloat("_Time01", Time.unscaledTime);
            yield return null;
        }

        glitchMaterial.SetFloat("_GlitchAmount", 0f);
        glitchImage.gameObject.SetActive(false);
    }
}