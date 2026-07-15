using UnityEngine;

public class RadarRotator : MonoBehaviour
{
    [SerializeField] private Transform rotatingPart;
    [SerializeField] private float rotationSpeed = 90f;
    [SerializeField] private bool onlyWhenPaused = true;

    private void Update()
    {
        if (onlyWhenPaused && !PauseManager.IsPaused) return;

        rotatingPart.Rotate(0f, 0f, -rotationSpeed * Time.unscaledDeltaTime);
    }
}