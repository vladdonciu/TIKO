using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerController25D_Anim))]
public class PushPullController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController25D_Anim playerController;
    [SerializeField] private Transform interactionOrigin;

    [Header("Input")]
    [SerializeField] private Key grabKey = Key.E;

    [Header("Detection")]
    [SerializeField] private LayerMask pushPullMask;
    [SerializeField] private float grabRange = 1.4f;

    [Header("Grab Settings")]
    [Tooltip("Distanța păstrată între centrul lui Tiko și centrul cutiei.")]
    [SerializeField] private float desiredBoxDistance = 0.95f;

    [Tooltip("Cât de aproape trebuie să fie cutia de poziția dorită.")]
    [SerializeField] private float positionTolerance = 0.03f;

    [Tooltip("Distanța maximă până la care se păstrează grab-ul.")]
    [SerializeField] private float maxGrabDistance = 2.2f;

    [Tooltip("Viteză maximă de corectare dacă distanța dintre Tiko și cutie diferă.")]
    [SerializeField] private float boxCorrectionSpeed = 8f;

    [Tooltip("Input minim necesar ca să pornească Push/Pull.")]
    [SerializeField] private float inputDeadZone = 0.1f;

    private PushPullBox grabbedBox;
    private Vector3 boxAxis;
    private float lastPlayerAxisPosition;

    private void Awake()
    {
        if (playerController == null)
            playerController = GetComponent<PlayerController25D_Anim>();

        if (interactionOrigin == null)
            interactionOrigin = transform;
    }

    private void Update()
    {
        if (grabbedBox == null)
        {
            if (IsGrabKeyHeld())
                TryGrabBox();

            return;
        }

        if (!IsGrabKeyHeld())
        {
            ReleaseBox();
            return;
        }

        UpdateGrab();
    }

    private bool IsGrabKeyHeld()
    {
        return Keyboard.current != null &&
               Keyboard.current[grabKey].isPressed;
    }

    private void TryGrabBox()
    {
        Collider[] hits = Physics.OverlapSphere(
            interactionOrigin.position,
            grabRange,
            pushPullMask,
            QueryTriggerInteraction.Ignore
        );

        PushPullBox closestBox = null;
        float closestDistance = float.MaxValue;

        foreach (Collider hit in hits)
        {
            PushPullBox box = hit.GetComponentInParent<PushPullBox>();

            if (box == null || box.IsGrabbed)
                continue;

            Vector3 offset = box.transform.position - transform.position;
            offset.y = 0f;

            float distance = offset.magnitude;

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestBox = box;
            }
        }

        if (closestBox == null)
            return;

        Vector3 directionToBox =
            closestBox.transform.position - transform.position;

        directionToBox.y = 0f;

        if (directionToBox.sqrMagnitude < 0.001f)
            return;

        if (!closestBox.Grab())
            return;

        grabbedBox = closestBox;
        boxAxis = directionToBox.normalized;

      

        lastPlayerAxisPosition = Vector3.Dot(
            transform.position,
            boxAxis
        );

        playerController.SetPushPullActive(true);
        playerController.SetPushPullAnimation(false, false);
    }

    private void UpdateGrab()
    {
        if (playerController == null || grabbedBox == null)
        {
            ReleaseBox();
            return;
        }

        Vector2 input = playerController.MoveInput;

        Vector3 currentToBox =
            grabbedBox.transform.position - transform.position;

        currentToBox.y = 0f;

        if (currentToBox.magnitude > maxGrabDistance)
        {
            ReleaseBox();
            return;
        }

        KeepFacingBox();

        if (input.sqrMagnitude < inputDeadZone * inputDeadZone)
        {
            playerController.SetPushPullAnimation(false, false);
            lastPlayerAxisPosition = Vector3.Dot(
                transform.position,
                boxAxis
            );
            return;
        }

        Vector3 moveDirection = new Vector3(
            input.x,
            0f,
            input.y
        ).normalized;

        float moveDot = Vector3.Dot(moveDirection, boxAxis);

        bool isPushing = moveDot > 0.25f;
        bool isPulling = moveDot < -0.25f;

        if (!isPushing && !isPulling)
        {
            playerController.SetPushPullAnimation(false, false);
            lastPlayerAxisPosition = Vector3.Dot(
                transform.position,
                boxAxis
            );
            return;
        }

        if (isPushing && !grabbedBox.CanPush)
        {
            playerController.SetPushPullAnimation(false, false);
            return;
        }

        if (isPulling && !grabbedBox.CanPull)
        {
            playerController.SetPushPullAnimation(false, false);
            return;
        }

        // Poziția ideală a cutiei este mereu în fața lui Tiko,
        // pe aceeași axă pe care a fost prinsă.
        Vector3 targetBoxPosition =
            transform.position + boxAxis * desiredBoxDistance;

        targetBoxPosition.y = grabbedBox.transform.position.y;

        Vector3 flatDifference =
            targetBoxPosition - grabbedBox.transform.position;

        flatDifference.y = 0f;

        // Nu trimitem cutia instant prin pereți. O corectăm gradual.
        Vector3 correction = Vector3.ClampMagnitude(
            flatDifference,
            boxCorrectionSpeed * Time.deltaTime
        );

        if (flatDifference.magnitude > positionTolerance)
        {
            grabbedBox.MoveTo(
                grabbedBox.transform.position + correction
            );
        }

        playerController.SetPushPullAnimation(
            isPushing,
            isPulling
        );

        lastPlayerAxisPosition = Vector3.Dot(
            transform.position,
            boxAxis
        );
    }

    private void KeepFacingBox()
    {
        if (grabbedBox == null)
            return;

        Vector3 direction =
            grabbedBox.transform.position - transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(
            direction.normalized,
            Vector3.up
        );

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            20f * Time.deltaTime
        );
    }

    private void ReleaseBox()
    {
        if (grabbedBox != null)
            grabbedBox.Release();

        grabbedBox = null;

        if (playerController != null)
        {
            playerController.SetPushPullActive(false);
            playerController.SetPushPullAnimation(false, false);
        }
    }

    private void OnDisable()
    {
        ReleaseBox();
    }

    private void OnDrawGizmosSelected()
    {
        Transform origin =
            interactionOrigin != null
                ? interactionOrigin
                : transform;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(origin.position, grabRange);
    }
}