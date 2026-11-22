using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Checkpoint : MonoBehaviour
{
    [Tooltip("ตำแหน่งที่ผู้เล่นจะเกิด ถ้าไม่ใส่จะใช้ตำแหน่งของโซนนี้")]
    [SerializeField] private Transform spawnPoint;

    private bool activated = false;

    private void Reset()
    {
        // ตั้งเป็น Trigger อัตโนมัติตอนเพิ่มสคริปต์
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (activated) return;

        if (other.CompareTag("Player"))
        {
            PlayerRespawn respawn = other.GetComponent<PlayerRespawn>();
            if (respawn == null) return;

            Transform point = spawnPoint != null ? spawnPoint : transform;
            respawn.SetRespawnPoint(point.position, point.rotation);

            activated = true;
            Debug.Log($"Checkpoint activated: {name}");
        }
    }
}
