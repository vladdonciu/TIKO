using UnityEngine;

public class EnemyShatter : MonoBehaviour
{
    [Header("Shatter Settings")]
    [SerializeField] private int fragmentCount = 12;
    [SerializeField] private float fragmentScale = 0.25f;
    [SerializeField] private Color fragmentColor = new Color(1f, 0f, 0.19f);

    [Header("Fall Settings")]
    [SerializeField] private float sidewaysDrift = 0.6f;
    [SerializeField] private float tinyPopForce = 0.8f;
    [SerializeField] private float torqueAmount = 1.5f;

    [Header("Fade Out Settings")]
    [SerializeField] private float minFadeDelay = 0.5f;
    [SerializeField] private float maxFadeDelay = 2f;
    [SerializeField] private float fadeDuration = 0.8f;

    [Header("Bounds Source")]
    [SerializeField] private Renderer sourceRenderer;

    public void Shatter()
    {
        Bounds bounds = GetBounds();

        for (int i = 0; i < fragmentCount; i++)
        {
            GameObject fragment = FragmentPool.Instance.GetFragment();

            Vector3 randomPoint = new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                Random.Range(bounds.min.y, bounds.max.y),
                Random.Range(bounds.min.z, bounds.max.z)
            );

            fragment.transform.SetParent(null);
            fragment.transform.position = randomPoint;
            fragment.transform.rotation = Random.rotation;
            fragment.transform.localScale = Vector3.one * fragmentScale * Random.Range(0.6f, 1.2f);

            Rigidbody rb = fragment.GetComponent<Rigidbody>();
            rb.mass = 0.2f;
            rb.isKinematic = false;

            Vector3 gentleDrift = new Vector3(
                Random.Range(-sidewaysDrift, sidewaysDrift),
                Random.Range(0f, tinyPopForce),
                Random.Range(-sidewaysDrift, sidewaysDrift)
            );

            rb.AddForce(gentleDrift, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * torqueAmount, ForceMode.Impulse);

            FragmentFade fade = fragment.GetComponent<FragmentFade>();
            fade.SetColors(fragmentColor, fragmentColor * 1.2f);
            fade.StartFade(minFadeDelay, maxFadeDelay, fadeDuration);
        }
    }

    private Bounds GetBounds()
    {
        if (sourceRenderer != null)
            return sourceRenderer.bounds;

        return new Bounds(transform.position, Vector3.one);
    }
}