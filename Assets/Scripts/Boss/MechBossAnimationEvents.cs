using UnityEngine;

// Attach this to the animator child object (same object that has the Animator component).
// Then in each animation clip, add Animation Events that call these methods at the right frame.
public class MechBossAnimationEvents : MonoBehaviour
{
    private MechBossAI bossAI;

    private void Awake()
    {
        Transform parent = transform.parent;
        if (parent != null)
            bossAI = parent.GetComponentInChildren<MechBossAI>();

        if (bossAI == null)
            Debug.LogError("[MechBossAnimationEvents] MechBossAI not found! Make sure this script is on the animator child object.");
    }

    // Primary animation event — wire this in the melee attack clip at the hit frame
    public void DamageTarget()
    {
        if (bossAI != null) bossAI.DamageMelee();
    }

    // Legacy alias — keeps any existing wired DamageMelee events working
    public void DamageMelee()
    {
        if (bossAI != null) bossAI.DamageMelee();
    }

    // Call this at the fire frame in the laser attack animation
    public void FireLaser()
    {
        if (bossAI != null) bossAI.FireLaser();
    }

}
