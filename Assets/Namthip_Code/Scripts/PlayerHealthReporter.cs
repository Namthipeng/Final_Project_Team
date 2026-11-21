using UnityEngine;

[RequireComponent(typeof(Health))]
public class PlayerHealthReporter : MonoBehaviour
{
    private Health health;

    private void Awake() => health = GetComponent<Health>();
    private void OnEnable() => health.Changed += Report;
    private void OnDisable() => health.Changed -= Report;

    private void Start() => Report(health.Current, health.Max);

    private void Report(int current, int max)
    {
        Debug.Log($"HP: {current}/{max}");
        GameEvents.PlayerHealthChanged(current, max);
    }

    // ตอนนี้แค่ log ไว้ก่อน เดี๋ยวขั้นหลังๆ จะต่อกับ Game Over
    private void OnDestroy() { }
}
