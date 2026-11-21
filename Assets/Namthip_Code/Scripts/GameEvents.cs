using System;

public static class GameEvents
{
    public static event Action<int, int> OnPlayerHealthChanged;

    // เก็บค่าล่าสุดไว้ เผื่อ UI โหลดทีหลังแล้วพลาด event แรก
    public static int LastHealth { get; private set; }
    public static int LastMaxHealth { get; private set; }

    public static void PlayerHealthChanged(int current, int max)
    {
        LastHealth = current;
        LastMaxHealth = max;
        OnPlayerHealthChanged?.Invoke(current, max);
    }
}
