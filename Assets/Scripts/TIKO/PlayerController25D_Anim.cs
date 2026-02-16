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

    [Header("Jump/Gravity")]
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
    [SerializeField] private float idleSpeedEpsilon = 0.1f; // important: pragul pentru idle

    [Header("Wheel Spin")]
    [SerializeField] private float wheelRadius = 0.2f;
    [SerializeField] private float wheelSpinMultiplier = 1f;
    [SerializeField] private Vector3 wheelLocalAxis = Vector3.right;

    [Header("Debug")]
    [SerializeField] private bool debugParams = true;
    [SerializeField] private bool crouchReleaseFailsafe = true;

    private CharacterController cc;

    private Vector2 moveInput;
    private Vector3 horizontalVelocity;
    private float verticalVelocity;

    private bool crouchHeld;
    private int jumpsLeft;

    private float tiltAngle;

    private float idleTimer;
    private float nextIdleChange;
    private int lastIdleSlot = -1;

    private Quaternion wheelInitialLocalRotation;
    private float wheelSpinAngleAccum;

    // Animator params [web:225]
    private static readonly int SpeedHash      = Animator.StringToHash("Speed");
    private static readonly int GroundedHash   = Animator.StringToHash("IsGrounded");
    private static readonly int CrouchHash     = Animator.StringToHash("IsCrouch");
    private static readonly int JumpHash       = Animator.StringToHash("Jump");
    private static readonly int IdleSlotHash   = Animator.StringToHash("IdleSlot");
    private static readonly int IdleNextHash   = Animator.StringToHash("IdleNext");

    private void Awake()
    {
        cc = GetComponent<CharacterController>();
        jumpsLeft = extraJumps;

        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!visualTransform) visualTransform = animator ? animator.transform : transform;

        if (wheel) wheelInitialLocalRotation = wheel.localRotation;

        nextIdleChange = Random.Range(idleChangeInterval.x, idleChangeInterval.y);
    }

    private void Start()
    {
        Debug.Log($"[TIKO] AnimatorRef={(animator ? animator.name : "NULL")}");
        Debug.Log($"[TIKO] Controller={(animator && animator.runtimeAnimatorController ? animator.runtimeAnimatorController.name : "NULL")}");
        
        if (!animator || !animator.runtimeAnimatorController)
        {
            Debug.LogError("[TIKO] Animator sau Controller lipsă! Animațiile nu vor merge.");
        }
    }

    private void Update()
    {
        // failsafe crouch (tastatură)
        if (crouchReleaseFailsafe && crouchHeld && Keyboard.current != null)
        {
            if (!Keyboard.current.leftShiftKey.isPressed)
                crouchHeld = false;
        }

        // direction XZ
        Vector3 desiredDir = new Vector3(moveInput.x, 0f, moveInput.y);
        if (desiredDir.sqrMagnitude > 1f) desiredDir.Normalize();

        float targetSpeed = moveSpeed * (crouchHeld ? crouchSpeedMultiplier : 1f);

        // accel/decel
        Vector3 desiredVel = desiredDir * targetSpeed;
        float accel = (desiredVel.sqrMagnitude > 0.001f) ? acceleration : deceleration;
        horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, desiredVel, accel * Time.deltaTime);

        // grounded & gravity
        bool grounded = cc.isGrounded;
        if (grounded)
        {
            if (verticalVelocity < 0f) verticalVelocity = -2f;
            jumpsLeft = extraJumps;
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }

        // move [web:92]
        Vector3 motion = horizontalVelocity;
        motion.y = verticalVelocity;
        cc.Move(motion * Time.deltaTime);

        // rotate
        if (rotateToMoveDirection && desiredDir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(desiredDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 18f * Time.deltaTime);
        }

        // animator params
        float planarSpeed = new Vector2(horizontalVelocity.x, horizontalVelocity.z).magnitude;
        float speed01 = Mathf.Clamp01(planarSpeed / moveSpeed);

        if (animator && animator.runtimeAnimatorController)
        {
            animator.SetFloat(SpeedHash, speed01);
            animator.SetBool(GroundedHash, grounded);
            animator.SetBool(CrouchHash, crouchHeld);

            // debug: vezi ce setezi
            if (debugParams && Time.frameCount % 60 == 0) // o dată pe secundă
            {
                Debug.Log($"[TIKO] Speed={speed01:F2} | Grounded={grounded} | Crouch={crouchHeld}");
            }

            TickIdleStateMachine(grounded, speed01);
        }

        // tilt
        if (enableMovementTilt && visualTransform)
        {
            float tiltMult = crouchHeld ? crouchTiltMultiplier : 1f;
            float targetTilt = -maxTiltAngle * speed01 * tiltMult;
            if (!grounded) targetTilt *= 0.3f;

            tiltAngle = Mathf.Lerp(tiltAngle, targetTilt, tiltSpeed * Time.deltaTime);
            visualTransform.localRotation = Quaternion.Euler(tiltAngle, 0f, 0f);
        }

        // wheel
        SpinWheel(planarSpeed);
    }

    private void TickIdleStateMachine(bool grounded, float speed01)
    {
        // Condiția pentru idle: grounded, speed mic, nu crouch
        if (!enableIdleRandom || !grounded || crouchHeld || speed01 >= idleSpeedEpsilon)
        {
            idleTimer = 0f;
            return;
        }

        idleTimer += Time.deltaTime;
        if (idleTimer < nextIdleChange) return;

        // alege slot 0/1/2 fără repetare
        int slot = Random.Range(0, 3);
        if (slot == lastIdleSlot) 
            slot = (slot + Random.Range(1, 3)) % 3;
        
        lastIdleSlot = slot;

        animator.SetInteger(IdleSlotHash, slot);
        animator.SetTrigger(IdleNextHash);

        if (debugParams)
            Debug.Log($"[TIKO] Idle change: slot={slot}");

        idleTimer = 0f;
        nextIdleChange = Random.Range(idleChangeInterval.x, idleChangeInterval.y);
    }

    private void SpinWheel(float planarSpeed)
    {
        if (!wheel) return;

        float angularSpeedRad = (wheelRadius > 0.0001f) ? (planarSpeed / wheelRadius) : 0f;
        float angularDeg = angularSpeedRad * Mathf.Rad2Deg * Time.deltaTime * wheelSpinMultiplier;

        wheelSpinAngleAccum += angularDeg;

        Quaternion spin = Quaternion.AngleAxis(wheelSpinAngleAccum, wheelLocalAxis.normalized);
        wheel.localRotation = wheelInitialLocalRotation * spin;
    }

    // ===== Send Messages callbacks [web:154]
    public void OnMove(InputValue value) 
    {
        moveInput = value.Get<Vector2>();
        if (debugParams && moveInput.sqrMagnitude > 0.01f)
            Debug.Log($"[TIKO] OnMove: {moveInput}");
    }

    public void OnCrouch(InputValue value) 
    {
        crouchHeld = value.isPressed;
        if (debugParams)
            Debug.Log($"[TIKO] OnCrouch: {crouchHeld}");
    }

    public void OnJump(InputValue value)
    {
        if (!enableJump) return;
        if (!value.isPressed) return;
        if (crouchHeld) return;

        bool grounded = cc.isGrounded;
        float jumpVel = Mathf.Sqrt(jumpHeight * -2f * gravity);

        if (grounded)
        {
            verticalVelocity = jumpVel;
            if (animator) animator.SetTrigger(JumpHash);
            if (debugParams) Debug.Log("[TIKO] Jump!");
            return;
        }

        if (jumpsLeft > 0)
        {
            jumpsLeft--;
            verticalVelocity = jumpVel;
            if (animator) animator.SetTrigger(JumpHash);
            if (debugParams) Debug.Log($"[TIKO] Double jump! ({jumpsLeft} left)");
        }
    }
}
