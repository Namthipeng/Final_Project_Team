using System.Collections;
using UnityEngine;

public class MuzzleFlash : MonoBehaviour
{
    [SerializeField] private GameObject flashObject;  // ลากโมเดลแฟลชใส่
    [SerializeField] private float showTime = 0.05f;  // แสดงกี่วินาที
    [SerializeField] private Light flashLight;        // ไม่บังคับ
    [SerializeField] private AudioSource audioSource; // ไม่บังคับ
    [SerializeField] private AudioClip shotSound;

    private Coroutine routine;

    private Vector3 baseScale;

    private void Awake()
    {
        if (flashObject != null)
        {
            baseScale = flashObject.transform.localScale;  // จำขนาดที่ตั้งไว้
            flashObject.SetActive(false);
        }
        if (flashLight != null) flashLight.enabled = false;
    }

    public void Play()
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(Flash());

        if (audioSource != null && shotSound != null)
            audioSource.PlayOneShot(shotSound);
    }

    private IEnumerator Flash()
    {
        if (flashObject != null)
        {
            flashObject.transform.localScale = baseScale * Random.Range(0.8f, 1.2f);
            flashObject.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            flashObject.SetActive(true);
        }
        if (flashLight != null) flashLight.enabled = true;

        yield return new WaitForSeconds(showTime);

        if (flashObject != null) flashObject.SetActive(false);
        if (flashLight != null) flashLight.enabled = false;
    }

    [ContextMenu("Test Play")] private void TestPlay() => Play();
}
