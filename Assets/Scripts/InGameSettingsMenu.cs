using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Attach to a "SettingsPanel" GameObject in the Level1 scene.
/// Press Escape to open/close. Controls music volume, game SFX volume,
/// save (PlayerPrefs), and quit to main menu.
/// </summary>
public class InGameSettingsMenu : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Singleton — keeps SettingsManager alive across scenes without needing
    // a separate PersistentCanvas component on this object.
    // -------------------------------------------------------------------------
    private static InGameSettingsMenu instance;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    [Header("Panel")]
    [Tooltip("The root panel GameObject to show/hide on Escape.")]
    [SerializeField] private GameObject settingsPanel;

    [Header("Audio Sources")]
    [Tooltip("The AudioSource playing background music.")]
    [SerializeField] private AudioSource musicSource;
    [Tooltip("The AudioSource used for game sound effects. Leave empty to control AudioListener volume instead.")]
    [SerializeField] private AudioSource sfxSource;

    [Header("Sliders")]
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider gameVolumeSlider;

    [Header("Main Menu Scene")]
    [Tooltip("Exact name of your main menu scene as listed in Build Settings.")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    // PlayerPrefs keys (shared with MainMenuSettings so volume persists)
    private const string KEY_MUSIC_VOL = "MusicVolume";
    private const string KEY_GAME_VOL  = "GameVolume";

    private bool isOpen = false;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// Finds "SettingsPanel" by name, including inactive GameObjects and
    /// objects that live in the DontDestroyOnLoad scene.
    /// Regular GameObject.Find() silently skips inactive objects.
    /// </summary>
    private static GameObject FindPanelByName(string panelName)
    {
        // Fast path: active objects (includes DontDestroyOnLoad scene)
        GameObject found = GameObject.Find(panelName);
        if (found != null) return found;

        // Slow path: search every loaded object including inactive ones
        foreach (GameObject go in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            // scene.IsValid() filters out prefab assets sitting in memory
            if (go.name == panelName && go.scene.IsValid())
                return go;
        }
        return null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // If we landed on the main menu, destroy this singleton so it
        // doesn't keep playing level music or intercepting Escape.
        if (scene.name == mainMenuSceneName)
        {
            instance = null;
            Destroy(gameObject);
            return;
        }

        // Re-find the settings panel in the new scene
        settingsPanel = FindPanelByName("SettingsPanel");
        if (settingsPanel != null)
        {
            Debug.Log("[InGameSettingsMenu] SettingsPanel re-found after scene load.");
            RebindSliders();
        }
        else
        {
            Debug.LogWarning("[InGameSettingsMenu] SettingsPanel not found in scene.");
        }

        RebindMusicSource();

        // Always close and reset when entering a new scene
        isOpen = false;
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        PauseController.SetPause(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }

    private void RebindMusicSource()
    {
        if (musicSource != null && musicSource.isPlaying) return;

        // The music AudioSource lives on the persistent Canvas.
        // Re-find it if the reference was lost or it stopped playing.
        if (musicSource == null)
        {
            PersistentCanvas pc = FindObjectOfType<PersistentCanvas>(true);
            if (pc != null)
                musicSource = pc.GetComponent<AudioSource>();
        }

        if (musicSource != null)
        {
            musicSource.loop = true;
            float savedVol = PlayerPrefs.GetFloat(KEY_MUSIC_VOL, 1f);
            musicSource.volume = savedVol;

            if (!musicSource.isPlaying)
                musicSource.Play();
        }
    }

    private void RebindSliders()
    {
        if (settingsPanel == null) return;

        Slider[] sliders = settingsPanel.GetComponentsInChildren<Slider>(true);
        musicVolumeSlider = null;
        gameVolumeSlider = null;

        foreach (Slider s in sliders)
        {
            string lowerName = s.gameObject.name.ToLower();
            if (lowerName.Contains("music"))
                musicVolumeSlider = s;
            else if (lowerName.Contains("game") || lowerName.Contains("sfx") || lowerName.Contains("volume"))
                gameVolumeSlider = s;
        }

        float savedMusic = PlayerPrefs.GetFloat(KEY_MUSIC_VOL, 1f);
        float savedGame  = PlayerPrefs.GetFloat(KEY_GAME_VOL,  1f);

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.onValueChanged.RemoveAllListeners();
            musicVolumeSlider.value = savedMusic;
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }

        if (gameVolumeSlider != null)
        {
            gameVolumeSlider.onValueChanged.RemoveAllListeners();
            gameVolumeSlider.value = savedGame;
            gameVolumeSlider.onValueChanged.AddListener(OnGameVolumeChanged);
        }

        ApplyMusicVolume(savedMusic);
        ApplyGameVolume(savedGame);
    }

    private void Start()
    {
        // Auto-find the panel by name if not assigned in the Inspector
        if (settingsPanel == null)
        {
            settingsPanel = FindPanelByName("SettingsPanel");
            if (settingsPanel == null)
                Debug.LogError("[InGameSettingsMenu] 'settingsPanel' is not assigned and no GameObject named 'SettingsPanel' was found. Escape key will not show the menu.");
            else
                Debug.Log("[InGameSettingsMenu] settingsPanel auto-found by name.");
        }

        // Load saved volumes
        float savedMusic = PlayerPrefs.GetFloat(KEY_MUSIC_VOL, 1f);
        float savedGame  = PlayerPrefs.GetFloat(KEY_GAME_VOL,  1f);

        // Apply to sources
        ApplyMusicVolume(savedMusic);
        ApplyGameVolume(savedGame);

        // Set sliders to saved values and hook up listeners
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.value = savedMusic;
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }

        if (gameVolumeSlider != null)
        {
            gameVolumeSlider.value = savedGame;
            gameVolumeSlider.onValueChanged.AddListener(OnGameVolumeChanged);
        }

        // Always start with the panel hidden
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            ToggleMenu();
    }

    // -------------------------------------------------------------------------
    // Menu open / close
    // -------------------------------------------------------------------------

    public void ToggleMenu()
    {
        isOpen = !isOpen;
        SetMenuOpen(isOpen);
    }

    private void SetMenuOpen(bool open)
    {
        isOpen = open;

        if (settingsPanel != null)
            settingsPanel.SetActive(open);

        PauseController.SetPause(open);

        // Cursor is usable while the menu is open
        Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible   = open;
    }

    // -------------------------------------------------------------------------
    // Volume
    // -------------------------------------------------------------------------

    private void OnMusicVolumeChanged(float value)
    {
        ApplyMusicVolume(value);
        PlayerPrefs.SetFloat(KEY_MUSIC_VOL, value);
        PlayerPrefs.Save();
    }

    private void OnGameVolumeChanged(float value)
    {
        ApplyGameVolume(value);
        PlayerPrefs.SetFloat(KEY_GAME_VOL, value);
        PlayerPrefs.Save();
    }

    private void ApplyMusicVolume(float value)
    {
        if (musicSource != null)
            musicSource.volume = value;
    }

    private void ApplyGameVolume(float value)
    {
        if (sfxSource != null)
            sfxSource.volume = value;
        else
            AudioListener.volume = value; // fallback: controls all audio
    }

    // -------------------------------------------------------------------------
    // Buttons
    // -------------------------------------------------------------------------

    /// <summary>
    /// Called by the Save button. Writes current settings to PlayerPrefs.
    /// </summary>
    public void OnSaveClicked()
    {
        PlayerPrefs.SetFloat(KEY_MUSIC_VOL, musicVolumeSlider != null ? musicVolumeSlider.value : 1f);
        PlayerPrefs.SetFloat(KEY_GAME_VOL,  gameVolumeSlider  != null ? gameVolumeSlider.value  : 1f);
        PlayerPrefs.Save();
        Debug.Log("[InGameSettingsMenu] Settings saved.");
    }

    /// <summary>
    /// Called by the Quit button. Resumes time then loads the main menu.
    /// </summary>
    public void OnQuitClicked()
    {
        if (musicSource != null)
            musicSource.Stop();

        PauseController.SetPause(false);
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    /// <summary>
    /// Called by a Resume / Close button inside the panel.
    /// </summary>
    public void OnResumeClicked()
    {
        SetMenuOpen(false);
    }
}
