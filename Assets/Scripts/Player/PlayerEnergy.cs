    using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerEnergy : MonoBehaviour
{
    [Header("Energy Settings")]
    public float maxEnergy = 100f;
    public float currentEnergy;
    [SerializeField] private float energyRegenRate = 5f; // Passive regen over time

    [Header("Kill Rewards")]
    [SerializeField] private float energyGainOnKill = 15f; // Energy gained when player kills an enemy
    [SerializeField] private bool showKillEnergyGainDebug = true;

    [Header("UI References")]
    [Tooltip("The background (total) energy bar Image.")]
    public Image energyBarTotal;
    [Tooltip("The foreground (current) energy bar Image.")]
    public Image energyBarCurrent;

    private void Start()
    {
        currentEnergy = maxEnergy;
        // Fallback UI binding in case UIConnector/persistent wiring is missing on a scene reload.
        if (energyBarCurrent == null || energyBarTotal == null)
            AutoBindEnergyUI();
        UpdateUI();
    }

    private void AutoBindEnergyUI()
    {
        GameObject totalObj = GameObject.Find("EnergybarTotal");
        GameObject currentObj = GameObject.Find("EnergybarCurrent");

        if (energyBarTotal == null && totalObj != null)
            energyBarTotal = totalObj.GetComponent<Image>();

        if (energyBarCurrent == null && currentObj != null)
            energyBarCurrent = currentObj.GetComponent<Image>();
    }

    private void Update()
    {
        // Optional: Passive regeneration so the player isn't stuck forever
        if (currentEnergy < maxEnergy)
        {
            GainEnergy(energyRegenRate * Time.deltaTime);
        }
    }

    // Call this from PlayerMovement.cs when transforming
    public bool CanAffordTransform(float cost)
    {
        return currentEnergy >= cost;
    }

    public void SpendEnergy(float amount)
    {
        currentEnergy -= amount;
        currentEnergy = Mathf.Clamp(currentEnergy, 0, maxEnergy);
        UpdateUI();
    }

    public void GainEnergy(float amount)
    {
        currentEnergy += amount;
        currentEnergy = Mathf.Clamp(currentEnergy, 0, maxEnergy);
        UpdateUI();
    }

    public void UpdateUI()
    {
        if (energyBarCurrent != null)
            energyBarCurrent.fillAmount = currentEnergy / maxEnergy;
    }

    // Called when player kills an enemy
    public void OnEnemyKilled()
    {
        float previousEnergy = currentEnergy;
        GainEnergy(energyGainOnKill);

        if (showKillEnergyGainDebug)
        {
            Debug.Log($"⚡ ENERGY REWARD: Gained {energyGainOnKill} energy for enemy kill! Energy: {previousEnergy:F1} → {currentEnergy:F1}");
        }
    }
}