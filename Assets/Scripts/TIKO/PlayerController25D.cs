using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController25D : MonoBehaviour
{
    [Header("Move")]
    [SerializeField] float moveSpeed = 5.5f;
    [SerializeField] float acceleration = 18f;
    [SerializeField] float deceleration = 22f;
    [SerializeField] bool rotateToMoveDirection = true;

    [Header("Movement Tilt Effect")]
    [SerializeField] bool enableMovementTilt = true;
    [SerializeField] float maxTiltAngle = 8f;        // cât de mult se apleacă (grade)
    [SerializeField] float tiltSpeed = 10f;          // cât de rapid se aplică tilt-ul
    [SerializeField] Transform visualTransform;      // obiectul care se va înclina (mesh/model)

    [Header("Gravity / Jump")]
    [SerializeField] bool enableJump = true;
    [SerializeField] float gravity = -25f;
    [SerializeField] float jumpHeight = 1.2f;
    [SerializeField] int extraJumps = 1;

    [Header("Crouch/Hide")]
    [SerializeField] bool enableCrouch = false;
    [SerializeField] float crouchSpeedMultiplier = 0.5f;

    CharacterController cc;

    Vector2 moveInput;
    Vector3 horizontalVelocity;
    float verticalVelocity;

    bool crouchHeld;
    int jumpsLeft;

    // Pentru tilt effect
    float currentTiltAngle = 0f;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        jumpsLeft = extraJumps;

        // Dacă nu e setat manual, încearcă să găsească primul copil
        if (visualTransform == null)
        {
            if (transform.childCount > 0)
                visualTransform = transform.GetChild(0);
            else
                visualTransform = transform; // fallback la root
        }
    }

    void Update()
    {
        // 1) Direcție mișcare în plan XZ din WASD
        Vector3 desiredDir = new Vector3(moveInput.x, 0f, moveInput.y);
        if (desiredDir.sqrMagnitude > 1f) desiredDir.Normalize();

        float speed = moveSpeed * (crouchHeld ? crouchSpeedMultiplier : 1f);

        // 2) "glide" accel/decel
        Vector3 desiredVel = desiredDir * speed;
        float accel = (desiredVel.sqrMagnitude > 0.001f) ? acceleration : deceleration;
        horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, desiredVel, accel * Time.deltaTime);

        // 3) Gravitație + reset jumps când e grounded
        if (cc.isGrounded)
        {
            if (verticalVelocity < 0f) verticalVelocity = -2f;
            jumpsLeft = extraJumps;
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }

        // 4) Aplică mișcarea
        Vector3 motion = horizontalVelocity;
        motion.y = verticalVelocity;
        cc.Move(motion * Time.deltaTime);

        // 5) Rotire spre direcția de mers (DOAR ROOT)
        if (rotateToMoveDirection && desiredDir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(desiredDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                18f * Time.deltaTime
            );
        }

        // 6) TILT EFFECT - aplecă pe spate când se mișcă (DOAR VISUAL)
        ApplyMovementTilt();
    }

    void ApplyMovementTilt()
    {
        if (!enableMovementTilt || visualTransform == null) return;

        // Calculează viteza de mișcare ca procent din viteza maximă
        float currentSpeed = new Vector2(horizontalVelocity.x, horizontalVelocity.z).magnitude;
        float speedPercent = Mathf.Clamp01(currentSpeed / moveSpeed);

        // Target tilt bazat pe viteză (se apleacă pe spate când merge)
        float targetTilt = -maxTiltAngle * speedPercent; // negativ = spate

        // Dacă playerul sare, reduce tilt-ul
        if (!cc.isGrounded)
        {
            targetTilt *= 0.3f; // în aer se întoarce mai spre poziție normală
        }

        // Smooth lerp către target tilt
        currentTiltAngle = Mathf.Lerp(currentTiltAngle, targetTilt, tiltSpeed * Time.deltaTime);

        // Aplică rotația DOAR pe axa X (pitch) local - păstrează Y și Z
        visualTransform.localRotation = Quaternion.Euler(currentTiltAngle, 0f, 0f);
    }

    // === INPUT CALLBACKS ===

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    public void OnCrouch(InputValue value)
    {
        if (!enableCrouch) return;
        crouchHeld = value.isPressed;
    }

    public void OnJump(InputValue value)
    {
        if (!enableJump) return;
        if (!value.isPressed) return;

        // Ground jump
        if (cc.isGrounded)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            return;
        }

        // Air jump (double jump)
        if (jumpsLeft > 0)
        {
            jumpsLeft--;
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }
}
