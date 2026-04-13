using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCombat : MonoBehaviour
{
    private const string AttackParam = "attack";

    [Header("References")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerEnergy playerEnergy;

    private Animator anim;
    private bool facingRight = true;

    // Quick melee uses attackPoint.position for OverlapCircleAll.
    // Because PlayerMovement no longer rotates the player root (to preserve minimap/icons),
    // we must mirror the attackPoint manually when facing direction changes.
    private Vector3 attackPointBaseLocalPos;
    private bool attackPointCached;
    private bool lastFacingRight;

    // Public accessor for PlayerAnimationEvents
    public PlayerMovement PlayerMovementRef => playerMovement;

    [Header("Weapon System")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float shootCooldown = 0.3f;
    private bool canShoot = true;

    [Header("Sound Effect Settings")]
    [SerializeField] private SoundEffectLibrary soundEffectLibrary;
    [SerializeField] private AudioSource attackAudioSource;
    [SerializeField] private string swordAttackSoundGroupName = "SwordAttack";
    [SerializeField] private int swordAttackSoundElementIndex = 0;
    [SerializeField] private string chargedShotSoundGroupName = "ChargedShotCannon";
    [SerializeField] private int chargedShotSoundElementIndex = 0;
    [SerializeField] private string gunChargeSoundGroupName = "ChargedGun";
    [SerializeField] private int gunChargeSoundElementIndex = 0;
    [SerializeField] private AudioSource chargeLoopAudioSource;

    [Header("Attack Settings")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float attackRange = 0.5f;
    [SerializeField] private LayerMask enemyLayers;
    [SerializeField] private float attackDamage = 10f;


    [Header("Charged Sword Attack")]
    [SerializeField] private float maxChargeTime = 1.5f;
    [SerializeField] private GameObject swordArcPrefab;

    private float chargeTimer;
    private bool isCharging;
    private bool isGunCharging; // Separate charging state for gun
    private bool gunChargeMouseReleased; // Track if mouse was released during gun charge
    private bool usingSword = true;
    private float storedChargeLevel; // Store charge for animation event
    private float storedGunChargeLevel; // Store gun charge for animation event
    
    [Header("Quick Attack Settings")]
    [SerializeField] private float quickClickThreshold = 0.2f; // Time threshold for quick vs hold
    private float mouseDownTimer;
    private bool mouseWasPressed;

    [Header("Combo Attack Settings")]
    [SerializeField] private float comboWindow = 0.5f; // Time after player stops clicking before combo resets
    private int comboCounter = 0; // 0 = idle, 1 = attack1 ready, 2 = attack2 ready, 3 = attack3 ready
    private float comboTimer = 0f; // Timer for combo window
    private bool isAttackAnimationPlaying = false; // Track if attack animation is currently playing

    [Header("Parry Settings")]
    [SerializeField] private float parryRange = 1.5f;
    [SerializeField] private float parryCooldown = 0.8f;
    [SerializeField] private float parryIFrameDuration = 0.3f;
    [SerializeField] private string parrySoundGroupName = "Parry";
    [SerializeField] private int parrySoundElementIndex = 0;
    [SerializeField] private string missedParrySoundGroupName = "MissedParry";
    [SerializeField] private int missedParrySoundElementIndex = 0;
    [SerializeField] private string successfulParrySoundGroupName = "SuccessfulParry";
    [SerializeField] private int successfulParrySoundElementIndex = 0;
    private bool canParry = true;

    [Header("Block Settings")]
    [Tooltip("Energy drained per successfully blocked ranged attack.")]
    [SerializeField] private float blockEnergyCost = 20f;
    // True while the player holds RMB in human form. Read by BossLaserProjectile to intercept hits.
    private bool isBlocking = false;
    private bool movementLockedByBlock = false;
    public bool IsBlocking => isBlocking;


    [Header("Weapon State")]
    [SerializeField] private bool usingGun = false;

    [Header("Weapon Switch Cooldown")]
    [Tooltip("Cooldown in seconds after switching weapons before you can switch again or attack.")]
    [SerializeField] private float weaponSwitchCooldown = 1.5f;
    private float weaponSwitchCooldownTimer = 0f;

    [Header("Gun Settings")]
    [SerializeField] private float gunChargeAutoFireTime = 2.5f;
    [SerializeField] private float gunDamage = 20f;
    [SerializeField] private GameObject chargedShotPrefab;
    [SerializeField] private Transform chargedShotFirePoint; // Separate fire point for charged shot
    [SerializeField] private float chargedShotForwardOffset = 1.5f; // how far in front of fire point

    [Header("Charged Skill Cooldowns")]
    [SerializeField] private float chargedSwordCooldown = 1f;
    [SerializeField] private float chargedShotCooldown = 2f;
    private float chargedSwordCooldownTimer;
    private float chargedShotCooldownTimer;

    private void Awake()
    {
        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();
        
        if (playerEnergy == null)
            playerEnergy = GetComponent<PlayerEnergy>();

        if (attackPoint != null)
        {
            attackPointBaseLocalPos = attackPoint.localPosition;
            attackPointCached = true;
            lastFacingRight = facingRight;
        }
    }

    private void Update()
    {
        if (playerMovement == null) return;

        // Tick weapon switch cooldown
        if (weaponSwitchCooldownTimer > 0f)
            weaponSwitchCooldownTimer = Mathf.Max(0f, weaponSwitchCooldownTimer - Time.deltaTime);

        TickChargedSkillCooldowns();

        // Keep these synced from movement
        anim = playerMovement.CurrentAnimator;
        facingRight = playerMovement.FacingRight;

        if (attackPointCached && facingRight != lastFacingRight)
        {
            // Mirror the point around the player root's local Y/Z.
            // Positive X in local space is "right" when facingRight=true.
            float x = attackPointBaseLocalPos.x * (facingRight ? 1f : -1f);
            attackPoint.localPosition = new Vector3(x, attackPointBaseLocalPos.y, attackPointBaseLocalPos.z);
            lastFacingRight = facingRight;
        }

        // Check if attack animation has finished and clear attacking state
        if (isAttackAnimationPlaying && anim != null)
        {
            AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
            // If we're back in idle/move state, clear the attacking flag
            if (stateInfo.fullPathHash == Animator.StringToHash("Base Layer.idle/move") || stateInfo.normalizedTime >= 1f)
            {
                isAttackAnimationPlaying = false;
                playerMovement.SetAttackingOrCharging(false);
            }
        }

        // Handle combo timer decay whenever we're not in an active attack animation
        if (comboTimer > 0f && !isAttackAnimationPlaying)
        {
            comboTimer -= Time.deltaTime;
            if (comboTimer <= 0f)
            {
                ResetCombo();
            }
        }
        // Update animator with combo counter (only for human form)
        if (anim != null && playerMovement.IsHuman)
        {
            anim.SetInteger("comboCounter", comboCounter);

            // Run&Shoot is now bool-driven: hold true while in gun stance.
            bool holdGunStance = usingGun && !isGunCharging && !isBlocking;
            anim.SetBool("shoot", holdGunStance);
        }

        // Handle charged attack for both sword and gun when human
        if (playerMovement.IsHuman && (usingSword || usingGun))
        {
            HandleChargedAttack();
        }
        else
        {
            // Reset both charging states if not human or no weapon
            if (isCharging)
            {
                isCharging = false;
                anim.SetBool("isCharging", false);
            }
            if (isGunCharging)
            {
                isGunCharging = false;
                anim.SetBool("isGunCharging", false);
                StopGunChargeSound();
            }
        }

        HandleCombatInput();
    }

    private void TickChargedSkillCooldowns()
    {
        if (chargedSwordCooldownTimer > 0f)
            chargedSwordCooldownTimer = Mathf.Max(0f, chargedSwordCooldownTimer - Time.deltaTime);

        if (chargedShotCooldownTimer > 0f)
            chargedShotCooldownTimer = Mathf.Max(0f, chargedShotCooldownTimer - Time.deltaTime);
    }

    void HandleChargedAttack()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            mouseWasPressed = true;
            mouseDownTimer = 0f;
        }

        if (Mouse.current.leftButton.isPressed && mouseWasPressed)
        {
            mouseDownTimer += Time.deltaTime;
            
            // Start charging only after threshold is exceeded
            if (mouseDownTimer >= quickClickThreshold)
            {
                if (usingGun && !isGunCharging)
                {
                    StartGunCharging();
                }
                else if (!usingGun && !isCharging)
                {
                    StartSwordCharging();
                }
            }
            
            // Continue charging for sword (gun charging handled separately below)
            if (!usingGun && isCharging)
            {
                ChargeSword();
            }
        }

        // Gun charge continues ticking even after mouse release — auto-fires when ready
        if (isGunCharging)
        {
            ChargeGun();

            // Determine max charge duration from the sound clip length (fallback to autoFireTime)
            float maxGunCharge = gunChargeAutoFireTime;
            if (chargeLoopAudioSource != null && chargeLoopAudioSource.clip != null)
            {
                maxGunCharge = chargeLoopAudioSource.clip.length;
            }

            // Auto-fire if mouse was released early and we hit the minimum charge time
            if (gunChargeMouseReleased && chargeTimer >= gunChargeAutoFireTime)
            {
                ReleaseGunCharge();
            }
            // Auto-fire if held past the full sound clip duration
            else if (chargeTimer >= maxGunCharge)
            {
                ReleaseGunCharge();
            }
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            if (usingGun && isGunCharging)
            {
                gunChargeMouseReleased = true;
                // If already past minimum charge time, fire immediately on release
                if (chargeTimer >= gunChargeAutoFireTime)
                {
                    ReleaseGunCharge();
                }
                // Otherwise let it keep running until auto-fire at 2.5s
            }
            else if (!usingGun && isCharging)
            {
                ReleaseSwordCharge();
            }
            else if (mouseWasPressed && mouseDownTimer < quickClickThreshold)
            {
                // Quick click - perform regular attack
                TryToAttack();
            }
            
            mouseWasPressed = false;
            mouseDownTimer = 0f;
        }
    }

void StartSwordCharging()
{
    if (isCharging) return;

    if (chargedSwordCooldownTimer > 0f)
    {
        Debug.Log("Charged sword is on cooldown.");
        return;
    }

    // Check if player has at least half energy before allowing sword charging
    if (playerEnergy == null || playerEnergy.currentEnergy < (playerEnergy.maxEnergy * 0.5f))
    {
        Debug.Log("⚔️ SWORD CHARGING BLOCKED: Energy less than half! Need at least 50% energy.");
        return;
    }

    isCharging = true;
    chargeTimer = 0f;
    anim.SetBool("isCharging", true);
    playerMovement.SetAttackingOrCharging(true);
    Debug.Log("Started sword charging");
}

void StartGunCharging()
{
    if (isGunCharging) return;

    if (chargedShotCooldownTimer > 0f)
    {
        Debug.Log("Charged shot is on cooldown.");
        return;
    }

    // Check if player has full energy before allowing gun charging
    if (playerEnergy == null || playerEnergy.currentEnergy < playerEnergy.maxEnergy)
    {
        Debug.Log("⚡ GUN CHARGING BLOCKED: Energy not full! Cannot charge gun.");
        return;
    }

    isGunCharging = true;
    gunChargeMouseReleased = false;
    chargeTimer = 0f;
    anim.SetBool("isGunCharging", true);
    playerMovement.SetAttackingOrCharging(true);

    // Start looping charge sound
    if (soundEffectLibrary != null && chargeLoopAudioSource != null)
    {
        AudioClip chargeClip = soundEffectLibrary.GetSoundEffect(gunChargeSoundGroupName, gunChargeSoundElementIndex);
        if (chargeClip != null)
        {
            chargeLoopAudioSource.clip = chargeClip;
            chargeLoopAudioSource.loop = true;
            chargeLoopAudioSource.Play();
        }
    }

    Debug.Log("Started gun charging");
}

void ChargeSword()
{
    if (!isCharging) return;

    // Stop charging if energy is less than half
    if (playerEnergy == null || playerEnergy.currentEnergy < (playerEnergy.maxEnergy * 0.5f))
    {
        Debug.Log("⚔️ SWORD CHARGING STOPPED: Energy below half during charging!");
        isCharging = false;
        anim.SetBool("isCharging", false);
        return;
    }

    chargeTimer += Time.deltaTime;
    chargeTimer = Mathf.Min(chargeTimer, maxChargeTime);

    float charge01 = chargeTimer / maxChargeTime;
    anim.SetFloat("chargeLevel", charge01);
}

void ChargeGun()
{
    if (!isGunCharging) return;

    // Stop charging if energy is no longer full
    if (playerEnergy == null || playerEnergy.currentEnergy < playerEnergy.maxEnergy)
    {
        Debug.Log("⚡ GUN CHARGING STOPPED: Energy depleted during charging!");
        isGunCharging = false;
        anim.SetBool("isGunCharging", false);
        StopGunChargeSound();
        playerMovement.SetAttackingOrCharging(false);
        return;
    }

    chargeTimer += Time.deltaTime;

    float charge01 = Mathf.Clamp01(chargeTimer / gunChargeAutoFireTime);
    anim.SetFloat("chargeLevel", charge01);
}

void ReleaseSwordCharge()
{
    if (!isCharging) return;

    // Check if player has at least half energy for charged sword attack
    if (playerEnergy == null || playerEnergy.currentEnergy < (playerEnergy.maxEnergy * 0.5f))
    {
        Debug.Log("⚔️ CHARGED SWORD BLOCKED: Not enough energy! Need at least half energy bar.");
        isCharging = false;
        anim.SetBool("isCharging", false);
        playerMovement.SetAttackingOrCharging(false);
        return;
    }

    isCharging = false;
    anim.SetBool("isCharging", false);
    anim.SetTrigger("chargedAttack");
    playerMovement.SetAttackingOrCharging(true);
    
    // Store charge level for animation event
    storedChargeLevel = chargeTimer / maxChargeTime;
    Debug.Log($"⚔️ CHARGED SWORD READY: Released sword charge: {storedChargeLevel:F2}");
}

void ReleaseGunCharge()
{
    if (!isGunCharging) return;

    // Check if player has full energy for charged shot
    if (playerEnergy == null || playerEnergy.currentEnergy < playerEnergy.maxEnergy)
    {
        Debug.Log("⚡ CHARGED SHOT BLOCKED: Not enough energy! Need full energy bar.");
        isGunCharging = false;
        anim.SetBool("isGunCharging", false);
        StopGunChargeSound();
        playerMovement.SetAttackingOrCharging(false);
        return;
    }

    isGunCharging = false;
    anim.SetBool("isGunCharging", false);
    StopGunChargeSound();
    anim.SetTrigger("chargedShot");
    playerMovement.SetAttackingOrCharging(true);
    
    // Store charge level for animation event
    storedGunChargeLevel = Mathf.Clamp01(chargeTimer / gunChargeAutoFireTime);
    Debug.Log($"⚡ CHARGED SHOT READY: Released gun charge: {storedGunChargeLevel:F2}");
}


private void StopGunChargeSound()
{
    if (chargeLoopAudioSource != null && chargeLoopAudioSource.isPlaying)
    {
        chargeLoopAudioSource.Stop();
        chargeLoopAudioSource.loop = false;
        chargeLoopAudioSource.clip = null;
    }
}

public void ActivateSwordArc(float charge01)
{
    if (swordArcPrefab == null)
    {
        Debug.LogError("SwordArcPrefab is not assigned!");
        return;
    }

    // Double-check energy before firing (safety check)
    if (playerEnergy != null && playerEnergy.currentEnergy < (playerEnergy.maxEnergy * 0.5f))
    {
        Debug.LogWarning("⚔️ CHARGED SWORD CANCELLED: Insufficient energy during firing!");
        return;
    }

    // Play sword attack sound for charged attack
    if (soundEffectLibrary != null && attackAudioSource != null)
    {
        soundEffectLibrary.PlaySoundEffect(attackAudioSource, swordAttackSoundGroupName, swordAttackSoundElementIndex);
        Debug.Log("Playing charged sword attack sound effect");
    }

    // Consume energy to half
    if (playerEnergy != null)
    {
        float halfEnergy = playerEnergy.maxEnergy * 0.5f;
        playerEnergy.SpendEnergy(playerEnergy.currentEnergy - halfEnergy);
        Debug.Log($"⚔️ ENERGY CONSUMED: Energy reduced to half for charged sword! Energy: {playerEnergy.currentEnergy}/{playerEnergy.maxEnergy}");
    }

    chargedSwordCooldownTimer = chargedSwordCooldown;

    // Instantiate the sword arc at the attack point
    GameObject arcInstance = Instantiate(swordArcPrefab, attackPoint.position, Quaternion.identity);
    
    // Set the direction based on facing direction
    float dir = facingRight ? 1 : -1;
    arcInstance.transform.localScale = new Vector3(dir, 1, 1);

    // Set the charge level for damage
    SwordArcDamage arc = arcInstance.GetComponent<SwordArcDamage>();
    if (arc != null)
    {
        arc.SetCharge(charge01);
    }

    // Destroy the arc after a short duration
    Destroy(arcInstance, 1f);

    // Allow transformation after charged sword
    if (playerMovement != null)
        playerMovement.SetAttackingOrCharging(false);

    Debug.Log($"⚔️ CHARGED SWORD FIRED: Charge level: {charge01:F2}, Energy reduced to half!");
}

// Method to be called from animation events
public void ActivateSwordArcFromAnimation()
{
    ActivateSwordArc(storedChargeLevel);
}

public void ActivateChargedShot(float charge01)
{
    Debug.Log($"ActivateChargedShot called on {gameObject.name}, chargedShotPrefab = {chargedShotPrefab}");
    
    if (chargedShotPrefab == null)
    {
        Debug.LogError($"ChargedShotPrefab is not assigned on {gameObject.name}! Check the PlayerCombat component.");
        return;
    }

    // Double-check energy before firing (safety check)
    if (playerEnergy != null && playerEnergy.currentEnergy < playerEnergy.maxEnergy)
    {
        Debug.Log("⚡ CHARGED SHOT CANCELLED: Insufficient energy during firing!");
        return;
    }

    // Consume all player energy
    if (playerEnergy != null)
    {
        playerEnergy.SpendEnergy(playerEnergy.maxEnergy);
        Debug.Log($"⚡ ENERGY CONSUMED: All energy spent for charged shot! Energy: {playerEnergy.currentEnergy}/{playerEnergy.maxEnergy}");
    }

    chargedShotCooldownTimer = chargedShotCooldown;

    // Play charged cannon sound effect
    if (soundEffectLibrary != null && attackAudioSource != null)
    {
        soundEffectLibrary.PlaySoundEffect(attackAudioSource, chargedShotSoundGroupName, chargedShotSoundElementIndex);
        Debug.Log("Playing charged cannon sound effect");
    }

        // Choose spawn point: prefer dedicated chargedShotFirePoint, fallback to regular firePoint
        Transform spawnPoint = chargedShotFirePoint != null ? chargedShotFirePoint : firePoint;
        if (spawnPoint == null)
        {
            Debug.LogError("No fire point assigned for charged shot (chargedShotFirePoint and firePoint are both null)!");
            return;
        }

        // Base position from spawn point
        Vector3 shotPos = spawnPoint.position;

        // Push the whole effect further in front of the player based on facing
        float dir = facingRight ? 1f : -1f;
        shotPos.x += dir * chargedShotForwardOffset;

        // Instantiate the charged shot at the offset position
        GameObject shotInstance = Instantiate(chargedShotPrefab, shotPos, Quaternion.identity);

        // Match bullet rendering so it appears in front of the player
        shotPos.z = -1f; // same z as Bullet
        shotInstance.transform.position = shotPos;

        SpriteRenderer shotSprite = shotInstance.GetComponent<SpriteRenderer>();
        if (shotSprite != null)
        {
            shotSprite.sortingOrder = 10; // same sorting order as Bullet
        }
    
    // Set up the charged shot
    ChargedShot chargedShot = shotInstance.GetComponent<ChargedShot>();
    if (chargedShot != null)
    {
        chargedShot.SetDirection(dir);
        chargedShot.SetCharge(charge01);
    }

    Debug.Log($"🔥 CHARGED SHOT FIRED: Charge level: {charge01:F2}, Energy depleted!");

    // Allow transformation after charged shot
    if (playerMovement != null)
        playerMovement.SetAttackingOrCharging(false);
}

// Method to be called from animation events
public void ActivateChargedShotFromAnimation()
{
    ActivateChargedShot(storedGunChargeLevel);
}

/// <summary>
/// Called by animation events when attack/charged attack animations finish
/// </summary>
public void OnAttackAnimationEnd()
{
    if (playerMovement != null)
    {
        playerMovement.SetAttackingOrCharging(false);
        playerMovement.EnableMovementAndJump(true);
    }

    if (anim != null)
        anim.SetBool(AttackParam, false);

    isAttackAnimationPlaying = false;
}


    private void HandleCombatInput()
    {
        if (Keyboard.current.qKey.wasPressedThisFrame && weaponSwitchCooldownTimer <= 0f)
            SwitchWeapon();

        if (Keyboard.current.fKey.wasPressedThisFrame)
            TryParry();

        // Block: hold RMB while grounded in human form to intercept boss ranged attacks.
        // Disabled in slime form, in the air, and while attacking or charging — those states
        // override block so the animations don't conflict.
        bool canBlock = playerMovement.IsHuman
                     && playerMovement.IsGrounded
                     && !isAttackAnimationPlaying
                     && !isCharging
                     && !isGunCharging;

        isBlocking = canBlock && Mouse.current.rightButton.isPressed;

        // Freeze movement while actively blocking.
        if (isBlocking)
        {
            if (!movementLockedByBlock)
            {
                playerMovement.EnableMovementAndJump(false);
                movementLockedByBlock = true;
            }
        }
        // Restore movement when block ends, but do not override attack/charge locks.
        else if (movementLockedByBlock)
        {
            bool canRestoreMovement = !isAttackAnimationPlaying && !isCharging && !isGunCharging;
            if (canRestoreMovement)
            {
                playerMovement.EnableMovementAndJump(true);
            }
            movementLockedByBlock = false;
        }

        if (anim != null && playerMovement.IsHuman)
        {
            anim.SetBool("isBlocking", isBlocking);

            // Ensure gun stance does not override block while RMB is held.
            if (isBlocking && usingGun)
                anim.SetBool("shoot", false);
        }

        // Note: Both sword and gun attacks (regular and charged) are now handled in HandleChargedAttack()
    }

    private void TryToAttack()
    {
        if (!playerMovement.IsHuman) return;
        
        if (usingGun)
        {
            // Quick gun shot
            TryToShoot();
        }
        else
        {
            // Sword attack only when grounded
            if (!playerMovement.IsGrounded) return;

            // If combo window expired, always restart from attack1.
            if (comboCounter > 0 && comboTimer <= 0f)
                ResetCombo();
            
            // Only allow attack if not already attacking
            if (anim != null)
            {
                // Allow chaining from idle/move to next combo stage within combo window.
                // This avoids needing to press during the exact transition frame.
                if (!isAttackAnimationPlaying && comboTimer > 0f)
                {
                    if (comboCounter == 1)
                    {
                        isAttackAnimationPlaying = true;
                        comboTimer = comboWindow;
                        anim.Play("humanAttack2", 0, 0f);
                        playerMovement.SetAttackingOrCharging(true);
                        Debug.Log("⚔️ ATTACK CHAINED: Playing humanAttack2 from combo window");
                        return;
                    }

                    if (comboCounter == 2)
                    {
                        isAttackAnimationPlaying = true;
                        comboTimer = comboWindow;
                        anim.Play("humanAttack3", 0, 0f);
                        playerMovement.SetAttackingOrCharging(true);
                        Debug.Log("⚔️ ATTACK CHAINED: Playing humanAttack3 from combo window");
                        return;
                    }
                }

                // Start/reset combo timer
                comboTimer = comboWindow;
                
                // Mark attack animation as playing
                isAttackAnimationPlaying = true;
                
                // Trigger the attack (animator will use comboCounter to choose animation)
                anim.SetBool(AttackParam, true);
                playerMovement.SetAttackingOrCharging(true);
                Debug.Log($"⚔️ ATTACK TRIGGERED: Current combo stage {comboCounter}");
            }
        }
    }

    /// <summary>
    /// Called by animation events at the end of each attack animation
    /// Increments the combo counter and resets the combo timer
    /// </summary>
    public void IncrementCombo()
    {
        comboCounter++;
        
        // Loop back to first attack after completing the 3rd
        if (comboCounter >= 3)
            comboCounter = 0;
        
        // Reset combo timer for next attack window
        comboTimer = comboWindow;
        
        // Always re-enable movement after each attack
        if (playerMovement != null)
        {
            playerMovement.EnableMovementAndJump(true);
            playerMovement.SetAttackingOrCharging(false);
            isAttackAnimationPlaying = false;
        }

        if (anim != null)
            anim.SetBool(AttackParam, false);
        
        Debug.Log($"⚔️ COMBO INCREMENTED: Now at stage {comboCounter}");
    }

    private void ResetCombo()
    {
        if (comboCounter > 0)
        {
            Debug.Log("⚔️ COMBO RESET: Window expired!");
            comboCounter = 0;
            comboTimer = 0f;
            
            // Ensure movement is enabled when combo resets
            if (playerMovement != null)
            {
                playerMovement.EnableMovementAndJump(true);
                playerMovement.SetAttackingOrCharging(false);
                isAttackAnimationPlaying = false;
            }

            if (anim != null)
                anim.SetBool(AttackParam, false);
        }
    }

    public void PlayAttackSound()
    {
        // Play sword attack sound from animation event
        if (soundEffectLibrary != null && attackAudioSource != null)
        {
            soundEffectLibrary.PlaySoundEffect(attackAudioSource, swordAttackSoundGroupName, swordAttackSoundElementIndex);
            Debug.Log("Playing sword attack sound effect from animation");
        }
    }

    public void DamageTarget()
    {
        if (usingGun) return;
        if (!playerMovement.IsGrounded) return;

        // Use OverlapCircleAll without layer filter so it can hit both enemies and boss
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(attackPoint.position, attackRange);

        foreach (Collider2D col in hitColliders)
        {
            // Skip anything on the player's own object
            if (col.GetComponentInParent<PlayerMovement>() != null) continue;

            IDamageable target = col.GetComponentInParent<IDamageable>();
            if (target != null)
            {
                target.TakeDamage(10f);
                Debug.Log("Dealt 10 QUICK damage to " + col.name);
            }
        }
    }

    private void TryToShoot()
    {
        if (!canShoot) return;
        if (!playerMovement.IsHuman) return;

        if (firePoint == null || bulletPrefab == null)
        {
            Debug.LogError("FirePoint or BulletPrefab is missing!");
            return;
        }

        canShoot = false;

        if (anim != null)
            anim.SetBool("shoot", true);

        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);

        // Keep your z/sorting behavior
        Vector3 bulletPos = bullet.transform.position;
        bulletPos.z = -1f;
        bullet.transform.position = bulletPos;

        SpriteRenderer bulletSprite = bullet.GetComponent<SpriteRenderer>();
        if (bulletSprite != null)
            bulletSprite.sortingOrder = 10;

        Bullet bulletScript = bullet.GetComponent<Bullet>();
        if (bulletScript != null)
        {
            float dir = facingRight ? 1f : -1f;
            bulletScript.SetDirection(dir);
            bulletScript.SetDamage(gunDamage);
        }

        Invoke(nameof(ResetShoot), shootCooldown);
    }

    private void ResetShoot()
    {
        canShoot = true;
    }

    private void SwitchWeapon()
    {
        if (!playerMovement.IsHuman)
            return;
        if (weaponSwitchCooldownTimer > 0f)
            return;

        usingGun = !usingGun;
        usingSword = !usingGun; // Keep these in sync

        // Avoid stale melee request when switching away from sword.
        if (anim != null && usingGun)
            anim.SetBool(AttackParam, false);

        if (anim != null)
            anim.SetTrigger("switchWeapon");

        weaponSwitchCooldownTimer = weaponSwitchCooldown;

        Debug.Log(usingGun ? "Switched to GUN" : "Switched to SWORD");
    }

    private void TryParry()
    {
        if (!playerMovement.IsHuman) return;
        if (!playerMovement.IsGrounded) return;
        if (usingGun) return;
        if (!canParry) return;

        if (attackPoint == null)
            return;

        bool parryConnected = false;
        Collider2D[] nearby = Physics2D.OverlapCircleAll(attackPoint.position, parryRange);
        foreach (Collider2D col in nearby)
        {
            if (col.GetComponentInParent<PlayerMovement>() != null) continue;

            EnemyAI enemy = col.GetComponentInParent<EnemyAI>();
            if (enemy != null && enemy.IsParryable)
            {
                Vector2 knockbackDir = ((Vector2)(enemy.transform.position - transform.position)).normalized;
                knockbackDir.y += 1f;
                knockbackDir.Normalize();
                enemy.GetParried(knockbackDir);
                parryConnected = true;
                break;
            }

            // Add support for parrying BigRedBossAI
            BigRedBossAI bigRedBoss = col.GetComponentInParent<BigRedBossAI>();
            if (bigRedBoss != null && bigRedBoss.IsParryable)
            {
                Vector2 knockbackDir = ((Vector2)(bigRedBoss.transform.position - transform.position)).normalized;
                knockbackDir.y += 1f;
                knockbackDir.Normalize();
                bigRedBoss.GetParried(knockbackDir);
                parryConnected = true;
                break;
            }

            MechBossAI boss = col.GetComponentInParent<MechBossAI>();
            if (boss != null && boss.IsParryable)
            {
                Vector2 knockbackDir = ((Vector2)(boss.transform.position - transform.position)).normalized;
                knockbackDir.y += 1f;
                knockbackDir.Normalize();
                boss.GetParried(knockbackDir);
                parryConnected = true;
                break;
            }
        }

        if (parryConnected)
        {
            if (anim != null)
                anim.SetTrigger("parry");

            playerMovement.SetInvulnerable(parryIFrameDuration);

            if (soundEffectLibrary != null && attackAudioSource != null && !string.IsNullOrEmpty(successfulParrySoundGroupName))
                soundEffectLibrary.PlaySoundEffect(attackAudioSource, successfulParrySoundGroupName, successfulParrySoundElementIndex);

            Debug.Log("✅ PARRY SUCCESS: Enemy staggered!");
        }
        else
        {
            if (soundEffectLibrary != null && attackAudioSource != null && !string.IsNullOrEmpty(missedParrySoundGroupName))
            {
                int randomIndex = Random.Range(0, 2);
                soundEffectLibrary.PlaySoundEffect(attackAudioSource, missedParrySoundGroupName, randomIndex);
            }

            Debug.Log("❌ PARRY MISS: No parryable enemy in range.");
        }

        canParry = false;
        Invoke(nameof(ResetParry), parryCooldown);
    }

    private void ResetParry()
    {
        canParry = true;
    }

    // ---------- Damage API ----------
    public float GetAttackDamage() => attackDamage;

    public void SetAttackDamage(float newDamage)
    {
        attackDamage = Mathf.Max(0f, newDamage);
    }

    public void AddAttackDamage(float amount)
    {
        attackDamage = Mathf.Max(0f, attackDamage + amount);
    }

    // ---------- Cooldown API ----------
    public float GetChargedSwordCooldownRemaining() => Mathf.Max(0f, chargedSwordCooldownTimer);
    public float GetChargedSwordCooldownDuration() => chargedSwordCooldown;
    public float GetChargedShotCooldownRemaining() => Mathf.Max(0f, chargedShotCooldownTimer);
    public float GetChargedShotCooldownDuration() => chargedShotCooldown;

    // Weapon switch cooldown accessors
    public float GetWeaponSwitchCooldownRemaining() => Mathf.Max(0f, weaponSwitchCooldownTimer);
    public float GetWeaponSwitchCooldownDuration() => weaponSwitchCooldown;

    // ---------- Weapon State API ----------
    public bool IsUsingGun => usingGun;
    public bool IsUsingSword => usingSword;

    // ---------- Gizmos ----------
    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}
