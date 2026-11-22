using UnityEngine;

public class EnemyRespawnManager : MonoBehaviour
{
    private EnemyRespawn[] enemies;

    private void Start()
    {
        // หา Enemy ทั้งหมดในฉาก รวมตัวที่ถูกซ่อนอยู่ด้วย
        enemies = FindObjectsByType<EnemyRespawn>(FindObjectsInactive.Include);
        Debug.Log($"[Manager] เจอ Enemy ที่มี EnemyRespawn ทั้งหมด {enemies.Length} ตัว");
    }

    private void OnEnable() { PlayerRespawn.OnPlayerRespawned += RespawnAll; }
    private void OnDisable() { PlayerRespawn.OnPlayerRespawned -= RespawnAll; }

    private void RespawnAll()
    {
        Debug.Log("[Manager] ผู้เล่นเกิดใหม่ → รีเซ็ต Enemy ทั้งหมด");
        foreach (EnemyRespawn e in enemies)
        {
            if (e != null) e.ResetEnemy();
        }
    }
}