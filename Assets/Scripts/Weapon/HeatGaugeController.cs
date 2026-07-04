using UnityEngine;

public class HeatGaugeController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Renderer gaugeRenderer;

    [Header("Heat Settings")]
    [SerializeField] private float heatPerShot = 0.12f;
    [SerializeField] private float coolRate = 0.35f;       // per secunda, normal
    [SerializeField] private float overheatCoolRate = 0.5f; // per secunda, dupa overheat

    [Header("Colors")]
    [SerializeField] private Color coolColor = new Color(0f, 1f, 1f);      // cyan
    [SerializeField] private Color midColor = new Color(1f, 0.55f, 0f);    // portocaliu
    [SerializeField] private Color hotColor = new Color(1f, 0f, 0.19f);    // crimson

    private float heat; // 0 = rece, 1 = overheat total
    private bool isOverheated;
    private Material gaugeMat;

    public bool IsOverheated => isOverheated;

    private void Awake()
    {
        gaugeMat = gaugeRenderer.material; // instanta unica, nu shared material
    }

    private void Update()
    {
        float rate = isOverheated ? overheatCoolRate : coolRate;
        heat = Mathf.Max(0f, heat - rate * Time.deltaTime);

        if (isOverheated && heat <= 0f)
            isOverheated = false;

        UpdateVisual();
    }

    public void AddHeat()
    {
        if (isOverheated) return;

        heat = Mathf.Clamp01(heat + heatPerShot);

        if (heat >= 1f)
            isOverheated = true;

        UpdateVisual(); // update instant, efect de "recoil" pe gauge
    }

    private void UpdateVisual()
    {
        float fill = 1f - heat; // coolant ramas: 1 = plin, 0 = gol
        gaugeMat.SetFloat("_FillAmount", fill);

        Color c = heat < 0.5f
            ? Color.Lerp(coolColor, midColor, heat / 0.5f)
            : Color.Lerp(midColor, hotColor, (heat - 0.5f) / 0.5f);

        gaugeMat.SetColor("_FillColor", c);
    }
}