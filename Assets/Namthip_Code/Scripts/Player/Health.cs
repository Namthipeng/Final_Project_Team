using System;
using UnityEngine;

public class Health : MonoBehaviour, IDamageable
{
    [SerializeField] private int maxHealth = 100;

    public int Current { get; private set; }
    public int Max => maxHealth;
    public bool IsDead => Current <= 0;

    public event Action<int, int> Changed; // (ปัจจุบัน, สูงสุด)
    public event Action Died;

    private void Awake()
    {
        Current = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        if (IsDead) return;

        Current = Mathf.Max(0, Current - amount);
        Changed?.Invoke(Current, maxHealth);

        if (IsDead)
        {
            Died?.Invoke();
        }
    }
    public void ResetHealth()
    {
        Current = maxHealth;
        Changed?.Invoke(Current, maxHealth); // ให้ UI อัปเดตตามด้วย
    }

    // ไว้เทส: คลิกขวาที่ชื่อคอมโพเนนต์ใน Inspector ตอน Play
    [ContextMenu("Test Damage 10")]
    private void TestDamage() => TakeDamage(10);
}