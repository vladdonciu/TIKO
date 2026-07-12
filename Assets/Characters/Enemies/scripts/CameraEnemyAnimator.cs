using UnityEngine;

public class CameraEnemyAnimator : MonoBehaviour
{
    private enum AnimationMode
    {
        IdleNormal,
        ScanRun,
        PatrolRun,
        ChaseRun,
        AttackIdle
    }

    [Header("References")]
    [SerializeField] private Animator animator;

    [Header("Idle Random")]
    [Tooltip("Idle 1 este blocat numai în timpul atacului. Idle 1/2/3 sunt disponibile în pauza naturală.")]
    [SerializeField] private Vector2 idleChangeInterval =
        new Vector2(1.5f, 3f);

    [Header("Speed Settings")]
    [Tooltip("Viteza Run pentru patrulare reală.")]
    [SerializeField] private float patrolAnimSpeed = 0.7f;

    [Tooltip("Viteza Run pentru chase.")]
    [SerializeField] private float chaseAnimSpeed = 1f;

    [Tooltip("Viteza Run folosită strict când Sentry se rotește în scan.")]
    [SerializeField] private float scanAnimSpeed = 0.38f;

    private static readonly int SpeedHash =
        Animator.StringToHash("Speed");

    private static readonly int IdleIndexHash =
        Animator.StringToHash("IdleIndex");

    private AnimationMode currentMode;

    private float idleTimer;
    private float idleInterval;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (animator == null)
        {
            Debug.LogError(
                $"[CameraEnemyAnimator] Animator nu a fost găsit pe {gameObject.name}!"
            );

            enabled = false;
            return;
        }

        ResetIdleTimer();
        SetAttackIdle();
    }

    private void Update()
    {
        if (animator == null)
            return;

        if (currentMode != AnimationMode.IdleNormal)
            return;

        TickIdleRandom();
    }

    // Folosit DOAR în timpul rotației de scan.
    public void SetScanning()
    {
        if (animator == null)
            return;

        if (currentMode == AnimationMode.ScanRun)
            return;

        currentMode = AnimationMode.ScanRun;

        animator.SetFloat(
            SpeedHash,
            scanAnimSpeed
        );
    }

    // Folosit pentru inamicii mobili care patrulează.
    public void SetPatrolling()
    {
        if (animator == null)
            return;

        if (currentMode == AnimationMode.PatrolRun)
            return;

        currentMode = AnimationMode.PatrolRun;

        animator.SetFloat(
            SpeedHash,
            patrolAnimSpeed
        );
    }

    // Folosit pentru chase real.
    public void SetChasing()
    {
        if (animator == null)
            return;

        if (currentMode == AnimationMode.ChaseRun)
            return;

        currentMode = AnimationMode.ChaseRun;

        animator.SetFloat(
            SpeedHash,
            chaseAnimSpeed
        );
    }

    // Idle normal: permite Idle 1, Idle 2 și Idle 3.
    // Este apelat imediat când scan-ul se termină sau este întrerupt.
    public void SetStationaryIdle()
    {
        SetNormalIdle();
    }

    // Idle normal pentru orice inamic care nu atacă.
    public void ForceIdle()
    {
        SetNormalIdle();
    }

    // Folosit strict în Attack / charge / shot.
    public void SetAttackIdle()
    {
        if (animator == null)
            return;

        if (currentMode == AnimationMode.AttackIdle)
            return;

        currentMode = AnimationMode.AttackIdle;

        animator.SetFloat(SpeedHash, 0f);
        animator.SetInteger(IdleIndexHash, 1);
    }

    private void SetNormalIdle()
    {
        if (animator == null)
            return;

        if (currentMode == AnimationMode.IdleNormal)
            return;

        currentMode = AnimationMode.IdleNormal;

        // Această linie trebuie să declanșeze instant Run → Idle.
        animator.SetFloat(SpeedHash, 0f);

        ResetIdleTimer();
    }

    private void TickIdleRandom()
    {
        idleTimer += Time.deltaTime;

        if (idleTimer < idleInterval)
            return;

        ResetIdleTimer();

        int currentIdle = animator.GetInteger(IdleIndexHash);
        int nextIdle;

        do
        {
            nextIdle = Random.Range(1, 4);
        }
        while (nextIdle == currentIdle);

        animator.SetInteger(
            IdleIndexHash,
            nextIdle
        );
    }

    private void ResetIdleTimer()
    {
        idleTimer = 0f;

        idleInterval = Random.Range(
            idleChangeInterval.x,
            idleChangeInterval.y
        );
    }
}