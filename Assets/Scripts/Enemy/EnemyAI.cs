using UnityEngine;
using UnityEngine.Rendering.Universal;

public class EnemyAI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform animatorObj;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask enemyLayer; // Layer for detecting other enemies
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float attackPointOffset = 1.5f;
    private AttackPointTrigger attackPointTrigger;
    private AttackTelegraph attackTelegraph;

    private Rigidbody2D rb;
    private Animator anim;
    private Transform player;
    private EnemyHealth enemyHealth;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float stopDistance = 0.5f;
    [SerializeField] private float horizontalAlignmentThreshold = 0.2f;
    
    [Header("Detection")]
    [SerializeField] private float detectionRadius = 5f;
    [SerializeField] private float loseTargetRadius = 7f; // Slightly larger to prevent flickering
    
    [Header("Patrol")]
    [SerializeField] private bool enablePatrol = true;
    [SerializeField] private float patrolDistance = 4f;
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float waitTimeAtEnd = 1f;

    private bool facingRight = true;
    private bool isGrounded;
    private bool isChasing = false;
    private bool isPatrolling = true;
    
    // Patrol state
    private Vector2 currentPatrolCenter;
    private Vector2 leftPatrolPoint;
    private Vector2 rightPatrolPoint;
    private bool movingRight = true;
    private bool waitingAtPatrolEnd = false;
    private float patrolWaitTimer;

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
        enemyHealth = GetComponent<EnemyHealth>();
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
            attackPointTrigger = attackPoint.GetComponent<AttackPointTrigger>();
            attackTelegraph = attackPoint.GetComponent<AttackTelegraph>();
        }
        // Look for the player right at the start
        FindTargetPlayer();

        // Apply initial facing visuals.
        ApplySpriteFacing();
        
        // Initialize patrol points around starting position
        SetupPatrolArea(transform.position);
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
    
    private void SetupPatrolArea(Vector2 centerPosition)
    {
        currentPatrolCenter = centerPosition;
        leftPatrolPoint = new Vector2(centerPosition.x - patrolDistance, centerPosition.y);
        rightPatrolPoint = new Vector2(centerPosition.x + patrolDistance, centerPosition.y);
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
            isPatrolling = false;
            ChasePlayer();
        }
        else if (enablePatrol)
        {
            isPatrolling = true;
            Patrol();
        }
        else
        {
            StopMoving();
        }
            
        UpdateAttackPointPosition();
        HandleFlip();
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
            
            // Set up new patrol area from current position where target was lost
            SetupPatrolArea(transform.position);
            waitingAtPatrolEnd = false; // Start patrolling immediately
        }
    }

    private void StopMoving()
    {
        if (!isAttacking)
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
    }
    
    private void Patrol()
    {
        if (isAttacking)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

        // Handle waiting at patrol ends
        if (waitingAtPatrolEnd)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            patrolWaitTimer -= Time.deltaTime;
            
            if (patrolWaitTimer <= 0f)
            {
                waitingAtPatrolEnd = false;
                movingRight = !movingRight; // Switch direction
            }
            return;
        }

        Vector2 targetPoint = movingRight ? rightPatrolPoint : leftPatrolPoint;
        float distanceToTarget = Mathf.Abs(transform.position.x - targetPoint.x);

        // Check if reached patrol point
        if (distanceToTarget <= 0.1f)
        {
            waitingAtPatrolEnd = true;
            patrolWaitTimer = waitTimeAtEnd;
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

        // Move toward target patrol point
        float direction = movingRight ? 1f : -1f;
        rb.linearVelocity = new Vector2(direction * patrolSpeed, rb.linearVelocity.y);
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
    
    private void ReverseDirection()
    {
        // Reverse movement direction for patrol
        if (isPatrolling)
        {
            movingRight = !movingRight;
        }
        
        // Stop current movement momentarily to prevent getting stuck
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        
        // Force flip to face the new direction immediately
        Flip();
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
        }
    }

    [Header("Combat Settings")]
    [SerializeField] private float damageAmount = 10f;
    [SerializeField] private float attackCooldown = 1.0f;
    private float nextAttackTime;
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
        if (health != null && Time.time >= nextAttackTime)
        {
            health.TakeDamage(damageAmount);

            PlayerMovement pm = player.GetComponentInParent<PlayerMovement>();
            if (pm != null)
            {
                Vector2 knockbackDir = (player.position - transform.position).normalized;
                knockbackDir.y += 0.5f;
                pm.ApplyKnockback(knockbackDir.normalized);
            }

            nextAttackTime = Time.time + attackCooldown;
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
                // Reverse direction when colliding with another enemy
                ReverseDirection();
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

    public void TriggerAttack()
    {
        if (!isAttacking && Time.time >= nextAttackTime && anim != null)
        {
            isAttacking = true;
            
            // Show telegraph warning before attack
            if (attackTelegraph != null)
            {
                attackTelegraph.ShowTelegraph();
                attackTelegraph.SetParryWindowColor(true);
            }
            
            isParryable = true;
            Invoke(nameof(CloseParryWindow), parryWindowDuration);
            anim.SetTrigger("attack");
            Invoke(nameof(ResetAttack), 2.3f);
        }
    }

    private void ResetAttack()
    {
        isAttacking = false;
        isParryable = false;
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
            anim.SetTrigger("stagger");

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
        
        // Draw patrol area
        if (enablePatrol)
        {
            // Use current patrol center in play mode, or transform position in editor
            Vector2 center = Application.isPlaying ? currentPatrolCenter : (Vector2)transform.position;
            Vector2 leftPoint = new Vector2(center.x - patrolDistance, center.y);
            Vector2 rightPoint = new Vector2(center.x + patrolDistance, center.y);
            
            // Draw patrol line
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(leftPoint, rightPoint);
            
            // Draw patrol endpoints
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(leftPoint, 0.2f);
            Gizmos.DrawWireSphere(rightPoint, 0.2f);
            
            // Show patrol center
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(center, 0.15f);
            
            // Show current patrol state
            if (Application.isPlaying && isPatrolling)
            {
                Gizmos.color = movingRight ? Color.green : Color.magenta;
                Gizmos.DrawWireSphere(transform.position, 0.1f);
            }
        }
    }
}
