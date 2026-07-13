using System.Collections;
using UnityEngine;
using UnityEngine.UI;


public class FlashVFXController : MonoBehaviour
{
    [Header("Particle Systems")]
    [SerializeField] private ParticleSystem energyBurst;
    [SerializeField] private ParticleSystem electricArcs;


    [Header("Light")]
    [SerializeField] private Light flashLight;
    [SerializeField] private float maxLightIntensity = 12f;
    [SerializeField] private float lightRiseDuration = 0.05f;
    [SerializeField] private float lightFallDuration = 0.25f;


    [Header("Screen Flash")]
    [SerializeField] private Image screenFlashImage;
    [SerializeField] private float maxScreenAlpha = 0.6f;
    [SerializeField] private float screenFlashRise = 0.04f;
    [SerializeField] private float screenFlashFall = 0.3f;


    [Header("Screen Flash - Sustained Profile")]
    [SerializeField] private float midAlpha = 0.15f;
    [SerializeField] private float midFadeInDuration = 0.15f;
    [SerializeField] private float midFadeOutDuration = 0.15f;


    [Header("Camera Shake")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float shakeDuration = 0.25f;
    [SerializeField] private float shakeMagnitude = 0.15f;


    [Header("Audio - Main")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip flashInSound;
    [SerializeField] private AudioClip transformLoopSound;
    [SerializeField] private AudioClip flashOutSound;


    [Header("Audio - Extra Layers")]
    [SerializeField] private AudioSource audioSourceLayer;
    [SerializeField] private AudioClip flashInLayerSound;
    [SerializeField] [Range(0f, 1f)] private float flashInLayerVolume = 0.7f;
    [SerializeField] private AudioClip transformLoopLayerSound;
    [SerializeField] [Range(0f, 1f)] private float transformLayerVolume = 0.7f;


    public float ScreenFlashRise => screenFlashRise;


    private Coroutine continuousShakeRoutine;
    private Coroutine sustainedFlashRoutine;
    private Vector3 originalCameraPos;


    public IEnumerator PlayFullFlash()
    {
        energyBurst?.Play();
        electricArcs?.Play();


        StartCoroutine(AnimateLight());


        if (cameraTransform != null)
            StartCoroutine(ShakeCamera());


        yield return null;
    }


    public void StartContinuousShake(float totalDuration, float magnitude = -1f)
    {
        if (cameraTransform == null) return;


        if (continuousShakeRoutine != null)
            StopCoroutine(continuousShakeRoutine);


        float mag = magnitude > 0f ? magnitude : shakeMagnitude;
        continuousShakeRoutine = StartCoroutine(ContinuousShakeRoutine(totalDuration, mag));
    }


    public void StopContinuousShake()
    {
        if (continuousShakeRoutine != null)
        {
            StopCoroutine(continuousShakeRoutine);
            continuousShakeRoutine = null;
        }


        if (cameraTransform != null)
            cameraTransform.localPosition = originalCameraPos;
    }


    public void StartSustainedScreenFlash(float totalDuration)
    {
        if (screenFlashImage == null) return;


        if (sustainedFlashRoutine != null)
            StopCoroutine(sustainedFlashRoutine);


        sustainedFlashRoutine = StartCoroutine(SustainedFlashRoutine(totalDuration));
    }


    private IEnumerator SustainedFlashRoutine(float totalDuration)
    {
        Color c = screenFlashImage.color;
        float t = 0f;


        while (t < screenFlashRise)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(0f, maxScreenAlpha, t / screenFlashRise);
            screenFlashImage.color = c;
            yield return null;
        }


        t = 0f;
        while (t < midFadeInDuration)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(maxScreenAlpha, midAlpha, t / midFadeInDuration);
            screenFlashImage.color = c;
            yield return null;
        }


        float holdDuration = Mathf.Max(0f,
            totalDuration - screenFlashRise - midFadeInDuration - midFadeOutDuration - screenFlashFall);


        float held = 0f;
        while (held < holdDuration)
        {
            held += Time.deltaTime;
            c.a = midAlpha;
            screenFlashImage.color = c;
            yield return null;
        }


        t = 0f;
        while (t < midFadeOutDuration)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(midAlpha, maxScreenAlpha, t / midFadeOutDuration);
            screenFlashImage.color = c;
            yield return null;
        }


        t = 0f;
        while (t < screenFlashFall)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(maxScreenAlpha, 0f, t / screenFlashFall);
            screenFlashImage.color = c;
            yield return null;
        }


        c.a = 0f;
        screenFlashImage.color = c;
        sustainedFlashRoutine = null;
    }


    private IEnumerator ContinuousShakeRoutine(float totalDuration, float magnitude)
    {
        originalCameraPos = cameraTransform.localPosition;
        float elapsed = 0f;


        float rampIn = Mathf.Min(0.1f, totalDuration * 0.15f);
        float rampOut = Mathf.Min(0.2f, totalDuration * 0.3f);


        while (elapsed < totalDuration)
        {
            elapsed += Time.deltaTime;


            float intensity;
            if (elapsed < rampIn)
                intensity = elapsed / rampIn;
            else if (elapsed > totalDuration - rampOut)
                intensity = (totalDuration - elapsed) / rampOut;
            else
                intensity = 1f;


            intensity = Mathf.Clamp01(intensity);


            float offsetX = (Mathf.PerlinNoise(Time.time * 25f, 0f) - 0.5f) * 2f * magnitude * intensity;
            float offsetY = (Mathf.PerlinNoise(0f, Time.time * 25f) - 0.5f) * 2f * magnitude * intensity;


            cameraTransform.localPosition = originalCameraPos + new Vector3(offsetX, offsetY, 0f);


            yield return null;
        }


        cameraTransform.localPosition = originalCameraPos;
        continuousShakeRoutine = null;
    }


    private IEnumerator AnimateLight()
    {
        if (flashLight == null) yield break;


        float t = 0f;
        while (t < lightRiseDuration)
        {
            t += Time.deltaTime;
            flashLight.intensity = Mathf.Lerp(0f, maxLightIntensity, t / lightRiseDuration);
            yield return null;
        }


        t = 0f;
        while (t < lightFallDuration)
        {
            t += Time.deltaTime;
            flashLight.intensity = Mathf.Lerp(maxLightIntensity, 0f, t / lightFallDuration);
            yield return null;
        }


        flashLight.intensity = 0f;
    }


    private IEnumerator ShakeCamera()
    {
        Vector3 originalPos = cameraTransform.localPosition;
        float elapsed = 0f;


        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            float damper = 1f - (elapsed / shakeDuration);


            float offsetX = Random.Range(-1f, 1f) * shakeMagnitude * damper;
            float offsetY = Random.Range(-1f, 1f) * shakeMagnitude * damper;


            cameraTransform.localPosition = originalPos + new Vector3(offsetX, offsetY, 0f);


            yield return null;
        }


        cameraTransform.localPosition = originalPos;
    }


    public void PlayFlashInSound()
    {
        if (audioSource != null && flashInSound != null)
            audioSource.PlayOneShot(flashInSound);


        if (audioSourceLayer != null && flashInLayerSound != null)
            audioSourceLayer.PlayOneShot(flashInLayerSound, flashInLayerVolume);
    }


    public void PlayTransformSound(float duration)
    {
        if (audioSource != null && transformLoopSound != null)
        {
            audioSource.clip = transformLoopSound;
            audioSource.loop = true;
            audioSource.Play();
        }


        if (audioSourceLayer != null && transformLoopLayerSound != null)
        {
            audioSourceLayer.clip = transformLoopLayerSound;
            audioSourceLayer.loop = true;
            audioSourceLayer.volume = transformLayerVolume;
            audioSourceLayer.Play();
        }


        StartCoroutine(StopTransformSoundAfter(duration));
    }


    private IEnumerator StopTransformSoundAfter(float duration)
    {
        yield return new WaitForSeconds(duration);


        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.loop = false;
        }


        if (audioSourceLayer != null)
        {
            audioSourceLayer.Stop();
            audioSourceLayer.loop = false;
        }
    }


    public void PlayFlashOutSound()
    {
        if (audioSource != null && flashOutSound != null)
            audioSource.PlayOneShot(flashOutSound);
    }
}