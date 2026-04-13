using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SkillCooldownUI : MonoBehaviour
{
    [Header("Gameplay References")]
    [SerializeField] private PlayerCombat playerCombat;
    [SerializeField] private PlayerMovement playerMovement;

    [Header("Skill Icons")]
    [SerializeField] private Image slashSkillIcon;
    [SerializeField] private Image chargedShotSkillIcon;
    [SerializeField] private Image dashSkillIcon;

    [Header("Cooldown Fill Overlays")]
    [Tooltip("Overlay image for sword slash cooldown (filled while cooling down).")]
    [SerializeField] private Image slashCooldownFill;
    [Tooltip("Overlay image for charged shot cooldown (filled while cooling down).")]
    [SerializeField] private Image chargedShotCooldownFill;
    [Tooltip("Overlay image for dash cooldown (filled while cooling down).")]
    [SerializeField] private Image dashCooldownFill;
    [Tooltip("Overlay image for weapon switch cooldown (filled while cooling down).")]
    [SerializeField] private Image weaponSwitchCooldownFill;

    [Header("Weapon Swap Icons")]
    [Tooltip("Gun icon image for switch/swap UI state.")]
    [SerializeField] private Image switchToGunIcon;
    [Tooltip("Sword icon image for switch/swap UI state.")]
    [SerializeField] private Image switchToSwordIcon;

    [Header("Transformation Icons")]
    [Tooltip("Human icon image for transformation UI state.")]
    [SerializeField] private Image transformToHumanIcon;
    [Tooltip("Slime icon image for transformation UI state.")]
    [SerializeField] private Image transformToSlimeIcon;

    [Header("Swap Effect Settings")]
    [SerializeField] private bool bringActiveIconToFront = true;
    [SerializeField] private float activeIconAlpha = 1f;
    [SerializeField] private float inactiveIconAlpha = 0.35f;
    [SerializeField] private float swapPulseDuration = 0.18f;
    [SerializeField] private float swapPulseScale = 1.2f;

    [Header("Skill Availability")]
    [SerializeField] private Color humanSkillTint = Color.white;
    [SerializeField] private Color slimeDisabledSkillTint = new Color(0.45f, 0.45f, 0.45f, 1f);

    [Header("Weapon Skill Visibility")]
    [SerializeField] private bool hideInactiveWeaponSkillIcon = true;
    [SerializeField] private float inactiveWeaponSkillAlpha = 0f;

    [Header("Cooldown Overlay Setup")]
    [SerializeField] private bool forceFilledCooldownImages = true;

    private bool initializedWeaponState;
    private bool initializedFormState;
    private bool previousUsingGun;
    private bool previousIsHuman;

    private Coroutine weaponPulseRoutine;
    private Coroutine formPulseRoutine;

    private Color slashCooldownBaseColor = Color.white;
    private Color chargedShotCooldownBaseColor = Color.white;
    private Color dashCooldownBaseColor = Color.white;
    private Color weaponSwitchCooldownBaseColor = Color.white;

    private void Awake()
    {
        RefreshPlayerReferences();
    }

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
        // Wait a couple frames so the new Player exists and its Awake/Start has run.
        StartCoroutine(RebindNextFrames());
    }

    private System.Collections.IEnumerator RebindNextFrames()
    {
        yield return null;
        yield return null;
        RefreshPlayerReferences();
    }

    /// <summary>
    /// Called by UIConnector after each scene load to re-bind to the new Player.
    /// </summary>
    public void RefreshPlayerReferences()
    {
        playerCombat   = FindFirstObjectByType<PlayerCombat>();
        playerMovement = FindFirstObjectByType<PlayerMovement>();

        initializedWeaponState = false;
        initializedFormState   = false;
    }

    private void Start()
    {
        CacheCooldownBaseColors();

        ConfigureCooldownOverlay(slashCooldownFill);
        ConfigureCooldownOverlay(chargedShotCooldownFill);
        ConfigureCooldownOverlay(dashCooldownFill);
        ConfigureCooldownOverlay(weaponSwitchCooldownFill);

        SetFill(slashCooldownFill, 0f, 1f);
        SetFill(chargedShotCooldownFill, 0f, 1f);
        SetFill(dashCooldownFill, 0f, 1f);
        SetFill(weaponSwitchCooldownFill, 0f, 1f);

        if (playerCombat != null)
        {
            previousUsingGun = playerCombat.IsUsingGun;
            initializedWeaponState = true;
            ApplyWeaponState(previousUsingGun, false);
            ApplyWeaponSkillVisibility(previousUsingGun);
        }

        if (playerMovement != null)
        {
            previousIsHuman = playerMovement.IsHuman;
            initializedFormState = true;
            ApplyFormState(previousIsHuman, false);
            ApplySkillAvailability(previousIsHuman);
        }

        UpdateSkillCooldownOverlays();
    }

    private void Update()
    {
        if (playerCombat != null)
        {
            SetFill(
                slashCooldownFill,
                playerCombat.GetChargedSwordCooldownRemaining(),
                playerCombat.GetChargedSwordCooldownDuration());

            SetFill(
                chargedShotCooldownFill,
                playerCombat.GetChargedShotCooldownRemaining(),
                playerCombat.GetChargedShotCooldownDuration());

            // Weapon switch cooldown
            if (weaponSwitchCooldownFill != null)
            {
                SetFill(weaponSwitchCooldownFill, playerCombat.GetWeaponSwitchCooldownRemaining(), playerCombat.GetWeaponSwitchCooldownDuration());
            }
        }

        if (playerMovement != null)
        {
            SetFill(
                dashCooldownFill,
                playerMovement.GetDashCooldownRemaining(),
                playerMovement.GetDashCooldownDuration());
        }

        UpdateSkillCooldownOverlays();

        UpdateSwapStateVisuals();
    }

    private static void SetFill(Image fillImage, float remaining, float total)
    {
        if (fillImage == null)
            return;

        if (total <= 0f)
        {
            fillImage.fillAmount = 0f;
            return;
        }

        fillImage.fillAmount = Mathf.Clamp01(remaining / total);
    }

    private void UpdateSwapStateVisuals()
    {
        if (playerCombat != null)
        {
            bool usingGun = playerCombat.IsUsingGun;
            if (!initializedWeaponState || usingGun != previousUsingGun)
            {
                ApplyWeaponState(usingGun, initializedWeaponState);
                ApplyWeaponSkillVisibility(usingGun);
                previousUsingGun = usingGun;
                initializedWeaponState = true;
            }
        }

        if (playerMovement != null)
        {
            bool isHuman = playerMovement.IsHuman;
            if (!initializedFormState || isHuman != previousIsHuman)
            {
                ApplyFormState(isHuman, initializedFormState);
                ApplySkillAvailability(isHuman);
                previousIsHuman = isHuman;
                initializedFormState = true;
            }

            // Gray out switch weapon and transformation icons if not human
            Color unavailable = new Color(0.45f, 0.45f, 0.45f, 1f);
            Color available = Color.white;
            bool grayOut = !isHuman;

            // Switch weapon icons
            if (switchToGunIcon != null)
                SetTintPreserveAlpha(switchToGunIcon, grayOut ? unavailable : available);
            if (switchToSwordIcon != null)
                SetTintPreserveAlpha(switchToSwordIcon, grayOut ? unavailable : available);

            // Transformation icons: only gray out if code fragment not collected
            bool canTransform = false;
            if (playerMovement != null)
            {
                var type = playerMovement.GetType();
                var canTransformField = type.GetField("canTransform", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (canTransformField != null)
                    canTransform = (bool)canTransformField.GetValue(playerMovement);
            }
            bool grayOutTransform = !canTransform;
            if (transformToHumanIcon != null)
                SetTintPreserveAlpha(transformToHumanIcon, grayOutTransform ? unavailable : available);
            if (transformToSlimeIcon != null)
                SetTintPreserveAlpha(transformToSlimeIcon, grayOutTransform ? unavailable : available);
        }
    }

    private void ApplyWeaponState(bool usingGun, bool playPulse)
    {
        Image active = usingGun ? switchToGunIcon : switchToSwordIcon;
        Image inactive = usingGun ? switchToSwordIcon : switchToGunIcon;

        ApplyActiveInactiveVisuals(active, inactive);

        if (playPulse)
        {
            if (weaponPulseRoutine != null)
                StopCoroutine(weaponPulseRoutine);

            weaponPulseRoutine = StartCoroutine(PulseIcon(active));
        }
    }

    private void ApplyFormState(bool isHuman, bool playPulse)
    {
        Image active = isHuman ? transformToHumanIcon : transformToSlimeIcon;
        Image inactive = isHuman ? transformToSlimeIcon : transformToHumanIcon;

        ApplyActiveInactiveVisuals(active, inactive);

        if (playPulse)
        {
            if (formPulseRoutine != null)
                StopCoroutine(formPulseRoutine);

            formPulseRoutine = StartCoroutine(PulseIcon(active));
        }
    }

    private void ApplyWeaponSkillVisibility(bool usingGun)
    {
        Image activeSkillIcon = usingGun ? chargedShotSkillIcon : slashSkillIcon;
        Image inactiveSkillIcon = usingGun ? slashSkillIcon : chargedShotSkillIcon;

        if (hideInactiveWeaponSkillIcon)
        {
            SetAlpha(activeSkillIcon, activeIconAlpha);
            SetAlpha(inactiveSkillIcon, inactiveWeaponSkillAlpha);
        }
        else
        {
            SetAlpha(activeSkillIcon, activeIconAlpha);
            SetAlpha(inactiveSkillIcon, inactiveIconAlpha);
        }

        if (bringActiveIconToFront)
        {
            if (activeSkillIcon != null)
                activeSkillIcon.rectTransform.SetAsLastSibling();
        }
    }

    private void UpdateSkillCooldownOverlays()
    {
        if (playerCombat != null)
        {
            bool swordCoolingDown = playerCombat.GetChargedSwordCooldownRemaining() > 0.001f;
            bool shotCoolingDown = playerCombat.GetChargedShotCooldownRemaining() > 0.001f;

            UpdateCooldownOverlayVisual(slashCooldownFill, slashSkillIcon, swordCoolingDown);
            UpdateCooldownOverlayVisual(chargedShotCooldownFill, chargedShotSkillIcon, shotCoolingDown);

            // Weapon switch cooldown
            if (weaponSwitchCooldownFill != null)
            {
                bool switchingCoolingDown = playerCombat.GetWeaponSwitchCooldownRemaining() > 0.001f;
                UpdateCooldownOverlayVisual(weaponSwitchCooldownFill, null, switchingCoolingDown);
            }
        }

        if (playerMovement != null)
        {
            bool dashCoolingDown = playerMovement.GetDashCooldownRemaining() > 0.001f;
            UpdateCooldownOverlayVisual(dashCooldownFill, dashSkillIcon, dashCoolingDown);
        }
    }

    private void UpdateCooldownOverlayVisual(Image overlay, Image owningSkillIcon, bool isCoolingDown)
    {
        if (overlay == null)
            return;

        if (overlay == slashCooldownFill)
            SetRgb(overlay, slashCooldownBaseColor);
        else if (overlay == chargedShotCooldownFill)
            SetRgb(overlay, chargedShotCooldownBaseColor);
        else if (overlay == dashCooldownFill)
            SetRgb(overlay, dashCooldownBaseColor);

        bool iconVisible = owningSkillIcon == null || owningSkillIcon.color.a > 0.01f;
        float baseOverlayAlpha = GetBaseOverlayAlpha(overlay);
        float overlayAlpha = (isCoolingDown && iconVisible) ? baseOverlayAlpha * activeIconAlpha : 0f;
        SetAlpha(overlay, overlayAlpha);

        if (bringActiveIconToFront && isCoolingDown && iconVisible)
            overlay.rectTransform.SetAsLastSibling();
    }

    private float GetBaseOverlayAlpha(Image overlay)
    {
        if (overlay == slashCooldownFill)
            return slashCooldownBaseColor.a;

        if (overlay == chargedShotCooldownFill)
            return chargedShotCooldownBaseColor.a;

        if (overlay == dashCooldownFill)
            return dashCooldownBaseColor.a;

        if (overlay == weaponSwitchCooldownFill)
            return weaponSwitchCooldownBaseColor.a;

        return overlay != null ? overlay.color.a : 1f;
    }

    private void ConfigureCooldownOverlay(Image overlay)
    {
        if (!forceFilledCooldownImages || overlay == null)
            return;

        overlay.type = Image.Type.Filled;
        overlay.fillMethod = Image.FillMethod.Radial360;
        overlay.fillClockwise = false;
    }

    private void ApplySkillAvailability(bool isHuman)
    {
        Color targetTint = isHuman ? humanSkillTint : slimeDisabledSkillTint;

        SetTintPreserveAlpha(slashSkillIcon, targetTint);
        SetTintPreserveAlpha(chargedShotSkillIcon, targetTint);
        SetTintPreserveAlpha(dashSkillIcon, targetTint);
    }

    private void CacheCooldownBaseColors()
    {
        if (slashCooldownFill != null)
            slashCooldownBaseColor = slashCooldownFill.color;

        if (chargedShotCooldownFill != null)
            chargedShotCooldownBaseColor = chargedShotCooldownFill.color;

        if (dashCooldownFill != null)
            dashCooldownBaseColor = dashCooldownFill.color;

        if (weaponSwitchCooldownFill != null)
            weaponSwitchCooldownBaseColor = weaponSwitchCooldownFill.color;
    }

    private void ApplyActiveInactiveVisuals(Image active, Image inactive)
    {
        SetAlpha(active, activeIconAlpha);
        SetAlpha(inactive, inactiveIconAlpha);

        if (bringActiveIconToFront && active != null)
            active.rectTransform.SetAsLastSibling();
    }

    private static void SetAlpha(Image image, float alpha)
    {
        if (image == null)
            return;

        Color c = image.color;
        c.a = Mathf.Clamp01(alpha);
        image.color = c;
    }

    private static void SetTintPreserveAlpha(Image image, Color tint)
    {
        if (image == null)
            return;

        Color c = image.color;
        c.r = tint.r;
        c.g = tint.g;
        c.b = tint.b;
        image.color = c;
    }

    private static void SetRgb(Image image, Color source)
    {
        if (image == null)
            return;

        Color c = image.color;
        c.r = source.r;
        c.g = source.g;
        c.b = source.b;
        image.color = c;
    }

    private System.Collections.IEnumerator PulseIcon(Image image)
    {
        if (image == null || image.rectTransform == null)
            yield break;

        RectTransform iconTransform = image.rectTransform;
        Vector3 startScale = Vector3.one;
        Vector3 targetScale = Vector3.one * Mathf.Max(1f, swapPulseScale);

        float halfDuration = Mathf.Max(0.01f, swapPulseDuration * 0.5f);
        float t = 0f;

        while (t < halfDuration)
        {
            t += Time.unscaledDeltaTime;
            float normalized = Mathf.Clamp01(t / halfDuration);
            iconTransform.localScale = Vector3.Lerp(startScale, targetScale, normalized);
            yield return null;
        }

        t = 0f;
        while (t < halfDuration)
        {
            t += Time.unscaledDeltaTime;
            float normalized = Mathf.Clamp01(t / halfDuration);
            iconTransform.localScale = Vector3.Lerp(targetScale, startScale, normalized);
            yield return null;
        }

        iconTransform.localScale = startScale;
    }
}
