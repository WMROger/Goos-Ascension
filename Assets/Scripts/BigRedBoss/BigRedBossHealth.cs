using System.Collections;
using UnityEngine;

public class BigRedBossHealth : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 50f;
    private float currentHealth;
    
    [Header("Health Bar UI")]
    [SerializeField] private Canvas healthBarCanvas;
    [SerializeField] private UnityEngine.UI.Slider healthBarSlider;
    [SerializeField] private float healthBarOffset = .5f; // How high above enemy to show health bar

    [Header("Death FX")]
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] private float destroyDelay = 2f;
    [SerializeField] private float explosionZ = -1f;
    [SerializeField] private Transform animatorObj;
    [SerializeField] private float deathAnimationDuration = 1f;

    // --- NEW: END GAME CREDITS ---
    [Header("End Game / Credits")]
    [Tooltip("Drag your giant End Credits Canvas or Panel here. It will activate when the boss explodes!")]
    [SerializeField] private GameObject creditsPanel;
    // -----------------------------

    [Header("Damage Flash")]
    [Tooltip("The color the sprite flashes when hit.")]
    [SerializeField] private Color flashColor = Color.red;
    [Tooltip("How long the flash lasts in seconds.")]
    [SerializeField] private float flashDuration = 0.1f;
    [Tooltip("How long the enemy stays red before an attack.")]
    [SerializeField] private float preAttackFlashDuration = 0.5f;

    [Header("Hit Blood FX")]
    [SerializeField] private GameObject bloodImpactPrefab;
    [SerializeField] private string bloodImpactTemplateName = "BloodImpact_Medium";
    [SerializeField] private Vector2 bloodSpawnOffset = new Vector2(0f, 0.35f);
    [SerializeField] private float bloodSpawnZ = -0.5f;
    [SerializeField] private float bloodSpawnJitter = 0.18f;
    [SerializeField] private float bloodSpawnMinInterval = 0.08f;

    private bool dead;
    private Coroutine flashCoroutine;
    private bool isFlashing = false;
    private float nextBloodSpawnTime = 0f;
    private GameObject resolvedBloodImpactTemplate;
    private bool missingBloodTemplateWarned;

    private Rigidbody2D rb;
    private Collider2D[] colliders;
    private BigRedBossAI enemyAI;
    private Animator anim;
    private SpriteRenderer[] spriteRenderers;
    private Color[] originalColors;

    private void Awake()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        colliders = GetComponentsInChildren<Collider2D>(true);
        enemyAI = GetComponent<BigRedBossAI>();
        
        if (animatorObj != null)
        {
            anim = animatorObj.GetComponent<Animator>();
        }
        
        // Cache sprite renderers and original colors
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        if (spriteRenderers != null && spriteRenderers.Length > 0)
        {
            originalColors = new Color[spriteRenderers.Length];
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                originalColors[i] = spriteRenderers[i].color;
            }
        }
        
        // Initialize health bar but keep it disabled by default
        SetupHealthBar();

        ResolveBloodImpactTemplate();
        
        // Hide credits at start just in case!
        if (creditsPanel != null) creditsPanel.SetActive(false);
    }

    private void Start()
    {
        // Always show health bar above enemy
        if (healthBarCanvas != null)
        {
            healthBarCanvas.gameObject.SetActive(true);
            UpdateHealthBarPosition();
        }
        
        UpdateHealthBar();
    }

    public void TakeDamage(float damage)
    {
        if (dead) return;

        currentHealth -= damage;
        
        // Update health bar
        UpdateHealthBar();
        
        // Trigger flash effect when damaged
        Flash();
        SpawnBloodImpact();
        
        Debug.Log($"💥 ENEMY HEALTH: {gameObject.name} took {damage} damage! Health: {currentHealth}/{maxHealth}");
        
        if (currentHealth <= 0f)
        {
            Die();
        }
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
            Debug.LogWarning($"[EnemyHealth] No blood impact template found. Assign bloodImpactPrefab or place an object named '{bloodImpactTemplateName}' in the scene.");
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

    public void FlashPreAttack()
    {
        if (dead) return;
        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(PreAttackFlashRoutine());
    }

    private IEnumerator PreAttackFlashRoutine()
    {
        if (spriteRenderers == null || spriteRenderers.Length == 0) yield break;
        isFlashing = true;

        for (int i = 0; i < spriteRenderers.Length; i++)
            if (spriteRenderers[i] != null)
                spriteRenderers[i].color = flashColor;

        yield return new WaitForSeconds(preAttackFlashDuration);

        RestoreOriginalColors();
        flashCoroutine = null;
    }

    private void Flash()
    {
        if (isFlashing) return;
        
        if (flashCoroutine != null) 
        {
            StopCoroutine(flashCoroutine);
            RestoreOriginalColors(); 
        }
        
        flashCoroutine = StartCoroutine(FlashRoutine());
    }

    private void RestoreOriginalColors()
    {
        if (spriteRenderers != null && originalColors != null)
        {
            for (int i = 0; i < spriteRenderers.Length && i < originalColors.Length; i++)
            {
                if (spriteRenderers[i] != null)
                    spriteRenderers[i].color = originalColors[i];
            }
        }
        isFlashing = false;
    }

    private IEnumerator FlashRoutine()
    {
        if (spriteRenderers == null || spriteRenderers.Length == 0) yield break;
        
        isFlashing = true;

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
                spriteRenderers[i].color = flashColor;
        }

        yield return new WaitForSeconds(flashDuration);

        RestoreOriginalColors();
        
        flashCoroutine = null;
    }

    private void Die()
    {
        dead = true;

        NotifyPlayerOfKill();

        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            flashCoroutine = null;
        }
        RestoreOriginalColors();

        if (enemyAI != null)
            enemyAI.enabled = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }

        if (colliders != null)
        {
            foreach (var c in colliders)
            {
                if (c != null && c.gameObject != gameObject) 
                {
                    c.enabled = false;
                }
            }
        }

        FollowHitbox followHitbox = GetComponentInChildren<FollowHitbox>();
        if (followHitbox != null)
        {
            followHitbox.enabled = false;
        }

        if (anim != null)
        {
            anim.SetTrigger("die");
        }
        
        if (healthBarCanvas != null)
        {
            healthBarCanvas.gameObject.SetActive(false);
        }
        
        Invoke(nameof(OnDeathAnimationComplete), deathAnimationDuration);
    }
    
    private void Update()
    {
        UpdateHealthBarPosition();

        #if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.K) && !dead)
        {
            Debug.Log("[DEBUG] Force-killing boss!");
            currentHealth = 0f;
            Die();
        }
        #endif
    }
    
    private void SetupHealthBar()
    {
        if (healthBarCanvas == null)
        {
            healthBarCanvas = GetComponentInChildren<Canvas>();
        }
        
        if (healthBarSlider == null && healthBarCanvas != null)
        {
            healthBarSlider = healthBarCanvas.GetComponentInChildren<UnityEngine.UI.Slider>();
        }
        
        if (healthBarCanvas != null)
        {
            healthBarCanvas.renderMode = RenderMode.WorldSpace;
            healthBarCanvas.transform.localScale = Vector3.one * 0.01f;
            UpdateHealthBarPosition();
            healthBarCanvas.gameObject.SetActive(false);
        }
        
        if (healthBarSlider != null)
        {
            healthBarSlider.minValue = 0f;
            healthBarSlider.maxValue = 1f;
            healthBarSlider.value = 1f;
        }
        else
        {
            Debug.LogWarning($"No health bar slider found for {gameObject.name}! Please assign healthBarSlider in inspector or add Canvas>Slider as child.");
        }
    }
    
    private void UpdateHealthBarPosition()
    {
        if (healthBarCanvas != null)
        {
            healthBarCanvas.transform.position = transform.position + Vector3.up * healthBarOffset;
            
            if (Camera.main != null)
            {
                healthBarCanvas.transform.rotation = Camera.main.transform.rotation;
            }
        }
    }
    
    private void UpdateHealthBar()
    {
        if (healthBarSlider != null)
        {
            healthBarSlider.value = currentHealth / maxHealth;
        }
    }

    private void OnDeathAnimationComplete()
    {
        var renderers = GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var r in renderers)
            r.enabled = false;

        if (explosionPrefab != null)
        {
            Instantiate(
                explosionPrefab,
                new Vector3(transform.position.x, transform.position.y, explosionZ),
                Quaternion.identity
            );
        }

        ShowCredits();

        Destroy(gameObject, destroyDelay);
    }
    
    private void ShowCredits()
    {
        if (creditsPanel != null)
        {
            creditsPanel.SetActive(true);
        }
        else
        {
            var creditsGO = new GameObject("EndCredits");
            DontDestroyOnLoad(creditsGO);
            creditsGO.AddComponent<EndCredits>();
        }
        Debug.Log("BOSS DEFEATED! Credits Rolling!");
    }

    private void NotifyPlayerOfKill()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            PlayerEnergy playerEnergy = player.GetComponentInParent<PlayerEnergy>();
            if (playerEnergy != null)
            {
                playerEnergy.OnEnemyKilled();
            }
            else
            {
                Debug.LogWarning("Player found but no PlayerEnergy component attached!");
            }
        }
        else
        {
            Debug.LogWarning("No GameObject with 'Player' tag found for energy reward!");
        }
    }
}