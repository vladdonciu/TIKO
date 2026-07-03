using UnityEngine;

public class CameraEnemyAnimator : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Animator animator;

    [Header("Idle Random")]
    public Vector2 idleChangeInterval = new Vector2(2f, 5f);

    [Header("Speed Settings")]
    public float patrolAnimSpeed = 0.7f;
    public float chaseAnimSpeed  = 1.0f;

    private static readonly int SpeedHash     = Animator.StringToHash("Speed");
    private static readonly int IdleIndexHash = Animator.StringToHash("IdleIndex");

    private float _idleTimer;
    private float _idleInterval;
    private bool  _isMoving;

    void Awake()
    {
        if (!animator)
            animator = GetComponentInChildren<Animator>();

        if (!animator)
            Debug.LogError($"[CameraEnemyAnimator] Animator nu a fost găsit pe {gameObject.name}!");

        _idleInterval = Random.Range(idleChangeInterval.x, idleChangeInterval.y);
    }

    void Update()
    {
        if (_isMoving) return;
        TickIdleRandom();
    }

    // ── Apelat din CameraEnemyAI când inamicul patrulează ──────
    public void SetPatrolling()
    {
        _isMoving = true;
        if (!animator) return;
        animator.SetFloat(SpeedHash, patrolAnimSpeed);
    }

    // ── Apelat din CameraEnemyAI când urmărește player-ul ──────
    public void SetChasing()
    {
        _isMoving = true;
        if (!animator) return;
        animator.SetFloat(SpeedHash, chaseAnimSpeed);
    }

    // ── Apelat din CameraEnemyAI când stă (Attack sau fără patrol) ──
    public void ForceIdle()
    {
        _isMoving = false;
        if (!animator) return;
        animator.SetFloat(SpeedHash, 0f);
    }

    // ── Idle random: schimbă CAMERA_idle1/2/3 ──────────────────
    private void TickIdleRandom()
    {
        _idleTimer += Time.deltaTime;
        if (_idleTimer < _idleInterval) return;

        _idleTimer    = 0f;
        _idleInterval = Random.Range(idleChangeInterval.x, idleChangeInterval.y);

        // Alege un idle diferit față de cel curent
        int current = animator.GetInteger(IdleIndexHash);
        int next;
        do { next = Random.Range(1, 4); } while (next == current);

        animator.SetInteger(IdleIndexHash, next);
    }
}