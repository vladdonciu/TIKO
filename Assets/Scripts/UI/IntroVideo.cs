using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class IntroVideo : MonoBehaviour
{
    [Header("Refs")]
    public VideoPlayer videoPlayer;
    public RawImage    rawImage;

    [Header("Settings")]
    public string videoFileName = "intro.mp4";  // numele fișierului
    public string nextScene     = "MainMenu";
    public bool   canSkip       = true;

    void Start()
    {
        // Pune fișierul .mp4 în Assets/StreamingAssets/
        string path = System.IO.Path.Combine(
            Application.streamingAssetsPath, videoFileName);

        videoPlayer.source = VideoSource.Url;
        videoPlayer.url    = path;

        StartCoroutine(PlayIntro());
    }

    IEnumerator PlayIntro()
    {
        videoPlayer.Prepare();
        while (!videoPlayer.isPrepared)
            yield return null;

        rawImage.texture = videoPlayer.texture;
        videoPlayer.Play();

        while (videoPlayer.isPlaying)
        {
            if (canSkip && Input.anyKeyDown)
                break;
            yield return null;
        }

        SceneManager.LoadScene(nextScene);
    }
}