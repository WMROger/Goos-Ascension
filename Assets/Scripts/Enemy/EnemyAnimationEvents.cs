using UnityEngine;

public class EnemyAnimationEvents : MonoBehaviour
{
    private EnemyAI enemyAI;

    private void Awake()
    {
        // Get parent, then search all children for EnemyAI
        Transform parent = transform.parent;
        if (parent != null)
        {
            enemyAI = parent.GetComponentInChildren<EnemyAI>();
        }
        
        if (enemyAI == null)
        {
            Debug.LogError("EnemyAI not found in parent's children! Make sure EnemyAI is a sibling of this object.");
        }
    }

    public void DamageTarget()
    {
        if (enemyAI != null)
            enemyAI.DamageTarget();
    }

    private void DisableMovement()
    {
        if (enemyAI != null)
            enemyAI.EnableMovementAndJump(false);
    }

    private void EnableMovement()
    {
        if (enemyAI != null)
            enemyAI.EnableMovementAndJump(true);
    }
}
