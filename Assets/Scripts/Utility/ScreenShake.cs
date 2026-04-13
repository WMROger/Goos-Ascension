using System.Collections;
using UnityEngine;

/// <summary>
/// Singleton screen-shake utility. Drop one instance anywhere in the scene
/// (e.g. on the Main Camera or a persistent manager object) and call
/// ScreenShake.Trigger() from anywhere.
/// </summary>
public class ScreenShake : MonoBehaviour
{
    public static ScreenShake Instance { get; private set; }

    [Header("Default Settings")]
    [SerializeField] private float defaultDuration  = 0.2f;
    [SerializeField] private float defaultMagnitude = 0.15f;

    private Coroutine shakeCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Trigger a camera shake. Safe to call even if no ScreenShake component exists in the scene.
    /// </summary>
    public static void Trigger(float duration = 0.2f, float magnitude = 0.15f)
    {
        if (Instance == null) return;
        Instance.DoShake(duration, magnitude);
    }

    private void DoShake(float duration, float magnitude)
    {
        if (shakeCoroutine != null)
            StopCoroutine(shakeCoroutine);
        shakeCoroutine = StartCoroutine(ShakeRoutine(duration, magnitude));
    }

    private IEnumerator ShakeRoutine(float duration, float magnitude)
    {
        Camera cam = Camera.main;
        if (cam == null) yield break;

        Vector3 originalPos = cam.transform.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            // Magnitude fades linearly to zero over the duration
            float strength = Mathf.Lerp(magnitude, 0f, elapsed / duration);
            cam.transform.localPosition = originalPos + (Vector3)Random.insideUnitCircle * strength;
            elapsed += Time.deltaTime;
            yield return null;
        }

        cam.transform.localPosition = originalPos;
        shakeCoroutine = null;
    }
}
