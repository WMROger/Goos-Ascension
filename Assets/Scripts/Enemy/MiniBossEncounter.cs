using UnityEngine;

public class MiniBossEncounter : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (KillQuestManager.Instance != null)
            {
                KillQuestManager.Instance.EncounterMiniBoss();
                Destroy(gameObject); // Deletes itself so it only triggers once!
            }
        }
    }
}