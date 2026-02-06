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
    [SerializeField] float maxTiltAngle = 8f;
    [SerializeField] float tiltSpeed = 10f;
    [SerializeField] Transform visualTransform;

    [Header("Gravity / Jump")]
    [SerializeField] bool enableJump = true;
    [SerializeField] float gravity = -25f;
    [SerializeField] float jumpHeight = 1.2f;
    [SerializeField] int extraJumps = 1;

    [Header("Crouch/Hide")]
    [SerializeField] bool enableCrouch = true;
    [SerializeField] KeyCode crouchKey = KeyCode.LeftShift;
    [SerializeField] float crouchSpeedMultiplier = 0.5f;
    [SerializeField] float crouchTiltMultiplier = 0.3f;
    [SerializeField] Transform bodyTransform;
    [SerializeField] float crouchBodyOffset = -0.3f;
    [SerializeField] float crouchTransitionSpeed = 8f;
    
    [Header("Crouch Tiptoeing Effect")]
    [SerializeField] bool enableTiptoeEffect = true;
    [SerializeField] float tiptoeSwayAngle = 5f;
    [SerializeField] float tiptoeSpeed = 8f;

    CharacterController cc;

    Vector2 moveInput;
    Vector3 horizontalVelocity;
    float verticalVelocity;

    bool crouchHeld;
    int jumpsLeft;

    float currentTiltAngle = 0f;
    float currentBodyYOffset = 0f;
    float tiptoeTimer = 0f;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        jumpsLeft = extraJumps;

        if (visualTransform == null)
        {
            if (transform.childCount > 0)
                visualTransform = transform.GetChild(0);
            else
                visualTransform = transform;
        }

        if (bodyTransform == null)
        {
            bodyTransform = visualTransform.Find("Body");
            if (bodyTransform == null)
            {
                Debug.LogWarning("Body Transform not found!");
            }
        }
    }

    void Update()
    {
        // === CROUCH INPUT - verificare manuală ===
        if (enableCrouch)
        {
            bool wasCrouching = crouchHeld;
            crouchHeld = Input.GetKey(crouchKey);
            
            // Debug când se schimbă starea
            if (wasCrouching != crouchHeld)
            {
                Debug.Log($"Crouch state changed: {crouchHeld}");
            }
        }

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

        // 5) Rotire spre direcția de mers
        if (rotateToMoveDirection && desiredDir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(desiredDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                18f * Time.deltaTime
            );
        }

        // 6) EFECTE VIZUALE
        ApplyMovementTilt();
        ApplyCrouchBodyOffset();
        ApplyTiptoeEffect();
    }

    void ApplyMovementTilt()
    {
        if (!enableMovementTilt || visualTransform == null) return;

        float currentSpeed = new Vector2(horizontalVelocity.x, horizontalVelocity.z).magnitude;
        float speedPercent = Mathf.Clamp01(currentSpeed / moveSpeed);

        float tiltMultiplier = crouchHeld ? crouchTiltMultiplier : 1f;
        float targetTilt = -maxTiltAngle * speedPercent * tiltMultiplier;

        if (!cc.isGrounded)
        {
            targetTilt *= 0.3f;
        }

        currentTiltAngle = Mathf.Lerp(currentTiltAngle, targetTilt, tiltSpeed * Time.deltaTime);
        visualTransform.localRotation = Quaternion.Euler(currentTiltAngle, 0f, 0f);
    }

    void ApplyCrouchBodyOffset()
    {
        if (!enableCrouch || bodyTransform == null) return;

        float targetYOffset = crouchHeld ? crouchBodyOffset : 0f;
        currentBodyYOffset = Mathf.Lerp(currentBodyYOffset, targetYOffset, crouchTransitionSpeed * Time.deltaTime);

        Vector3 bodyPos = bodyTransform.localPosition;
        bodyPos.y = currentBodyYOffset;
        bodyTransform.localPosition = bodyPos;
    }

    void ApplyTiptoeEffect()
    {
        if (!enableTiptoeEffect || !crouchHeld || bodyTransform == null) return;

        float currentSpeed = new Vector2(horizontalVelocity.x, horizontalVelocity.z).magnitude;
        
        if (currentSpeed > 0.1f && cc.isGrounded)
        {
            tiptoeTimer += Time.deltaTime * tiptoeSpeed;
            float swayAngle = Mathf.Sin(tiptoeTimer) * tiptoeSwayAngle;

            Vector3 bodyEuler = bodyTransform.localEulerAngles;
            bodyEuler.y = swayAngle;
            bodyTransform.localRotation = Quaternion.Euler(bodyEuler.x, bodyEuler.y, bodyEuler.z);
        }
        else
        {
            Vector3 bodyEuler = bodyTransform.localEulerAngles;
            bodyEuler.y = Mathf.Lerp(bodyEuler.y, 0f, 10f * Time.deltaTime);
            bodyTransform.localRotation = Quaternion.Euler(bodyEuler.x, bodyEuler.y, bodyEuler.z);
            tiptoeTimer = 0f;
        }
    }

    // === INPUT CALLBACKS (doar pentru Move și Jump) ===
    
    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    public void OnJump(InputValue value)
    {
        if (!enableJump) return;
        if (!value.isPressed) return;
        if (crouchHeld) return;

        if (cc.isGrounded)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            return;
        }

        if (jumpsLeft > 0)
        {
            jumpsLeft--;
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }
    
    // NU MAI AVEM OnCrouch - am șters complet callback-ul
}
