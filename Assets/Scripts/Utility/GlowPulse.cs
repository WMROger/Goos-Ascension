using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Attach to a Light2D to make it pulse like a living bioluminescent glow.
/// </summary>
public class GlowPulse : MonoBehaviour
{
    [Header("Pulse Settings")]
    [SerializeField] private float minIntensity = 4f;
    [SerializeField] private float maxIntensity = 6f;
    [SerializeField] private float pulseSpeed   = 1.4f;

    [Header("Flicker (optional — adds cave atmosphere)")]
    [SerializeField] private bool  enableFlicker       = true;
    [SerializeField] private float flickerStrength     = 0.08f;
    [SerializeField] private float flickerSpeed        = 12f;

    private Light2D light2D;
    private float   timeOffset;

    private void Awake()
    {
        light2D    = GetComponent<Light2D>();
        timeOffset = Random.Range(0f, Mathf.PI * 2f); // each light starts at a random phase
    }

    private void Update()
    {
        if (light2D == null) return;

        // Smooth sine-wave pulse
        float pulse = Mathf.Sin((Time.time + timeOffset) * pulseSpeed);
        float intensity = Mathf.Lerp(minIntensity, maxIntensity, (pulse + 1f) * 0.5f);

        // Optional high-frequency flicker on top
        if (enableFlicker)
        {
            float flicker = Mathf.PerlinNoise(Time.time * flickerSpeed, timeOffset) - 0.5f;
            intensity += flicker * flickerStrength;
        }

        light2D.intensity = intensity;
    }
}
