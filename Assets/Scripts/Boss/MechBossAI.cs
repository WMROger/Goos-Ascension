
using System.Collections;
using UnityEngine;


public class MechBossAI : MonoBehaviour
{
    // ...existing fields...

    // ...existing fields...

    /// <summary>
    /// Called by MechBossHealth after taking damage to randomly trigger a defensive state.
    /// </summary>
    public void TryTriggerDefensiveStates()
    {
        // If any defensive state is already active, do not trigger another
        if (isImmune || isArmorBuffed || isBlocking)
            return;

        float roll = Random.value;
        if (roll < immuneChance)
        {
            SetImmune(true, Random.Range(immuneMinDuration, immuneMaxDuration));
            return;
        }
        roll -= immuneChance;
        if (roll < armorBuffChance)
        {
            SetArmorBuff(true, Random.Range(armorBuffMinDuration, armorBuffMaxDuration));
            return;
        }
        roll -= armorBuffChance;
        if (roll < blockChance)
        {
            SetBlock(true, Random.Range(blockMinDuration, blockMaxDuration));
            return;
        }
        // No state triggered
    }
    [Header("References")]
    [SerializeField] private Transform animatorObj;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Transform meleeAttackPoint;
    [SerializeField] private float meleeAttackPointOffset = 1.8f;
    [SerializeField] private Transform laserFirePoint;
    private Vector3 laserFirePointLocalPos;
    [SerializeField] private GameObject laserProjectilePrefab;

    private Rigidbody2D rb;
    private Animator anim;
    private Transform player;
    private MechBossHealth bossHealth;

    [Header("Movement")]

    [Header("Detection")]

    [Header("Water Avoidance")]

    [Header("Melee Attack")]

    [Header("Laser Attack")]

    [Header("Phase 2 Settings")]

    [Header("Boss Glow Settings")]

    // State

    // Parry / Stagger State


    // Defensive Mechanics
    [Header("Defensive Mechanics")]
    [SerializeField] private float immuneMinDuration = 1f;
    [SerializeField] private float immuneMaxDuration = 2f;
    [SerializeField] private float armorBuffMinDuration = 2f;
    [SerializeField] private float armorBuffMaxDuration = 3f;
    [SerializeField] private float blockMinDuration = 1f;
    [SerializeField] private float blockMaxDuration = 2f;
    [SerializeField, Range(0f, 1f)] private float immuneChance = 0.33f;
    [SerializeField, Range(0f, 1f)] private float armorBuffChance = 0.33f;
    [SerializeField, Range(0f, 1f)] private float blockChance = 0.33f;
    private bool isImmune = false;
    private bool isArmorBuffed = false;
    private bool isBlocking = false;
    private float immuneTimer = 0f;
    private float armorBuffTimer = 0f;
    private float blockTimer = 0f;
    public bool IsImmune => isImmune;
    public bool IsArmorBuffed => isArmorBuffed;
    public bool IsBlocking => isBlocking;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private float phase2MoveSpeed = 3.8f;
    [SerializeField] private float stopDistance = 1.8f;

    [Header("Detection")]
    [SerializeField] private float detectionRadius = 12f;
    [SerializeField] private float loseTargetRadius = 18f;

    [Header("Water Avoidance")]
    [SerializeField] private LayerMask waterLayer;
    [SerializeField] private float waterCheckDistance = 1.2f;  // How far ahead to scan
    [SerializeField] private float waterCheckHeightOffset = 0f; // Adjust if boss origin isn't at feet

    [Header("Melee Attack")]
    [SerializeField] private float meleeDamage = 20f;
    [SerializeField] private float meleeAttackRange = 2.5f;
    [SerializeField] private float meleeAttackCooldown = 2f;
    [SerializeField] private float meleeAttackRadius = 2f;
    [SerializeField] private float meleeHitDelay = 0.12f;        // Fallback hit frame if animation event is missing
    [SerializeField] private AttackTelegraph meleeTelegraph;         // Assign via Inspector (AttackTelegraph on MeleeAttackPoint)
    [SerializeField] private float meleeTelegraphDuration = 0.8f;   // Parry window — gold pulse before swing

    [Header("Laser Attack")]
    [SerializeField] private float laserDamage = 15f;
    [SerializeField] private float laserAttackRange = 10f;
    [SerializeField] private float laserAttackCooldown = 3f;
    [SerializeField] private float laserProjectileSpeed = 9f;

    [Header("Phase 2 Settings")]
    [SerializeField] private float phase2AttackSpeedMultiplier = 1.4f;

    [Header("Boss Glow Settings")]
    [SerializeField] private float glowDuration = 5f;           // total immunity window
    [SerializeField] private float glowWindupDuration = 0.8f;   // freeze while charge-up anim plays
    [SerializeField] private float glowCooldown = 18f;          // seconds between periodic glow cycles
    [SerializeField] private Color glowTintColor = new Color(1f, 0.85f, 0.3f, 1f); // golden tint
    private float nextGlowTime = 0f;
    private bool isGlowing = false;
    private SpriteRenderer[] bossSpriteRenderers;
    private Color[] bossOriginalColors;

    // State
    private bool isChasing = false;
    private bool isAttacking = false;
    private bool facingRight = true;
    private bool isGrounded = false;
    private bool isPhase2 = false;
    private bool isBlockedByWater = false;

    // Parry / Stagger State
    [Header("Parry Settings")]
    [SerializeField] private float bossStaggerDuration = 1.0f;
    [SerializeField] private float parryKnockbackForce = 12f;
    [SerializeField] private float knockbackFreeTime = 0.2f;    // seconds physics runs freely before braking
    [SerializeField] private float knockbackDecayRate = 5f;     // units/sec deceleration after free phase
    private bool isParryable = false;
    private bool isStaggered = false;
    private float staggerTimer = 0f;
    private Coroutine currentAttackCoroutine;
    public bool IsParryable => isParryable;

    // Cached player components (refreshed when player reference changes)
    private PlayerMovement playerMovement;

    private float nextMeleeAttackTime = 0f;
    private float nextLaserAttackTime = 0f;
    private float currentMoveSpeed;
    private bool meleeDamageResolvedThisAttack = false;

    // Animator parameter hashes (must match your Animator Controller parameter names)
    private static readonly int AnimXVelocity   = Animator.StringToHash("xVelocity");
    private static readonly int AnimIsGrounded   = Animator.StringToHash("isGrounded");
    private static readonly int AnimMeleeAttack  = Animator.StringToHash("meleeAttack");
    private static readonly int AnimLaserAttack  = Animator.StringToHash("laserAttack");
    private static readonly int AnimPhase2       = Animator.StringToHash("phase2");
    private static readonly int AnimBossGlow     = Animator.StringToHash("bossGlow");

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        bossHealth = GetComponent<MechBossHealth>();
        currentMoveSpeed = moveSpeed;

        if (animatorObj != null)
            anim = animatorObj.GetComponent<Animator>();

        // Cache all sprite renderers for glow tinting
        bossSpriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        bossOriginalColors  = new Color[bossSpriteRenderers.Length];
        for (int i = 0; i < bossSpriteRenderers.Length; i++)
            bossOriginalColors[i] = bossSpriteRenderers[i].color;

        if (laserFirePoint != null)
            laserFirePointLocalPos = laserFirePoint.localPosition;

        ResolveMeleeTelegraphReference();

        FindTargetPlayer();
    }

    private void Start()
    {
        // Ensure the boss visual (animator child) is active when the scene starts.
        // Prevents a hidden state caused by mis-assigned worldHealthBarCanvas in MechBossHealth.
        if (animatorObj != null)
            animatorObj.gameObject.SetActive(true);

        // Boss appears and attacks immediately — no entry delay
        isChasing = true;

        // Start hidden so the telegraph only appears during attack windups.
        if (meleeTelegraph != null)
            meleeTelegraph.HideTelegraph();
    }

    private void ResolveMeleeTelegraphReference()
    {
        if (meleeTelegraph != null)
            return;

        if (meleeAttackPoint == null)
        {
            Debug.LogWarning("[MechBossAI] meleeAttackPoint is not assigned, cannot resolve melee telegraph.");
            return;
        }

        meleeTelegraph = meleeAttackPoint.GetComponent<AttackTelegraph>();
        if (meleeTelegraph == null)
            meleeTelegraph = meleeAttackPoint.GetComponentInChildren<AttackTelegraph>(true);

        if (meleeTelegraph == null)
        {
            // Match Sentinel Minion behavior by ensuring the telegraph component exists on attack point.
            meleeTelegraph = meleeAttackPoint.gameObject.AddComponent<AttackTelegraph>();
            Debug.Log("[MechBossAI] Added missing AttackTelegraph to meleeAttackPoint at runtime.");
        }
    }

    private void FindTargetPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerMovement = playerObj.GetComponent<PlayerMovement>();
        }
        else
        {
            player = null;
            playerMovement = null;
        }
    }

    // Returns the best position to target — uses the active hitbox so both forms are tracked correctly
    private Vector3 GetPlayerTargetPosition()
    {
        if (player == null) return Vector3.zero;

        // If we have PlayerMovement, target whichever form's hitbox is currently active
        if (playerMovement != null)
        {
            // Walk children and return the first active child collider's position
            foreach (Transform child in player)
            {
                if (child.gameObject.activeInHierarchy)
                {
                    Collider2D col = child.GetComponent<Collider2D>();
                    if (col != null) return col.bounds.center;
                }
            }
        }
        return player.position;
    }

    // Returns true when the player is currently in Slime form
    private bool IsPlayerSlime()
    {
        return playerMovement != null && !playerMovement.IsHuman;
    }

    // Reliably finds PlayerHealth regardless of where it sits in the player hierarchy
    private PlayerHealth FindPlayerHealth()
    {
        if (player == null) return null;
        PlayerHealth health = player.GetComponent<PlayerHealth>();
        if (health == null) health = player.GetComponentInParent<PlayerHealth>();
        if (health == null) health = player.GetComponentInChildren<PlayerHealth>();
        return health;
    }

    // Setters for defensive states
    public void SetImmune(bool value, float duration = 0f)
    {
        isImmune = value;
        if (isImmune)
        {
            immuneTimer = duration > 0f ? duration : immuneMaxDuration;
        }
        else
        {
            immuneTimer = 0f;
        }
        if (anim != null)
            anim.SetBool("immune", isImmune);
    }

    public void SetArmorBuff(bool value, float duration = 0f)
    {
        isArmorBuffed = value;
        if (isArmorBuffed)
        {
            armorBuffTimer = duration > 0f ? duration : armorBuffMaxDuration;
        }
        else
        {
            armorBuffTimer = 0f;
        }
        if (anim != null)
            anim.SetBool("armorBuff", isArmorBuffed);
    }

    public void SetBlock(bool value, float duration = 0f)
    {
        isBlocking = value;
        if (isBlocking)
        {
            blockTimer = duration > 0f ? duration : blockMaxDuration;
        }
        else
        {
            blockTimer = 0f;
        }
        if (anim != null)
            anim.SetBool("block", isBlocking);
    }

    private void Update()
    {
        // Handle stagger from parry
        if (isStaggered)
        {
            staggerTimer -= Time.deltaTime;
            float elapsed = bossStaggerDuration - staggerTimer;

            if (elapsed < knockbackFreeTime)
            {
                // Free-slide phase: boss launches back under full physics — sells its mass
            }
            else
            {
                // Braking phase: heavy, gradual deceleration — boss grinds to a stop
                float decayedX = Mathf.MoveTowards(rb.linearVelocity.x, 0f, knockbackDecayRate * Time.deltaTime);
                rb.linearVelocity = new Vector2(decayedX, rb.linearVelocity.y);
            }

            if (staggerTimer <= 0f)
            {
                isStaggered = false;
                isAttacking = false;
            }
            UpdateAnimations();
            return;
        }

        // Defensive state timers
        if (isImmune)
        {
            immuneTimer -= Time.deltaTime;
            if (immuneTimer <= 0f)
            {
                SetImmune(false);
            }
        }
        if (isArmorBuffed)
        {
            armorBuffTimer -= Time.deltaTime;
            if (armorBuffTimer <= 0f)
            {
                SetArmorBuff(false);
            }
        }
        if (isBlocking)
        {
            blockTimer -= Time.deltaTime;
            if (blockTimer <= 0f)
            {
                SetBlock(false);
            }
        }


        CheckGrounded();
        CheckPlayerDetection();

        if (isChasing && player != null)
            DecideAction();
        else
            StopMoving();

        UpdateAttackPointPosition();
        HandleFlip();
        UpdateAnimations();
    }

    private void DecideAction()
    {
        if (isAttacking) return;

        Vector3 targetPos = GetPlayerTargetPosition();
        float dist = Vector2.Distance(transform.position, targetPos);

        // If player is in slime form and in water and boss is blocked — use ranged attack if possible
        bool playerInWater = IsPlayerSlime() && isBlockedByWater;

        // Phase 2 Glow check — highest priority in Phase 2 (but doesn't stop for attacks; fires inline)
        if (isPhase2 && !isGlowing && Time.time >= nextGlowTime)
        {
            currentAttackCoroutine = StartCoroutine(DoGlowingPhase());
            return;
        }

        // Priority: Melee > Laser > Chase
        if (Time.time >= nextMeleeAttackTime && dist <= meleeAttackRange && !playerInWater)
            currentAttackCoroutine = StartCoroutine(DoMeleeAttack());
        else if (Time.time >= nextLaserAttackTime && dist <= laserAttackRange)
            currentAttackCoroutine = StartCoroutine(DoLaserAttack());
        else if (!isBlockedByWater)
            ChasePlayer();
        else
            StopMoving(); // Boss waits at water's edge
    }

    private void ChasePlayer()
    {
        if (player == null || isAttacking) return;

        Vector3 targetPos = GetPlayerTargetPosition();
        float dist = Vector2.Distance(transform.position, targetPos);
        if (dist <= stopDistance)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

        float dir = Mathf.Sign(targetPos.x - transform.position.x);

        // Stop at water's edge — boss cannot enter water
        if (IsWaterAhead(dir))
        {
            isBlockedByWater = true;
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

        isBlockedByWater = false;
        rb.linearVelocity = new Vector2(dir * currentMoveSpeed, rb.linearVelocity.y);
    }

    // Checks for water directly ahead (horizontal) and for water pits at floor level ahead
    private bool IsWaterAhead(float direction)
    {
        if (waterLayer == 0) return false; // Layer not assigned, skip check

        Vector2 bodyOrigin = (Vector2)transform.position + Vector2.up * waterCheckHeightOffset;

        // Horizontal body-level scan
        if (Physics2D.Raycast(bodyOrigin, new Vector2(direction, 0f), waterCheckDistance, waterLayer))
            return true;

        // Floor-level scan: check the ground one step ahead for a water surface
        Vector2 aheadFloor = new Vector2(transform.position.x + direction * waterCheckDistance,
                                          transform.position.y - 0.3f);
        if (Physics2D.OverlapPoint(aheadFloor, waterLayer) != null)
            return true;

        return false;
    }

    // Called when the boss accidentally lands in water (failsafe)
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Water"))
        {
            // Boss is heavy — immediately push it back up and out
            rb.linearVelocity = new Vector2(-rb.linearVelocity.x, 5f);
            isBlockedByWater = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Water"))
            isBlockedByWater = false;
    }

    private IEnumerator DoMeleeAttack()
    {
        isAttacking = true;
        meleeDamageResolvedThisAttack = false;
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        ResolveMeleeTelegraphReference();

        // Face the player before the telegraph so the indicator appears on the correct side.
        if (player != null)
        {
            float dir = GetPlayerTargetPosition().x - transform.position.x;
            if (dir > 0.1f && !facingRight) Flip();
            else if (dir < -0.1f && facingRight) Flip();
        }

        // ── Telegraph phase ─────────────────────────────────────────────────────────
        // Open the parry window and show the gold pulsing indicator at the attack point.
        isParryable = true;
        if (meleeTelegraph != null)
        {
            meleeTelegraph.SetParryWindowColor(true);
            meleeTelegraph.ShowTelegraph();
        }

        yield return new WaitForSeconds(meleeTelegraphDuration);

        // Close the parry window and hide the indicator.
        isParryable = false;
        if (meleeTelegraph != null)
        {
            meleeTelegraph.SetParryWindowColor(false);
            meleeTelegraph.HideTelegraph();
        }

        // If GetParried() was called during the telegraph window it already stopped this
        // coroutine via StopCoroutine, so this guard is a belt-and-suspenders safety check.
        if (isStaggered)
        {
            isAttacking = false;
            currentAttackCoroutine = null;
            yield break;
        }
        // ────────────────────────────────────────────────────────────────────────────

        // Trigger the melee animation.
        // If the animation event is missing/mis-timed, apply a fallback hit from code.
        if (anim != null) anim.SetTrigger(AnimMeleeAttack);
        yield return new WaitForSeconds(meleeHitDelay);
        DamageMelee();

        float cooldown = meleeAttackCooldown / (isPhase2 ? phase2AttackSpeedMultiplier : 1f);
        nextMeleeAttackTime = Time.time + cooldown;

        yield return new WaitForSeconds(cooldown);
        isAttacking = false;
        currentAttackCoroutine = null;
    }

    private IEnumerator DoLaserAttack()
    {
        isAttacking = true;
        isParryable = true;
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

        if (anim != null) anim.SetTrigger(AnimLaserAttack);

        float cooldown = laserAttackCooldown / (isPhase2 ? phase2AttackSpeedMultiplier : 1f);
        nextLaserAttackTime = Time.time + cooldown;

        // Lock-on phase: boss tracks and faces the player while charging up
        float windupTime = 0.5f;
        float elapsed    = 0f;
        while (elapsed < windupTime)
        {
            if (player != null)
            {
                float dir = GetPlayerTargetPosition().x - transform.position.x;
                if (dir > 0.1f && !facingRight) Flip();
                else if (dir < -0.1f && facingRight) Flip();
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        isParryable = false;

        if (isPhase2)
        {
            // Phase 2 burst — center shot, then two angled follow-ups
            FireLaser();
            yield return new WaitForSeconds(0.12f);
            FireLaserWithAngleOffset(10f);
            yield return new WaitForSeconds(0.12f);
            FireLaserWithAngleOffset(-10f);
        }
        else
        {
            FireLaser();
        }

        // Brief recovery so the attack has weight before the boss resumes chasing
        yield return new WaitForSeconds(0.4f);
        isAttacking = false;
        currentAttackCoroutine = null;
    }

    private IEnumerator DoGlowingPhase()
    {
        isGlowing = true;
        isAttacking = true; // Freeze movement during windup
        rb.linearVelocity = Vector2.zero;

        // Trigger the charge-up animation
        if (anim != null) anim.SetTrigger(AnimBossGlow);

        // Apply golden tint immediately so the visual sells the buildup
        ApplyGlowTint();

        // Tell health component the boss is now immune
        if (bossHealth != null) bossHealth.SetImmune(true);

        // Windup freeze — plays charge-up animation
        yield return new WaitForSeconds(glowWindupDuration);

        // Boss is now free to move and attack while immune for the remaining duration
        isAttacking = false;
        float immunityRemaining = glowDuration - glowWindupDuration;
        yield return new WaitForSeconds(immunityRemaining);

        // Glow ends — restore tint and lift immunity
        ClearGlowTint();
        if (bossHealth != null) bossHealth.SetImmune(false);

        isGlowing = false;
        nextGlowTime = Time.time + glowCooldown;
        currentAttackCoroutine = null;

        Debug.Log("[MechBossAI] BossGlowing phase ended. Boss is now vulnerable.");
    }

    /// <summary>
    /// Called externally (e.g. from MechBossHealth at 25% HP) to force an immediate glow cycle.
    /// Bypasses the cooldown timer.
    /// </summary>
    public void TriggerGlow()
    {
        if (isGlowing || !isPhase2) return;
        nextGlowTime = 0f; // Let DecideAction pick it up on the next frame
    }

    private void ApplyGlowTint()
    {
        if (bossSpriteRenderers == null) return;
        foreach (SpriteRenderer sr in bossSpriteRenderers)
            if (sr != null) sr.color = glowTintColor;
    }

    private void ClearGlowTint()
    {
        if (bossSpriteRenderers == null || bossOriginalColors == null) return;
        for (int i = 0; i < bossSpriteRenderers.Length; i++)
            if (bossSpriteRenderers[i] != null && i < bossOriginalColors.Length)
                bossSpriteRenderers[i].color = bossOriginalColors[i];
    }

    // Called by coroutine (and optionally by MechBossAnimationEvents)
    public void DamageMelee()
    {
        // Allow only one melee hit resolution per swing.
        if (meleeDamageResolvedThisAttack)
            return;
        meleeDamageResolvedThisAttack = true;

        if (player == null) return;

        Vector3 hitOrigin = meleeAttackPoint != null ? meleeAttackPoint.position : transform.position;
        float dist = Vector2.Distance(hitOrigin, GetPlayerTargetPosition());
        if (dist > meleeAttackRadius) return;

        PlayerHealth health = FindPlayerHealth();
        if (health != null)
        {
            health.TakeDamage(meleeDamage);

            PlayerMovement pm = health.GetComponent<PlayerMovement>();
            if (pm != null)
            {
                Vector2 knockbackDir = (player.position - transform.position).normalized;
                knockbackDir.y += 0.4f;
                pm.ApplyKnockback(knockbackDir.normalized);
            }
        }
    }

    // Called by coroutine (and optionally by MechBossAnimationEvents)
    public void FireLaser()
    {
        if (laserFirePoint == null || laserProjectilePrefab == null || player == null) return;

        Vector2 direction = ((Vector2)GetPlayerTargetPosition() - (Vector2)laserFirePoint.position).normalized;
        SpawnLaser(direction);
    }

    // Fires a laser rotated by 'degrees' relative to the direct aim direction.
    // Used for Phase 2 spread bursts.
    private void FireLaserWithAngleOffset(float degrees)
    {
        if (laserFirePoint == null || laserProjectilePrefab == null || player == null) return;

        Vector2 baseDir = ((Vector2)GetPlayerTargetPosition() - (Vector2)laserFirePoint.position).normalized;
        float   rad     = degrees * Mathf.Deg2Rad;
        Vector2 rotated = new Vector2(
            baseDir.x * Mathf.Cos(rad) - baseDir.y * Mathf.Sin(rad),
            baseDir.x * Mathf.Sin(rad) + baseDir.y * Mathf.Cos(rad)
        );
        SpawnLaser(rotated.normalized);
    }

    private void SpawnLaser(Vector2 direction)
    {
        GameObject laser = Instantiate(laserProjectilePrefab, laserFirePoint.position, Quaternion.identity);

        // Support prefabs where BossLaserProjectile is on the root or a child object
        BossLaserProjectile projectile = laser.GetComponent<BossLaserProjectile>();
        if (projectile == null)
            projectile = laser.GetComponentInChildren<BossLaserProjectile>(true);

        if (projectile != null)
            projectile.Initialize(direction, laserProjectileSpeed, laserDamage);
        else
            Debug.LogError("[MechBossAI] Laser prefab is missing a BossLaserProjectile component! " +
                           "Add BossLaserProjectile to the prefab root.");
    }

    // Called by MechBossHealth when health crosses 50%
    public void EnterPhase2()
    {
        if (isPhase2) return;
        isPhase2 = true;
        currentMoveSpeed = phase2MoveSpeed;

        if (anim != null) anim.SetTrigger(AnimPhase2);

        StartCoroutine(Phase2TransitionPause());
    }

    private IEnumerator Phase2TransitionPause()
    {
        isAttacking = true;
        rb.linearVelocity = Vector2.zero;
        yield return new WaitForSeconds(1.5f);
        isAttacking = false;
    }

    public void DisableAI()
    {
        Debug.Log("[MechBossAI] DisableAI called. Stopping all coroutines and disabling AI.");
        StopAllCoroutines();
        ClearGlowTint();
        if (bossHealth != null) bossHealth.SetImmune(false);
        isGlowing = false;
        isAttacking = true;
        rb.linearVelocity = Vector2.zero;
        enabled = false;
    }

    public void GetParried(Vector2 knockbackDir)
    {
        if (isStaggered) return;

        if (currentAttackCoroutine != null)
        {
            StopCoroutine(currentAttackCoroutine);
            currentAttackCoroutine = null;
        }

        // Immediately hide the telegraph indicator so it doesn't linger after the parry.
        if (meleeTelegraph != null)
            meleeTelegraph.HideTelegraph();

        isAttacking = true;
        isParryable = false;
        isStaggered = true;
        staggerTimer = bossStaggerDuration;

        // Apply knockback impulse — boss lurches back from the deflection
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(knockbackDir * parryKnockbackForce, ForceMode2D.Impulse);

        if (anim != null)
            anim.SetTrigger("stagger");

        Debug.Log($"[MechBossAI] Boss was parried! Knockback applied, staggering for {bossStaggerDuration}s.");
    }

    private void CheckPlayerDetection()
    {
        if (player == null || !player.gameObject.activeInHierarchy)
        {
            FindTargetPlayer();
            if (player == null) { isChasing = false; return; }
        }

        float dist = Vector2.Distance(transform.position, player.position);
        if (!isChasing && dist <= detectionRadius)
            isChasing = true;
        else if (isChasing && dist > loseTargetRadius)
            isChasing = false;
    }

    private void StopMoving()
    {
        if (!isAttacking)
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
    }

    private void CheckGrounded()
    {
        isGrounded = Physics2D.Raycast(transform.position, Vector2.down, 0.3f, groundLayer);
    }

    private void HandleFlip()
    {
        if (rb.linearVelocity.x > 0.05f && !facingRight)
            Flip();
        else if (rb.linearVelocity.x < -0.05f && facingRight)
            Flip();
        else if (!isAttacking && player != null)
        {
            float dir = player.position.x - transform.position.x;
            if (dir > 0.1f && !facingRight) Flip();
            else if (dir < -0.1f && facingRight) Flip();
        }
    }

    private void Flip()
    {
        facingRight = !facingRight;
        
        // --- FIXED: Use Scale instead of Rotation so 2D Lighting doesn't break! ---
        if (animatorObj != null)
        {
            Vector3 newScale = animatorObj.localScale;
            newScale.x = facingRight ? Mathf.Abs(newScale.x) : -Mathf.Abs(newScale.x);
            animatorObj.localScale = newScale;
        }
        // -------------------------------------------------------------------------

        if (laserFirePoint != null)
        {
            laserFirePoint.localPosition = new Vector3(
                facingRight ? Mathf.Abs(laserFirePointLocalPos.x) : -Mathf.Abs(laserFirePointLocalPos.x),
                laserFirePointLocalPos.y,
                laserFirePointLocalPos.z
            );
        }

        UpdateAttackPointPosition();
    }

    private void UpdateAttackPointPosition()
    {
        if (meleeAttackPoint == null) return;
        float offset = facingRight ? meleeAttackPointOffset : -meleeAttackPointOffset;
        meleeAttackPoint.position = transform.position + new Vector3(offset, 0, 0);
    }

    private void UpdateAnimations()
    {
        if (anim == null) return;
        anim.SetFloat(AnimXVelocity, Mathf.Abs(rb.linearVelocity.x));
        anim.SetBool(AnimIsGrounded, isGrounded);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, loseTargetRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, meleeAttackRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, laserAttackRange);

        // Water check rays
        Gizmos.color = Color.blue;
        Vector3 origin = transform.position + Vector3.up * waterCheckHeightOffset;
        Gizmos.DrawRay(origin, Vector3.right * waterCheckDistance);
        Gizmos.DrawRay(origin, Vector3.left * waterCheckDistance);
    }
}