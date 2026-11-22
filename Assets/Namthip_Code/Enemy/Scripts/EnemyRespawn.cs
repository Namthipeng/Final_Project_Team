using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

public class EnemyRespawn : MonoBehaviour
{
    [Tooltip("ไม่บังคับ: ผูกฟังก์ชันเพิ่มเติมตอนเกิดใหม่ (HP และ AI รีเซ็ตให้อัตโนมัติแล้ว)")]
    public UnityEvent onRespawn;

    private Vector3 startPosition;
    private Quaternion startRotation;

    private NavMeshAgent agent;
    private Rigidbody rb;

    private void Awake()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;

        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
    }

    public void ResetEnemy()
    {
        Debug.Log($"[EnemyRespawn] รีเซ็ต {name}");

        // เปิดตัวก่อน เผื่อ Enemy ถูกซ่อนไว้ (SetActive(false))
        gameObject.SetActive(true);

        // เปิดคอลลิเดอร์ที่ EnemyAI ปิดตอนตาย
        foreach (Collider col in GetComponentsInChildren<Collider>(true))
            col.enabled = true;

        // ย้ายตำแหน่ง แล้วเปิด Agent และ Warp ให้ติดกับ NavMesh
        transform.SetPositionAndRotation(startPosition, startRotation);
        if (agent != null)
        {
            agent.enabled = true;
            agent.Warp(startPosition);
        }

        if (rb != null && !rb.isKinematic)
        {
            rb.linearVelocity = Vector3.zero; // Unity 2022 ใช้ rb.velocity
            rb.angularVelocity = Vector3.zero;
        }

        // รีเซ็ต State, HP, แอนิเมชัน และกลับไปเดินลาดตระเวน
        EnemyAI ai = GetComponent<EnemyAI>();
        if (ai != null) ai.ResetAI();
        else Debug.LogWarning($"[EnemyRespawn] {name} ไม่มี EnemyAI");

        onRespawn.Invoke();
    }
}
