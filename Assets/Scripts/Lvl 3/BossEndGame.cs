using UnityEngine;

public class BossEndGame : MonoBehaviour
{
    [Tooltip("Drag your giant End Credits Canvas or Panel here")]
    public GameObject creditsPanel;

    private void Update()
    {
        // For tomorrow's exhibit, if you don't have a health script for the boss yet,
        // you can just press 'P' on the keyboard to instantly "kill" it and show the credits!
        if (Input.GetKeyDown(KeyCode.P))
        {
            TriggerCredits();
        }
    }

    // Call this method from your Boss's health script when its health hits 0!
    public void TriggerCredits()
    {
        if (creditsPanel != null)
        {
            creditsPanel.SetActive(true);
            
            // Optional: Destroy the boss so it disappears
            Destroy(gameObject); 
        }
    }
}