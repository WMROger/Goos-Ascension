using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Attach to a "CodeFragmentPanel" GameObject in your scene (root level, disabled by default).
/// Supports two flows:
///   1. ShowPanel()            — called by CodeFragment on collection (unpauses on Got It)
///   2. ShowPanelFromSettings()— called by the Help button in Settings
///                               (hides Settings, restores it on Got It, keeps game paused)
/// </summary>
public class CodeFragmentPanel : MonoBehaviour
{
    [Header("Keybind Text")]
    [Tooltip("Assign a TMP Text — the keybind list is written automatically at runtime.")]
    [SerializeField] private TMP_Text keybindText;

    [Header("Settings Panel Reference")]
    [Tooltip("Drag the SettingsPanel GameObject here so it can be hidden/restored when using the Help button.")]
    [SerializeField] private GameObject settingsPanelGameObject;

    private bool openedFromSettings = false;

    // --- FIXED: Properly escaped quotation marks and clean TMP formatting ---
    private const string KEYBIND_CONTENT =
        "<align=\"center\"><b>— CONTROLS —</b></align>\n\n" +

        "<b>A / D</b><pos=45%>Move Left / Right\n" +
        "<b>Space</b><pos=45%>Jump\n" +
        "<b>Left Shift</b><pos=45%>Dash\n" +
        "<b>E</b><pos=45%>Transform  <size=70%>(Slime ↔ Human)</size>\n\n" +

        "<align=\"center\"><b>— COMBAT —</b></align>\n\n" +

        "<b>Q</b><pos=45%>Switch Weapon\n" +
        "<b>F</b><pos=45%>Parry\n" +
        "<b>Right Click</b><pos=45%>Block\n" +
        "<b>Left Click</b><pos=45%>Attack / Shoot\n" +
        "<b>Hold Click</b><pos=45%>Charge Attack\n\n" +

        "<align=\"center\"><b>— MENU —</b></align>\n\n" +

        "<b>Escape</b><pos=45%>Open Settings";
    // ----------------------------------------------------------------------


    private void OnEnable()
    {
        if (keybindText != null)
            keybindText.text = KEYBIND_CONTENT;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
    }

    // -------------------------------------------------------------------------
    // Flow 1 — CodeFragment collection
    // -------------------------------------------------------------------------

    /// <summary>
    /// Called by CodeFragment when the player picks it up.
    /// Pauses the game. Got It will unpause.
    /// </summary>
    public void ShowPanel()
    {
        openedFromSettings = false;
        PauseController.SetPause(true);
        gameObject.SetActive(true);
    }

    // -------------------------------------------------------------------------
    // Flow 2 — Help button inside Settings
    // -------------------------------------------------------------------------

    /// <summary>
    /// Called by the Help (?) button in the Settings menu.
    /// Hides Settings, shows this panel. Got It restores Settings (game stays paused).
    /// </summary>
    public void ShowPanelFromSettings()
    {
        openedFromSettings = true;

        if (settingsPanelGameObject != null)
            settingsPanelGameObject.SetActive(false);

        gameObject.SetActive(true);
    }

    // -------------------------------------------------------------------------
    // Got It
    // -------------------------------------------------------------------------

    /// <summary>
    /// Called by the "Got It!" button.
    /// </summary>
    public void OnGotItClicked()
    {
        gameObject.SetActive(false);

        if (openedFromSettings)
        {
            // Restore Settings — game stays paused, cursor stays visible
            if (settingsPanelGameObject != null)
                settingsPanelGameObject.SetActive(true);
        }
        else
        {
            // Opened from CodeFragment — resume normally
            PauseController.SetPause(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
        }
    }
}