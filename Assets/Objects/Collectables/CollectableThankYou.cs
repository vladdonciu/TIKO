using System.Collections;
using UnityEngine;

public class CollectibleThankYou : MonoBehaviour
{
    [Header("Objects to hide")]
    [SerializeField] private GameObject collectibleVisual;
    [SerializeField] private GameObject surroundingEffect;

    [Header("UI")]
    [SerializeField] private GameObject thankYouCanvas;
    [SerializeField] private float showCanvasTime = 2.5f;

    [Header("Audio")]
    [SerializeField] private AudioClip collectSound;
    [SerializeField] private float collectVolume = 1f;

    [Header("Optional")]
    [SerializeField] private bool destroyInsteadOfHide = false;

    private bool collected = false;

    private void Reset()
    {
        BoxCollider bc = GetComponent<BoxCollider>();
        if (bc != null)
            bc.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collected) return;
        if (!other.CompareTag("Player")) return;

        collected = true;

        if (collectSound != null)
            AudioSource.PlayClipAtPoint(collectSound, transform.position, collectVolume);

        if (thankYouCanvas != null)
            StartCoroutine(ShowThankYou());

        if (surroundingEffect != null)
        {
            if (destroyInsteadOfHide) Destroy(surroundingEffect);
            else surroundingEffect.SetActive(false);
        }

        if (collectibleVisual != null)
        {
            if (destroyInsteadOfHide) Destroy(collectibleVisual);
            else collectibleVisual.SetActive(false);
        }
        else
        {
            if (destroyInsteadOfHide) Destroy(gameObject);
            else gameObject.SetActive(false);
        }
    }

    private IEnumerator ShowThankYou()
    {
        thankYouCanvas.SetActive(true);
        yield return new WaitForSeconds(showCanvasTime);
        thankYouCanvas.SetActive(false);
    }
}