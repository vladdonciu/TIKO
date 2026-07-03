using UnityEngine;

[CreateAssetMenu(fileName = "NewEmotion", menuName = "Tiko/Emotion")]
public class RobotEmotion : ScriptableObject
{
    public string emotionName;

    [Header("Eye Shape")]
    [Range(0.05f, 0.4f)]  public float eyeWidth = 0.12f;
    [Range(0.1f, 0.6f)]   public float eyeHeight = 0.35f;
    [Range(0f, 1f)]        public float cornerRadius = 0.3f;

    [Header("Position")]
    [Range(0.05f, 0.3f)]   public float eyeSpacing = 0.15f;
    [Range(-0.2f, 0.2f)]   public float eyeVerticalOffset = 0.05f;

    [Header("Deformation")]
    [Range(-45f, 45f)] public float leftEyeRotation = 0f;
    [Range(-45f, 45f)] public float rightEyeRotation = 0f;
    [Range(0.5f, 1.5f)] public float leftEyeSquashX = 1f;
    [Range(0.5f, 1.5f)] public float leftEyeSquashY = 1f;
    [Range(0.5f, 1.5f)] public float rightEyeSquashX = 1f;
    [Range(0.5f, 1.5f)] public float rightEyeSquashY = 1f;

    [Header("Emission HDR - Glow")]
    [ColorUsage(false, true)]
    public Color eyeEmissionColor = new Color(0f, 2f, 3f, 1f);
    [ColorUsage(false, true)]
    public Color screenEmissionColor = new Color(0.05f, 0.08f, 0.12f, 1f);
    public Color screenBaseColor = new Color(0.02f, 0.02f, 0.04f, 1f);
    [Range(0f, 0.15f)] public float glowSoftness = 0.04f;
    [Range(0f, 3f)]    public float glowIntensity = 1.2f;

    [Header("Screen Effects")]
    [Range(0f, 1f)]   public float scanlineIntensity = 0.08f;
    [Range(20f, 500f)] public float scanlineCount = 180f;
    [Range(0f, 5f)]   public float scanlineSpeed = 0.8f;
    [Range(0f, 0.1f)] public float screenFlicker = 0.02f;
    [Range(0f, 0.1f)] public float screenNoise = 0.015f;
    [Range(0f, 1f)]   public float vignetteIntensity = 0.3f;

    [Header("Eye Tracking")]
    [Range(0f, 1f)]   public float lookInfluence = 1f;
    [Range(0f, 0.1f)] public float maxLookOffset = 0.05f;

    [Header("Blink")]
    public float blinkInterval = 3f;
    [Range(0.05f, 0.5f)] public float blinkDuration = 0.15f;
}