using UnityEngine;

public class BossLaserProjectile : MonoBehaviour
{
    [SerializeField] private float lifetime = 5f;

    // How much player energy a successful block consumes. Matched to PlayerCombat.blockEnergyCost.
    [SerializeField] private float blockEnergyCost = 20f;

    private float damage;
    private Vector2 travelDir;
    private Rigidbody2D rb;

    private void Awake()
    {
        // Ensure all colliders are triggers — laser should never physically block the player.
        foreach (Collider2D col in GetComponentsInChildren<Collider2D>(true))
            col.isTrigger = true;

        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody2D>();

        rb.bodyType        = RigidbodyType2D.Kinematic;
        rb.gravityScale    = 0f;
        rb.interpolation   = RigidbodyInterpolation2D.Interpolate; // smooth sub-frame movement
    }

    /// <summary>Called by MechBossAI immediately after instantiation.</summary>
    public void Initialize(Vector2 dir, float speed, float dmg)
    {
        damage    = dmg;
        travelDir = dir.normalized;

        // Rotate sprite to face travel direction
        float angle = Mathf.Atan2(travelDir.y, travelDir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        // Drive movement through Rigidbody velocity — reliable trigger detection
        // and smooth interpolated rendering every frame
        rb.linearVelocity = travelDir * speed;

        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Ignore the firing boss and its children
        if (other.GetComponentInParent<MechBossAI>() != null) return;
        // Ignore sibling lasers
        if (other.GetComponent<BossLaserProjectile>() != null) return;

        PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
        if (health != null)
        {
            PlayerCombat combat = other.GetComponentInParent<PlayerCombat>();

            if (combat != null && combat.IsBlocking)
            {
                // Player is holding block — attempt to absorb with energy/stamina.
                PlayerEnergy energy = other.GetComponentInParent<PlayerEnergy>();

                if (energy != null && energy.currentEnergy > 0f)
                {
                    // Stamina absorbs the hit — drain energy, no health damage, no knockback.
                    energy.SpendEnergy(blockEnergyCost);
                    Debug.Log($"[Block] Ranged attack blocked! Energy cost: {blockEnergyCost}. Remaining: {energy.currentEnergy:F1}");
                }
                else
                {
                    // Stamina depleted — shield breaks, full damage goes through to health.
                    health.TakeDamage(damage);
                    Debug.Log("[Block] Shield broken — no energy remaining, full damage applied to health.");
                }
            }
            else
            {
                // Unblocked hit — full damage and knockback.
                health.TakeDamage(damage);

                PlayerMovement pm = other.GetComponentInParent<PlayerMovement>();
                if (pm != null)
                    pm.ApplyKnockback(travelDir);
            }

            Destroy(gameObject);
            return;
        }

        if (other.CompareTag("Ground") || other.gameObject.layer == LayerMask.NameToLayer("Ground"))
            Destroy(gameObject);
    }
}
