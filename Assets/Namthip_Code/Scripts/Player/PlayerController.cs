using UnityEngine;

/// <summary>
/// ควบคุมตัวละคร: ยืนปกติ (Idle), เดิน, วิ่ง, เล็งปืน (Aim Idle), ยิงปืน (Shoot)
///
/// ปุ่มควบคุม
///   WASD / ลูกศร    = เดิน
///   Left Shift      = วิ่ง
///   เมาส์ขวา (ค้าง)  = เล็งปืน
///   เมาส์ซ้าย        = ยิง (ต้องเล็งอยู่)
///
/// หมายเหตุ: ชื่อไฟล์ต้องเป็น PlayerController.cs ให้ตรงกับชื่อคลาส
/// ตัวละครต้องมี Character Controller ที่ตั้งค่าเอง
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Speed")]
    public float walkSpeed = 2.5f;
    public float runSpeed = 5f;
    public float aimWalkSpeed = 1.5f;
    public float rotationSpeed = 12f;
    public float gravity = -20f;

    [Header("Shooting")]
    public Transform firePoint;          // จุดปลายกระบอกปืน (Empty GameObject)
    public GameObject bulletPrefab;      // กระสุน (ต้องมี Rigidbody) ไม่ใส่ก็ได้
    public float fireRate = 0.3f;        // ยิงได้ทุกกี่วินาที
    public float bulletSpeed = 30f;

    [Header("Ammo")]
    public int magazineSize = 12;       // กระสุนต่อแม็ก
    public int reserveAmmo = 48;        // กระสุนสำรอง
    public float reloadTime = 1.8f;     // ให้ใกล้เคียงความยาวคลิปรีโหลด
    public int currentAmmo;

    [Header("References")]
    public Animator animator;
    public Transform cameraTransform;    // ถ้าไม่ใส่ จะใช้ Camera.main

    private CharacterController controller;
    private float verticalVelocity;
    private float nextFireTime;
    private bool isAiming;
    public bool IsAiming => isAiming;
    private bool isReloading;
    public bool IsReloading => isReloading;
    private bool isDead;
    public bool IsDead => isDead;
    private int startReserveAmmo;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsAimingHash = Animator.StringToHash("IsAiming");
    private static readonly int ShootHash = Animator.StringToHash("Shoot");
    private static readonly int ReloadHash = Animator.StringToHash("Reload");


    void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        // โค้ดคุมการเคลื่อนที่เอง ไม่ใช้ Root Motion จากท่าทาง
        if (animator != null) animator.applyRootMotion = false;
        currentAmmo = magazineSize;
        startReserveAmmo = reserveAmmo;
    }

    void Update()
    {
        if (isDead) return;
        // หากล้องใหม่ทุกเฟรมจนกว่าจะเจอ (กัน error ตอนกล้องยังไม่พร้อม)
        if (cameraTransform == null)
        {
            if (Camera.main == null) return;
            cameraTransform = Camera.main.transform;
        }

        HandleAim();
        HandleMovement();
        HandleReload();
        HandleShoot();
    }

    // 1) เล็งปืน
    void HandleAim()
    {
        isAiming = Input.GetMouseButton(1);
        animator.SetBool(IsAimingHash, isAiming);
    }

    // 2) ยืนปกติ / เดิน / วิ่ง
    void HandleMovement()
    {


        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 input = new Vector3(h, 0f, v).normalized;

        // ทิศทางอิงตามกล้อง
        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 moveDir = camForward * input.z + camRight * input.x;

        // เลือกความเร็วตามสถานะ  
        float targetSpeed;
        if (isAiming) targetSpeed = aimWalkSpeed;
        else if (Input.GetKey(KeyCode.LeftShift)) targetSpeed = runSpeed;
        else targetSpeed = walkSpeed;

        bool hasInput = input.sqrMagnitude > 0.01f;
        float currentSpeed = hasInput ? targetSpeed : 0f;

        // หมุนตัวละคร
        if (isAiming)
        {
            // ตอนเล็ง: หันไปทางที่กล้องมอง
            if (camForward.sqrMagnitude > 0.001f)
            {
                Quaternion aimRot = Quaternion.LookRotation(camForward);
                transform.rotation = Quaternion.Slerp(transform.rotation, aimRot, rotationSpeed * Time.deltaTime);
            }
        }
        else if (hasInput)
        {
            // ปกติ: หันไปทางที่เดิน
            Quaternion targetRot = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }

        // แรงโน้มถ่วง
        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;
        verticalVelocity += gravity * Time.deltaTime;

        Vector3 velocity = moveDir * currentSpeed;
        velocity.y = verticalVelocity;
        controller.Move(velocity * Time.deltaTime);

        // ส่งความเร็วให้ Animator (damp ให้ท่าเปลี่ยนนุ่มๆ)
        animator.SetFloat(SpeedHash, currentSpeed, 0.1f, Time.deltaTime);

        // แปลงทิศเดินให้เป็นทิศ "ซ้าย/ขวา/หน้า/หลัง" เทียบกับตัวละคร
        // (moveDir เป็น normalized อยู่แล้ว ถ้าไม่กดปุ่มจะเป็น 0)
        Vector3 localDir = transform.InverseTransformDirection(moveDir);

    }

    // 3) ยิงปืน
    void HandleShoot()
    {
        if (!isAiming || isReloading) return;

        if (Input.GetMouseButton(0) && Time.time >= nextFireTime)
        {
            // กระสุนหมด -> รีโหลดอัตโนมัติ
            if (currentAmmo <= 0)
            {
                StartReload();
                return;
            }

            nextFireTime = Time.time + fireRate;
            currentAmmo--;
            animator.SetTrigger(ShootHash);
            SpawnBullet();
        }
    }

    // 4) รีโหลด
    void HandleReload()
    {
        if (Input.GetKeyDown(KeyCode.R))
            StartReload();
    }

    void StartReload()
    {
        if (isReloading) return;
        if (currentAmmo >= magazineSize) return;   // แม็กเต็มแล้ว
        if (reserveAmmo <= 0) return;              // ไม่มีกระสุนสำรอง

        StartCoroutine(ReloadRoutine());
    }

    System.Collections.IEnumerator ReloadRoutine()
    {
        isReloading = true;
        animator.SetTrigger(ReloadHash);

        yield return new WaitForSeconds(reloadTime);

        int load = Mathf.Min(magazineSize - currentAmmo, reserveAmmo);
        currentAmmo += load;
        reserveAmmo -= load;

        isReloading = false;
    }


    void SpawnBullet()
    {
        if (bulletPrefab == null || firePoint == null) return;

        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        if (rb != null)
            rb.linearVelocity = firePoint.forward * bulletSpeed; // Unity 6; ถ้า Unity 2022 ใช้ rb.velocity
        Destroy(bullet, 5f);
    }

    public void OnDeath()
    {
        isDead = true;
        isAiming = false;
        isReloading = false;
        StopAllCoroutines(); // ยกเลิกการรีโหลดที่ค้างอยู่

        animator.SetBool(IsAimingHash, false);
        animator.SetFloat(SpeedHash, 0f);
    }

    // เรียกจาก PlayerRespawn ตอนเกิดใหม่
    public void OnRespawn()
    {
        Debug.Log("OnRespawn called");
        isDead = false;
        verticalVelocity = 0f;
        nextFireTime = 0f;
        currentAmmo = magazineSize;
        reserveAmmo = startReserveAmmo;

        HealZone.ResetAll();
    }

    void OnGUI()
    {
        GUI.Label(new Rect(20, 20, 300, 30),
            isReloading ? "Reloading..." : $"Ammo: {currentAmmo} / {reserveAmmo}");
    }
}