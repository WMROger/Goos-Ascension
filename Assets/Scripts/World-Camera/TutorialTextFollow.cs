using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
public class TutorialTextFollow : MonoBehaviour
{
    [Header("Tracking Settings")]
    [Tooltip("Drag your Player object here")]
    public Transform playerTransform;
    [Tooltip("Adjust this to push the text higher above the head")]
    public Vector3 offset = new Vector3(0, 2f, 0); 

    [Header("Fading Settings")]
    [Tooltip("How many seconds before it starts fading?")]
    public float timeBeforeFade = 3f;
    [Tooltip("How long does the fade animation take?")]
    public float fadeDuration = 1f;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Camera mainCam;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        mainCam = Camera.main;

        // Start the timer to fade out
        StartCoroutine(FadeOutRoutine());
    }

    // We use LateUpdate so it moves AFTER the player moves (prevents jitter)
    void LateUpdate()
    {
        if (playerTransform != null && mainCam != null)
        {
            // Convert the Player's 3D world position into 2D screen pixels
            Vector3 screenPos = mainCam.WorldToScreenPoint(playerTransform.position + offset);
            
            // Snap the UI to those pixels
            rectTransform.position = screenPos;
        }
    }

    IEnumerator FadeOutRoutine()
    {
        // 1. Wait for the reading time
        yield return new WaitForSeconds(timeBeforeFade);

        // 2. Animate the fade
        float elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            // Smoothly transition alpha from 1 (solid) to 0 (invisible)
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeDuration);
            yield return null;
        }

        // 3. Ensure it is completely invisible
        canvasGroup.alpha = 0f;

        // 4. Turn the object off so it stops doing math in the background
        gameObject.SetActive(false); 
    }
}