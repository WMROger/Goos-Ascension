using UnityEngine;

public class AttackPointTrigger : MonoBehaviour
{
    private EnemyAI enemyAI;
    private bool playerInZone = false;

    private void Awake()
    {
        // Find EnemyAI in the parent's children
        Transform parent = transform.parent;
        if (parent != null)
        {
            enemyAI = parent.GetComponentInChildren<EnemyAI>();
        }

        if (enemyAI == null)
        {
            Debug.LogError("EnemyAI not found! Make sure this AttackPoint is a child of the enemy parent.");
        }
    }

    private void Update()
    {
        // Keep attacking while player is in zone
        if (playerInZone && enemyAI != null)
        {
            enemyAI.TriggerAttack();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Check if the player entered the attack zone
        if (collision.GetComponentInParent<PlayerMovement>() != null)
        {
            playerInZone = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        // Check if the player left the attack zone
        if (collision.GetComponentInParent<PlayerMovement>() != null)
        {
            playerInZone = false;
        }
    }

    public bool IsPlayerInZone()
    {
        return playerInZone;
    }
}
