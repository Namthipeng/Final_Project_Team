using UnityEngine;

public class TopDownCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;
    public Vector3 targetOffset = new Vector3(0, 1f, 0);

    [Header("Angle & Distance")]
    [Range(30f, 85f)] public float pitch = 60f;     // ห้ามถึง 90
    public float yaw = 0f;
    public float distance = 12f;
    public float minDistance = 6f;
    public float maxDistance = 20f;
    public float zoomSpeed = 2f;
    public float zoomSmooth = 8f;

    [Header("Follow")]
    public float smoothTime = 0.12f;

    [Header("Aim Rotate (คลิกขวาค้าง)")]
    public float mouseSensitivity = 3f;
    public bool lockCursorWhileAiming = true;       // ล็อกเมาส์ให้หมุนได้ไม่จำกัด

    [Header("Rotate (Q / E) ไม่บังคับ")]
    public bool allowKeyRotate = true;
    public float keyRotateSpeed = 90f;

    [Header("Bounds (ไม่บังคับ)")]
    public bool useBounds = false;
    public Vector2 minBounds = new Vector2(-20, -20);
    public Vector2 maxBounds = new Vector2(20, 20);

    private Vector3 velocity;
    private float targetDistance;

    void Start()
    {
        targetDistance = distance;
        if (target != null)
        {
            Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
            transform.rotation = rot;
            transform.position = Focus() - rot * Vector3.forward * distance;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;
        float dt = Time.deltaTime;

        // ซูมด้วยล้อเมาส์
        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.01f)
            targetDistance = Mathf.Clamp(targetDistance - scroll * zoomSpeed, minDistance, maxDistance);
        distance = Mathf.Lerp(distance, targetDistance, zoomSmooth * dt);

        // คลิกขวาค้าง = หมุนกล้องตามเมาส์ (ตัวละครหันตามกล้อง)
        if (Input.GetMouseButtonDown(1) && lockCursorWhileAiming)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        if (Input.GetMouseButton(1))
        {
            yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
        }
        if (Input.GetMouseButtonUp(1) && lockCursorWhileAiming)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // หมุนด้วย Q / E
        if (allowKeyRotate)
        {
            if (Input.GetKey(KeyCode.Q)) yaw -= keyRotateSpeed * dt;
            if (Input.GetKey(KeyCode.E)) yaw += keyRotateSpeed * dt;
        }

        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);

        Vector3 focus = Focus();
        if (useBounds)
        {
            focus.x = Mathf.Clamp(focus.x, minBounds.x, maxBounds.x);
            focus.z = Mathf.Clamp(focus.z, minBounds.y, maxBounds.y);
        }

        Vector3 desired = focus - rot * Vector3.forward * distance;
        transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
        transform.rotation = rot;
    }

    Vector3 Focus() => target.position + targetOffset;
}