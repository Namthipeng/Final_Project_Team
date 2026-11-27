using UnityEngine;

/// <summary>
/// กล้องตามผู้เล่น: เลื่อนนุ่มๆ + เขยิบไปทางเมาส์ตอนเล็ง + หมุนมุมด้วย Q/E
/// แปะบน Main Camera (ห้ามเป็นลูกของ Player)
/// </summary>
public class TopDownCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;                 // ลาก Player มาใส่ (ว่างไว้ = หา PlayerController เอง)
    public float lookHeight = 1.2f;          // จุดที่กล้องโฟกัสบนตัวผู้เล่น

    [Header("Position")]
    public float distance = 8f;              // ระยะห่าง
    public float height = 7f;                // ความสูง
    public float yaw = 0f;                   // มุมหมุนรอบตัวผู้เล่น (องศา)
    public float followSpeed = 8f;

    [Header("Mouse Lead")]
    public bool followMouse = true;
    public float mouseLeadAmount = 0.25f;    // 0 = ไม่เขยิบ, ยิ่งมากยิ่งเขยิบไปทางเมาส์
    public float maxLead = 3f;               // ระยะเขยิบสูงสุด (เมตร)
    public bool onlyWhenAiming = true;       // เขยิบเฉพาะตอนกดคลิกขวา

    [Header("Rotate (Q / E)")]
    public bool allowRotate = true;
    public float rotateSpeed = 90f;          // องศา/วินาที

    private PlayerController player;
    private Camera cam;
    private Vector3 lead;

    private void Start()
    {
        cam = GetComponent<Camera>();
        if (target == null)
        {
            player = FindAnyObjectByType<PlayerController>();
            if (player != null) target = player.transform;
        }
        else player = target.GetComponent<PlayerController>();
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // หมุนมุมกล้องด้วย Q / E
        if (allowRotate)
        {
            if (Input.GetKey(KeyCode.Q)) yaw -= rotateSpeed * Time.deltaTime;
            if (Input.GetKey(KeyCode.E)) yaw += rotateSpeed * Time.deltaTime;
        }

        // เขยิบไปทางเมาส์
        Vector3 desiredLead = Vector3.zero;
        bool canLead = followMouse && (!onlyWhenAiming || (player != null && player.IsAiming));
        if (canLead && cam != null)
        {
            Vector3 focus = target.position + Vector3.up * lookHeight;
            Plane plane = new Plane(Vector3.up, focus);
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (plane.Raycast(ray, out float d))
            {
                Vector3 offset = ray.GetPoint(d) - focus;
                offset.y = 0f;
                desiredLead = Vector3.ClampMagnitude(offset * mouseLeadAmount, maxLead);
            }
        }
        lead = Vector3.Lerp(lead, desiredLead, 6f * Time.deltaTime);

        // คำนวณตำแหน่งกล้อง
        Vector3 focusPoint = target.position + Vector3.up * lookHeight + lead;
        Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
        Vector3 desiredPos = focusPoint + rot * new Vector3(0f, height, -distance);

        transform.position = Vector3.Lerp(transform.position, desiredPos, followSpeed * Time.deltaTime);
        transform.rotation = Quaternion.LookRotation(focusPoint - transform.position);
    }
}