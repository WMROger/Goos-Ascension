using UnityEngine;

/// <summary>
/// Simple static pause controller for managing game pause state
/// </summary>
public static class PauseController
{
    private static bool isGamePaused = false;
    
    /// <summary>
    /// Gets whether the game is currently paused
    /// </summary>
    public static bool IsGamePaused => isGamePaused;
    
    /// <summary>
    /// Sets the game pause state
    /// </summary>
    /// <param name="paused">True to pause the game, false to resume</param>
    public static void SetPause(bool paused)
    {
        isGamePaused = paused;
        // Use 0.0001f instead of 0f to allow UI interactions during pause
        Time.timeScale = paused ? 0.0001f : 1f;
        
        Debug.Log($"PauseController: Game {(paused ? "paused" : "unpaused")}, timeScale = {Time.timeScale}");
        
        // Optional: You can add audio pause/resume logic here if needed
        // AudioListener.pause = paused;
    }
}