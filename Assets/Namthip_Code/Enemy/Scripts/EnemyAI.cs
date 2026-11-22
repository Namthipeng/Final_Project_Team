using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent), typeof(Health))]
public class EnemyAI : MonoBehaviour
{
    private enum State { Patrol, Chase, Attack, Dead }

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private Transform[] patrolPoints;

    [Header("Patrol")]
    [SerializeField] private float walkSpeed = 2f;
    [SerializeField] private float waitAtPoint = 2f;

    [Header("Detection")]
    [SerializeField] private float sightRange = 12f;
    [SerializeField, Range(10, 360)] private float sightAngle = 120f;
    [SerializeField] private float senseRange = 3f;      // ใกล้กว่านี้ รู้ตัวแม้อยู่ข้างหลัง
    [SerializeField] private float loseSightTime = 3f;   // หลุดสายตานานเท่านี้แล้วเลิกตาม
    [SerializeField] private float alertTime = 10f;      // โดนยิงแล้วรู้ตำแหน่งผู้เล่นนานเท่านี้

    [Header("Combat")]
    [SerializeField] private float runSpeed = 4.5f;
    [SerializeField] private float attackRange = 8f;
    [SerializeField] private int damage = 10;
    [SerializeField] private float fireInterval = 0.6f;
    [SerializeField] private float spreadAngle = 4f;     // ยิ่งมากยิ่งยิงพลาด
    [SerializeField] private float turnSpeed = 360f;
    [SerializeField] private MuzzleFlash muzzleFlash;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int ShootHash = Animator.StringToHash("Shoot");
    private static readonly int DieHash = Animator.StringToHash("Die");

    private NavMeshAgent agent;
    private Health health;
    private Transform player;
    private Health playerHealth;

    private State state = State.Patrol;
    private int patrolIndex;
    private float waitTimer;
    private float lostTimer;
    private float nextFireTime;
    private int lastHealth;
    private float alertTimer;

    private Vector3 Eye => transform.position + Vector3.up * 1.5f;
    private Vector3 PlayerTarget => player.position + Vector3.up * 1f;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        health = GetComponent<Health>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        lastHealth = health.Max;
    }

    private void OnEnable()
    {
        health.Died += Die;
        health.Changed += OnDamaged;
    }

    private void OnDisable()
    {
        health.Died -= Die;
        health.Changed -= OnDamaged;
    }

    private void Start()
    {
        GoToPatrolPoint();
    }

    private void Update()
    {
        if (state == State.Dead) return;

        if (alertTimer > 0f) alertTimer -= Time.deltaTime;

        FindPlayer();
        bool canSee = CanSeePlayer();

        switch (state)
        {
            case State.Patrol: DoPatrol(canSee); break;
            case State.Chase: DoChase(canSee); break;
            case State.Attack: DoAttack(canSee); break;
        }

        animator.SetFloat(SpeedHash, agent.velocity.magnitude, 0.1f, Time.deltaTime);
    }

    // ---------------- สถานะ ----------------

    private void DoPatrol(bool canSee)
    {
        if (canSee) { EnterChase(); return; }
        if (patrolPoints == null || patrolPoints.Length == 0) return;

        bool arrived = !agent.pathPending &&
                       agent.remainingDistance <= agent.stoppingDistance + 0.2f;
        if (!arrived) return;

        waitTimer += Time.deltaTime;
        if (waitTimer >= waitAtPoint)
        {
            patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
            GoToPatrolPoint();
        }
    }

    private void DoChase(bool canSee)
    {
        if (canSee)
        {
            lostTimer = 0f;
            agent.SetDestination(player.position);

            if (Vector3.Distance(transform.position, player.position) <= attackRange)
            {
                state = State.Attack;
                agent.isStopped = true;
            }
        }
        else
        {
            lostTimer += Time.deltaTime;
            if (lostTimer >= loseSightTime) GoToPatrolPoint();
        }
    }

    private void DoAttack(bool canSee)
    {
        if (!canSee) { EnterChase(); return; }

        Vector3 flat = player.position - transform.position;
        flat.y = 0f;

        if (flat.magnitude > attackRange * 1.2f) { EnterChase(); return; }

        // หันหาผู้เล่น
        if (flat.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, Quaternion.LookRotation(flat), turnSpeed * Time.deltaTime);
        }

        bool facing = Vector3.Angle(transform.forward, flat) < 10f;
        if (facing && Time.time >= nextFireTime) Shoot();
    }

    private void EnterChase()
    {
        state = State.Chase;
        lostTimer = 0f;
        agent.isStopped = false;
        agent.speed = runSpeed;
    }

    private void GoToPatrolPoint()
    {
        state = State.Patrol;
        waitTimer = 0f;
        agent.isStopped = false;
        agent.speed = walkSpeed;

        if (patrolPoints != null && patrolPoints.Length > 0)
            agent.SetDestination(patrolPoints[patrolIndex].position);
    }

    // ---------------- ยิง ----------------

    private void Shoot()
    {
        Debug.Log("Enemy Shoot, flash = " + muzzleFlash);
        nextFireTime = Time.time + fireInterval;
        animator.SetTrigger(ShootHash);
        if (muzzleFlash != null) muzzleFlash.Play();    

        Vector3 dir = (PlayerTarget - Eye).normalized;
        dir = Quaternion.Euler(
            Random.Range(-spreadAngle, spreadAngle),
            Random.Range(-spreadAngle, spreadAngle), 0f) * dir;

        Debug.DrawRay(Eye, dir * attackRange, Color.red, 0.1f);

        if (!RaycastFirst(Eye, dir, attackRange * 1.5f, out RaycastHit hit)) return;
        if (hit.collider.GetComponentInParent<EnemyAI>() != null) return; // ไม่ทำร้ายพวกเดียวกัน

        hit.collider.GetComponentInParent<IDamageable>()?.TakeDamage(damage);
    }

    // ---------------- การมองเห็น ----------------

    // หาผู้เล่นจากสคริปต์ PlayerController (ไม่ต้องพึ่ง Tag)
    private void FindPlayer()
    {
        if (player != null) return;
        PlayerController pc = FindAnyObjectByType<PlayerController>();
        if (pc == null) return;
        player = pc.transform;
        playerHealth = pc.GetComponent<Health>();
    }

    private bool CanSeePlayer()
    {
        if (player == null) return false;
        if (playerHealth != null && playerHealth.IsDead) return false;
        if (alertTimer > 0f) return true; // โดนยิงอยู่ ถือว่ารู้ตำแหน่งผู้เล่นเสมอ

        Vector3 to = PlayerTarget - Eye;
        float dist = to.magnitude;
        if (dist > sightRange) return false;

        if (dist > senseRange)
        {
            Vector3 flat = new Vector3(to.x, 0f, to.z);
            if (Vector3.Angle(transform.forward, flat) > sightAngle * 0.5f) return false;
        }

        // มีอะไรบังระหว่างทางไหม (กำแพง ฯลฯ)
        bool hitSomething = RaycastFirst(Eye, to.normalized, dist, out RaycastHit hit);
        return !hitSomething || hit.collider.transform.IsChildOf(player);
    }

    // ยิง Ray แล้วคืนสิ่งแรกที่โดน โดยข้ามตัวศัตรูเอง
    private bool RaycastFirst(Vector3 origin, Vector3 dir, float dist, out RaycastHit best)
    {
        best = default;
        float nearest = float.MaxValue;
        bool found = false;

        foreach (RaycastHit h in Physics.RaycastAll(origin, dir, dist, ~0, QueryTriggerInteraction.Ignore))
        {
            if (h.collider.transform.IsChildOf(transform)) continue;
            if (h.distance < nearest) { nearest = h.distance; best = h; found = true; }
        }
        return found;
    }

    // ---------------- โดนยิง / ตาย / รีเซ็ต ----------------

    private void OnDamaged(int current, int max)
    {
        // HP ต้อง "ลดลง" ถึงจะนับว่าโดนยิง (ResetHealth ทำให้ HP เพิ่ม จึงไม่ไล่)
        bool tookDamage = current < lastHealth;
        lastHealth = current;

        Debug.Log($"[Enemy] โดนยิง HP={current} tookDamage={tookDamage}");
        if (!tookDamage || current <= 0) return;

        FindPlayer();
        if (player == null) { Debug.LogWarning("[Enemy] หาผู้เล่นไม่เจอ"); return; }

        alertTimer = alertTime; // รู้แล้วว่าใครยิง

        if (state == State.Patrol) EnterChase();
        Debug.Log("[Enemy] เข้าโหมดไล่");
    }

    private void Die()
    {
        if (state == State.Dead) return;   // กันถูกเรียกซ้ำ

        
        state = State.Dead;

        // หยุด NavMeshAgent ให้เด็ดขาด
        if (agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.velocity = Vector3.zero;
        }
        agent.enabled = false;

        // ปิด Root Motion กันตัวเลื่อนตามท่าตาย
        animator.applyRootMotion = false;
        animator.SetFloat(SpeedHash, 0f);
        animator.SetTrigger(DieHash);

        // ปิด Collider กันโดนยิงซ้ำ
        foreach (Collider col in GetComponentsInChildren<Collider>())
            col.enabled = false;

        // ซ่อนแทนการลบ เพื่อให้เกิดใหม่ได้
        Invoke(nameof(Hide), 4f);
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }

    // เรียกจาก EnemyRespawn ตอนผู้เล่นเกิดใหม่
    public void ResetAI()
    {
        CancelInvoke(nameof(Hide)); // กัน Enemy ที่เพิ่งตายถูกซ่อนหลังรีสปอนไปแล้ว

        state = State.Patrol;
        patrolIndex = 0;
        waitTimer = 0f;
        lostTimer = 0f;
        nextFireTime = 0f;
        alertTimer = 0f;

        health.ResetHealth();
        lastHealth = health.Current;

        animator.Rebind();   // กลับท่าเริ่มต้น เคลียร์ Trigger Die
        animator.Update(0f);

        GoToPatrolPoint();
    }

    // ---------------- วาดวงในหน้า Scene ----------------

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, sightRange);
        Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(transform.position, senseRange);

        Gizmos.color = Color.yellow;
        Vector3 l = Quaternion.Euler(0, -sightAngle * 0.5f, 0) * transform.forward;
        Vector3 r = Quaternion.Euler(0, sightAngle * 0.5f, 0) * transform.forward;
        Gizmos.DrawRay(transform.position, l * sightRange);
        Gizmos.DrawRay(transform.position, r * sightRange);

        if (patrolPoints == null) return;
        Gizmos.color = Color.green;
        for (int i = 0; i < patrolPoints.Length; i++)
        {
            if (patrolPoints[i] == null) continue;
            Gizmos.DrawSphere(patrolPoints[i].position, 0.2f);
            Transform next = patrolPoints[(i + 1) % patrolPoints.Length];
            if (next != null) Gizmos.DrawLine(patrolPoints[i].position, next.position);
        }
    }
}