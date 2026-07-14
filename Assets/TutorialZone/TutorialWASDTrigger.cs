using System.Collections;
using UnityEngine;

public class TutorialWASDTrigger : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Canvas-ul World Space care conține holograma WASD")]
    [SerializeField] private GameObject wasdHologramRoot;

    [Header("Timing")]
    [Tooltip("Câte secunde rămâne holograma vizibilă")]
    [SerializeField] private float displayDuration = 10f;

    private HologramSpawnProjector hologramProjector;
    private bool hasBeenUsed;

    private void Awake()
    {
        if (wasdHologramRoot != null)
        {
            hologramProjector =
                wasdHologramRoot.GetComponent<HologramSpawnProjector>();

            // Holograma trebuie să fie ascunsă la început.
            wasdHologramRoot.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Rulează o singură dată.
        if (hasBeenUsed)
            return;

        // Tiko trebuie să aibă tag-ul Player.
        if (!other.CompareTag("Player"))
            return;

        hasBeenUsed = true;
        StartCoroutine(PlayTutorialRoutine());
    }

    private IEnumerator PlayTutorialRoutine()
    {
        if (wasdHologramRoot == null)
        {
            DisableTutorialTrigger();
            yield break;
        }

        // Pornim holograma. OnEnable de pe HologramSpawnProjector
        // va porni animația de spawn.
        wasdHologramRoot.SetActive(true);

        // Dacă dintr-un motiv OnEnable nu a declanșat animația,
        // o pornim explicit.
        if (hologramProjector != null)
        {
            hologramProjector.StartSpawnEffect();
        }

        // Holograma rămâne pe ecran 10 secunde.
        yield return new WaitForSeconds(displayDuration);

        // Pornește animația de despawn, apoi ascunde holograma.
        if (hologramProjector != null)
        {
            hologramProjector.StartDespawnEffect(() =>
            {
                if (wasdHologramRoot != null)
                    wasdHologramRoot.SetActive(false);

                DisableTutorialTrigger();
            });
        }
        else
        {
            // Fallback dacă scriptul de projector nu e pus.
            wasdHologramRoot.SetActive(false);
            DisableTutorialTrigger();
        }
    }

    private void DisableTutorialTrigger()
    {
        // Nu mai poate fi activat după prima folosire.
        gameObject.SetActive(false);
    }
}