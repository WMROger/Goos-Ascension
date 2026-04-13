using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

public class BossCutsceneTrigger : MonoBehaviour
{
    [Header("References")]
    public GameObject player;
    public BigRedBossAI boss;
    public CinemachineCamera bossCam;
    public CinemachineCamera playerCam;
    public float bossAnimDuration = 2.0f;

    private bool cutscenePlayed = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!cutscenePlayed && other.CompareTag("Player"))
        {
            cutscenePlayed = true;
            player = other.gameObject;
            StartCoroutine(PlayBossCutscene());
        }
    }

    private IEnumerator PlayBossCutscene()
    {
        // 1. Disable player input
        var playerMovement = player.GetComponent<PlayerMovement>();
        if (playerMovement != null)
            playerMovement.enabled = false;

        // 2. Switch camera to boss
        if (bossCam != null && playerCam != null)
        {
            bossCam.Priority = 30;
            playerCam.Priority = 10;
            Debug.Log("[Cutscene] Switched to BossCam (Priority 30)");
        }
        yield return new WaitForSeconds(0.5f);

        // 3. Show dialogue
        if (DialogueManager.Instance != null)
            yield return DialogueManager.Instance.ShowDialogue("TARGET ACQUIRED. EXTERMINATE ALL... [glitches] ...Goo, please... run away... I can't stop my hands...");
        else
        {
            Debug.Log("BigRedBoss: So, you’ve finally made it this far... Foolish mortal! Witness my true power!");
            yield return new WaitForSeconds(2.5f);
        }

        // 4. Play Skill2 animation
        if (boss != null)
            boss.PlaySkill2CutsceneAnimation(bossAnimDuration);

        // 5. Camera shake or pulse (optional, add your effect here)
        // ...
        yield return new WaitForSeconds(bossAnimDuration);

        // 6. (Optional) Cut to player for reaction
        // ...

        // 7. Switch camera back to player
        if (bossCam != null && playerCam != null)
        {
            bossCam.Priority = 10;
            playerCam.Priority = 30;
            Debug.Log("[Cutscene] Switched to PlayerCam (Priority 30)");
        }
        yield return new WaitForSeconds(0.2f);

        // 8. Re-enable player input
        if (playerMovement != null)
            playerMovement.enabled = true;

        // 9. Start boss fight music (optional)
        // ...

        // 10. Disable trigger so it doesn't repeat
        gameObject.SetActive(false);
    }
}
