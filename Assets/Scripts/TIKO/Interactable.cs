using UnityEngine;
using UnityEngine.Events;

public class Interactable : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] string interactPrompt = "Press E";
    [SerializeField] float interactionRange = 2f;
    [SerializeField] bool requireLookAt = false;
    
    [Header("Chest Inventory")]
    [SerializeField] bool isChest = true;
    [SerializeField] Sprite chestInventoryImage;
    
    [Header("UI References")] // NOU - referințe manuale
    [SerializeField] InteractionUI interactionUI;
    [SerializeField] ChestInventoryUI chestUI;
    
    [Header("Events")]
    public UnityEvent OnInteract;
    
    Transform player;
    bool isPlayerInRange = false;
    bool isChestOpen = false;

    void Start()
    {
        // AUTO-DETECT doar dacă nu sunt setate manual
        if (interactionUI == null)
        {
            interactionUI = FindObjectOfType<InteractionUI>();
        }
        
        if (chestUI == null)
        {
            chestUI = FindObjectOfType<ChestInventoryUI>();
        }
        
        // DEBUG
        Debug.Log($"InteractionUI: {(interactionUI != null ? "✓" : "✗")}");
        Debug.Log($"ChestInventoryUI: {(chestUI != null ? "✓" : "✗")}");
        
        // Găsește playerul
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        
        if (player == null)
        {
            Debug.LogError("Player not found! Make sure player has 'Player' tag.");
        }
    }

    void Update()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);
        isPlayerInRange = distance <= interactionRange;

        if (isPlayerInRange)
        {
            if (isChestOpen && distance > interactionRange * 1.2f)
            {
                CloseChest();
            }
            
            if (!isChestOpen && interactionUI != null)
            {
                interactionUI.ShowPrompt(interactPrompt, transform.position);
            }
        }
        else
        {
            if (interactionUI != null)
            {
                interactionUI.HidePrompt();
            }
            
            if (isChestOpen)
            {
                CloseChest();
            }
        }
    }

    public void Interact()
    {
        if (!isPlayerInRange) return;

        if (isChest)
        {
            if (isChestOpen)
            {
                CloseChest();
            }
            else
            {
                OpenChest();
            }
        }
        else
        {
            OnInteract?.Invoke();
        }
    }

    void OpenChest()
    {
        isChestOpen = true;
        
        if (interactionUI != null)
        {
            interactionUI.HidePrompt();
        }
        
        // DEBUG mai detaliat
        Debug.Log($"Opening chest. chestUI null? {chestUI == null}, image null? {chestInventoryImage == null}");
        
        if (chestUI != null && chestInventoryImage != null)
        {
            chestUI.ShowChestInventory(chestInventoryImage);
        }
        else
        {
            Debug.LogError($"Cannot show inventory! ChestUI: {chestUI != null}, Image: {chestInventoryImage != null}");
        }
        
        Debug.Log("Chest opened!");
    }

    void CloseChest()
    {
        isChestOpen = false;
        
        if (chestUI != null)
        {
            chestUI.HideChestInventory();
        }
        
        Debug.Log("Chest closed!");
    }

    public bool IsPlayerInRange() => isPlayerInRange;
    public bool IsChestOpen() => isChestOpen;

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}
