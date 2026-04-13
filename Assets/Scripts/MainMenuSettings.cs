using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Attach to a single "SettingsManager" GameObject in the MainMenu scene.
/// Wire up the AudioSource, UI elements, and music clip in the Inspector.
/// All settings are saved with PlayerPrefs so they persist between sessions.
/// </summary>
public class MainMenuSettings : MonoBehaviour
{
    [Header("Music")]
    [Tooltip("The AudioSource that plays the background music.")]
    [SerializeField] private AudioSource musicSource;
    [Tooltip("The music clip to play on the main menu.")]
    [SerializeField] private AudioClip menuMusicClip;

    [Header("Settings UI")]
    [Tooltip("Toggle for muting/unmuting the music. Label it 'Mute Music'.")]
    [SerializeField] private Toggle muteToggle;
    [Tooltip("Dropdown listing all available screen resolutions.")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [Tooltip("Toggle for switching between fullscreen and windowed mode.")]
    [SerializeField] private Toggle fullscreenToggle;

    // PlayerPrefs keys
    private const string KEY_MUTED      = "MusicMuted";
    private const string KEY_RESOLUTION = "ResolutionIndex";
    private const string KEY_FULLSCREEN = "Fullscreen";

    private Resolution[] availableResolutions;

    private void Start()
    {
        InitMusic();
        InitResolution();
        InitFullscreen();
    }

    // -------------------------------------------------------------------------
    // Music
    // -------------------------------------------------------------------------

    private void InitMusic()
    {
        if (musicSource == null) return;

        if (menuMusicClip != null)
        {
            musicSource.clip  = menuMusicClip;
            musicSource.loop  = true;
            musicSource.playOnAwake = false;
            musicSource.Play();
        }

        bool savedMute = PlayerPrefs.GetInt(KEY_MUTED, 0) == 1;
        musicSource.mute = savedMute;

        if (muteToggle != null)
        {
            muteToggle.isOn = savedMute;
            muteToggle.onValueChanged.AddListener(OnMuteChanged);
        }
    }

    public void OnMuteChanged(bool mute)
    {
        if (musicSource != null)
            musicSource.mute = mute;

        PlayerPrefs.SetInt(KEY_MUTED, mute ? 1 : 0);
        PlayerPrefs.Save();
    }

    // -------------------------------------------------------------------------
    // Resolution
    // -------------------------------------------------------------------------

    private void InitResolution()
    {
        if (resolutionDropdown == null) return;

        // Deduplicate resolutions that only differ in refresh rate
        var seen    = new HashSet<string>();
        var unique  = new List<Resolution>();
        foreach (Resolution r in Screen.resolutions)
        {
            string key = $"{r.width}x{r.height}";
            if (seen.Add(key))
                unique.Add(r);
        }
        availableResolutions = unique.ToArray();

        resolutionDropdown.ClearOptions();
        var options = new List<string>();

        int savedIndex   = PlayerPrefs.GetInt(KEY_RESOLUTION, -1);
        int currentIndex = 0;

        for (int i = 0; i < availableResolutions.Length; i++)
        {
            options.Add($"{availableResolutions[i].width} x {availableResolutions[i].height}");

            if (savedIndex < 0 &&
                availableResolutions[i].width  == Screen.currentResolution.width &&
                availableResolutions[i].height == Screen.currentResolution.height)
            {
                currentIndex = i;
            }
        }

        resolutionDropdown.AddOptions(options as List<string>);
            resolutionDropdown.value = savedIndex >= 0 ? savedIndex : currentIndex;
        resolutionDropdown.RefreshShownValue();
        resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
    }

    public void OnResolutionChanged(int index)
    {
        if (availableResolutions == null || index >= availableResolutions.Length) return;

        Resolution res = availableResolutions[index];
        Screen.SetResolution(res.width, res.height, Screen.fullScreen);

        PlayerPrefs.SetInt(KEY_RESOLUTION, index);
        PlayerPrefs.Save();
    }

    // -------------------------------------------------------------------------
    // Fullscreen / Windowed
    // -------------------------------------------------------------------------

    private void InitFullscreen()
    {
        if (fullscreenToggle == null) return;

        int defaultFullscreen = Screen.fullScreen ? 1 : 0;
        bool isFullscreen = PlayerPrefs.GetInt(KEY_FULLSCREEN, defaultFullscreen) == 1;

        Screen.fullScreen = isFullscreen;
        fullscreenToggle.isOn = isFullscreen;
        fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
    }

    public void OnFullscreenChanged(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;

        PlayerPrefs.SetInt(KEY_FULLSCREEN, isFullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }
}
