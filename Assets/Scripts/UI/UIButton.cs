using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;

[RequireComponent(typeof(Image))]
public class UIButton : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler,
    IPointerClickHandler
{
    [Header("Sprites")]
    public Sprite normalSprite;
    public Sprite hoverSprite;
    public Sprite pressedSprite;

    [Header("Scale Anim")]
    public bool  enableScale  = true;
    public float hoverScale   = 1.05f;
    public float pressedScale = 0.95f;
    public float scaleSpeed   = 12f;

    [Header("On Click")]
    public UnityEvent onClick;

    private Image   img;
    private Vector3 initialScale;
    private Vector3 targetScale;
    private bool    initialized = false;

    void Awake()
    {
        img = GetComponent<Image>();
        if (normalSprite == null)
            normalSprite = img.sprite;
    }

    void Start()
    {
        // Canvas Scaler termină după primul frame
        // folosim LateUpdate o singură dată să capturăm scala corectă
    }

    void LateUpdate()
    {
        if (!initialized)
        {
            initialScale = transform.localScale;
            targetScale  = initialScale;
            initialized  = true;
        }

        if (enableScale)
            transform.localScale = Vector3.Lerp(
                transform.localScale, targetScale, scaleSpeed * Time.deltaTime);
    }

    public void OnPointerEnter(PointerEventData e)
    {
        if (!initialized) return;
        if (hoverSprite)  img.sprite = hoverSprite;
        if (enableScale)  targetScale = initialScale * hoverScale;
        CursorManager.SetInteract();
    }

    public void OnPointerExit(PointerEventData e)
    {
        if (!initialized) return;
        if (normalSprite) img.sprite = normalSprite;
        if (enableScale)  targetScale = initialScale;
        CursorManager.SetDefault();
    }

    public void OnPointerDown(PointerEventData e)
    {
        if (!initialized) return;
        if (pressedSprite) img.sprite = pressedSprite;
        if (enableScale)   targetScale = initialScale * pressedScale;
    }

    public void OnPointerUp(PointerEventData e)
    {
        if (!initialized) return;
        if (hoverSprite)  img.sprite = hoverSprite;
        if (enableScale)  targetScale = initialScale * hoverScale;
    }

    public void OnPointerClick(PointerEventData e)
    {
        onClick?.Invoke();
    }
}