using UnityEngine;
using UnityEngine.UI;

public class OptionsMenu : MonoBehaviour
{
    [Header("Panels")]
    public GameObject optionsPanel;
    public GameObject mainMenuPanel;

    [Header("Sliders")]
    public Slider masterSlider;
    public Slider musicSlider;

    void Start()
    {
        // Setează slider-ele la valorile salvate
        masterSlider.value = PlayerPrefs.GetFloat("MasterVolume", 1f);
        musicSlider.value  = PlayerPrefs.GetFloat("MusicVolume",  1f);

        // Leagă slider-ele la funcții
        masterSlider.onValueChanged.AddListener(OnMasterChanged);
        musicSlider.onValueChanged.AddListener(OnMusicChanged);
    }

    void OnEnable()
    {
        // Sincronizează când se redeschide panoul
        if (masterSlider) masterSlider.value = PlayerPrefs.GetFloat("MasterVolume", 1f);
        if (musicSlider)  musicSlider.value  = PlayerPrefs.GetFloat("MusicVolume",  1f);
    }

    void OnMasterChanged(float value)
    {
        AudioManager.Instance?.SetMasterVolume(value);
    }

    void OnMusicChanged(float value)
    {
        AudioManager.Instance?.SetMusicVolume(value);
    }

    public void OpenOptions()
    {
        optionsPanel.SetActive(true);
        mainMenuPanel.SetActive(false);
    }

    public void CloseOptions()
    {
        optionsPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }
}