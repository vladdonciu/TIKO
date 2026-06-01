using UnityEngine;

public class CursorManager : MonoBehaviour
{
    [Header("Cursors")]
    public Texture2D defaultCursor;
    public Texture2D clickCursor;
    public Texture2D interactCursor;

    [Header("Hotspot")]
    public Vector2 hotspot = Vector2.zero;

    private static CursorManager instance;

    void Awake()
    {
        if (instance != null) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        SetDefault();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) SetClick();
        if (Input.GetMouseButtonUp(0))   SetDefault();
    }

    // ── Statice — apelabile din orice script ──────────────────
    public static void SetDefault()
    {
        if (instance?.defaultCursor)
            Cursor.SetCursor(instance.defaultCursor, instance.hotspot, CursorMode.Auto);
    }

    public static void SetClick()
    {
        if (instance?.clickCursor)
            Cursor.SetCursor(instance.clickCursor, instance.hotspot, CursorMode.Auto);
    }

    public static void SetInteract()
    {
        if (instance?.interactCursor)
            Cursor.SetCursor(instance.interactCursor, instance.hotspot, CursorMode.Auto);
        else
            SetDefault();
    }

    public static void Hide()
    {
        Cursor.visible = false;
    }

    public static void Show()
    {
        Cursor.visible = true;
        SetDefault();
    }
}