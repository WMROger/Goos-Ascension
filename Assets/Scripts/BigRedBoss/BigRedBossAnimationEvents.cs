using UnityEngine;

public class BigRedBossAnimationEvents : MonoBehaviour
{
    private BigRedBossAI enemyAI;

    private void Awake()
    {
        // Get parent, then search all children for BigRedBossAI
        Transform parent = transform.parent;
        if (parent != null)
        {
            enemyAI = parent.GetComponentInChildren<BigRedBossAI>();
        }
        
        if (enemyAI == null)
        {
            Debug.LogError("BigRedBossAI not found in parent's children! Make sure BigRedBossAI is a sibling of this object.");
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
