using UnityEngine;

public class PlayerAnimationEvents : MonoBehaviour
{

    private PlayerMovement player;
    private PlayerCombat playerCombat;

    private void Awake()
    {   
        player = GetComponentInParent<PlayerMovement>();
        playerCombat = GetComponentInParent<PlayerCombat>();
        
        if (playerCombat == null)
        {
            Debug.LogError("PlayerCombat not found in parent! Make sure PlayerCombat is on the same GameObject or parent.");
        }
    }

    public void DamageTarget()
    {
        if (playerCombat != null)
            playerCombat.DamageTarget();
    }

    public void ActivateSwordArc()
    {
        if (playerCombat != null)
            playerCombat.ActivateSwordArcFromAnimation();
    }

    public void ActivateChargedShot()
    {
        if (playerCombat != null)
            playerCombat.ActivateChargedShotFromAnimation();
    }

    // Optional: direct event name match for charged shot
    public void ActivateChargedShotFromAnimation()
    {
        if (playerCombat != null)
            playerCombat.ActivateChargedShotFromAnimation();
    }

    // Used by animation events that call "PlaySound()" for sword attacks
    public void PlaySound()
    {
        if (playerCombat != null)
            playerCombat.PlayAttackSound();
    }

    public void PlayAttackSound()
    {
        if (playerCombat != null)
            playerCombat.PlayAttackSound();
    }

    public void OnAttackAnimationEnd()
    {
        if (playerCombat != null)
            playerCombat.OnAttackAnimationEnd();
    }

    public void IncrementCombo()
    {
        if (playerCombat != null)
            playerCombat.IncrementCombo();
    }

    public void DisableMovementAndJump() => player.EnableMovementAndJump(false);    

    public void EnableMovementAndJump() => player.EnableMovementAndJump(true);

    
}
