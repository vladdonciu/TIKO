using System.Collections;
using UnityEngine;

public class ComputerInteract : MonoBehaviour
{
    [Header("Referinte")]
    public DoorController doorController;
    public GameObject[] redLights;    // ← array
    public GameObject[] greenLights;  // ← array

    [Header("Settings")]
    public float interactDistance = 3f;
    public KeyCode interactKey = KeyCode.E;

    private Transform player;
    private bool isActivated = false;
    private bool showPrompt = false;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        SetLights(redLights, true);
        SetLights(greenLights, false);
    }

    void Update()
    {
        if (isActivated) return;

        float dist = Vector3.Distance(transform.position, player.position);
        showPrompt = dist <= interactDistance;

        if (showPrompt && Input.GetKeyDown(interactKey))
            Activate();
    }

    void Activate()
    {
        isActivated = true;

        SetLights(redLights, false);
        SetLights(greenLights, true);

        // ❌ Șterge astea două linii
        // doorController.redLights   = redLights;
        // doorController.greenLights = greenLights;

        doorController.OpenDoors();
        StartCoroutine(ResetAfterCycle());
    }

    private void SetLights(GameObject[] lights, bool state)
    {
        if (lights == null) return;
        foreach (var light in lights)
            if (light) light.SetActive(state);
    }

    private IEnumerator ResetAfterCycle()
    {
        float totalTime = doorController.stayOpenDuration
                        + doorController.slideDuration
                        + 0.2f;
        yield return new WaitForSeconds(totalTime);
        isActivated = false;
    }

    void OnGUI()
    {
        if (showPrompt && !isActivated)
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 18;
            style.normal.textColor = Color.white;
            style.alignment = TextAnchor.MiddleCenter;

            GUI.Label(
                new Rect(Screen.width / 2 - 150, Screen.height / 2 + 50, 300, 35),
                "[E] Activeaza calculatorul",
                style
            );
        }
    }
}