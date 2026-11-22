using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class HealZone : MonoBehaviour
{
    // รายชื่อวงทั้งหมดในเกม ไว้สั่งรีเซ็ตพร้อมกัน
    private static readonly List<HealZone> All = new List<HealZone>();

    [SerializeField] private int healAmount = 50;
    [SerializeField] private bool healToFull = false;
    [SerializeField] private bool onlyWhenHurt = true;
    [SerializeField] private float spinSpeed = 60f;
    [SerializeField] private AudioClip healSound;
    [SerializeField] private GameObject healEffect;
    [SerializeField] private float effectLifeTime = 1.5f;

    private bool used;

    private void Reset() => GetComponent<Collider>().isTrigger = true;

    private void Awake() => GetComponent<Collider>().isTrigger = true;

    private void OnEnable() { if (!All.Contains(this)) All.Add(this); }
    private void OnDestroy() { All.Remove(this); }

    private void Update()
    {
        transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (used) return;

        Health hp = other.GetComponentInParent<Health>();
        if (hp == null || hp.IsDead) return;
        if (other.GetComponentInParent<PlayerController>() == null) return;
        if (onlyWhenHurt && hp.Current >= hp.Max) return;

        used = true;
        hp.Heal(healToFull ? hp.Max : healAmount);

        if (healSound != null) Play2D(healSound);
        if (healEffect != null)
        {
            GameObject fx = Instantiate(healEffect, transform.position, Quaternion.identity);
            Destroy(fx, effectLifeTime);
        }

        gameObject.SetActive(false);   // ซ่อนแทนการลบ เพื่อให้กลับมาได้
    }

    private void ResetZone()
    {
        used = false;
        gameObject.SetActive(true);
    }

    // เรียกตอนผู้เล่นเกิดใหม่ เพื่อให้ทุกวงกลับมา
    public static void ResetAll()
    {
        // ใช้สำเนาของรายการ เพราะวงที่ซ่อนอยู่จะไม่อยู่ใน All ถ้าเรา Remove ตอน OnDisable
        foreach (HealZone z in AllIncludingHidden)
            if (z != null) z.ResetZone();
    }

    // เก็บวงทั้งหมดรวมที่ซ่อนอยู่ (ลงทะเบียนตอน Awake ไม่ลบตอนปิด)
    private static readonly List<HealZone> AllIncludingHidden = new List<HealZone>();

    private void Start()
    {
        if (!AllIncludingHidden.Contains(this)) AllIncludingHidden.Add(this);
    }

    private static void Play2D(AudioClip clip, float volume = 1f)
    {
        GameObject go = new GameObject("HealSound");
        AudioSource src = go.AddComponent<AudioSource>();
        src.clip = clip;
        src.volume = volume;
        src.spatialBlend = 0f;
        src.Play();
        Destroy(go, clip.length + 0.1f);
    }
}
