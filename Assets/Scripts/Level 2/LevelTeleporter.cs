using UnityEngine;
using UnityEngine.SceneManagement; // Required for loading scenes!

public class LevelTeleporter : MonoBehaviour
{
    [Header("Teleporter Settings")]
    [Tooltip("Type the EXACT name of your Level 3 scene file here (e.g., 'Level3' or 'Level 3')")]
    public string nextLevelName = "Level3";

    private void OnTriggerEnter2D(Collider2D other)
    {
        // When something touches the teleporter, check if it's the player
        if (IsPlayerCollider(other))
        {
            Debug.Log("Player entered teleporter! Loading: " + nextLevelName);
            
            // Load the next level!
            SceneManager.LoadScene(nextLevelName);
        }
    }

    // Failsafe check to make sure it's the player (works for both slime and human forms)
    private bool IsPlayerCollider(Collider2D other)
    {
        if (other == null) return false;
        if (other.CompareTag("Player")) return true;
        
        // Check if a child hitbox touched it
        return other.GetComponentInParent<PlayerMovement>() != null;
    }
}