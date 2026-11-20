using UnityEngine;
using UnityEngine.AI;

public class EnemyAI3D : MonoBehaviour
{
    [Header("Patrol")]
    public Transform[] patrolPoints;
    public float walkSpeed = 2f;
    public float waitAtPoint = 1.5f;

    [Header("Detection")]
    public Transform player;
    public float viewDistance = 12f;
    [Range(0, 180)] public float viewAngle = 100f;   // มุมมองรวม (องศา)
    public LayerMask obstacleMask;                   // กำแพง/สิ่งกีดขวางที่บังสายตา
    public Transform eye;                            // จุดสายตา (หัวศัตรู) ไม่ใส่ก็ใช้ตัวเอง
    public float loseSightTime = 2f;                 // เสียเป้าแล้วจะยังจำได้อีกกี่วินาที

    [Header("Shooting")]
    public GameObject bulletPrefab;
    public Transform firePoint;                      // ปลายกระบอกปืน
    public float fireRate = 0.7f;
    public float turnSpeed = 8f;
    public float stopDistance = 6f;                  // เข้าใกล้ผู้เล่นแค่ไหนแล้วหยุดยิง

    NavMeshAgent agent;
    Animator anim;
    int currentPoint;
    float waitTimer;
    float nextFireTime;
    float lastSeenTime = -999f;

    static readonly int SpeedHash = Animator.StringToHash("Speed");
    static readonly int ShootHash = Animator.StringToHash("Shoot");

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        if (eye == null) eye = transform;
    }

    void Start()
    {
        agent.speed = walkSpeed;
        if (patrolPoints.Length > 0) agent.SetDestination(patrolPoints[0].position);
    }

    void Update()
    {
        if (CanSeePlayer()) lastSeenTime = Time.time;

        bool hasTarget = Time.time - lastSeenTime < loseSightTime;

        if (hasTarget) Combat();
        else Patrol();

        // ส่งความเร็วจริงให้ Animator ใช้ blend Idle/Walk
        anim.SetFloat(SpeedHash, agent.velocity.magnitude, 0.1f, Time.deltaTime);
    }

    // ---------- ลาดตระเวน ----------
    void Patrol()
    {
        if (patrolPoints.Length == 0) return;

        agent.isStopped = false;

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= waitAtPoint)
            {
                waitTimer = 0f;
                currentPoint = (currentPoint + 1) % patrolPoints.Length;
                agent.SetDestination(patrolPoints[currentPoint].position);
            }
        }
    }

    // ---------- เจอผู้เล่น ----------
    void Combat()
    {
        float dist = Vector3.Distance(transform.position, player.position);

        // ไกลเกินก็เดินเข้าไป ใกล้พอแล้วหยุดยิง
        if (dist > stopDistance)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
        }
        else
        {
            agent.isStopped = true;
        }

        FacePlayer();

        if (agent.isStopped && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + fireRate;
            anim.SetTrigger(ShootHash);
            Shoot(); // ถ้าใช้ Animation Event ให้ลบบรรทัดนี้ แล้วให้ event เรียก Shoot() แทน
        }
    }

    // เรียกจาก Animation Event ได้ (ชื่อฟังก์ชัน "Shoot")
    public void Shoot()
    {
        if (bulletPrefab == null || firePoint == null || player == null) return;

        // เล็งไปที่กลางลำตัวผู้เล่น
        Vector3 aimPoint = player.position + Vector3.up * 1.2f;
        Vector3 dir = (aimPoint - firePoint.position).normalized;
        Instantiate(bulletPrefab, firePoint.position, Quaternion.LookRotation(dir));
    }

    void FacePlayer()
    {
        Vector3 dir = player.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;
        Quaternion look = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, look, turnSpeed * Time.deltaTime);
    }

    // ---------- ตรวจจับ ----------
    bool CanSeePlayer()
    {
        if (player == null) return false;

        Vector3 target = player.position + Vector3.up * 1.2f;
        Vector3 toPlayer = target - eye.position;

        if (toPlayer.magnitude > viewDistance) return false;

        // อยู่ในมุมมองไหม
        Vector3 flat = new Vector3(toPlayer.x, 0, toPlayer.z);
        if (Vector3.Angle(transform.forward, flat) > viewAngle * 0.5f) return false;

        // มีอะไรบังไหม
        return !Physics.Raycast(eye.position, toPlayer.normalized, toPlayer.magnitude, obstacleMask);
    }

    // วาดรูปกรวยสายตาใน Scene view
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 pos = eye != null ? eye.position : transform.position;
        Vector3 left = Quaternion.Euler(0, -viewAngle * 0.5f, 0) * transform.forward * viewDistance;
        Vector3 right = Quaternion.Euler(0, viewAngle * 0.5f, 0) * transform.forward * viewDistance;
        Gizmos.DrawLine(pos, pos + left);
        Gizmos.DrawLine(pos, pos + right);
        Gizmos.DrawWireSphere(transform.position, viewDistance);
    }
}
