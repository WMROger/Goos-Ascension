using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{

    public static SceneController instance;

    void Awake()
    {
        instance = this;
    }

    public void ChangeLevelTo(string levelName)
    {
        SceneManager.LoadScene(levelName);
    }

}
