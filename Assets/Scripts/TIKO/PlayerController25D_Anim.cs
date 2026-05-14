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
    [SerializeField] private float moveSpeed             = 5.5f;
    [SerializeField] private float acceleration          = 18f;
    [SerializeField] private float deceleration          = 22f;
    [SerializeField] private bool  rotateToMoveDirection = true;
    [SerializeField] private float crouchSpeedMultiplier = 0.5f;

    [Header("Jump / Gravity")]
    [SerializeField] private bool  enableJump  = true;
    [SerializeField] private float gravity     = -25f;
    [SerializeField] private float jumpHeight  = 1.2f;
    [SerializeField] private int   extraJumps  = 1;

    [Header("Tilt")]
    [SerializeField] private bool  enableMovementTilt    = true;
    [SerializeField] private float maxTiltAngle          = 8f;
    [SerializeField] private float tiltSpeed             = 10f;
    [SerializeField] private float crouchTiltMultiplier  = 0.3f;

    [Header("Idle Random (State Machine)")]
    [SerializeField] private bool    enableIdleRandom    = true;
    [SerializeField] private Vector2 idleChangeInterval  = new Vector2(3f, 7f);
    [SerializeField] private float   idleSpeedEpsilon    = 0.1f;

    [Header("Wheel Spin")]
    [SerializeField] private float   wheelRadius          = 0.2f;
    [SerializeField] private float   wheelSpinMultiplier  = 1f;
    [SerializeField] private Vector3 wheelLocalAxis       = Vector3.right;

    [Header("Debug")]
    [SerializeField] private bool debugParams          = false;
    [SerializeField] private bool crouchReleaseFailsafe = true;

    // ── Componente ───────────────────────────────────────────────
    private CharacterController cc;

    // ── State ────────────────────────────────────────────────────
    private Vector2 moveInput;
    private Vector3 horizontalVelocity;
    private float   verticalVelocity;
    private bool    crouchHeld;
    private int     jumpsLeft;
    private float   tiltAngle;

    // ── Idle random ──────────────────────────────────────────────
    private float idleTimer;
    private float nextIdleChange;
    private int   lastIdleSlot = -1;

    // ── Wheel ────────────────────────────────────────────────────
    private Quaternion wheelInitialLocalRotation;
    private float      wheelSpinAngleAccum;

    // ── Platform ─────────────────────────────────────────────────
    private MovingPlatform currentPlatform;

    // ── Proprietate publică citită de inamici ────────────────────
    public bool IsStealth => crouchHeld;

    // ── Animator hashes ──────────────────────────────────────────
    private static readonly int SpeedHash    = Animator.StringToHash("Speed");
    private static readonly int GroundedHash = Animator.StringToHash("IsGrounded");
    private static readonly int CrouchHash   = Animator.StringToHash("IsCrouch");
    private static readonly int JumpHash     = Animator.StringToHash("Jump");
    private static readonly int IdleSlotHash = Animator.StringToHash("IdleSlot");
    private static readonly int IdleNextHash = Animator.StringToHash("IdleNext");

    // ─── AWAKE ───────────────────────────────────────────────────
    void Awake()
    {
        cc        = GetComponent<CharacterController>();
        jumpsLeft = extraJumps;

        if (!animator)        animator        = GetComponentInChildren<Animator>();
        if (!visualTransform) visualTransform = animator ? animator.transform : transform;
        if (wheel)            wheelInitialLocalRotation = wheel.localRotation;

        nextIdleChange = Random.Range(idleChangeInterval.x, idleChangeInterval.y);
    }

    // ─── START ───────────────────────────────────────────────────
    void Start()
    {
        if (!animator || !animator.runtimeAnimatorController)
            Debug.LogError("[TIKO] Animator sau Controller lipsă! Animațiile nu vor merge.");
        else if (debugParams)
            Debug.Log($"[TIKO] Animator: {animator.name} | Controller: {animator.runtimeAnimatorController.name}");
    }

    // ─── UPDATE ──────────────────────────────────────────────────
    void Update()
    {
        Vector3 platformDelta = Vector3.zero;
        if (currentPlatform != null)
            platformDelta = currentPlatform.Velocity * Time.deltaTime;
        currentPlatform = null;

        HandleCrouchFailsafe();
        HandleMovement(out float planarSpeed, out float speed01);
        HandleGravity();
        ApplyMotion(platformDelta);
        HandleRotation();
        UpdateAnimator(speed01);
        HandleTilt(speed01);
        SpinWheel(planarSpeed);
    }

    // ─── CROUCH FAILSAFE ─────────────────────────────────────────
    void HandleCrouchFailsafe()
    {
        if (!crouchReleaseFailsafe || !crouchHeld) return;

        bool shiftHeld = Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;

        // Suport gamepad opțional
        bool gamepadHeld = Gamepad.current != null && Gamepad.current.leftShoulder.isPressed;

        if (!shiftHeld && !gamepadHeld)
            crouchHeld = false;
    }

    // ─── MOVEMENT ────────────────────────────────────────────────
    void HandleMovement(out float planarSpeed, out float speed01)
    {
        Vector3 desiredDir = new Vector3(moveInput.x, 0f, moveInput.y);
        if (desiredDir.sqrMagnitude > 1f) desiredDir.Normalize();

        float targetSpeed = moveSpeed * (crouchHeld ? crouchSpeedMultiplier : 1f);
        Vector3 desiredVel = desiredDir * targetSpeed;

        float accel = (desiredVel.sqrMagnitude > 0.001f) ? acceleration : deceleration;
        horizontalVelocity = Vector3.MoveTowards(
            horizontalVelocity, desiredVel, accel * Time.deltaTime);

        planarSpeed = new Vector2(horizontalVelocity.x, horizontalVelocity.z).magnitude;
        speed01     = Mathf.Clamp01(planarSpeed / moveSpeed);
    }

    // ─── GRAVITY ─────────────────────────────────────────────────
    void HandleGravity()
    {
        bool grounded = cc.isGrounded;

        if (grounded)
        {
            if (verticalVelocity < 0f) verticalVelocity = -2f;
            jumpsLeft = extraJumps;          // reset double jump la aterizare
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }
    }

    // ─── APPLY MOTION ────────────────────────────────────────────
    void ApplyMotion(Vector3 platformDelta)
    {
        Vector3 motion = (horizontalVelocity + new Vector3(0f, verticalVelocity, 0f))
                         * Time.deltaTime + platformDelta;
        cc.Move(motion);
    }

    // ─── ROTATION ────────────────────────────────────────────────
    void HandleRotation()
    {
        if (!rotateToMoveDirection) return;

        Vector3 desiredDir = new Vector3(moveInput.x, 0f, moveInput.y);
        if (desiredDir.sqrMagnitude < 0.001f) return;

        Quaternion targetRot = Quaternion.LookRotation(desiredDir, Vector3.up);
        transform.rotation   = Quaternion.Slerp(transform.rotation, targetRot, 18f * Time.deltaTime);
    }

    // ─── ANIMATOR ────────────────────────────────────────────────
    void UpdateAnimator(float speed01)
    {
        if (!animator || !animator.runtimeAnimatorController) return;

        bool grounded = cc.isGrounded;

        animator.SetFloat(SpeedHash,    speed01, 0.05f, Time.deltaTime);
        animator.SetBool(GroundedHash,  grounded);
        animator.SetBool(CrouchHash,    crouchHeld);

        TickIdleStateMachine(grounded, speed01);
    }

    // ─── IDLE RANDOM ─────────────────────────────────────────────
    void TickIdleStateMachine(bool grounded, float speed01)
    {
        if (!enableIdleRandom || !grounded || crouchHeld || speed01 >= idleSpeedEpsilon)
        {
            idleTimer = 0f;
            return;
        }

        idleTimer += Time.deltaTime;
        if (idleTimer < nextIdleChange) return;

        int slot = Random.Range(0, 3);
        if (slot == lastIdleSlot)
            slot = (slot + Random.Range(1, 3)) % 3;

        lastIdleSlot = slot;
        animator.SetInteger(IdleSlotHash, slot);
        animator.SetTrigger(IdleNextHash);

        if (debugParams) Debug.Log($"[TIKO] Idle slot={slot}");

        idleTimer      = 0f;
        nextIdleChange = Random.Range(idleChangeInterval.x, idleChangeInterval.y);
    }

    // ─── TILT ────────────────────────────────────────────────────
    void HandleTilt(float speed01)
    {
        if (!enableMovementTilt || !visualTransform) return;

        bool grounded    = cc.isGrounded;
        float tiltMult   = crouchHeld ? crouchTiltMultiplier : 1f;
        float targetTilt = -maxTiltAngle * speed01 * tiltMult;
        if (!grounded) targetTilt *= 0.3f;

        tiltAngle = Mathf.Lerp(tiltAngle, targetTilt, tiltSpeed * Time.deltaTime);
        visualTransform.localRotation = Quaternion.Euler(tiltAngle, 0f, 0f);
    }

    // ─── WHEEL SPIN ──────────────────────────────────────────────
    void SpinWheel(float planarSpeed)
    {
        if (!wheel) return;

        float angularDeg = (wheelRadius > 0.0001f)
            ? (planarSpeed / wheelRadius) * Mathf.Rad2Deg * Time.deltaTime * wheelSpinMultiplier
            : 0f;

        wheelSpinAngleAccum = (wheelSpinAngleAccum + angularDeg) % 360f; // evită overflow
        Quaternion spin     = Quaternion.AngleAxis(wheelSpinAngleAccum, wheelLocalAxis.normalized);
        wheel.localRotation = wheelInitialLocalRotation * spin;
    }

    // ─── INPUT CALLBACKS ─────────────────────────────────────────
    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    public void OnCrouch(InputValue value)
    {
        crouchHeld = value.isPressed;
        if (debugParams) Debug.Log($"[TIKO] Crouch/Stealth: {crouchHeld}");
    }

    public void OnJump(InputValue value)
    {
        if (!enableJump || !value.isPressed || crouchHeld) return;

        float jumpVel = Mathf.Sqrt(jumpHeight * -2f * gravity);

        if (cc.isGrounded)
        {
            verticalVelocity = jumpVel;
            animator?.SetTrigger(JumpHash);
            if (debugParams) Debug.Log("[TIKO] Jump!");
            return;
        }

        if (jumpsLeft > 0)
        {
            jumpsLeft--;
            verticalVelocity = jumpVel;
            animator?.SetTrigger(JumpHash);
            if (debugParams) Debug.Log($"[TIKO] Double jump! ({jumpsLeft} left)");
        }
    }

    // ─── PLATFORM ────────────────────────────────────────────────
    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.collider.CompareTag("MovingPlatform"))
            currentPlatform = hit.collider.GetComponent<MovingPlatform>();
    }
}