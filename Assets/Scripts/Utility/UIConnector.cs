using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Attach to the persistent Canvas.
/// Every time a new scene loads, this finds the new Player and rewires
/// all UI references (health bar, energy slider) automatically.
/// </summary>
public class UIConnector : MonoBehaviour
{
    [Header("Health Bar")]
    [Tooltip("The 'total' health bar background Image inside this Canvas.")]
    [SerializeField] private Image healthBarTotal;
    [Tooltip("The 'current' health bar fill Image inside this Canvas.")]
    [SerializeField] private Image healthBarCurrent;

    [Header("Energy Bar")]
    [Tooltip("The 'total' energy bar background Image inside this Canvas.")]
    [SerializeField] private Image energyBarTotal;
    [Tooltip("The 'current' energy bar fill Image inside this Canvas.")]
    [SerializeField] private Image energyBarCurrent;

    [Header("Skill Icons")]
    [Tooltip("The SkillCooldownUI component inside this Canvas.")]
    [SerializeField] private SkillCooldownUI skillCooldownUI;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Wait one frame so all scene objects have finished their Awake/Start
        StartCoroutine(RewireNextFrame());
    }

    private System.Collections.IEnumerator RewireNextFrame()
    {
        // Wait two frames to ensure all scene objects have fully initialized
        yield return null;
        yield return null;

        // Rewire PlayerHealth — search entire scene, not just by tag hierarchy
        PlayerHealth health = FindFirstObjectByType<PlayerHealth>();
        if (health != null)
        {
            health.healthBarTotal   = healthBarTotal;
            health.healthBarCurrent = healthBarCurrent;
            health.UpdateUI();
            Debug.Log($"[UIConnector] Health bar rewired to '{health.gameObject.name}'.");
        }
        else
        {
            Debug.LogWarning("[UIConnector] PlayerHealth not found in scene.");
        }

        // Rewire PlayerEnergy
        PlayerEnergy energy = FindFirstObjectByType<PlayerEnergy>();
        if (energy != null)
        {
            energy.energyBarTotal = energyBarTotal;
            energy.energyBarCurrent = energyBarCurrent;
            energy.UpdateUI();
            Debug.Log($"[UIConnector] Energy bar rewired to '{energy.gameObject.name}'.");
        }
        else
        {
            Debug.LogWarning("[UIConnector] PlayerEnergy not found in scene.");
        }

        // Rewire SkillCooldownUI
        if (skillCooldownUI != null)
        {
            skillCooldownUI.RefreshPlayerReferences();
            Debug.Log("[UIConnector] SkillCooldownUI rewired to new Player.");
        }
    }
}
