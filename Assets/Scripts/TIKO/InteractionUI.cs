using UnityEngine;
using TMPro;

public class InteractionUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] GameObject promptPanel;
    [SerializeField] TextMeshProUGUI promptText;
    
    [Header("Settings")]
    [SerializeField] bool worldSpace = false; // false = screen space
    [SerializeField] Vector3 worldOffset = new Vector3(0, 1.5f, 0);

    Camera mainCamera;

    void Awake()
    {
        mainCamera = Camera.main;
        
        if (promptPanel != null)
        {
            promptPanel.SetActive(false);
        }
    }

    public void ShowPrompt(string text, Vector3 worldPosition)
    {
        if (promptPanel == null) return;

        promptPanel.SetActive(true);
        
        if (promptText != null)
        {
            promptText.text = text;
        }

        if (worldSpace && mainCamera != null)
        {
            // Convertește poziția world la screen space
            Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPosition + worldOffset);
            promptPanel.transform.position = screenPos;
        }
    }

    public void HidePrompt()
    {
        if (promptPanel != null)
        {
            promptPanel.SetActive(false);
        }
    }
}
