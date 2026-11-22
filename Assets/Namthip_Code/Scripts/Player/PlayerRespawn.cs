using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerRespawn : MonoBehaviour
{
    // ระบบอื่น (เช่น Enemy) สมัครรับ event นี้ได้
    public static event Action OnPlayerDied;
    public static event Action OnPlayerRespawned;

    [Header("Respawn")]
    [SerializeField] private Transform defaultSpawnPoint;
    [SerializeField] private float respawnDelay = 3f; // ให้ยาวกว่าคลิปตายนิดหน่อย

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [Tooltip("ชื่อ Trigger ท่าตาย ใส่หลายชื่อได้ ระบบจะสุ่มเล่น")]
    [SerializeField] private string[] deathTriggers = { "Die" };
    [Tooltip("ชื่อ State ท่ายืนปกติใน Animator (ต้องสะกดตรงเป๊ะ)")]
    [SerializeField] private string Locomotion = "Locomotion";

    public bool IsDead { get; private set; }

    private Vector3 spawnPosition;
    private Quaternion spawnRotation;

    private CharacterController controller;
    private PlayerController playerController;
    private Health health;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerController = GetComponent<PlayerController>();
        health = GetComponent<Health>();

        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    private void OnEnable()
{
    if (health != null) health.Died += Die;
}

private void OnDisable()
{
    if (health != null) health.Died -= Die;
}

    private void Start()
    {
        if (defaultSpawnPoint != null)
            SetRespawnPoint(defaultSpawnPoint.position, defaultSpawnPoint.rotation);
        else
            SetRespawnPoint(transform.position, transform.rotation);
    }

    // Checkpoint เรียกฟังก์ชันนี้เพื่อเปลี่ยนจุดเกิด
    public void SetRespawnPoint(Vector3 position, Quaternion rotation)
    {
        spawnPosition = position;
        spawnRotation = rotation;
    }

    public void Die()
    {
        if (IsDead) return;
        IsDead = true;

        // 1) ล็อกการควบคุมใน PlayerController
        if (playerController != null) playerController.OnDeath();

        // 2) เล่นแอนิเมชันตาย (สุ่มจากรายการ)
        if (animator != null && deathTriggers.Length > 0)
        {
            string trigger = deathTriggers[UnityEngine.Random.Range(0, deathTriggers.Length)];
            animator.SetTrigger(trigger);
        }

        OnPlayerDied?.Invoke();
        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnDelay);
        Respawn();
    }

    private void Respawn()
    {
        // ปิด CharacterController ก่อนย้าย ไม่งั้น Unity จะเขียนตำแหน่งเดิมทับ
        controller.enabled = false;
        transform.SetPositionAndRotation(spawnPosition, spawnRotation);
        controller.enabled = true;

        // รีเซ็ต Animator กลับท่ายืน
        if (animator != null)
        {
            animator.Rebind();   // กลับ Default State (Locomotion สีส้ม) และเคลียร์ Trigger ทั้งหมด
            animator.Update(0f); // ให้ท่าเปลี่ยนทันทีในเฟรมนี้
        }

        if (health != null) health.ResetHealth();
        if (playerController != null) playerController.OnRespawn();

        IsDead = false;

        // ประกาศ: ผู้เล่นเกิดใหม่แล้ว Enemy ไปเกิดใหม่ตามได้เลย
        OnPlayerRespawned?.Invoke();
    }
}
