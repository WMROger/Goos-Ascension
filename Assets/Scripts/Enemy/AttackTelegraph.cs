using UnityEngine;
using System.Collections;

public class AttackTelegraph : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Sprite telegraphSprite;
    private Color originalColor;
    private Vector3 originalScale;
    private Coroutine telegraphCoroutine;

    [Header("Telegraph Settings")]
    [SerializeField] private float pulseDuration = 0.4f; // Duration of one pulse cycle
    [SerializeField] private Color telegraphColor = new Color(1f, 0.2f, 0.2f, 0.85f); // Bright red
    [SerializeField] private float pulseScaleMultiplier = 1.3f; // How much to scale up per pulse
    [SerializeField] private Color parryWindowColor = new Color(1f, 0.85f, 0f, 0.9f); // Gold during parry window
    private Color defaultTelegraphColor;

    private void Awake()
    {
        // Create a sprite renderer if one doesn't exist
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sortingLayerName = "UI";
            spriteRenderer.sortingOrder = 10;
        }

        // Create a simple white circle sprite if none exists
        if (spriteRenderer.sprite == null)
        {
            telegraphSprite = CreateDefaultSprite();
            spriteRenderer.sprite = telegraphSprite;
        }
        else
        {
            telegraphSprite = spriteRenderer.sprite;
        }

        // Store original color and scale
        originalColor = spriteRenderer.color;
        originalScale = transform.localScale;
        defaultTelegraphColor = telegraphColor;
        
        // Start invisible
        SetVisibility(false);
    }

    /// <summary>
    /// Creates a simple default white circle sprite
    /// </summary>
    private Sprite CreateDefaultSprite()
    {
        // Create a simple white circle texture
        int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "TelegraphCircle";

        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float radius = size * 0.4f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                if (distance <= radius)
                {
                    pixels[y * size + x] = Color.white;
                }
                else
                {
                    pixels[y * size + x] = Color.clear;
                }
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        // Create sprite from texture
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100);
        sprite.name = "TelegraphCircle";

        return sprite;
    }

    /// <summary>
    /// Starts a continuous pulsating red indicator at the attack point.
    /// Call HideTelegraph() to stop it.
    /// </summary>
    public void ShowTelegraph()
    {
        if (telegraphCoroutine != null)
            StopCoroutine(telegraphCoroutine);

        telegraphCoroutine = StartCoroutine(TelegraphPulseLoopRoutine());
    }

    /// <summary>
    /// Stops the pulsating indicator immediately.
    /// </summary>
    public void HideTelegraph()
    {
        if (telegraphCoroutine != null)
        {
            StopCoroutine(telegraphCoroutine);
            telegraphCoroutine = null;
        }
        transform.localScale = originalScale;
        SetColor(originalColor);
        telegraphColor = defaultTelegraphColor;
        SetVisibility(false);
    }

    private IEnumerator TelegraphPulseLoopRoutine()
    {
        SetVisibility(true);

        Vector3 baseScale = originalScale;
        Vector3 bigScale  = originalScale * pulseScaleMultiplier;

        while (true)
        {
            float halfCycle = pulseDuration * 0.5f;
            float elapsed = 0f;

            // Expand and brighten
            while (elapsed < halfCycle)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / halfCycle;
                transform.localScale = Vector3.Lerp(baseScale, bigScale, t);
                Color c = telegraphColor;
                c.a = Mathf.Lerp(0.4f, telegraphColor.a, t);
                SetColor(c);
                yield return null;
            }

            // Shrink and dim
            elapsed = 0f;
            while (elapsed < halfCycle)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / halfCycle;
                transform.localScale = Vector3.Lerp(bigScale, baseScale, t);
                Color c = telegraphColor;
                c.a = Mathf.Lerp(telegraphColor.a, 0.4f, t);
                SetColor(c);
                yield return null;
            }
        }
    }

    private void SetVisibility(bool visible)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = visible;
        }
    }

    private void SetColor(Color color)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = color;
        }
    }

    /// <summary>
    /// Switches the telegraph color to gold when the parry window is open, red otherwise.
    /// </summary>
    public void SetParryWindowColor(bool parryActive)
    {
        telegraphColor = parryActive ? parryWindowColor : defaultTelegraphColor;
    }

    public float GetPulseDuration()
    {
        return pulseDuration;
    }
}
