using UnityEngine;
using UnityEngine.Rendering.Universal;

public class BigRedBossAI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform animatorObj;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask enemyLayer; // Layer for detecting other enemies
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float attackPointOffset = 1.5f;
    private BigRedBossAttackPointTrigger attackPointTrigger;
    private BigRedBossAttackTelegraph attackTelegraph;

    private Rigidbody2D rb;
    private Animator anim;
    private Transform player;
    private BigRedBossHealth enemyHealth;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float stopDistance = 0.5f;
    [SerializeField] private float horizontalAlignmentThreshold = 0.2f;
    
    [Header("Detection")]
    [SerializeField] private float detectionRadius = 5f;
    [SerializeField] private float loseTargetRadius = 7f; // Slightly larger to prevent flickering
    
    private bool facingRight = true;
    private bool isGrounded;
    private bool isChasing = false;

    // For correct 2D lighting with normal maps, flip sprites using SpriteRenderer.flipX
    // instead of rotating the whole animator object (which can invert tangent space).
    private SpriteRenderer[] spriteRenderersToFlip;
    private bool[] baseFlipX;
    
    // Enemy collision avoidance
    private float lastCollisionTime = 0f;
    private float collisionCooldown = 1f; // Prevent rapid direction changes

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        enemyHealth = GetComponent<BigRedBossHealth>();

        // Prevent the collider from snagging on tilemap tile-edge seams.
        ApplyZeroFrictionMaterial();

        if (animatorObj != null)
        {
            anim = animatorObj.GetComponent<Animator>();

            // Cache all renderers we need to mirror.
            spriteRenderersToFlip = animatorObj.GetComponentsInChildren<SpriteRenderer>(true);
            baseFlipX = new bool[spriteRenderersToFlip.Length];
            for (int i = 0; i < spriteRenderersToFlip.Length; i++)
            {
                if (spriteRenderersToFlip[i] != null)
                    baseFlipX[i] = spriteRenderersToFlip[i].flipX;
            }
        }
        // Get the AttackPointTrigger from the attack point child
        if (attackPoint != null)
        {
            attackPointTrigger = attackPoint.GetComponent<BigRedBossAttackPointTrigger>();
            attackTelegraph = attackPoint.GetComponent<BigRedBossAttackTelegraph>();
        }
        // Look for the player right at the start
        FindTargetPlayer();

        // Apply initial facing visuals.
        ApplySpriteFacing();
    }

    private void ApplyZeroFrictionMaterial()
    {
        PhysicsMaterial2D frictionless = new PhysicsMaterial2D("BossNoFriction")
        {
            friction = 0f,
            bounciness = 0f
        };

        Collider2D[] cols = GetComponentsInChildren<Collider2D>(true);
        foreach (Collider2D col in cols)
        {
            if (!col.isTrigger)
                col.sharedMaterial = frictionless;
        }

        if (rb != null)
            rb.sharedMaterial = frictionless;
    }

    private void ApplySpriteFacing()
    {
        if (spriteRenderersToFlip == null || baseFlipX == null)
            return;

        for (int i = 0; i < spriteRenderersToFlip.Length; i++)
        {
            SpriteRenderer sr = spriteRenderersToFlip[i];
            if (sr == null)
                continue;

            bool original = i < baseFlipX.Length ? baseFlipX[i] : false;
            sr.flipX = facingRight ? original : !original;
        }
    }
    


    private void FindTargetPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        player = (playerObj != null) ? playerObj.transform : null;
    }

    private void ChasePlayer()
    {
        if (isAttacking)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

        // Ensure valid target first
        if (player == null || !player.gameObject.activeInHierarchy)
        {
            FindTargetPlayer();
            if (player == null || !player.gameObject.activeInHierarchy)
            {
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
                return;
            }
        }

        float horizontalDifference = Mathf.Abs(player.position.x - transform.position.x);

        if (player.position.y > transform.position.y && horizontalDifference < horizontalAlignmentThreshold)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

        float distance = Vector2.Distance(transform.position, player.position);
        if (distance <= stopDistance)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

        float dir = Mathf.Sign(player.position.x - transform.position.x);
        rb.linearVelocity = new Vector2(dir * moveSpeed, rb.linearVelocity.y);
    }





    private void Update()
    {
        // Handle stagger state (from successful player parry)
        if (isStaggered)
        {
            staggerTimer -= Time.deltaTime;
            float elapsed = staggerDuration - staggerTimer;

            if (elapsed < knockbackFreeTime)
            {
                // Free-slide phase: physics carries the enemy unimpeded — full launch distance
                // (no velocity change; Rigidbody2D gravity + friction handle it naturally)
            }
            else
            {
                // Braking phase: smoothly bleed off horizontal velocity — enemy skids to a stop
                float decayedX = Mathf.MoveTowards(rb.linearVelocity.x, 0f, knockbackDecayRate * Time.deltaTime);
                rb.linearVelocity = new Vector2(decayedX, rb.linearVelocity.y);
            }

            if (staggerTimer <= 0f)
                isStaggered = false;
            return;
        }

        CheckGrounded();
        CheckPlayerDetection();

        if (isChasing)
        {
            ChasePlayer();
        }
        else
        {
            StopMoving();
        }

        // Always face the player if they exist
        if (player != null && player.gameObject.activeInHierarchy)
        {
            float playerDir = player.position.x - transform.position.x;
            if (playerDir > 0 && !facingRight)
                Flip();
            else if (playerDir < 0 && facingRight)
                Flip();
        }

        UpdateAttackPointPosition();
        UpdateAnimations();
    }

    private void CheckPlayerDetection()
    {
        if (player == null || !player.gameObject.activeInHierarchy)
        {
            FindTargetPlayer();
            if (player == null || !player.gameObject.activeInHierarchy)
            {
                isChasing = false;
                return;
            }
        }

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (!isChasing && distanceToPlayer <= detectionRadius)
        {
            isChasing = true;
        }
        else if (isChasing && distanceToPlayer > loseTargetRadius)
        {
            isChasing = false;
        }
    }

    private void StopMoving()
    {
        if (!isAttacking)
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
    }
    


    private void HandleFlip()
    {
        if (rb.linearVelocity.x > 0 && !facingRight)
            Flip();
        else if (rb.linearVelocity.x < 0 && facingRight)
            Flip();
    }

    private void Flip()
    {
        facingRight = !facingRight;
        // Use flipX (not Y-rotation) so URP 2D normal-map lighting stays correct.
        ApplySpriteFacing();
        UpdateAttackPointPosition();
    }
    


    private void UpdateAttackPointPosition()
    {
        if (attackPoint == null)
            return;

        // Position the attack point in front of the enemy based on facing direction
        float offset = facingRight ? attackPointOffset : -attackPointOffset;
        Vector3 newPosition = transform.position + new Vector3(offset, 0, 0);
        attackPoint.position = newPosition;
    }

    private void CheckGrounded()
    {
        isGrounded = Physics2D.Raycast(
            transform.position,
            Vector2.down,
            0.2f,
            groundLayer
        );
    }

    private void UpdateAnimations()
    {
        if (anim != null)
        {
            anim.SetFloat("xVelocity", Mathf.Abs(rb.linearVelocity.x));
            anim.SetBool("isGrounded", isGrounded);
            anim.SetBool("isMoving", isChasing && Mathf.Abs(rb.linearVelocity.x) > 0.05f);
        }
    }

    [Header("Combat Settings")]
    [SerializeField] private float damageAmount = 10f;
    [SerializeField] private float attackCooldown = 1.0f;
    private float nextAttackTime;
    private float nextSkill1Time;
    private float nextSkill2Time;

    [Header("Skill Cooldowns")]
    [SerializeField] private float skill1Cooldown = 5f;
    [SerializeField] private float skill2Cooldown = 8f;
    private bool canMove = true;
    private bool isAttacking = false;

    [Header("Parry Settings")]
    [SerializeField] private float parryWindowDuration = 1.0f;
    [SerializeField] private float staggerDuration = 1.5f;
    [SerializeField] private float parryKnockbackForce = 18f;
    [SerializeField] private float knockbackFreeTime = 0.25f;   // seconds physics runs freely before braking
    [SerializeField] private float knockbackDecayRate = 6f;     // units/sec deceleration after free phase
    private bool isParryable = false;
    private bool isStaggered = false;
    private float staggerTimer = 0f;
    public bool IsParryable => isParryable;

    public void DamageTarget()
    {
        if (player == null)
            return;

        // Abort if the enemy was successfully parried
        if (isStaggered) return;
        isParryable = false;

        // Stop the pulsing indicator when the attack actually lands
        if (attackTelegraph != null)
            attackTelegraph.HideTelegraph();

        // Only damage if player is actually in the attack point zone
        if (attackPointTrigger != null && !attackPointTrigger.IsPlayerInZone())
            return;

        PlayerHealth health = player.GetComponentInParent<PlayerHealth>();
        if (health != null)
        {
            health.TakeDamage(damageAmount);

            PlayerMovement pm = player.GetComponentInParent<PlayerMovement>();
            if (pm != null)
            {
                Vector2 knockbackDir = (player.position - transform.position).normalized;
                knockbackDir.y += 0.5f;
                pm.ApplyKnockback(knockbackDir.normalized);
            }
        }
    }

    public void EnableMovementAndJump(bool enable)
    {
        canMove = enable;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Check if collided with another enemy and if enough time has passed since last collision
        if (Time.time - lastCollisionTime >= collisionCooldown)
        {
            EnemyAI otherEnemy = collision.gameObject.GetComponent<EnemyAI>();
            if (otherEnemy != null)
            {
                // No patrol or direction reversal needed
                lastCollisionTime = Time.time;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Check if player entered the attack point
        if (collision.GetComponentInParent<PlayerMovement>() != null && !isAttacking && Time.time >= nextAttackTime)
        {
            TriggerAttack();
        }
    }

    private int SelectRandomSkill()
    {
        // Equal probability: 33% each (0 = BasicAttack, 1 = Skill1, 2 = Skill2)
        return Random.Range(0, 3);
    }

    public void TriggerAttack()
    {
        // Only attack if player is in attack point zone
        if (!isAttacking && Time.time >= nextAttackTime && anim != null && attackPointTrigger != null && attackPointTrigger.IsPlayerInZone())
        {
            isAttacking = true;

            // Select random skill and set animator parameter
            int selectedSkill = SelectRandomSkill();

            // Skill cooldown logic
            if (selectedSkill == 1 && Time.time < nextSkill1Time)
            {
                isAttacking = false;
                return; // Skill1 on cooldown
            }
            if (selectedSkill == 2 && Time.time < nextSkill2Time)
            {
                isAttacking = false;
                return; // Skill2 on cooldown
            }

            anim.SetInteger("attackType", selectedSkill);

            // Show telegraph warning before attack
            if (attackTelegraph != null)
            {
                attackTelegraph.ShowTelegraph();
                attackTelegraph.SetParryWindowColor(true);
            }

            isParryable = true;
            Invoke(nameof(CloseParryWindow), parryWindowDuration);
            anim.SetBool("attack", true); // Set attack bool to true
            Invoke(nameof(ResetAttack), 1.3f);

            // Set cooldowns
            if (selectedSkill == 1)
                nextSkill1Time = Time.time + skill1Cooldown;
            else if (selectedSkill == 2)
                nextSkill2Time = Time.time + skill2Cooldown;
            nextAttackTime = Time.time + attackCooldown;
        }
    }

    private void ResetAttack()
    {
        isAttacking = false;
        isParryable = false;
        if (anim != null)
            anim.SetBool("attack", false); // Set attack bool to false
        if (attackTelegraph != null)
            attackTelegraph.HideTelegraph();
    }

    public void GetParried(Vector2 knockbackDir)
    {
        if (isStaggered) return;

        CancelInvoke(nameof(ResetAttack));
        CancelInvoke(nameof(CloseParryWindow));

        isAttacking = false;
        isParryable = false;
        isStaggered = true;
        staggerTimer = staggerDuration;
        nextAttackTime = Time.time + staggerDuration;

        // Apply knockback impulse — enemy launches away from the player
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(knockbackDir * parryKnockbackForce, ForceMode2D.Impulse);

        if (attackTelegraph != null)
            attackTelegraph.HideTelegraph();

        if (anim != null)
        {
            anim.SetBool("attack", false); // Ensure attack bool is reset
            anim.SetTrigger("stagger");
        }

        Debug.Log($"[EnemyAI] {gameObject.name} was parried! Knockback applied, staggering for {staggerDuration}s.");
    }

    private void CloseParryWindow()
    {
        isParryable = false;
        if (attackTelegraph != null)
            attackTelegraph.SetParryWindowColor(false);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        // Damage is only dealt through DamageTarget() when the attack animation is triggered
        // This method now does nothing to prevent constant damage
    }

    private void OnDrawGizmosSelected()
    {
        // Draw detection radius
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        
        // Draw lose target radius
        Gizmos.color = Color.orange;
        Gizmos.DrawWireSphere(transform.position, loseTargetRadius);
        
        // Draw current chase state
        if (isChasing)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.3f);
        }
        
        // Patrol area removed
    }
    // --- CUTSCENE SUPPORT ---
    /// <summary>
    /// Plays the Skill2 animation for the boss cutscene, disables normal AI actions temporarily.
    /// </summary>
    public void PlaySkill2CutsceneAnimation(float animDuration = 2.0f)
    {
        isAttacking = true;
        canMove = false;
        if (anim != null)
        {
            anim.SetInteger("attackType", 2); // Skill2
            anim.SetBool("attack", true);
        }
        if (attackTelegraph != null)
            attackTelegraph.ShowTelegraph();

        // Reset after animation
        Invoke(nameof(EndSkill2CutsceneAnimation), animDuration);
    }

    private void EndSkill2CutsceneAnimation()
    {
        isAttacking = false;
        canMove = true;
        if (anim != null)
            anim.SetBool("attack", false);
        if (attackTelegraph != null)
            attackTelegraph.HideTelegraph();
    }
}
