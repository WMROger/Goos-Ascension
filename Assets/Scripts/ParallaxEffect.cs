using UnityEngine;

public class ParallaxEffect : MonoBehaviour
{
    [Header("Parallax Settings")]
    [Tooltip("How much the background moves left/right (0 = still, 1 = follows perfectly)")]
    public float parallaxEffect;
    
    [Tooltip("How much the background moves up/down (1 = follows perfectly)")]
    public float parallaxEffectY = 1f;

    private Transform cameraTransform;
    
    // Track our absolute starting points
    private float startPositionX;
    private float startPositionY;
    private float backgroundLength;

    void Start()
    {
        cameraTransform = Camera.main.transform;
        
        // Save the exact spawn position of this background
        startPositionX = transform.position.x;
        startPositionY = transform.position.y; 

        Renderer backgroundRenderer = GetComponent<Renderer>();
        if (backgroundRenderer == null)
        {
            Debug.LogError($"ParallaxEffect: No Renderer found on {gameObject.name}. Add a SpriteRenderer/TilemapRenderer.");
            enabled = false;
            return;
        }

        backgroundLength = backgroundRenderer.bounds.size.x;
    }

    void LateUpdate()
    {
        // 1. Calculate the absolute distance the background SHOULD move based on camera position
        float distX = (cameraTransform.position.x * parallaxEffect);
        float distY = (cameraTransform.position.y * parallaxEffectY);

        // 2. Set the exact position (This instantly fixes the jitter!)
        transform.position = new Vector3(startPositionX + distX, startPositionY + distY, transform.position.z);

        // 3. Keep track of how far the camera has moved relative to the loop
        float cameraRelativePosition = cameraTransform.position.x * (1 - parallaxEffect);

        // 4. Looping logic for the X axis
        if (cameraRelativePosition > startPositionX + backgroundLength)
        {
            startPositionX += backgroundLength; 
        }
        else if (cameraRelativePosition < startPositionX - backgroundLength)
        {
            startPositionX -= backgroundLength;
        }
    }
}