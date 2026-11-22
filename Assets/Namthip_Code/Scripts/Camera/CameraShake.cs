using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    private float power;
    private float duration;
    private float timer;
    private Vector3 appliedOffset;

    private void Awake() => Instance = this;

    public void Shake(float shakePower = 0.08f, float shakeDuration = 0.1f)
    {
        power = shakePower;
        duration = shakeDuration;
        timer = shakeDuration;
    }

    private void LateUpdate()
    {
        
        transform.position -= appliedOffset;
        appliedOffset = Vector3.zero;

        if (timer <= 0f) return;

        timer -= Time.unscaledDeltaTime;
        float fade = Mathf.Clamp01(timer / duration);
        Vector2 rnd = Random.insideUnitCircle * power * fade;

        
        appliedOffset = transform.right * rnd.x + transform.up * rnd.y;
        transform.position += appliedOffset;
    }
}
