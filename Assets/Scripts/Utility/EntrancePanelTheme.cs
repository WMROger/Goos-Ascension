using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Attach to each entrance UI panel (PromptPanel, EnemyWarningPanel, ConfirmationPanel).
/// Call Apply() at runtime, or it runs automatically in Start().
/// Paints a dark-fantasy cave look that matches the game's aesthetic.
/// </summary>
public class EntrancePanelTheme : MonoBehaviour
{
    public enum PanelType { Prompt, Warning, Confirmation }

    [SerializeField] private PanelType panelType = PanelType.Confirmation;

    // ── Shared palette ───────────────────────────────────────────────────────
    static readonly Color BG_DARK       = new Color(0.05f, 0.03f, 0.02f, 0.93f); // near-black parchment
    static readonly Color BORDER_AMBER  = new Color(0.55f, 0.40f, 0.05f, 1f);    // antique gold border
    static readonly Color TEXT_GOLD     = new Color(1.00f, 0.85f, 0.40f, 1f);    // title gold
    static readonly Color TEXT_CREAM    = new Color(0.91f, 0.84f, 0.64f, 1f);    // body cream
    static readonly Color TEXT_WARNING  = new Color(1.00f, 0.42f, 0.21f, 1f);    // warning orange-red
    static readonly Color BTN_YES_NRM   = new Color(0.11f, 0.27f, 0.20f, 1f);    // dark teal-green
    static readonly Color BTN_YES_HOV   = new Color(0.17f, 0.42f, 0.31f, 1f);
    static readonly Color BTN_YES_PRE   = new Color(0.07f, 0.18f, 0.13f, 1f);
    static readonly Color BTN_NO_NRM    = new Color(0.27f, 0.08f, 0.08f, 1f);    // dark crimson
    static readonly Color BTN_NO_HOV    = new Color(0.42f, 0.13f, 0.13f, 1f);
    static readonly Color BTN_NO_PRE    = new Color(0.18f, 0.05f, 0.05f, 1f);
    static readonly Color BTN_TEXT_YES  = new Color(0.59f, 0.84f, 0.70f, 1f);    // soft mint
    static readonly Color BTN_TEXT_NO   = new Color(1.00f, 0.42f, 0.42f, 1f);    // soft red
    static readonly Color PROMPT_BG     = new Color(0.04f, 0.04f, 0.10f, 0.85f); // dark blue-black

    private void Start() => Apply();

    public void Apply()
    {
        switch (panelType)
        {
            case PanelType.Prompt:       StylePrompt();       break;
            case PanelType.Warning:      StyleWarning();      break;
            case PanelType.Confirmation: StyleConfirmation(); break;
        }
    }

    // ── Prompt ───────────────────────────────────────────────────────────────

    private void StylePrompt()
    {
        SetBG(gameObject, PROMPT_BG, 12f);

        TMP_Text label = GetComponentInChildren<TMP_Text>();
        if (label != null)
        {
            label.color     = TEXT_GOLD;
            label.fontSize  = 22f;
            label.fontStyle = FontStyles.Bold;
            SetOutline(label, Color.black, 0.3f);
        }
    }

    // ── Enemy Warning ────────────────────────────────────────────────────────

    private void StyleWarning()
    {
        Color warningBG = new Color(0.25f, 0.05f, 0.02f, 0.92f);
        SetBG(gameObject, warningBG, 8f);

        TMP_Text label = GetComponentInChildren<TMP_Text>();
        if (label != null)
        {
            label.color     = TEXT_WARNING;
            label.fontSize  = 26f;
            label.fontStyle = FontStyles.Bold;
            SetOutline(label, Color.black, 0.35f);
        }
    }

    // ── Confirmation ─────────────────────────────────────────────────────────

    private void StyleConfirmation()
    {
        // Panel background
        SetBG(gameObject, BG_DARK, 16f);

        // Add an inner amber border image if there is a direct Image child named "Border"
        // (works even without a special sprite by tinting a child Image)
        foreach (Transform child in transform)
        {
            if (child.name.ToLower().Contains("border"))
            {
                Image border = child.GetComponent<Image>();
                if (border != null) border.color = BORDER_AMBER;
            }
        }

        // Style every TMP_Text child
        foreach (TMP_Text t in GetComponentsInChildren<TMP_Text>())
        {
            bool isTitle = t.name.ToLower().Contains("title") ||
                           t.fontSize >= 30f;

            t.color     = isTitle ? TEXT_GOLD : TEXT_CREAM;
            t.fontSize  = isTitle ? 32f : 22f;
            t.fontStyle = isTitle ? FontStyles.Bold : FontStyles.Normal;
            t.alignment = TextAlignmentOptions.Center;
            SetOutline(t, Color.black, isTitle ? 0.4f : 0.25f);
        }

        // Style buttons
        Button[] buttons = GetComponentsInChildren<Button>();
        foreach (Button btn in buttons)
        {
            bool isYes = btn.name.ToLower().Contains("yes") ||
                         btn.name.ToLower().Contains("confirm");

            StyleButton(btn,
                isYes ? BTN_YES_NRM : BTN_NO_NRM,
                isYes ? BTN_YES_HOV : BTN_NO_HOV,
                isYes ? BTN_YES_PRE : BTN_NO_PRE,
                isYes ? BTN_TEXT_YES : BTN_TEXT_NO,
                isYes ? "Continue" : "Stay Here");
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void SetBG(GameObject go, Color color, float cornerRadius)
    {
        Image img = go.GetComponent<Image>();
        if (img == null) img = go.AddComponent<Image>();
        img.color = color;
        img.type  = Image.Type.Sliced;
    }

    private void StyleButton(Button btn, Color normal, Color hover, Color pressed,
                              Color textColor, string label)
    {
        // Background
        Image img = btn.GetComponent<Image>();
        if (img != null) img.color = normal;

        // Transition colours
        ColorBlock cb      = btn.colors;
        cb.normalColor     = normal;
        cb.highlightedColor = hover;
        cb.pressedColor    = pressed;
        cb.selectedColor   = hover;
        cb.fadeDuration    = 0.12f;
        btn.colors         = cb;

        // Label
        TMP_Text txt = btn.GetComponentInChildren<TMP_Text>();
        if (txt != null)
        {
            txt.text      = label;
            txt.color     = textColor;
            txt.fontSize  = 22f;
            txt.fontStyle = FontStyles.Bold;
            txt.alignment = TextAlignmentOptions.Center;
            SetOutline(txt, Color.black, 0.3f);
        }
    }

    private void SetOutline(TMP_Text t, Color color, float thickness)
    {
        t.outlineColor     = color;
        t.outlineWidth     = thickness;
    }
}
