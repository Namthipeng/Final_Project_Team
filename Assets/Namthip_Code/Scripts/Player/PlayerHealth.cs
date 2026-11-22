using UnityEngine;

[RequireComponent(typeof(PlayerRespawn))]
public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;

    public int CurrentHealth { get; private set; }

    private PlayerRespawn respawn;

    private void Awake()
    {
        respawn = GetComponent<PlayerRespawn>();
        CurrentHealth = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        // ตายอยู่แล้วไม่รับดาเมจเพิ่ม กัน Die() ถูกเรียกซ้ำ
        if (respawn.IsDead) return;

        CurrentHealth -= amount;

        if (CurrentHealth <= 0)
        {
            CurrentHealth = 0;
            respawn.Die();
        }
    }

    public void ResetHealth()
    {
        CurrentHealth = maxHealth;
    }
}