using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
[RequireComponent(typeof(TilemapCollider2D))]
public class SecretWallFade : MonoBehaviour
{
    [Tooltip("How invisible the wall becomes when the player is inside (0 = fully invisible, 0.3 = ghostly)")]
    [SerializeField] private float fadedAlpha = 0.3f;

    [Tooltip("How fast it fades in and out")]
    [SerializeField] private float fadeSpeed = 5f;

    [Tooltip("How long the player must be inside before the wall starts fading (seconds)")]
    [SerializeField] private float fadeDelay = 0.3f;

    private Tilemap tilemap;
    private float targetAlpha = 1f;
    private int playerOverlapCount = 0;
    private float fadeDelayTimer = 0f;
    private bool waitingToFade = false;

    private void Awake()
    {
        tilemap = GetComponent<Tilemap>();
        GetComponent<TilemapCollider2D>().isTrigger = true;
    }

    private void Update()
    {
        // Count down the delay before starting to fade in
        if (waitingToFade)
        {
            fadeDelayTimer -= Time.deltaTime;
            if (fadeDelayTimer <= 0f)
            {
                waitingToFade = false;
                targetAlpha = fadedAlpha;
            }
        }

        Color c = tilemap.color;
        if (Mathf.Abs(c.a - targetAlpha) > 0.001f)
        {
            c.a = Mathf.Lerp(c.a, targetAlpha, fadeSpeed * Time.deltaTime);
            tilemap.color = c;
        }
        else if (c.a != targetAlpha)
        {
            c.a = targetAlpha;
            tilemap.color = c;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsPlayerCollider(other)) return;
        if (!IsPlayerCenterInside(other)) return;

        playerOverlapCount++;
        if (playerOverlapCount == 1)
        {
            fadeDelayTimer = fadeDelay;
            waitingToFade  = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsPlayerCollider(other)) return;

        playerOverlapCount = Mathf.Max(0, playerOverlapCount - 1);
        if (playerOverlapCount == 0)
        {
            // Cancel pending fade if player left before the delay expired
            waitingToFade = false;
            targetAlpha   = 1f;
        }
    }

    private bool IsPlayerCollider(Collider2D col)
    {
        return col.GetComponentInParent<PlayerMovement>() != null;
    }

    private bool IsPlayerCenterInside(Collider2D col)
    {
        Bounds worldBounds = tilemap.GetComponent<Renderer>() != null
            ? tilemap.GetComponent<Renderer>().bounds
            : new Bounds(transform.position, tilemap.localBounds.size);

        return worldBounds.Contains(col.bounds.center);
    }
}
