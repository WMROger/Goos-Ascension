using UnityEngine;

public class ChargedShot : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float baseSpeed = 15f;
    [SerializeField] private float chargedSpeedMultiplier = 1f;

    [Header("Damage")]
    [SerializeField] private float baseDamage = 20f;
    [SerializeField] private float maxChargeDamage = 100f; // Cap maximum damage to 100

    [Header("Effects")]
    [SerializeField] private float knockbackForce = 500f;

    private float direction = 1f;
    private float chargeLevel = 0f;
    private float currentDamage;
    private float currentSpeed;

    private Animator animator;
    private bool animationFinished;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        // The charged shot must never physically block the player who fired it.
        // We disable physics collisions with every player collider at spawn time.
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Collider2D[] myColliders = GetComponents<Collider2D>();
            foreach (Collider2D playerCol in player.GetComponentsInChildren<Collider2D>(true))
                foreach (Collider2D myCol in myColliders)
                    Physics2D.IgnoreCollision(myCol, playerCol, true);
        }
    }

    public void SetDirection(float dir)
    {
        direction = Mathf.Sign(dir);
        if (direction < 0)
            transform.localScale = new Vector3(-1, 1, 1);
    }

    public void SetCharge(float charge01)
    {
        chargeLevel = Mathf.Clamp01(charge01);
        currentDamage = Mathf.Lerp(baseDamage, maxChargeDamage, chargeLevel);
        currentDamage = Mathf.Min(currentDamage, 100f); // Ensure cap
        currentSpeed = baseSpeed * (1f + (chargedSpeedMultiplier - 1f) * chargeLevel);
        Debug.Log($"ChargedShot created with charge: {chargeLevel:F2}, damage: {currentDamage}, speed: {currentSpeed}");
    }

    private void Update()
    {
        if (!animationFinished && animator != null)
        {
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            if (state.normalizedTime >= 1f)
            {
                animationFinished = true;
                Destroy(gameObject);
            }
        }
    }

    // Returns true for any collider that belongs to the player hierarchy.
    // PlayerHealth is on the player root, so GetComponentInParent works from any child collider.
    private bool IsPlayerCollider(Collider2D col)
    {
        return col.GetComponentInParent<PlayerHealth>() != null;
    }

    private void ApplyDamageAndKnockback(Collider2D col, IDamageable target)
    {
        float clampedDamage = Mathf.Min(currentDamage, 100f);
        target.TakeDamage(clampedDamage);
        Debug.Log($"ChargedShot hit dealt {clampedDamage} damage to {col.name}");

        bool isBoss = col.GetComponentInParent<MechBossAI>() != null;
        if (!isBoss)
        {
            Rigidbody2D enemyRb = col.GetComponentInParent<Rigidbody2D>();
            if (enemyRb != null)
            {
                Vector2 knockbackDir = new Vector2(direction, 0.2f).normalized;
                enemyRb.AddForce(knockbackDir * knockbackForce * (1f + chargeLevel));
            }
        }
    }

    // --- Trigger path (mobs/boss with trigger colliders) ---

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsPlayerCollider(other)) return;

        IDamageable target = other.GetComponentInParent<IDamageable>();
        if (target != null)
        {
            ApplyDamageAndKnockback(other, target);
            return;
        }

        if (other.CompareTag("Ground"))
        {
            CreateImpactEffect();
            Destroy(gameObject);
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (IsPlayerCollider(other)) return;

        IDamageable target = other.GetComponentInParent<IDamageable>();
        if (target != null)
        {
            float tickDamage = Mathf.Min(currentDamage, 100f) * Time.deltaTime;
            target.TakeDamage(tickDamage);
        }
    }

    // --- Collision path (boss/enemies with non-trigger colliders) ---
    // When the ChargedShot's own non-trigger collider meets a solid enemy collider,
    // deal damage once and then disable the blocking so the enemy is no longer stopped.

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (IsPlayerCollider(collision.collider)) return;

        IDamageable target = collision.collider.GetComponentInParent<IDamageable>();
        if (target != null)
        {
            ApplyDamageAndKnockback(collision.collider, target);

            // Stop physically blocking this collider after the first hit.
            Collider2D[] myColliders = GetComponents<Collider2D>();
            foreach (Collider2D myCol in myColliders)
                Physics2D.IgnoreCollision(myCol, collision.collider, true);
            return;
        }

        if (collision.gameObject.CompareTag("Ground"))
        {
            CreateImpactEffect();
            Destroy(gameObject);
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (IsPlayerCollider(collision.collider)) return;

        IDamageable target = collision.collider.GetComponentInParent<IDamageable>();
        if (target != null)
        {
            float tickDamage = Mathf.Min(currentDamage, 100f) * Time.deltaTime;
            target.TakeDamage(tickDamage);
        }
    }

    private void CreateImpactEffect()
    {
        Debug.Log("ChargedShot impact effect!");
    }
}
