
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// For interface compliance
public class MechBossHealth : MonoBehaviour, IDamageable
{
    public void TakeDamage(float damage)
    {
        // Default to ranged=false, melee=false for generic calls
        TakeDamage(damage, false, false);
    }

// ...existing code...
    [Header("Health")]
    [SerializeField] private float maxHealth = 500f;
    private float currentHealth;

    [Header("World Space Health Bar")]
    [SerializeField] private Canvas worldHealthBarCanvas;
    [SerializeField] private Slider worldHealthBarSlider;
    [SerializeField] private float healthBarOffset = 2.8f;

    [Header("Screen Space Boss Bar (optional)")]
    [Tooltip("Assign a Screen Space - Overlay Canvas with a Slider child for a cinematic boss HP bar.")]
    [SerializeField] private Canvas screenHealthBarCanvas;
    [SerializeField] private Slider screenHealthBarSlider;

    [Header("Phase 2 Threshold")]
    [SerializeField] private float phase2Threshold = 0.5f;

    [Header("Death FX")]
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] private float destroyDelay = 3f;
    [SerializeField] private float explosionZ = -1f;
    [SerializeField] private Transform animatorObj;
    [SerializeField] private float deathAnimationDuration = 2f;

    [Header("Damage Flash")]
    [SerializeField] private Color flashColor = Color.red;
    [SerializeField] private float flashDuration = 0.12f;

    [Header("Hit Blood FX")]
    [SerializeField] private GameObject bloodImpactPrefab;
    [SerializeField] private string bloodImpactTemplateName = "BloodImpact_Medium";
    [SerializeField] private Vector2 bloodSpawnOffset = new Vector2(0f, 1.1f);
    [SerializeField] private float bloodSpawnZ = -0.5f;
    [SerializeField] private float bloodSpawnJitter = 0.28f;
    [SerializeField] private float bloodSpawnMinInterval = 0.08f;

    [Header("Immunity (Glow Phase)")]
    [SerializeField] private AudioSource blockedAudioSource;
    [SerializeField] private AudioClip blockedSoundClip;
    [SerializeField] private float blockedShakeDuration  = 0.15f;
    [SerializeField] private float blockedShakeMagnitude = 0.1f;

    private bool dead = false;
    private bool phase2Triggered = false;
    private bool glow25Triggered  = false;
    private bool isImmune = false;
    public bool IsImmune => isImmune;
    private Coroutine flashCoroutine;
    private bool isFlashing = false;
    private float nextBloodSpawnTime = 0f;
    private GameObject resolvedBloodImpactTemplate;
    private bool missingBloodTemplateWarned;

    private Rigidbody2D rb;
    private Collider2D[] colliders;
    private MechBossAI bossAI;
    private Animator anim;
    private SpriteRenderer[] spriteRenderers;
    private Color[] originalColors;

    private void Awake()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        colliders = GetComponentsInChildren<Collider2D>(true);
        bossAI = GetComponent<MechBossAI>();

        if (animatorObj != null)
            anim = animatorObj.GetComponent<Animator>();

        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        if (spriteRenderers != null)
        {
            originalColors = new Color[spriteRenderers.Length];
            for (int i = 0; i < spriteRenderers.Length; i++)
                originalColors[i] = spriteRenderers[i].color;
        }

        Debug.Log($"[MechBossHealth] Awake on '{name}'. worldHealthBarCanvas={worldHealthBarCanvas}, screenHealthBarCanvas={screenHealthBarCanvas}");

        SetupWorldHealthBar();
        ResolveBloodImpactTemplate();
    }

    private void Start()
    {
        // Always show health bars
        if (worldHealthBarCanvas != null)
            worldHealthBarCanvas.gameObject.SetActive(true);
        if (screenHealthBarCanvas != null)
            screenHealthBarCanvas.gameObject.SetActive(true);

        UpdateHealthBars();
    }

    private void Update()
    {
        UpdateWorldHealthBarPosition();
    }

    public void TakeDamage(float damage, bool isRanged = false, bool isMelee = false)
    {
        if (dead) return;

        // Defensive state checks (priority: immune > block > armorBuff)
        if (bossAI != null)
        {
            bossAI.TryTriggerDefensiveStates();
            // Immune blocks ALL damage (melee and ranged)
            if (bossAI.IsImmune)
            {
                if (blockedAudioSource != null && blockedSoundClip != null)
                    blockedAudioSource.PlayOneShot(blockedSoundClip);
                ScreenShake.Trigger(blockedShakeDuration, blockedShakeMagnitude);
                Debug.Log("[MechBoss] Hit blocked — boss is immune.");
                return;
            }
            // Block blocks melee only
            if (bossAI.IsBlocking && isMelee)
            {
                if (blockedAudioSource != null && blockedSoundClip != null)
                    blockedAudioSource.PlayOneShot(blockedSoundClip);
                ScreenShake.Trigger(blockedShakeDuration, blockedShakeMagnitude);
                Debug.Log("[MechBoss] Melee hit blocked — boss is blocking.");
                return;
            }
            // ArmorBuff reduces all damage
            if (bossAI.IsArmorBuffed)
            {
                damage *= 0.5f; // 50% damage reduction
            }
        }

        // Legacy: Glow phase immunity (if not using new immune state)
        if (isImmune)
        {
            if (blockedAudioSource != null && blockedSoundClip != null)
                blockedAudioSource.PlayOneShot(blockedSoundClip);
            ScreenShake.Trigger(blockedShakeDuration, blockedShakeMagnitude);
            Debug.Log("[MechBoss] Hit blocked — boss is immune during Glow phase.");
            return;
        }

        currentHealth -= damage;
        UpdateHealthBars();
        Flash();
        SpawnBloodImpact();

        Debug.Log($"[MechBoss] Took {damage} damage. HP: {currentHealth}/{maxHealth}");

        if (!phase2Triggered && currentHealth / maxHealth <= phase2Threshold)
        {
            phase2Triggered = true;
            if (bossAI != null) bossAI.EnterPhase2();
        }

        // Force a glow cycle at 25% HP (Phase 2 only — TriggerGlow guards this internally)
        if (!glow25Triggered && currentHealth / maxHealth <= 0.25f)
        {
            glow25Triggered = true;
            if (bossAI != null) bossAI.TriggerGlow();
        }

        if (currentHealth <= 0f)
            Die();
    }

    private void SpawnBloodImpact()
    {
        GameObject bloodTemplate = ResolveBloodImpactTemplate();
        if (bloodTemplate == null) return;
        if (Time.time < nextBloodSpawnTime) return;
        if (!gameObject.activeInHierarchy) return;

        Vector3 spawnPos = transform.position + (Vector3)bloodSpawnOffset;
        spawnPos.x += Random.Range(-bloodSpawnJitter, bloodSpawnJitter);
        spawnPos.y += Random.Range(-bloodSpawnJitter * 0.5f, bloodSpawnJitter * 0.5f);
        spawnPos.z = bloodSpawnZ;

        GameObject bloodInstance = Instantiate(
            bloodTemplate,
            spawnPos,
            Quaternion.Euler(0f, 0f, Random.Range(0f, 360f))
        );

        bloodInstance.SetActive(true);
        ForcePlayBloodParticles(bloodInstance);
        ApplyBloodSortingBehindTarget(bloodInstance);
        ScheduleBloodImpactDestroy(bloodInstance);
        nextBloodSpawnTime = Time.time + bloodSpawnMinInterval;
    }

    private GameObject ResolveBloodImpactTemplate()
    {
        if (bloodImpactPrefab != null)
            return bloodImpactPrefab;

        if (resolvedBloodImpactTemplate != null)
            return resolvedBloodImpactTemplate;

        GameObject fromPath = GameObject.Find("Particles/FX/" + bloodImpactTemplateName);
        if (fromPath != null)
        {
            resolvedBloodImpactTemplate = fromPath;
            return resolvedBloodImpactTemplate;
        }

        GameObject fromName = GameObject.Find(bloodImpactTemplateName);
        if (fromName != null)
        {
            resolvedBloodImpactTemplate = fromName;
            return resolvedBloodImpactTemplate;
        }

        ParticleSystem[] particles = Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < particles.Length; i++)
        {
            if (particles[i] == null) continue;

            if (particles[i].name == bloodImpactTemplateName || particles[i].name.Contains("BloodImpact"))
            {
                resolvedBloodImpactTemplate = particles[i].gameObject;
                return resolvedBloodImpactTemplate;
            }
        }

        if (!missingBloodTemplateWarned)
        {
            Debug.LogWarning($"[MechBossHealth] No blood impact template found. Assign bloodImpactPrefab or place an object named '{bloodImpactTemplateName}' in the scene.");
            missingBloodTemplateWarned = true;
        }

        return null;
    }

    private void ForcePlayBloodParticles(GameObject bloodInstance)
    {
        if (bloodInstance == null) return;

        ParticleSystem[] systems = bloodInstance.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < systems.Length; i++)
        {
            if (systems[i] == null) continue;
            systems[i].Clear(true);
            systems[i].Play(true);
        }
    }

    private void ApplyBloodSortingBehindTarget(GameObject bloodInstance)
    {
        if (bloodInstance == null || spriteRenderers == null || spriteRenderers.Length == 0) return;

        SpriteRenderer targetRenderer = null;
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
            {
                targetRenderer = spriteRenderers[i];
                break;
            }
        }

        if (targetRenderer == null) return;

        var particleRenderers = bloodInstance.GetComponentsInChildren<ParticleSystemRenderer>(true);
        for (int i = 0; i < particleRenderers.Length; i++)
        {
            particleRenderers[i].sortingLayerID = targetRenderer.sortingLayerID;
            particleRenderers[i].sortingOrder = targetRenderer.sortingOrder - 1;
        }
    }

    private void ScheduleBloodImpactDestroy(GameObject bloodInstance)
    {
        if (bloodInstance == null) return;
        StartCoroutine(DestroyBloodImpactWhenFinished(bloodInstance));
    }

    private IEnumerator DestroyBloodImpactWhenFinished(GameObject bloodInstance)
    {
        if (bloodInstance == null) yield break;

        ParticleSystem[] systems = bloodInstance.GetComponentsInChildren<ParticleSystem>(true);
        if (systems == null || systems.Length == 0)
        {
            Destroy(bloodInstance);
            yield break;
        }

        while (bloodInstance != null)
        {
            bool anyAlive = false;
            for (int i = 0; i < systems.Length; i++)
            {
                if (systems[i] != null && systems[i].IsAlive(true))
                {
                    anyAlive = true;
                    break;
                }
            }

            if (!anyAlive)
                break;

            yield return null;
        }

        if (bloodInstance != null)
            Destroy(bloodInstance);
    }

    /// <summary>
    /// Called by MechBossAI to toggle immunity on/off during the Glow phase.
    /// </summary>
    public void SetImmune(bool immune)
    {
        isImmune = immune;
        Debug.Log($"[MechBoss] Immunity set to {immune}.");
    }

    private void Flash()
    {
        // Stop any ongoing flash so a new hit always shows the color immediately
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            RestoreOriginalColors();
        }
        flashCoroutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        if (spriteRenderers == null || spriteRenderers.Length == 0) yield break;
        isFlashing = true;

        for (int i = 0; i < spriteRenderers.Length; i++)
            if (spriteRenderers[i] != null) spriteRenderers[i].color = flashColor;

        yield return new WaitForSeconds(flashDuration);
        RestoreOriginalColors();
        flashCoroutine = null;
    }

    private void RestoreOriginalColors()
    {
        if (spriteRenderers != null && originalColors != null)
            for (int i = 0; i < spriteRenderers.Length && i < originalColors.Length; i++)
                if (spriteRenderers[i] != null) spriteRenderers[i].color = originalColors[i];
        isFlashing = false;
    }

    private void Die()
    {
        if (KillQuestManager.Instance != null) KillQuestManager.Instance.DefeatMiniBoss();
        dead = true;

        NotifyPlayerOfKill();

        if (flashCoroutine != null) { StopCoroutine(flashCoroutine); flashCoroutine = null; }
        RestoreOriginalColors();

        if (bossAI != null) bossAI.DisableAI();

        if (rb != null) { rb.linearVelocity = Vector2.zero; rb.simulated = false; }

        foreach (var c in colliders)
            if (c != null && c.gameObject != gameObject) c.enabled = false;

        if (anim != null) anim.SetTrigger("die");

        if (worldHealthBarCanvas != null) worldHealthBarCanvas.gameObject.SetActive(false);
        if (screenHealthBarCanvas != null) screenHealthBarCanvas.gameObject.SetActive(false);

        Invoke(nameof(OnDeathAnimationComplete), deathAnimationDuration);
    }

    private void OnDeathAnimationComplete()
    {
        if (explosionPrefab != null)
        {
            for (int i = 0; i < 4; i++)
            {
                Vector3 offset = new Vector3(Random.Range(-1.2f, 1.2f), Random.Range(0f, 2f), explosionZ);
                Instantiate(explosionPrefab, transform.position + offset, Quaternion.identity);
            }
        }

        foreach (var r in GetComponentsInChildren<SpriteRenderer>(true))
            r.enabled = false;

        Destroy(gameObject, destroyDelay);
    }

    private void SetupWorldHealthBar()
    {
        if (worldHealthBarCanvas == null)
        {
            Debug.Log("[MechBossHealth] SetupWorldHealthBar: worldHealthBarCanvas is null, skipping world-space HP bar setup.");
            return;
        }

        // Safety: if the canvas is a parent (ancestor) of any boss sprite, applying
        // localScale = 0.01 or SetActive(false) would hide/shrink the entire boss.
        // Detect this misconfiguration and bail out with a clear error.
        if (spriteRenderers != null)
        {
            foreach (SpriteRenderer sr in spriteRenderers)
            {
                if (sr != null && sr.transform.IsChildOf(worldHealthBarCanvas.transform))
                {
                    Debug.LogError(
                        "[MechBossHealth] worldHealthBarCanvas is a PARENT of the boss sprites — " +
                        "this is why the boss is invisible! " +
                        "FIX: in the Prefab, make the health bar canvas a CHILD of the boss root (not the root itself). " +
                        "Health bar setup skipped to prevent shrinking/hiding the boss.");
                    return;
                }
            }
        }

        if (worldHealthBarSlider == null)
            worldHealthBarSlider = worldHealthBarCanvas.GetComponentInChildren<Slider>();

        worldHealthBarCanvas.renderMode = RenderMode.WorldSpace;
        worldHealthBarCanvas.transform.localScale = Vector3.one * 0.01f;
        UpdateWorldHealthBarPosition();
        worldHealthBarCanvas.gameObject.SetActive(false);
    }

    private void UpdateWorldHealthBarPosition()
    {
        if (worldHealthBarCanvas == null) return;

        worldHealthBarCanvas.transform.position = transform.position + Vector3.up * healthBarOffset;
        if (Camera.main != null)
        {
            worldHealthBarCanvas.transform.rotation = Camera.main.transform.rotation;
        }
    }

    private void UpdateHealthBars()
    {
        float ratio = currentHealth / maxHealth;

        if (worldHealthBarSlider != null)
            worldHealthBarSlider.value = ratio;

        if (screenHealthBarSlider != null)
            screenHealthBarSlider.value = ratio;
    }

    private void NotifyPlayerOfKill()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            PlayerEnergy energy = playerObj.GetComponentInParent<PlayerEnergy>();
            if (energy != null) energy.OnEnemyKilled();
        }
    }

    public void ShowHealthBars()
    {
        if (worldHealthBarCanvas != null) worldHealthBarCanvas.gameObject.SetActive(true);
        if (screenHealthBarCanvas != null) screenHealthBarCanvas.gameObject.SetActive(true);
        UpdateHealthBars();
    }

    public float GetHealthPercent() => currentHealth / maxHealth;
    public bool IsDead() => dead;
}
