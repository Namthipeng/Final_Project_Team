using UnityEngine;

[RequireComponent(typeof(Health), typeof(EnemyAnimation))]
public class EnemyHealthLink : MonoBehaviour
{
    private Health health;
    private EnemyAnimation anim;

    private void Awake()
    {
        health = GetComponent<Health>();
        anim = GetComponent<EnemyAnimation>();
    }

    private void OnEnable()
    {
        health.Changed += OnChanged;
        health.Died += anim.PlayDie;
    }

    private void OnDisable()
    {
        health.Changed -= OnChanged;
        health.Died -= anim.PlayDie;
    }

    private void OnChanged(int current, int max)
    {
        if (current > 0) anim.PlayHit();
    }
}