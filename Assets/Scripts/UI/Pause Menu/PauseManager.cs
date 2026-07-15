using UnityEngine;
using UnityEngine.InputSystem;

public class PauseManager : MonoBehaviour
{
    public static bool IsPaused { get; private set; }

    [Header("References")]
    [SerializeField] private BackgroundBlur backgroundBlur;
    [SerializeField] private PauseMenuFade menuFade;
    [SerializeField] private CRTWobbleEffect wobbleEffect;

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (IsPaused) Resume();
            else Pause();
        }
    }

    public void Pause()
    {
        if (IsPaused) return;
        IsPaused = true;
        Time.timeScale = 0f;
        AudioListener.pause = true;

        if (backgroundBlur != null) backgroundBlur.CaptureAndBlur();
        if (menuFade != null) menuFade.Show();
        if (wobbleEffect != null) wobbleEffect.Enable(true);
    }

    public void Resume()
    {
        if (!IsPaused) return;
        IsPaused = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;

        if (backgroundBlur != null) backgroundBlur.ClearBlur();
        if (menuFade != null) menuFade.Hide();
        if (wobbleEffect != null) wobbleEffect.Enable(false);
    }

    public void QuitToMenu()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    public void RestartLevel()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        UnityEngine.SceneManagement.SceneManager.LoadScene(scene.name);
    }
}