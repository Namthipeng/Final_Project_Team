using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Gun : MonoBehaviour
{
    [Header("การยิง")]
    [SerializeField] private Camera cam;               // ว่างได้ จะใช้ Main Camera เอง
    [SerializeField] private int damage = 50;          // ดาเมจต่อนัด
    [SerializeField] private float range = 100f;       // ระยะยิง
    [SerializeField] private float fireRate = 0.2f;    // เว้นกี่วินาทีต่อนัด
    [SerializeField] private LayerMask hitMask = ~0;   // ชนอะไรได้บ้าง

    [Header("เป้าเล็ง")]
    [SerializeField] private Texture2D crosshairTexture;   // ว่างได้ จะวาดเครื่องหมาย + ให้เอง
    [SerializeField] private float crosshairSize = 32f;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color enemyColor = Color.red;

    [Header("แสงวาบตอนยิง (ไม่ใส่ก็ได้)")]
    [SerializeField] private Light muzzleLight;
    [SerializeField] private float flashTime = 0.05f;

    private float nextFireTime;
    private bool aimingAtTarget;   // เมาส์อยู่บนศัตรูหรือไม่
    private bool isAiming;

    private void Start()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null)
            Debug.LogError("Gun: ไม่เจอกล้อง ให้ตั้ง Tag ของกล้องเป็น MainCamera", this);

        if (muzzleLight != null) muzzleLight.enabled = false;

        Cursor.visible = true;   // เริ่มต้นเป็นเมาส์ปกติ
    }

    private void OnDisable()
    {
        Cursor.visible = true;   // คืนเมาส์ปกติเมื่อปิดปืน
    }

    private void Update()
    {
        if (cam == null || Mouse.current == null) return;

        // กดคลิกขวาค้าง = โหมดเล็ง
        isAiming = Mouse.current.rightButton.isPressed;
        Cursor.visible = !isAiming;   // เล็งอยู่ซ่อนเมาส์ ปล่อยแล้วเมาส์กลับมา
        Cursor.lockState = CursorLockMode.None;

        if (!isAiming)
        {
            aimingAtTarget = false;
            return;   // ไม่ได้เล็ง ยิงไม่ได้
        }

        // เช็กว่าเมาส์อยู่บนศัตรูไหม (ไว้เปลี่ยนสีเป้า)
        aimingAtTarget = false;
        if (TryGetMouseHit(out RaycastHit hover))
            aimingAtTarget = hover.collider.GetComponentInParent<Health>() != null;

        // คลิกซ้ายค้าง ลากไปยิง
        if (Mouse.current.leftButton.isPressed && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + fireRate;
            Shoot();
        }
    }

    // ยิงเส้นจากกล้องผ่านตำแหน่งเมาส์
    private bool TryGetMouseHit(out RaycastHit hit)
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = cam.ScreenPointToRay(mousePos);
        return Physics.Raycast(ray, out hit, range, hitMask, QueryTriggerInteraction.Ignore);
    }

    private void Shoot()
    {
        if (muzzleLight != null) StartCoroutine(Flash());

        if (!TryGetMouseHit(out RaycastHit hit))
        {
            Debug.Log("ยิงไม่โดนอะไร");
            return;
        }

        Debug.Log($"ยิงโดน: {hit.collider.name}");

        Health target = hit.collider.GetComponentInParent<Health>();

        if (target == null) return;   // โดนพื้น/กำแพง ไม่ต้องทำอะไร

        target.TakeDamage(damage);
        Debug.Log($"[NPC] {target.name} เหลือเลือด {target.Current}/{target.Max}");
    }

    private IEnumerator Flash()
    {
        muzzleLight.enabled = true;
        yield return new WaitForSeconds(flashTime);
        muzzleLight.enabled = false;
    }

    // วาดเป้าเล็งตามเมาส์
    private void OnGUI()
    {
        if (!isAiming || Mouse.current == null) return;

        Vector2 m = Mouse.current.position.ReadValue();
        float x = m.x;
        float y = Screen.height - m.y;   // GUI นับ y จากด้านบน ต้องกลับด้าน

        GUI.color = aimingAtTarget ? enemyColor : normalColor;

        if (crosshairTexture != null)
        {
            float h = crosshairSize * 0.5f;
            GUI.DrawTexture(new Rect(x - h, y - h, crosshairSize, crosshairSize), crosshairTexture);
        }
        else
        {
            // ไม่มีรูป วาดเครื่องหมาย + เอง
            float len = crosshairSize * 0.5f;
            float gap = 4f;
            float t = 2f;
            Texture2D w = Texture2D.whiteTexture;
            GUI.DrawTexture(new Rect(x - len, y - t / 2, len - gap, t), w);   // ซ้าย
            GUI.DrawTexture(new Rect(x + gap, y - t / 2, len - gap, t), w);   // ขวา
            GUI.DrawTexture(new Rect(x - t / 2, y - len, t, len - gap), w);   // บน
            GUI.DrawTexture(new Rect(x - t / 2, y + gap, t, len - gap), w);   // ล่าง
        }

        GUI.color = Color.white;
    }
}
