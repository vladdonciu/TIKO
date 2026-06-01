using UnityEngine;

public class CameraEnemyAI : MonoBehaviour
{
    public enum State { Patrol, Chase, Attack }

    [Header("Refs")]
    public Transform eye;
    public Transform[] patrolPoints;
    public CameraEnemyLaser laser;
    public CameraEnemyAnimator enemyAnimator;

    [Header("Detection")]
    public float viewDistance   = 12f;
    public float viewAngle      = 90f;
    public float attackDistance = 6f;

    [Header("Movement")]
    public float patrolSpeed  = 2f;
    public float chaseSpeed   = 4f;
    public float rotateSpeed  = 6f;

    [Header("Patrol")]
    public float waypointReachDistance = 0.4f;

    [Header("Separation")]
    public float separationRadius   = 1.2f;
    public float separationStrength = 2f;
    public LayerMask enemyMask;

    private Transform player;
    private PlayerController25D_Anim tikoCtrl;
    private Rigidbody rb;
    private int   patrolIndex  = 0;
    private State currentState = State.Patrol;

    // ─── START ──────────────────────────────────────────────────
    void Start()
    {
        rb = GetComponent<Rigidbody>();

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p)
        {
            player   = p.transform;
            tikoCtrl = p.GetComponent<PlayerController25D_Anim>();
        }

        if (!eye)           eye           = transform;
        if (!enemyAnimator) enemyAnimator = GetComponentInChildren<CameraEnemyAnimator>();

        if (patrolPoints == null || patrolPoints.Length == 0)
            enemyAnimator?.ForceIdle();
    }

    // ─── UPDATE ─────────────────────────────────────────────────
    void Update()
    {
        if (!player) return;

        switch (currentState)
        {
            case State.Patrol: TickPatrol(); break;
            case State.Chase:  TickChase();  break;
            case State.Attack: TickAttack(); break;
        }

        laser?.TickLaser(currentState == State.Attack);
    }

    // ─── PATROL ─────────────────────────────────────────────────
    void TickPatrol()
    {
        if (CanSeePlayer())
        {
            currentState = State.Chase;
            return;
        }

        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            enemyAnimator?.ForceIdle();
            return;
        }

        enemyAnimator?.SetPatrolling();

        Transform target = patrolPoints[patrolIndex];
        if (target == null) return;

        MoveTowards(target.position, patrolSpeed);

        if (Vector3.Distance(transform.position, target.position) < waypointReachDistance)
            patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
    }

    // ─── CHASE ──────────────────────────────────────────────────
    void TickChase()
    {
        float dist = Vector3.Distance(transform.position, player.position);

        if (dist <= attackDistance)
        {
            currentState = State.Attack;
            return;
        }

        if (!CanSeePlayer())
        {
            currentState = State.Patrol;
            return;
        }

        enemyAnimator?.SetChasing();
        MoveTowards(player.position, chaseSpeed);
    }

    // ─── ATTACK ─────────────────────────────────────────────────
    void TickAttack()
    {
        float dist = Vector3.Distance(transform.position, player.position);

        if (dist > attackDistance || !CanSeePlayer())
        {
            currentState = State.Chase;
            return;
        }

        enemyAnimator?.ForceIdle();

        // Rotire spre player fără mișcare
        Vector3 dir = (player.position - transform.position);
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir.normalized);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, targetRot, rotateSpeed * Time.deltaTime);
        }

        // Oprește orice mișcare reziduală
        if (rb) rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
    }

    // ─── CAN SEE PLAYER ─────────────────────────────────────────
    bool CanSeePlayer()
    {
        if (!player) return false;

        Vector3 targetPos = player.position + Vector3.up * 0.5f;
        Vector3 dir       = (targetPos - eye.position).normalized;
        float   dist      = Vector3.Distance(eye.position, targetPos);

        bool isStealth = tikoCtrl != null && tikoCtrl.IsStealth;

        if (isStealth)
        {
            // STEALTH: distanță redusă + doar din față
            if (dist > viewDistance * 0.4f) return false;
            float angle = Vector3.Angle(eye.forward, dir);
            if (angle > viewAngle * 0.5f) return false;
        }
        else
        {
            // NORMAL: 360° până la viewDistance complet
            if (dist > viewDistance) return false;
            // fără restricție de unghi
        }

        // Raycast — obstacolele blochează indiferent de mod
        if (Physics.Raycast(eye.position, dir, out RaycastHit hit, dist))
        {
            Debug.DrawLine(eye.position, hit.point,
                hit.collider.CompareTag("Player") ? Color.red : Color.yellow);
            return hit.collider.CompareTag("Player");
        }

        Debug.DrawLine(eye.position, targetPos, Color.green);
        return false;
    }

    // ─── MOVE TOWARDS (prin Rigidbody — respectă coliziunile) ───
    void MoveTowards(Vector3 target, float speed)
    {
        Vector3 dir = target - transform.position;
        dir.y = 0f;

        dir += GetSeparation();

        if (dir.sqrMagnitude < 0.001f) return;

        Vector3 move = dir.normalized * speed * Time.deltaTime;
        rb.MovePosition(transform.position + move);

        // Rotire manuală
        Quaternion rot = Quaternion.LookRotation(dir.normalized);
        transform.rotation = Quaternion.Slerp(
            transform.rotation, rot, rotateSpeed * Time.deltaTime);
    }

    // ─── SEPARATION ─────────────────────────────────────────────
    Vector3 GetSeparation()
    {
        Collider[] hits  = Physics.OverlapSphere(transform.position, separationRadius, enemyMask);
        Vector3    force = Vector3.zero;

        foreach (var h in hits)
        {
            if (h.transform == transform) continue;
            Vector3 away = transform.position - h.transform.position;
            float   d    = away.magnitude;
            if (d > 0.001f) force += away.normalized / d;
        }

        return force * separationStrength;
    }

    // ─── GIZMOS ─────────────────────────────────────────────────
    void OnDrawGizmosSelected()
    {
        Transform origin = eye ? eye : transform;

        // Con normal 360° (verde)
        Gizmos.color = new Color(0f, 1f, 0f, 0.15f);
        Gizmos.DrawWireSphere(origin.position, viewDistance);

        // Con stealth (mov, mai mic)
        float stealthDist = viewDistance * 0.4f;
        float halfAngle   = viewAngle * 0.5f;
        Gizmos.color = new Color(0.6f, 0f, 1f, 0.5f);
        Vector3 leftDir  = Quaternion.Euler(0, -halfAngle, 0) * origin.forward;
        Vector3 rightDir = Quaternion.Euler(0,  halfAngle, 0) * origin.forward;
        Gizmos.DrawLine(origin.position, origin.position + leftDir  * stealthDist);
        Gizmos.DrawLine(origin.position, origin.position + rightDir * stealthDist);
        Gizmos.DrawLine(origin.position, origin.position + origin.forward * stealthDist);

        // Attack range (roșu)
        Gizmos.color = new Color(1f, 0f, 0f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, attackDistance);

        // Separation (galben)
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, separationRadius);

        // Patrol points (cyan)
        if (patrolPoints == null || patrolPoints.Length == 0) return;
        Gizmos.color = Color.cyan;
        for (int i = 0; i < patrolPoints.Length; i++)
        {
            if (patrolPoints[i] == null) continue;
            Gizmos.DrawSphere(patrolPoints[i].position, 0.2f);
            int next = (i + 1) % patrolPoints.Length;
            if (patrolPoints[next] != null)
                Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[next].position);
        }

        DrawViewCone(origin); // conul animat verde/galben/roșu după state
    }

    void DrawViewCone(Transform origin)
    {
        Color coneColor = currentState switch
        {
            State.Chase  => Color.yellow,
            State.Attack => Color.red,
            _            => Color.green
        };

        Gizmos.color = coneColor;
        float halfAngle = viewAngle * 0.5f;
        int   segments  = 30;

        Gizmos.DrawLine(origin.position, origin.position + origin.forward * viewDistance);

        Vector3 leftDir  = Quaternion.Euler(0, -halfAngle, 0) * origin.forward;
        Vector3 rightDir = Quaternion.Euler(0,  halfAngle, 0) * origin.forward;
        Gizmos.DrawLine(origin.position, origin.position + leftDir  * viewDistance);
        Gizmos.DrawLine(origin.position, origin.position + rightDir * viewDistance);

        Vector3 prev = origin.position + leftDir * viewDistance;
        for (int i = 1; i <= segments; i++)
        {
            float   t    = i / (float)segments;
            float   ang  = Mathf.Lerp(-halfAngle, halfAngle, t);
            Vector3 d    = Quaternion.Euler(0, ang, 0) * origin.forward;
            Vector3 next = origin.position + d * viewDistance;
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
}