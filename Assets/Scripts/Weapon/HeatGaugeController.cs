using UnityEngine;

public class HeatGaugeController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Renderer gaugeRenderer;
    [SerializeField] private AudioSource audioSource;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip overheatClip;
    [SerializeField] private AudioClip cooldownReadyClip;

    [Header("Heat Settings")]
    [SerializeField] private float heatPerShot = 0.12f;
    [SerializeField] private float coolRate = 0.35f;
    [SerializeField] private float overheatCoolRate = 0.5f;

    [Header("Colors")]
    [SerializeField] private Color coolColor = new Color(0f, 1f, 1f);
    [SerializeField] private Color midColor = new Color(1f, 0.55f, 0f);
    [SerializeField] private Color hotColor = new Color(1f, 0f, 0.19f);

    private float heat;
    private bool isOverheated;
    private Material gaugeMat;

    public bool IsOverheated => isOverheated;

    private void Awake()
    {
        gaugeMat = gaugeRenderer.material;
    }

    private void Update()
    {
        float rate = isOverheated ? overheatCoolRate : coolRate;
        heat = Mathf.Max(0f, heat - rate * Time.deltaTime);

        if (isOverheated && heat <= 0f)
        {
            isOverheated = false;
            PlayCooldownReady(); // NOU: sunet cand arma e gata de tras din nou
        }

        UpdateVisual();
    }

    public void AddHeat()
    {
        if (isOverheated) return;

        heat = Mathf.Clamp01(heat + heatPerShot);

        if (heat >= 1f)
        {
            isOverheated = true;
            PlayOverheat(); // NOU: sunet la momentul exact de overheat
        }

        UpdateVisual();
    }

    private void PlayOverheat()
    {
        if (audioSource != null && overheatClip != null)
            audioSource.PlayOneShot(overheatClip);
    }

    private void PlayCooldownReady()
    {
        if (audioSource != null && cooldownReadyClip != null)
            audioSource.PlayOneShot(cooldownReadyClip);
    }

    private void UpdateVisual()
    {
        float fill = 1f - heat;
        gaugeMat.SetFloat("_FillAmount", fill);

        Color c = heat < 0.5f
            ? Color.Lerp(coolColor, midColor, heat / 0.5f)
            : Color.Lerp(midColor, hotColor, (heat - 0.5f) / 0.5f);

        gaugeMat.SetColor("_FillColor", c);
    }
}