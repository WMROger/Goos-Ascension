using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class EndCredits : MonoBehaviour
{
    [Header("Credits Content")]
    [SerializeField] private string gameTitle = "GOO ASCENSION";
    [SerializeField] private string[] creditEntries = new string[]
    {
        "CREATED BY",
        "Arl Jacob Necesario",
        "Ethan Gabriel Rolloque",
        "Luthar James Jimenez",
        "",
        "GAME DIRECTOR",
        "Luthar James Jimenez",
        "",
        "LEAD PROGRAMMER",
        "Ethan Gabriel Rolloque",
        "",
        "ASSISTANT PROGRAMMER",
        "Luthar James Jimenez",
        "",
        "LEVEL DESIGN",
        "Luthar James Jimenez",
        "Ethan Gabriel Rolloque",
        "",
        "ART & ANIMATION",
        "Arl Jacob Necesario",
        "",
        "SOUND & MUSIC",
        "Ethan Gabriel Rolloque",
        "Arl Jacob Necesario",
        "",
        "ASSET ARTIST",
        "Arl Jacob Necesario",
        "",
        "ASSETS",
        "brullov - Oak Woods Tileset",
        "nylmoth - Sunken Caves Tileset",
        "Various artists on itch.io",
        "",
        "SOUND EFFECTS",
        "Various artists on itch.io",
        "",
        "MUSIC",
        "The Voice in My Heart - Evan Call",
        "Time Flows Ever Onward - Evan Call",
        "",
        "SPECIAL THANKS",
        "You, the player!",
        "",
        "",
        "THANK YOU FOR PLAYING!"
    };

    [Header("Scroll Settings")]
    [SerializeField] private float scrollSpeed = 50f;
    [SerializeField] private float pauseBeforeScroll = 2f;
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private Canvas canvas;
    private RectTransform creditsContainer;
    private bool isScrolling;

    private void OnEnable()
    {
        BuildCreditsUI();
        StartCoroutine(ScrollCredits());
    }

    private void BuildCreditsUI()
    {
        var canvasGO = new GameObject("CreditsCanvas");
        canvasGO.transform.SetParent(transform, false);
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        var bgGO = new GameObject("Background");
        bgGO.transform.SetParent(canvasGO.transform, false);
        var bgRT = bgGO.AddComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.color = Color.black;

        var containerGO = new GameObject("CreditsContainer");
        containerGO.transform.SetParent(canvasGO.transform, false);
        creditsContainer = containerGO.AddComponent<RectTransform>();
        creditsContainer.anchorMin = new Vector2(0.5f, 0f);
        creditsContainer.anchorMax = new Vector2(0.5f, 0f);
        creditsContainer.pivot = new Vector2(0.5f, 1f);
        creditsContainer.sizeDelta = new Vector2(800f, 0f);
        creditsContainer.anchoredPosition = new Vector2(0f, -50f);

        var layout = containerGO.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.spacing = 8f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        var fitter = containerGO.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/lithosbold SDF");

        AddCreditLine(gameTitle, font, 72f, Color.white, 60f);

        foreach (string entry in creditEntries)
        {
            if (string.IsNullOrEmpty(entry))
            {
                AddSpacer(30f);
            }
            else if (entry == entry.ToUpper())
            {
                AddCreditLine(entry, font, 28f, new Color(0.6f, 0.8f, 1f), 20f);
            }
            else
            {
                AddCreditLine(entry, font, 36f, Color.white, 10f);
            }
        }

        AddSpacer(200f);
    }

    private void AddCreditLine(string text, TMP_FontAsset font, float fontSize, Color color, float bottomPadding)
    {
        var lineGO = new GameObject("CreditLine");
        lineGO.transform.SetParent(creditsContainer, false);

        var tmp = lineGO.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        if (font != null) tmp.font = font;

        var le = lineGO.AddComponent<LayoutElement>();
        le.preferredHeight = fontSize + bottomPadding;
    }

    private void AddSpacer(float height)
    {
        var spacer = new GameObject("Spacer");
        spacer.transform.SetParent(creditsContainer, false);
        spacer.AddComponent<RectTransform>();
        var le = spacer.AddComponent<LayoutElement>();
        le.preferredHeight = height;
    }

    private void DisableGameBehindCredits()
    {
        // Disable player input
        PlayerMovement pm = FindObjectOfType<PlayerMovement>(true);
        if (pm != null) pm.enabled = false;

        PlayerCombat pc = FindObjectOfType<PlayerCombat>(true);
        if (pc != null) pc.enabled = false;

        // Disable settings menu so Escape doesn't open it
        InGameSettingsMenu settings = FindObjectOfType<InGameSettingsMenu>(true);
        if (settings != null) settings.enabled = false;

        // Freeze the game world but use unscaledTime for credits scrolling
        Time.timeScale = 0f;
    }

    private IEnumerator ScrollCredits()
    {
        DisableGameBehindCredits();

        yield return new WaitForSecondsRealtime(pauseBeforeScroll);

        isScrolling = true;
        float normalSpeed = scrollSpeed;
        float currentSpeed = scrollSpeed;
        bool isFast = false;
        float totalHeight = creditsContainer.rect.height + 1200f;
        float scrolled = 0f;

        while (scrolled < totalHeight)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                break;
            }

            if (Input.GetKeyDown(KeyCode.Space) && scrolled > 100f)
            {
                isFast = !isFast;
                currentSpeed = isFast ? normalSpeed * 3f : normalSpeed;
            }

            float delta = currentSpeed * Time.unscaledDeltaTime;
            creditsContainer.anchoredPosition += new Vector2(0f, delta);
            scrolled += delta;
            yield return null;
        }

        isScrolling = false;
        yield return new WaitForSecondsRealtime(1f);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 1f;

        Destroy(gameObject);
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
