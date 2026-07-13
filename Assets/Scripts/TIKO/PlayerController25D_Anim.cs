using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController25D_Anim : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Animator animator;
    [SerializeField] private Transform visualTransform;
    [SerializeField] private Transform wheel;

    [Header("Move")]
    [SerializeField] private float moveSpeed = 5.5f;
    [SerializeField] private float acceleration = 18f;
    [SerializeField] private float deceleration = 22f;
    [SerializeField] private bool rotateToMoveDirection = true;
    [SerializeField] private float crouchSpeedMultiplier = 0.5f;
    [SerializeField] private float pushPullSpeedMultiplier = 0.4f;

    [Header("Push / Pull Visual")]
    [Tooltip("Mută modelul lui Tiko spre cutie în timpul Push.")]
    [SerializeField] private float pushVisualOffset = 0.22f;

    [Tooltip("Mută modelul lui Tiko spre cutie în timpul Pull.")]
    [SerializeField] private float pullVisualOffset = 0.42f;

    [SerializeField] private float pushPullVisualOffsetSpeed = 12f;

    [Header("Jump / Gravity")]
    [SerializeField] private bool enableJump = true;
    [SerializeField] private float gravity = -25f;
    [SerializeField] private float jumpHeight = 1.2f;
    [SerializeField] private int extraJumps = 1;

    [Header("Tilt")]
    [SerializeField] private bool enableMovementTilt = true;
    [SerializeField] private float maxTiltAngle = 8f;
    [SerializeField] private float tiltSpeed = 10f;
    [SerializeField] private float crouchTiltMultiplier = 0.3f;

    [Header("Idle Random (State Machine)")]
    [SerializeField] private bool enableIdleRandom = true;
    [SerializeField] private Vector2 idleChangeInterval = new Vector2(3f, 7f);
    [SerializeField] private float idleSpeedEpsilon = 0.1f;

    [Header("Wheel Spin")]
    [SerializeField] private float wheelRadius = 0.2f;
    [SerializeField] private float wheelSpinMultiplier = 1f;
    [SerializeField] private Vector3 wheelLocalAxis = Vector3.right;

    [Header("Debug")]
    [SerializeField] private bool debugParams = false;
    [SerializeField] private bool crouchReleaseFailsafe = true;
    [SerializeField] private bool enableDebugStateKeys = false;

    private CharacterController cc;

    private Vector2 moveInput;
    private Vector3 horizontalVelocity;
    private float verticalVelocity;
    private bool crouchHeld;
    private int jumpsLeft;
    private float tiltAngle;

    private bool isDead;
    private bool isPushing;
    private bool isPulling;
    private bool pushPullActive;

    private Vector3 visualInitialLocalPosition;
    private float pushPullVisualOffsetCurrent;

    private float idleTimer;
    private float nextIdleChange;
    private int lastIdleSlot = -1;

    private Quaternion wheelInitialLocalRotation;
    private float wheelSpinAngleAccum;

    private MovingPlatform currentPlatform;

    public bool IsStealth => crouchHeld;
    public bool IsBusy { get; set; }
    public Vector2 MoveInput => moveInput;

    private static readonly int SpeedHash =
        Animator.StringToHash("Speed");

    private static readonly int GroundedHash =
        Animator.StringToHash("IsGrounded");

    private static readonly int CrouchHash =
        Animator.StringToHash("IsCrouch");

    private static readonly int JumpHash =
        Animator.StringToHash("Jump");

    private static readonly int IdleSlotHash =
        Animator.StringToHash("IdleSlot");

    private static readonly int IdleNextHash =
        Animator.StringToHash("IdleNext");

    private static readonly int IsDeadHash =
        Animator.StringToHash("IsDead");

    private static readonly int IsPushingHash =
        Animator.StringToHash("IsPushing");

    private static readonly int IsPullingHash =
        Animator.StringToHash("IsPulling");

    private static readonly int DeathTriggerHash =
        Animator.StringToHash("DeathTrigger");

    private void Awake()
    {
        cc = GetComponent<CharacterController>();
        jumpsLeft = extraJumps;

        if (!animator)
            animator = GetComponentInChildren<Animator>();

        if (!visualTransform)
            visualTransform = animator ? animator.transform : transform;

        if (visualTransform)
            visualInitialLocalPosition = visualTransform.localPosition;

        if (wheel)
            wheelInitialLocalRotation = wheel.localRotation;

        nextIdleChange = Random.Range(
            idleChangeInterval.x,
            idleChangeInterval.y
        );
    }

    private void Start()
    {
        if (!animator || !animator.runtimeAnimatorController)
        {
            Debug.LogError(
                "[TIKO] Animator sau Animator Controller lipsă."
            );
        }
        else if (debugParams)
        {
            Debug.Log(
                $"[TIKO] Animator: {animator.name} | " +
                $"Controller: {animator.runtimeAnimatorController.name}"
            );
        }
    }

    private void Update()
    {
        Vector3 platformDelta = Vector3.zero;

        if (currentPlatform != null)
            platformDelta = currentPlatform.Velocity * Time.deltaTime;

        currentPlatform = null;

        HandleDebugStateInput();
        HandleCrouchFailsafe();

        HandleMovement(out float planarSpeed, out float speed01);
        HandleGravity();
        ApplyMotion(platformDelta);

        HandleRotation();
        UpdateAnimator(speed01);
        HandleTilt(speed01);
        UpdatePushPullVisualOffset();
        SpinWheel(planarSpeed);
    }

    private void HandleDebugStateInput()
    {
        if (!enableDebugStateKeys || Keyboard.current == null)
            return;

        if (Keyboard.current.digit0Key.wasPressedThisFrame)
        {
            isDead = !isDead;

            if (isDead)
            {
                SetPushPullActive(false);
                animator?.SetTrigger(DeathTriggerHash);
            }

            if (debugParams)
                Debug.Log($"[TIKO] IsDead: {isDead}");
        }
    }

    private void HandleCrouchFailsafe()
    {
        if (!crouchReleaseFailsafe || !crouchHeld)
            return;

        bool shiftHeld =
            Keyboard.current != null &&
            Keyboard.current.leftShiftKey.isPressed;

        bool gamepadHeld =
            Gamepad.current != null &&
            Gamepad.current.leftShoulder.isPressed;

        if (!shiftHeld && !gamepadHeld)
            crouchHeld = false;
    }

    private void HandleMovement(
        out float planarSpeed,
        out float speed01)
    {
        Vector3 desiredDir = new Vector3(
            moveInput.x,
            0f,
            moveInput.y
        );

        if (desiredDir.sqrMagnitude > 1f)
            desiredDir.Normalize();

        float speedMultiplier = 1f;

        if (crouchHeld)
            speedMultiplier = crouchSpeedMultiplier;

        if (pushPullActive)
            speedMultiplier = pushPullSpeedMultiplier;

        if (isDead)
            speedMultiplier = 0f;

        float targetSpeed = moveSpeed * speedMultiplier;
        Vector3 desiredVelocity = desiredDir * targetSpeed;

        float currentAcceleration =
            desiredVelocity.sqrMagnitude > 0.001f
                ? acceleration
                : deceleration;

        horizontalVelocity = Vector3.MoveTowards(
            horizontalVelocity,
            desiredVelocity,
            currentAcceleration * Time.deltaTime
        );

        planarSpeed = new Vector2(
            horizontalVelocity.x,
            horizontalVelocity.z
        ).magnitude;

        speed01 = Mathf.Clamp01(planarSpeed / moveSpeed);
    }

    private void HandleGravity()
    {
        if (cc.isGrounded)
        {
            if (verticalVelocity < 0f)
                verticalVelocity = -2f;

            jumpsLeft = extraJumps;
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }
    }

    private void ApplyMotion(Vector3 platformDelta)
    {
        Vector3 motion =
            (horizontalVelocity + new Vector3(0f, verticalVelocity, 0f))
            * Time.deltaTime
            + platformDelta;

        cc.Move(motion);
    }

    private void HandleRotation()
    {
        if (
            !rotateToMoveDirection ||
            isDead ||
            pushPullActive)
        {
            return;
        }

        Vector3 desiredDir = new Vector3(
            moveInput.x,
            0f,
            moveInput.y
        );

        if (desiredDir.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(
            desiredDir,
            Vector3.up
        );

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            18f * Time.deltaTime
        );
    }

    private void UpdateAnimator(float speed01)
    {
        if (!animator || !animator.runtimeAnimatorController)
            return;

        bool grounded = cc.isGrounded;

        animator.SetFloat(
            SpeedHash,
            speed01,
            0.05f,
            Time.deltaTime
        );

        animator.SetBool(GroundedHash, grounded);
        animator.SetBool(CrouchHash, crouchHeld);
        animator.SetBool(IsDeadHash, isDead);
        animator.SetBool(IsPushingHash, isPushing);
        animator.SetBool(IsPullingHash, isPulling);

        TickIdleStateMachine(grounded, speed01);
    }

    private void TickIdleStateMachine(
        bool grounded,
        float speed01)
    {
        if (
            !enableIdleRandom ||
            !grounded ||
            crouchHeld ||
            isDead ||
            isPushing ||
            isPulling ||
            speed01 >= idleSpeedEpsilon)
        {
            idleTimer = 0f;
            return;
        }

        idleTimer += Time.deltaTime;

        if (idleTimer < nextIdleChange)
            return;

        int slot = Random.Range(0, 3);

        if (slot == lastIdleSlot)
            slot = (slot + Random.Range(1, 3)) % 3;

        lastIdleSlot = slot;

        animator.SetInteger(IdleSlotHash, slot);
        animator.SetTrigger(IdleNextHash);

        if (debugParams)
            Debug.Log($"[TIKO] Idle slot = {slot}");

        idleTimer = 0f;

        nextIdleChange = Random.Range(
            idleChangeInterval.x,
            idleChangeInterval.y
        );
    }

    private void HandleTilt(float speed01)
    {
        if (!enableMovementTilt || !visualTransform)
            return;

        float tiltMultiplier = crouchHeld
            ? crouchTiltMultiplier
            : 1f;

        float targetTilt =
            -maxTiltAngle * speed01 * tiltMultiplier;

        if (!cc.isGrounded)
            targetTilt *= 0.3f;

        if (isDead)
            targetTilt = 0f;

        tiltAngle = Mathf.Lerp(
            tiltAngle,
            targetTilt,
            tiltSpeed * Time.deltaTime
        );

        visualTransform.localRotation = Quaternion.Euler(
            tiltAngle,
            0f,
            0f
        );
    }

    private void UpdatePushPullVisualOffset()
    {
        if (!visualTransform)
            return;

        float targetOffset = 0f;

        if (pushPullActive)
        {
            if (isPulling)
                targetOffset = pullVisualOffset;
            else if (isPushing)
                targetOffset = pushVisualOffset;
            else
                targetOffset = pushVisualOffset;
        }

        pushPullVisualOffsetCurrent = Mathf.Lerp(
            pushPullVisualOffsetCurrent,
            targetOffset,
            pushPullVisualOffsetSpeed * Time.deltaTime
        );

        visualTransform.localPosition =
            visualInitialLocalPosition +
            Vector3.back * pushPullVisualOffsetCurrent;
    }

    private void SpinWheel(float planarSpeed)
    {
        if (!wheel)
            return;

        float angularDegrees = wheelRadius > 0.0001f
            ? (planarSpeed / wheelRadius)
              * Mathf.Rad2Deg
              * Time.deltaTime
              * wheelSpinMultiplier
            : 0f;

        wheelSpinAngleAccum =
            (wheelSpinAngleAccum + angularDegrees) % 360f;

        Quaternion spin = Quaternion.AngleAxis(
            wheelSpinAngleAccum,
            wheelLocalAxis.normalized
        );

        wheel.localRotation = wheelInitialLocalRotation * spin;
    }

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    public void OnCrouch(InputValue value)
    {
        if (
            isDead ||
            isPushing ||
            isPulling ||
            pushPullActive ||
            IsBusy)
        {
            return;
        }

        crouchHeld = value.isPressed;

        if (debugParams)
            Debug.Log($"[TIKO] Crouch/Stealth: {crouchHeld}");
    }

    public void OnJump(InputValue value)
    {
        if (
            !enableJump ||
            !value.isPressed ||
            crouchHeld ||
            pushPullActive ||
            isDead ||
            IsBusy)
        {
            return;
        }

        float jumpVelocity = Mathf.Sqrt(
            jumpHeight * -2f * gravity
        );

        if (cc.isGrounded)
        {
            verticalVelocity = jumpVelocity;
            animator?.SetTrigger(JumpHash);

            if (debugParams)
                Debug.Log("[TIKO] Jump!");

            return;
        }

        if (jumpsLeft > 0)
        {
            jumpsLeft--;

            verticalVelocity = jumpVelocity;
            animator?.SetTrigger(JumpHash);

            if (debugParams)
            {
                Debug.Log(
                    $"[TIKO] Double Jump! ({jumpsLeft} left)"
                );
            }
        }
    }

    public void SetPushPullActive(bool active)
    {
        pushPullActive = active;

        if (!active)
        {
            isPushing = false;
            isPulling = false;
        }
    }

    public void SetPushPullAnimation(bool pushing, bool pulling)
    {
        if (isDead || IsBusy || !pushPullActive)
        {
            isPushing = false;
            isPulling = false;
            return;
        }

        isPushing = pushing;
        isPulling = pulling;
    }

    private void OnControllerColliderHit(
        ControllerColliderHit hit)
    {
        if (hit.collider.CompareTag("MovingPlatform"))
        {
            currentPlatform =
                hit.collider.GetComponent<MovingPlatform>();
        }
    }
}