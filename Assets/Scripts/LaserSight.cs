using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class LaserSight : MonoBehaviour
{
    [Header("References")]
    public PlayerController player;      // ลาก Player มาใส่ (ถ้าไม่ใส่จะหาจากตัวแม่)
    public Transform firePoint;          // ลาก FirePoint เดียวกับที่ใช้ยิง
    public Transform hitMarker;          // จุดแสงตรงที่เล็งโดน (ไม่บังคับ)

    [Header("Laser")]
    public float maxDistance = 30f;
    public float width = 0.03f;
    public Color color = new Color(1f, 0.1f, 0.1f, 1f);
    public LayerMask hitMask = ~0;       // เลเยอร์ที่เลเซอร์ชน
    public bool ignoreTriggers = true;

    private LineRenderer line;
    private readonly RaycastHit[] hits = new RaycastHit[16];

    void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.positionCount = 2;
        line.useWorldSpace = true;
        line.startWidth = width;
        line.endWidth = width;
        line.numCapVertices = 4;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;

        // ถ้ายังไม่ได้ใส่ Material จะสร้างแบบเรียบง่ายให้เอง
        if (line.sharedMaterial == null)
            line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = color;
        line.endColor = color;

        if (player == null) player = GetComponentInParent<PlayerController>();
        line.enabled = false;
        if (hitMarker != null) hitMarker.gameObject.SetActive(false);
    }

    void LateUpdate()
    {
        bool aiming = player != null && player.IsAiming && firePoint != null;
        line.enabled = aiming;
        if (hitMarker != null) hitMarker.gameObject.SetActive(aiming);
        if (!aiming) return;

        Vector3 start = firePoint.position;
        Vector3 dir = firePoint.forward;
        Vector3 end = start + dir * maxDistance;
        Vector3 normal = -dir;

        // หาจุดชนที่ใกล้ที่สุด โดยข้ามตัวผู้เล่นเอง
        int count = Physics.RaycastNonAlloc(start, dir, hits, maxDistance, hitMask,
            ignoreTriggers ? QueryTriggerInteraction.Ignore : QueryTriggerInteraction.Collide);

        float nearest = float.MaxValue;
        for (int i = 0; i < count; i++)
        {
            if (hits[i].transform.IsChildOf(player.transform)) continue;
            if (hits[i].distance < nearest)
            {
                nearest = hits[i].distance;
                end = hits[i].point;
                normal = hits[i].normal;
            }
        }

        line.SetPosition(0, start);
        line.SetPosition(1, end);

        if (hitMarker != null)
        {
            hitMarker.position = end + normal * 0.02f;
            hitMarker.rotation = Quaternion.LookRotation(normal);
        }
    }
}
