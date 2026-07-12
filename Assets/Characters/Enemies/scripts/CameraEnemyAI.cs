using UnityEngine;

public class CameraEnemyAI : MonoBehaviour
{
    private enum EnemyState
    {
        Idle,
        Patrol,
        Suspicious,
        Chase,
        Attack,
        Search,
        ReturnToPatrol
    }

    [Header("References")]
    [SerializeField] private Transform eye;
    [SerializeField] private CameraEnemyLaser laser;
    [SerializeField] private CameraEnemyAnimator enemyAnimator;

    [Header("Ambient Audio")]
    [Tooltip("AudioSource separat pentru hum/idle/patrol. Nu folosi AudioSource-ul laserului.")]
    [SerializeField] private AudioSource ambientAudio;

    [Tooltip("Pitch normal pentru Idle, Patrol, Search și Return.")]
    [SerializeField] private float ambientIdlePitch = 1f;

    [Tooltip("Pitch mai rapid pentru Chase și Attack.")]
    [SerializeField] private float ambientRunPitch = 1.12f;

    [Tooltip("Cât de repede se schimbă pitch-ul între stări.")]
    [SerializeField] private float ambientPitchLerpSpeed = 5f;

    [Header("Player")]
    [SerializeField] private Transform player;
    [SerializeField] private StealthCloakController playerStealth;

    [Header("Detection")]
    [Tooltip("Distanța maximă la care inamicul poate vedea Tiko.")]
    [SerializeField] private float detectionRange = 12f;

    [Tooltip("Unghiul total al conului vizual la distanță.")]
    [Range(10f, 360f)]
    [SerializeField] private float viewAngle = 110f;

    [Tooltip("Cât timp trebuie văzut Tiko înainte să înceapă chase.")]
    [SerializeField] private float detectionConfirmTime = 0.35f;

    [Tooltip("În această rază, Sentry detectează Tiko la 360°, chiar și din spate. Necesită line-of-sight.")]
    [SerializeField] private float closeDetectionRange = 2.25f;

    [Tooltip("Dacă este activ, Ghost face Tiko complet nedetectabil la orice distanță.")]
    [SerializeField] private bool ghostBlocksAllDetection = true;

    [Tooltip("Bifează Player, Ground, Environment, Wall și Obstacles.")]
    [SerializeField] private LayerMask visionMask = ~0;

    [Header("Movement")]
    [Tooltip("ON pentru inamicul mobil; OFF pentru sentry staționar.")]
    [SerializeField] private bool canMove = true;

    [Tooltip("Doar pentru sentry: Idle normal când nu scanează; Idle 1 doar la Attack.")]
    [SerializeField] private bool stationaryUsesIdleOnly = true;

    [SerializeField] private float patrolSpeed = 1.5f;
    [SerializeField] private float chaseSpeed = 3.75f;
    [SerializeField] private float stoppingDistance = 5f;
    [SerializeField] private float rotationSpeed = 540f;

    [Header("Stationary Sentry Scan")]
    [Tooltip("Activează scanarea naturală pentru turela staționară.")]
    [SerializeField] private bool useStationaryScan = true;

    [Tooltip("Viteza de rotație în grade pe secundă în timpul scanării.")]
    [SerializeField] private float scanRotationSpeed = 35f;

    [Tooltip("Unghiul minim rotit într-o scanare.")]
    [SerializeField] private float minScanAngle = 55f;

    [Tooltip("Unghiul maxim rotit într-o scanare.")]
    [SerializeField] private float maxScanAngle = 120f;

    [Tooltip("Timp minim de pauză idle între două scanări.")]
    [SerializeField] private float minScanPause = 3.2f;

    [Tooltip("Timp maxim de pauză idle între două scanări.")]
    [SerializeField] private float maxScanPause = 5.5f;

    [Header("Patrol")]
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float patrolWaitTime = 1f;
    [SerializeField] private float patrolPointReachDistance = 0.15f;

    [Header("Target Memory")]
    [SerializeField] private float loseTargetDelay = 1.2f;
    [SerializeField] private float searchDuration = 2.5f;

    [Header("Simple Obstacle Avoidance")]
    [Tooltip("Layerele pereților/props-urilor care blochează deplasarea.")]
    [SerializeField] private LayerMask obstacleMask;

    [SerializeField] private float obstacleCheckDistance = 1.2f;
    [SerializeField] private float avoidanceAngle = 55f;
    [SerializeField] private float avoidanceDuration = 0.7f;

    private EnemyState currentState;
    private int patrolIndex;

    private float stateTimer;
    private float visibleTimer;
    private float lostTargetTimer;

    private Vector3 lastKnownPlayerPosition;

    private float avoidanceTimer;
    private int avoidanceSide = 1;

    private bool isScanning;
    private float scanAngleRemaining;
    private float scanPauseTimer;
    private int currentScanDirection;

    private bool IsStationary => !canMove && stationaryUsesIdleOnly;

    private void Awake()
    {
        if (eye == null)
            eye = transform;

        if (laser == null)
            laser = GetComponent<CameraEnemyLaser>();

        if (enemyAnimator == null)
            enemyAnimator = GetComponent<CameraEnemyAnimator>();

        FindPlayer();

        currentState = HasPatrolPoints() && canMove
            ? EnemyState.Patrol
            : EnemyState.Idle;

        scanPauseTimer = Random.Range(
            minScanPause,
            maxScanPause
        );

        if (ambientAudio != null)
            ambientAudio.pitch = ambientIdlePitch;
    }

    private void Update()
    {
        UpdateAmbientAudio();

        if (player == null)
        {
            FindPlayer();

            if (player == null)
                return;
        }

        bool canSeePlayer = CanSeePlayer();

        switch (currentState)
        {
            case EnemyState.Idle:
                UpdateIdle(canSeePlayer);
                break;

            case EnemyState.Patrol:
                UpdatePatrol(canSeePlayer);
                break;

            case EnemyState.Suspicious:
                UpdateSuspicious(canSeePlayer);
                break;

            case EnemyState.Chase:
                UpdateChase(canSeePlayer);
                break;

            case EnemyState.Attack:
                UpdateAttack(canSeePlayer);
                break;

            case EnemyState.Search:
                UpdateSearch(canSeePlayer);
                break;

            case EnemyState.ReturnToPatrol:
                UpdateReturnToPatrol(canSeePlayer);
                break;
        }
    }

    private void FindPlayer()
    {
        GameObject playerObject =
            GameObject.FindGameObjectWithTag("Player");

        if (playerObject == null)
            return;

        player = playerObject.transform;

        playerStealth =
            playerObject.GetComponent<StealthCloakController>();
    }

    private void UpdateIdle(bool canSeePlayer)
    {
        laser?.TickLaser(false);

        if (canSeePlayer)
        {
            StopStationaryScan();
            EnterState(EnemyState.Suspicious);
            return;
        }

        UpdateStationaryScan();
    }

    private void UpdatePatrol(bool canSeePlayer)
    {
        laser?.TickLaser(false);

        if (canSeePlayer)
        {
            EnterState(EnemyState.Suspicious);
            return;
        }

        if (!canMove || !HasPatrolPoints())
        {
            EnterState(EnemyState.Idle);
            return;
        }

        Transform targetPoint = patrolPoints[patrolIndex];

        if (FlatDistance(transform.position, targetPoint.position) <=
            patrolPointReachDistance)
        {
            SetIdleAnimation();

            stateTimer += Time.deltaTime;

            if (stateTimer >= patrolWaitTime)
            {
                patrolIndex =
                    (patrolIndex + 1) % patrolPoints.Length;

                stateTimer = 0f;
            }

            return;
        }

        stateTimer = 0f;

        MoveTowards(targetPoint.position, patrolSpeed);
        enemyAnimator?.SetPatrolling();
    }

    private void UpdateSuspicious(bool canSeePlayer)
    {
        StopStationaryScan();

        SetIdleAnimation();
        laser?.TickLaser(false);

        if (!canSeePlayer)
        {
            EnterState(GetDefaultState());
            return;
        }

        FacePosition(player.position);

        visibleTimer += Time.deltaTime;

        if (visibleTimer >= detectionConfirmTime)
        {
            lastKnownPlayerPosition = player.position;

            EnterState(EnemyState.Chase);
        }
    }

    private void UpdateChase(bool canSeePlayer)
    {
        StopStationaryScan();

        laser?.TickLaser(false);

        if (!canSeePlayer)
        {
            SetIdleAnimation();

            lostTargetTimer += Time.deltaTime;

            if (lostTargetTimer >= loseTargetDelay)
                EnterState(EnemyState.Search);

            return;
        }

        lastKnownPlayerPosition = player.position;
        lostTargetTimer = 0f;

        float distanceToPlayer =
            FlatDistance(transform.position, player.position);

        if (IsStationary)
        {
            FacePosition(player.position);

            SetIdleAnimation();

            if (distanceToPlayer <= stoppingDistance)
                EnterState(EnemyState.Attack);

            return;
        }

        if (distanceToPlayer <= stoppingDistance)
        {
            EnterState(EnemyState.Attack);
            return;
        }

        MoveTowards(player.position, chaseSpeed);
        enemyAnimator?.SetChasing();
    }

    private void UpdateAttack(bool canSeePlayer)
    {
        StopStationaryScan();

        if (!canSeePlayer)
        {
            laser?.TickLaser(false);

            EnterState(EnemyState.Chase);
            return;
        }

        lastKnownPlayerPosition = player.position;

        float distanceToPlayer =
            FlatDistance(transform.position, player.position);

        if (!IsStationary && distanceToPlayer > stoppingDistance)
        {
            laser?.TickLaser(false);

            EnterState(EnemyState.Chase);
            return;
        }

        FacePosition(player.position);

        // Attack / charge / shot: doar Idle 1.
        enemyAnimator?.SetAttackIdle();

        laser?.TickLaser(true);
    }

    private void UpdateSearch(bool canSeePlayer)
    {
        laser?.TickLaser(false);

        if (canSeePlayer)
        {
            StopStationaryScan();

            EnterState(EnemyState.Chase);
            return;
        }

        if (IsStationary)
        {
            UpdateStationaryScan();

            stateTimer += Time.deltaTime;

            if (stateTimer >= searchDuration)
                EnterState(GetDefaultState());

            return;
        }

        if (FlatDistance(
            transform.position,
            lastKnownPlayerPosition) > patrolPointReachDistance)
        {
            MoveTowards(lastKnownPlayerPosition, patrolSpeed);

            enemyAnimator?.SetChasing();
            return;
        }

        SetIdleAnimation();

        stateTimer += Time.deltaTime;

        if (stateTimer >= searchDuration)
            EnterState(EnemyState.ReturnToPatrol);
    }

    private void UpdateReturnToPatrol(bool canSeePlayer)
    {
        laser?.TickLaser(false);

        if (canSeePlayer)
        {
            EnterState(EnemyState.Chase);
            return;
        }

        if (!HasPatrolPoints() || !canMove)
        {
            EnterState(EnemyState.Idle);
            return;
        }

        Transform returnPoint = patrolPoints[patrolIndex];

        if (FlatDistance(transform.position, returnPoint.position) <=
            patrolPointReachDistance)
        {
            EnterState(EnemyState.Patrol);
            return;
        }

        MoveTowards(returnPoint.position, patrolSpeed);
        enemyAnimator?.SetPatrolling();
    }

    private void UpdateStationaryScan()
    {
        if (!IsStationary || !useStationaryScan)
        {
            enemyAnimator?.SetStationaryIdle();
            return;
        }

        if (isScanning)
        {
            float angleThisFrame =
                scanRotationSpeed * Time.deltaTime;

            angleThisFrame = Mathf.Min(
                angleThisFrame,
                scanAngleRemaining
            );

            transform.Rotate(
                Vector3.up,
                angleThisFrame * currentScanDirection,
                Space.World
            );

            scanAngleRemaining -= angleThisFrame;

            // Run doar cât se rotește.
            enemyAnimator?.SetScanning();

            if (scanAngleRemaining <= 0f)
            {
                isScanning = false;

                scanPauseTimer = Random.Range(
                    minScanPause,
                    maxScanPause
                );

                // Oprește Run imediat; idle random normal.
                enemyAnimator?.SetStationaryIdle();
            }

            return;
        }

        enemyAnimator?.SetStationaryIdle();

        scanPauseTimer -= Time.deltaTime;

        if (scanPauseTimer <= 0f)
            BeginNewScan();
    }

    private void BeginNewScan()
    {
        float safeMinAngle = Mathf.Max(1f, minScanAngle);
        float safeMaxAngle =
            Mathf.Max(safeMinAngle, maxScanAngle);

        currentScanDirection =
            Random.value < 0.5f ? -1 : 1;

        scanAngleRemaining = Random.Range(
            safeMinAngle,
            safeMaxAngle
        );

        isScanning = true;
    }

    private void StopStationaryScan()
    {
        isScanning = false;
        scanAngleRemaining = 0f;
    }

    private bool CanSeePlayer()
    {
        if (player == null)
            return false;

        bool playerIsStealthed =
            playerStealth != null &&
            playerStealth.IsStealthed;

        // Ghost = invizibil complet, inclusiv la proximitate.
        if (ghostBlocksAllDetection && playerIsStealthed)
            return false;

        Vector3 origin = eye.position;

        Vector3 target =
            player.position + Vector3.up * 0.5f;

        Vector3 toPlayer = target - origin;

        float distanceToPlayer = toPlayer.magnitude;

        if (distanceToPlayer > detectionRange)
            return false;

        if (distanceToPlayer <= 0.001f)
            return true;

        // Verifică peretele înaintea oricărei reguli de unghi.
        if (!HasLineOfSight(
            origin,
            toPlayer,
            distanceToPlayer))
        {
            return false;
        }

        // Aproape: detecție 360°.
        if (distanceToPlayer <= closeDetectionRange)
            return true;

        // La distanță: doar în conul frontal.
        Vector3 flatDirection =
            player.position - transform.position;

        flatDirection.y = 0f;

        if (flatDirection.sqrMagnitude < 0.001f)
            return true;

        float angleToPlayer = Vector3.Angle(
            transform.forward,
            flatDirection.normalized
        );

        return angleToPlayer <= viewAngle * 0.5f;
    }

    private bool HasLineOfSight(
        Vector3 origin,
        Vector3 toPlayer,
        float distanceToPlayer)
    {
        if (Physics.Raycast(
            origin,
            toPlayer.normalized,
            out RaycastHit hit,
            distanceToPlayer + 0.1f,
            visionMask,
            QueryTriggerInteraction.Ignore))
        {
            return hit.collider.GetComponentInParent<TikoHealth>() != null;
        }

        return false;
    }

    private void MoveTowards(Vector3 targetPosition, float speed)
    {
        if (!canMove)
            return;

        Vector3 flatTarget = targetPosition;
        flatTarget.y = transform.position.y;

        Vector3 wantedDirection =
            (flatTarget - transform.position).normalized;

        if (wantedDirection.sqrMagnitude <= 0.001f)
            return;

        Vector3 moveDirection =
            GetAvoidanceDirection(wantedDirection);

        FaceDirection(moveDirection);

        transform.position +=
            moveDirection * speed * Time.deltaTime;
    }

    private Vector3 GetAvoidanceDirection(Vector3 wantedDirection)
    {
        Vector3 origin =
            transform.position + Vector3.up * 0.35f;

        if (avoidanceTimer > 0f)
        {
            avoidanceTimer -= Time.deltaTime;

            Vector3 sideDirection =
                Quaternion.Euler(
                    0f,
                    avoidanceAngle * avoidanceSide,
                    0f
                ) * wantedDirection;

            return sideDirection.normalized;
        }

        if (Physics.Raycast(
            origin,
            wantedDirection,
            obstacleCheckDistance,
            obstacleMask,
            QueryTriggerInteraction.Ignore))
        {
            avoidanceSide =
                Random.value > 0.5f ? 1 : -1;

            avoidanceTimer = avoidanceDuration;

            Vector3 sideDirection =
                Quaternion.Euler(
                    0f,
                    avoidanceAngle * avoidanceSide,
                    0f
                ) * wantedDirection;

            return sideDirection.normalized;
        }

        return wantedDirection;
    }

    private void FacePosition(Vector3 targetPosition)
    {
        Vector3 direction =
            targetPosition - transform.position;

        direction.y = 0f;

        FaceDirection(direction);
    }

    private void FaceDirection(Vector3 direction)
    {
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
            return;

        Quaternion desiredRotation = Quaternion.LookRotation(
            direction.normalized,
            Vector3.up
        );

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            desiredRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    private void UpdateAmbientAudio()
    {
        if (ambientAudio == null)
            return;

        bool isAggressive =
            currentState == EnemyState.Chase ||
            currentState == EnemyState.Attack;

        float targetPitch = isAggressive
            ? ambientRunPitch
            : ambientIdlePitch;

        ambientAudio.pitch = Mathf.Lerp(
            ambientAudio.pitch,
            targetPitch,
            ambientPitchLerpSpeed * Time.deltaTime
        );

        if (!ambientAudio.isPlaying)
            ambientAudio.Play();
    }

    private void SetIdleAnimation()
    {
        enemyAnimator?.ForceIdle();
    }

    private float FlatDistance(Vector3 first, Vector3 second)
    {
        first.y = 0f;
        second.y = 0f;

        return Vector3.Distance(first, second);
    }

    private bool HasPatrolPoints()
    {
        return patrolPoints != null &&
            patrolPoints.Length > 0;
    }

    private EnemyState GetDefaultState()
    {
        return HasPatrolPoints() && canMove
            ? EnemyState.Patrol
            : EnemyState.Idle;
    }

    private void EnterState(EnemyState newState)
    {
        if (currentState == newState)
            return;

        currentState = newState;

        stateTimer = 0f;
        visibleTimer = 0f;
        lostTargetTimer = 0f;
        avoidanceTimer = 0f;

        if (newState != EnemyState.Idle &&
            newState != EnemyState.Search)
        {
            StopStationaryScan();
        }

        if (newState == EnemyState.Idle)
        {
            scanPauseTimer = Random.Range(
                minScanPause,
                maxScanPause
            );
        }
    }

    private void OnDisable()
    {
        if (ambientAudio != null &&
            ambientAudio.isPlaying)
        {
            ambientAudio.Stop();
        }

        StopStationaryScan();

        laser?.TickLaser(false);
    }

    private void OnDrawGizmosSelected()
    {
        Transform viewOrigin =
            eye != null ? eye : transform;

        Gizmos.color = Color.yellow;

        Gizmos.DrawWireSphere(
            transform.position,
            detectionRange
        );

        Vector3 leftEdge =
            Quaternion.Euler(
                0f,
                -viewAngle * 0.5f,
                0f
            ) * transform.forward;

        Vector3 rightEdge =
            Quaternion.Euler(
                0f,
                viewAngle * 0.5f,
                0f
            ) * transform.forward;

        Gizmos.color = Color.red;

        Gizmos.DrawLine(
            viewOrigin.position,
            viewOrigin.position + leftEdge * detectionRange
        );

        Gizmos.DrawLine(
            viewOrigin.position,
            viewOrigin.position + rightEdge * detectionRange
        );

        Gizmos.color = Color.cyan;

        Gizmos.DrawWireSphere(
            transform.position,
            closeDetectionRange
        );
    }
}