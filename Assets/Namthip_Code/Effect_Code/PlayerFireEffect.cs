using UnityEngine;

public class PlayerFireEffect : MonoBehaviour
{
    [SerializeField] private MuzzleFlash muzzleFlash;
    [SerializeField] private float shakePower = 0.08f;
    [SerializeField] private float shakeDuration = 0.1f;

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            muzzleFlash.Play();
            if (CameraShake.Instance != null)
                CameraShake.Instance.Shake(shakePower, shakeDuration);
        }
    }
}