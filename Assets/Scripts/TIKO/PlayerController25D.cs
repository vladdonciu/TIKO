using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController25D : MonoBehaviour
{
    [Header("Move")]
    [SerializeField] float moveSpeed = 5.5f;
    [SerializeField] float acceleration = 18f;   // cât de “glide” e
    [SerializeField] float deceleration = 22f;
    [SerializeField] bool rotateToMoveDirection = true;

    [Header("Gravity / Jump")]
    [SerializeField] bool enableJump = true;
    [SerializeField] float gravity = -25f;
    [SerializeField] float jumpHeight = 1.2f;
    [SerializeField] int extraJumps = 1; // 1 = double jump

    [Header("Crouch/Hide")]
    [SerializeField] bool enableCrouch = false;
    [SerializeField] float crouchSpeedMultiplier = 0.5f;

    CharacterController cc;

    Vector2 moveInput;
    Vector3 horizontalVelocity;   // XZ
    float verticalVelocity;       // Y

    bool crouchHeld;
    int jumpsLeft;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        jumpsLeft = extraJumps;
    }

    void Update()
    {
        // 1) Direcție mișcare în plan XZ din WASD
        Vector3 desiredDir = new Vector3(moveInput.x, 0f, moveInput.y);
        if (desiredDir.sqrMagnitude > 1f) desiredDir.Normalize(); // diagonala constantă

        float speed = moveSpeed * (crouchHeld ? crouchSpeedMultiplier : 1f);

        // 2) “glide” accel/decel
        Vector3 desiredVel = desiredDir * speed;
        float accel = (desiredVel.sqrMagnitude > 0.001f) ? acceleration : deceleration;
        horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, desiredVel, accel * Time.deltaTime);

        // 3) Gravitație + reset jumps când e grounded
        if (cc.isGrounded)
        {
            if (verticalVelocity < 0f) verticalVelocity = -2f; // ține lipit de sol
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
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(desiredDir, Vector3.up),
                18f * Time.deltaTime
            );
        }
    }

    // PlayerInput (Send Messages) -> numele acțiunii "Move" cheamă "OnMove"
    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    // PlayerInput (Send Messages): pentru button poți folosi InputValue.isPressed
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
