using UnityEngine;
using UnityEngine.Playables; // Required for Timeline!

public class IntroManager : MonoBehaviour
{
    [Header("Cutscene Connections")]
    public PlayableDirector director;
    
    [Header("Things to Lock/Unlock")]
    [Tooltip("Drag your Player object here")]
    public MonoBehaviour playerMovementScript; 
    [Tooltip("Drag your 'A D to Move' UI object here")]
    public GameObject tutorialUI;

    void Start()
    {
        // 1. As soon as the game starts, lock the player and hide the UI
        if (playerMovementScript != null) playerMovementScript.enabled = false;
        if (tutorialUI != null) tutorialUI.SetActive(false);

        // 2. Tell the Director to let us know when the cutscene is totally finished
        if (director != null)
        {
            director.stopped += OnCutsceneFinished;
        }
    }

    // 3. This runs the exact millisecond the Timeline ends!
    void OnCutsceneFinished(PlayableDirector pd)
    {
        // Give the player their controls back!
        if (playerMovementScript != null) playerMovementScript.enabled = true;
        
        // Make the "A D to Move" tutorial pop up over their head!
        if (tutorialUI != null) tutorialUI.SetActive(true);
    }
}