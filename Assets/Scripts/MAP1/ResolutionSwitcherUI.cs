using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ResolutionSwitcherUI : MonoBehaviour
{
    [Header("UI")]
    public Button downButton;
    public Button upButton;

    [Header("Resolutions (Width x Height)")]
    public List<Vector2Int> resolutions = new()
    {
        new Vector2Int(1280, 720),
        new Vector2Int(1600, 900),
        new Vector2Int(1920, 1080),
        new Vector2Int(2560, 1440),
    };

    [Header("Fullscreen")]
    public FullScreenMode fullScreenMode = FullScreenMode.FullScreenWindow;

    int index;

    void Awake()
    {
        // Pick closest starting resolution by height (simple heuristic)
        index = FindClosestIndex(Screen.width, Screen.height);

        if (downButton) downButton.onClick.AddListener(StepDown);
        if (upButton) upButton.onClick.AddListener(StepUp);

        Apply();
    }

    int FindClosestIndex(int w, int h)
    {
        int best = 0;
        int bestScore = int.MaxValue;

        for (int i = 0; i < resolutions.Count; i++)
        {
            int dw = resolutions[i].x - w;
            int dh = resolutions[i].y - h;
            int score = dw * dw + dh * dh;
            if (score < bestScore)
            {
                bestScore = score;
                best = i;
            }
        }
        return best;
    }

    void StepDown()
    {
        index = Mathf.Max(0, index - 1);
        Apply();
    }

    void StepUp()
    {
        index = Mathf.Min(resolutions.Count - 1, index + 1);
        Apply();
    }

    void Apply()
    {
        var r = resolutions[index];

        // refreshRate = 0 lets Unity choose default
        Screen.SetResolution(r.x, r.y, fullScreenMode, 0);

        Debug.Log($"Resolution set to {r.x}x{r.y} ({fullScreenMode})");
    }
}