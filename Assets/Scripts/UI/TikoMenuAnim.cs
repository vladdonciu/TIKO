using UnityEngine;

public class TikoMenuAnim : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Animator animator;

    [Header("Idle Random")]
    [SerializeField] private bool    enableIdleRandom   = true;
    [SerializeField] private Vector2 idleChangeInterval = new Vector2(3f, 7f);

    [Header("Wheel Spin (opțional)")]
    [SerializeField] private Transform wheel;
    [SerializeField] private float     wheelRadius          = 0.2f;
    [SerializeField] private float     wheelSpinMultiplier  = 1f;
    [SerializeField] private Vector3   wheelLocalAxis       = Vector3.right;

    private static readonly int IdleSlotHash = Animator.StringToHash("IdleSlot");
    private static readonly int IdleNextHash = Animator.StringToHash("IdleNext");
    private static readonly int SpeedHash    = Animator.StringToHash("Speed");
    private static readonly int GroundedHash = Animator.StringToHash("IsGrounded");

    private float      idleTimer;
    private float      nextIdleChange;
    private int        lastIdleSlot = -1;
    private Quaternion wheelInitialLocalRotation;
    private float      wheelSpinAngleAccum;

    void Awake()
    {
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (wheel)     wheelInitialLocalRotation = wheel.localRotation;

        nextIdleChange = Random.Range(idleChangeInterval.x, idleChangeInterval.y);
    }

    void Start()
    {
        if (!animator || !animator.runtimeAnimatorController)
        {
            Debug.LogError("[TIKO Menu] Animator lipsă!");
            return;
        }

        // Forțează starea de idle de la start
        animator.SetFloat(SpeedHash,   0f);
        animator.SetBool(GroundedHash, true);
    }

    void Update()
    {
        TickIdleRandom();
        SpinWheel();
    }

    void TickIdleRandom()
    {
        if (!enableIdleRandom || !animator) return;

        idleTimer += Time.deltaTime;
        if (idleTimer < nextIdleChange) return;

        int slot = Random.Range(0, 3);
        if (slot == lastIdleSlot)
            slot = (slot + Random.Range(1, 3)) % 3;

        lastIdleSlot = slot;
        animator.SetInteger(IdleSlotHash, slot);
        animator.SetTrigger(IdleNextHash);

        idleTimer      = 0f;
        nextIdleChange = Random.Range(idleChangeInterval.x, idleChangeInterval.y);
    }

    void SpinWheel()
    {
        if (!wheel) return;

        // Pe main screen roata se rotește lent constant
        float angularDeg = (wheelRadius > 0.0001f)
            ? (1f / wheelRadius) * Mathf.Rad2Deg * Time.deltaTime * wheelSpinMultiplier
            : 0f;

        wheelSpinAngleAccum = (wheelSpinAngleAccum + angularDeg) % 360f;
        Quaternion spin     = Quaternion.AngleAxis(wheelSpinAngleAccum, wheelLocalAxis.normalized);
        wheel.localRotation = wheelInitialLocalRotation * spin;
    }
}