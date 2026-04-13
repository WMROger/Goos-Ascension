using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class SwordArcDamage : MonoBehaviour
{
    [Header("Damage Settings")]
    [SerializeField] private float baseDamage = 20f;
    [SerializeField] private float knockbackForce = 8f;

    [Header("Movement Settings")]
    [SerializeField] private float travelSpeed = 8f;
    [SerializeField] private float travelDistance = 25f;

    [Header("Pierce Settings")]
    [SerializeField] private int pierceCount = 100;

    private float currentDamage;
    private readonly HashSet<Collider2D> hitTargets = new HashSet<Collider2D>();
    private Vector3 startPosition;
    private Vector3 direction;
    private int enemiesHit;
    private int groundLayer = -1;
    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        groundLayer = LayerMask.NameToLayer("Ground");

        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody2D>();

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.useFullKinematicContacts = true;
    }

    public void SetCharge(float charge01)
    {
        currentDamage = baseDamage;
        Debug.Log($"Sword arc charge set to {charge01:F2}, damage: {currentDamage}");
    }

    private void Start()
    {
        if (currentDamage == 0f)
            currentDamage = baseDamage;

        startPosition = transform.position;
        direction = transform.localScale.x >= 0f ? Vector3.right : Vector3.left;

        ApplyVelocity();
    }

    private void OnEnable()
    {
        hitTargets.Clear();
        enemiesHit = 0;
    }

    private void Update()
    {
        if (Vector3.Distance(startPosition, transform.position) >= travelDistance)
            Destroy(gameObject);
    }

    private void ApplyVelocity()
    {
        if (rb == null)
            return;

        rb.linearVelocity = (Vector2)direction * travelSpeed;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null)
            return;

        if (IsEnvironmentBlocker(other.gameObject))
        {
            Destroy(gameObject);
            return;
        }

        if (hitTargets.Contains(other))
            return;

        IDamageable target = other.GetComponentInParent<IDamageable>();
        if (target == null)
            return;

        hitTargets.Add(other);
        enemiesHit++;

        target.TakeDamage(currentDamage);

        Rigidbody2D enemyRb = other.GetComponentInParent<Rigidbody2D>();
        if (enemyRb != null)
        {
            Vector2 knockbackDirection = (other.transform.position - transform.position).normalized;
            enemyRb.AddForce(knockbackDirection * knockbackForce, ForceMode2D.Impulse);
        }

        if (enemiesHit >= pierceCount)
            Destroy(gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision == null || collision.collider == null)
            return;

        if (IsEnvironmentBlocker(collision.gameObject))
            Destroy(gameObject);
    }

    private bool IsEnvironmentBlocker(GameObject obj)
    {
        if (obj == null)
            return false;

        if (obj.CompareTag("Ground"))
            return true;

        return groundLayer != -1 && obj.layer == groundLayer;
    }
}
