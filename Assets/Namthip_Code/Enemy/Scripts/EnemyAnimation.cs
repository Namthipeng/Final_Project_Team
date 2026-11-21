using UnityEngine;

[RequireComponent(typeof(Animator))]
public class EnemyAnimation : MonoBehaviour
{
    [Header("เคลื่อนที่")]
    [SerializeField] private float speedDamp = 0.1f;

    [Header("ตาย")]
    [SerializeField] private bool disableCollidersOnDeath = true;
    [SerializeField] private float destroyAfterDeath = 0f; // 0 = ไม่ลบศพ

    private Animator animator;
    private Vector3 lastPosition;
    private bool isDead;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int ShootHash = Animator.StringToHash("Shoot");
    private static readonly int DieHash = Animator.StringToHash("Die");
    private static readonly int HitHash = Animator.StringToHash("Hit");

    private void Awake()
    {
        animator = GetComponent<Animator>();
        lastPosition = transform.position;
    }

    private void Update()
    {
        if (isDead) return;

        float speed = (transform.position - lastPosition).magnitude / Time.deltaTime;
        lastPosition = transform.position;
        animator.SetFloat(SpeedHash, speed, speedDamp, Time.deltaTime);
    }

    public void PlayShoot()
    {
        if (!isDead) animator.SetTrigger(ShootHash);
    }

    public void PlayHit()
    {
        if (!isDead) animator.SetTrigger(HitHash);
    }

    public void PlayDie()
    {
        if (isDead) return;
        isDead = true;

        animator.SetTrigger(DieHash);

        if (disableCollidersOnDeath)
            foreach (var col in GetComponentsInChildren<Collider>())
                col.enabled = false;

        if (destroyAfterDeath > 0f)
            Destroy(gameObject, destroyAfterDeath);
    }

    // เรียกจาก Animation Event ในคลิป Firing Rifle
    public void OnShootFrame()
    {
        // ใส่โค้ดยิงกระสุนตรงนี้ในขั้นต่อไป
    }
}