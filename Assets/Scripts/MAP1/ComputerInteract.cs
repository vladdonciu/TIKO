using System.Collections;
using UnityEngine;

public class ComputerInteract : MonoBehaviour
{
    [Header("Referinte")]
    public DoorController doorController;
    public GameObject redLight;
    public GameObject greenLight;

    [Header("Settings")]
    public float interactDistance = 3f;
    public KeyCode interactKey = KeyCode.E;

    private Transform player;
    private bool isActivated = false;
    private bool showPrompt = false;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;

        // Bec verde ascuns la start
        if (greenLight) greenLight.SetActive(false);
        if (redLight)   redLight.SetActive(true);
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

        // Bec rosu → verde
        if (redLight)   redLight.SetActive(false);
        if (greenLight) greenLight.SetActive(true);

        // Paseaza referintele la DoorController
        doorController.redLight   = redLight;
        doorController.greenLight = greenLight;

        // Porneste usile
        doorController.OpenDoors();

        // Permite reactivarea dupa ciclu complet
        StartCoroutine(ResetAfterCycle());
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