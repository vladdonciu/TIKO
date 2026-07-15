using UnityEngine;
using UnityEngine.UI;

public class CRTWobbleEffect : MonoBehaviour
{
    [SerializeField] private RawImage targetImage;
    [SerializeField] private Material wobbleMaterial;

    public void Enable(bool state)
    {
        if (state)
        {
            targetImage.material = wobbleMaterial;
        }
        else
        {
            targetImage.material = null;
        }
    }

    private void Update()
    {
        if (wobbleMaterial != null && targetImage.material == wobbleMaterial)
        {
            wobbleMaterial.SetFloat("_Time01", Time.unscaledTime);
        }
    }
}