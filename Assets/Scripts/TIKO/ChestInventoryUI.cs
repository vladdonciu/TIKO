using UnityEngine;
using UnityEngine.UI;

public class ChestInventoryUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] GameObject inventoryPanel;
    [SerializeField] Image inventoryImage;
    
    [Header("Animation (optional)")]
    [SerializeField] bool useAnimation = true;
    [SerializeField] float fadeSpeed = 5f;

    CanvasGroup canvasGroup;

    void Awake()
    {
        Debug.Log("ChestInventoryUI Awake called");
        
        if (inventoryPanel == null)
        {
            Debug.LogError("InventoryPanel is NULL! Assign it in Inspector.");
            return;
        }

        inventoryPanel.SetActive(false);
        
        if (useAnimation)
        {
            canvasGroup = inventoryPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = inventoryPanel.AddComponent<CanvasGroup>();
            }
        }
        
        Debug.Log($"ChestInventoryUI initialized. Panel: {inventoryPanel.name}");
    }

    public void ShowChestInventory(Sprite inventorySprite)
    {
        Debug.Log("ShowChestInventory called!");
        
        if (inventoryPanel == null)
        {
            Debug.LogError("Cannot show inventory - inventoryPanel is NULL!");
            return;
        }

        if (inventorySprite == null)
        {
            Debug.LogWarning("Inventory sprite is NULL!");
        }

        inventoryPanel.SetActive(true);
        Debug.Log($"Inventory panel activated: {inventoryPanel.activeSelf}");
        
        if (inventoryImage != null && inventorySprite != null)
        {
            inventoryImage.sprite = inventorySprite;
            Debug.Log($"Sprite assigned: {inventorySprite.name}");
        }
        else
        {
            Debug.LogError($"Image: {inventoryImage != null}, Sprite: {inventorySprite != null}");
        }

        if (useAnimation && canvasGroup != null)
        {
            StopAllCoroutines();
            StartCoroutine(FadeIn());
        }
    }

    public void HideChestInventory()
    {
        Debug.Log("HideChestInventory called!");
        
        if (inventoryPanel == null) return;

        if (useAnimation && canvasGroup != null)
        {
            StopAllCoroutines();
            StartCoroutine(FadeOut());
        }
        else
        {
            inventoryPanel.SetActive(false);
        }
    }

    System.Collections.IEnumerator FadeIn()
    {
        canvasGroup.alpha = 0f;
        while (canvasGroup.alpha < 1f)
        {
            canvasGroup.alpha += Time.deltaTime * fadeSpeed;
            yield return null;
        }
    }

    System.Collections.IEnumerator FadeOut()
    {
        while (canvasGroup.alpha > 0f)
        {
            canvasGroup.alpha -= Time.deltaTime * fadeSpeed;
            yield return null;
        }
        inventoryPanel.SetActive(false);
    }
}
