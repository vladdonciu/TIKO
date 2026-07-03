using UnityEngine;

public class CameraEnemyLaser : MonoBehaviour
{
    [Header("Laser Settings")]
    public float laserDamage = 10f;
    public float fireRate = 1.2f;
    public float laserRange = 20f;

    [Header("VFX Prefabs")]
    public GameObject laserBeamPrefab;
    public GameObject laserDotPrefab;

    [Header("Beam Settings")]
    public float beamWidth = 0.02f;
    public float dotLerpSpeed = 20f;

    [Header("Eye")]
    public Transform eye;

    private Transform player;
    private TikoHealth tikoHealth;
    private GameObject beamInstance;
    private GameObject dotInstance;
    private float fireTimer = 0f;

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p)
        {
            player = p.transform;
            tikoHealth = p.GetComponent<TikoHealth>();
        }

        if (laserBeamPrefab)
        {
            beamInstance = Instantiate(laserBeamPrefab);
            beamInstance.SetActive(false);
        }

        if (laserDotPrefab)
        {
            dotInstance = Instantiate(laserDotPrefab);
            dotInstance.SetActive(false);
        }

        if (!eye) eye = transform;
    }

    void Update()
    {
        fireTimer += Time.deltaTime;
    }

    public void TickLaser(bool attacking)
    {
        if (!attacking)
        {
            HideAll();
            return;
        }

        if (!player) return;

        Vector3 origin = eye.position;
        Vector3 target = player.position + Vector3.up * 0.5f;
        Vector3 dir = (target - origin).normalized;

        if (Physics.Raycast(origin, dir, out RaycastHit hit, laserRange))
        {
            ShowBeam(origin, hit.point);
            ShowDot(hit.point);

            if (hit.collider.CompareTag("Player") && fireTimer >= fireRate)
            {
                fireTimer = 0f;
                tikoHealth?.TakeDamage(laserDamage);
            }
        }
        else
        {
            Vector3 end = origin + dir * laserRange;
            ShowBeam(origin, end);
            ShowDot(end);
        }
    }

    void ShowBeam(Vector3 from, Vector3 to)
    {
        if (!beamInstance) return;

        float length = Vector3.Distance(from, to);
        Vector3 mid = (from + to) / 2f;

        beamInstance.SetActive(true);
        beamInstance.transform.position = mid;
        beamInstance.transform.up = (to - from).normalized;
        beamInstance.transform.localScale = new Vector3(beamWidth, length / 2f, beamWidth);
    }

    void ShowDot(Vector3 pos)
    {
        if (!dotInstance) return;

        dotInstance.SetActive(true);
        dotInstance.transform.position = Vector3.Lerp(dotInstance.transform.position, pos, dotLerpSpeed * Time.deltaTime);
    }

    void HideAll()
    {
        if (beamInstance) beamInstance.SetActive(false);
        if (dotInstance) dotInstance.SetActive(false);
    }
}