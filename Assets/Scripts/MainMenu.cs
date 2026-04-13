using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public class MainMenu : MonoBehaviour
{
    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 1f;
    }

    [Header("UI References")]
    [Tooltip("Drag your MainMenu container here so it disappears!")]
    public GameObject mainMenuUI; 
    public GameObject BGImage; 

    public GameObject loadingScreen;
    public Slider slider;

    [Header("Audio")]
    [Tooltip("Drag the object playing your Main Menu music here!")]
    public AudioSource mainMenuMusic; 

    [Header("Video Settings")]
    public VideoPlayer VideoRenderTexture; 
    
    [Tooltip("Used ONLY for the visual slider fill speed now.")]
    public float minimumLoadingTime = 8f; 

    private bool isVideoFinished = false;

    public void PlayGame()
    {
        // 1. CRITICAL FIX: Ensure the game isn't secretly paused from a previous session!
        Time.timeScale = 1f; 

        PlayerPrefs.DeleteKey("TransformUnlocked");
        PlayerPrefs.DeleteKey("PlayerIsHuman");
        PlayerPrefs.Save();

        // 2. Start loading the next scene in the Build Settings
        int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;
        Debug.Log("Attempting to load Scene Index: " + nextSceneIndex);
        StartCoroutine(LoadLevelAsync(nextSceneIndex));
    }

    IEnumerator LoadLevelAsync(int sceneIndex)
    {
        // REMOVED the SetActive(false) lines so the Coroutine stays alive!
        
        if (mainMenuMusic != null) mainMenuMusic.Stop(); // Keeping this so the music still stops!

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneIndex);
        // ... rest of the code stays exactly the same
        
        if (operation == null)
        {
            Debug.LogError("CRITICAL ERROR: SceneManager could not find Scene Index " + sceneIndex + "! Is it in your Build Settings?");
            yield break; // Stop the coroutine so it doesn't crash
        }

        operation.allowSceneActivation = false;
        loadingScreen.SetActive(true);

        isVideoFinished = false;

        if (VideoRenderTexture != null)
        {
            VideoRenderTexture.loopPointReached += OnVideoFinished;
            // Removed .Prepare() as .Play() automatically handles it and prevents sync bugs
            VideoRenderTexture.Play(); 
        }
        else
        {
            isVideoFinished = true; 
        }

        float elapsedTime = 0f; 
        float safeSliderTime = minimumLoadingTime > 0.1f ? minimumLoadingTime : 8f;

        while (!operation.isDone)
        {
            // CRITICAL FIX: Use unscaledDeltaTime so the timer runs even if the game glitches and thinks it's paused
            elapsedTime += Time.unscaledDeltaTime; 
            slider.value = Mathf.Clamp01(elapsedTime / safeSliderTime);

            if (operation.progress >= 0.9f)
            {
                bool videoStoppedPlaying = VideoRenderTexture != null && !VideoRenderTexture.isPlaying && elapsedTime > 2f;

                if (isVideoFinished || videoStoppedPlaying)
                {
                    Debug.Log("Video finished! Activating Scene...");
                    operation.allowSceneActivation = true;
                }
            }

            yield return null;
        }
    }

    void OnVideoFinished(VideoPlayer vp)
    {
        Debug.Log("Video event 'loopPointReached' fired!");
        isVideoFinished = true;
        vp.loopPointReached -= OnVideoFinished; 
    }

    public void GoToSettingsMenu()
    {
        Time.timeScale = 1f; // Always unpause when switching scenes
        SceneManager.LoadScene("SettingsMenu");
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f; // Always unpause when switching scenes
        SceneManager.LoadScene("MainMenu");
    }

    public void QuitGame()
    {
        Debug.Log("Quit!");
        Application.Quit();
    }
}