using UnityEngine;
using UnityEngine.SceneManagement;

public class PersistentCanvas : MonoBehaviour
{
    private static PersistentCanvas instance;

    [SerializeField] private string mainMenuSceneName = "MainMenu";

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
        if (scene.name == mainMenuSceneName)
        {
            instance = null;
            Destroy(gameObject);
        }
    }
}
