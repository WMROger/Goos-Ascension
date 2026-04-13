using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public enum QuestType { KillMobs, FindLever }

public class KillQuestManager : MonoBehaviour
{
    public static KillQuestManager Instance { get; private set; }

    [Header("Quest Settings")]
    public QuestType currentQuest = QuestType.KillMobs;
    
    public bool isQuestActive = false;
    private bool isQuestComplete = false;

    // --- NEW: Boss States ---
    private bool miniBossEncountered = false;
    private bool miniBossDefeated = false;

    [Header("Kill Quest Targets (Level 1)")]
    public int targetSlimes = 1;
    public int targetSentinels = 2;

    private int slimesKilled = 0;
    private int sentinelsKilled = 0;

    [Header("UI References (Leave Empty in Level 2!)")]
    public TMP_Text questText;
    public GameObject questUIPanel;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (questUIPanel == null || questText == null)
        {
            ReconnectToPersistentUI();
        }
    }

    private void ReconnectToPersistentUI()
    {
        TMP_Text[] allTexts = Resources.FindObjectsOfTypeAll<TMP_Text>();
        foreach (TMP_Text t in allTexts)
        {
            if (t.gameObject.scene.name != null && t.gameObject.name == "KillCount")
            {
                questText = t;
                questUIPanel = t.transform.parent.gameObject; 
                break;
            }
        }
    }

    private void Start()
    {
        if (currentQuest == QuestType.FindLever)
        {
            isQuestActive = true;
            if (questUIPanel != null) questUIPanel.SetActive(true);
            UpdateQuestUI();
        }
        else 
        {
            if (questUIPanel != null) questUIPanel.SetActive(false);
            if (questText != null) questText.text = "";
        }
    }

    public void StartQuest()
    {
        if (currentQuest == QuestType.KillMobs)
        {
            isQuestActive = true;
            isQuestComplete = false;
            slimesKilled = 0;
            sentinelsKilled = 0;

            if (questUIPanel != null) questUIPanel.SetActive(true);
            UpdateQuestUI();
        }
    }

    public void OnEnemyKilled(string enemyType)
    {
        if (!isQuestActive || isQuestComplete || currentQuest != QuestType.KillMobs) return;

        if (enemyType == "Slime" && slimesKilled < targetSlimes) slimesKilled++;
        else if (enemyType == "Sentinel" && sentinelsKilled < targetSentinels) sentinelsKilled++;

        CheckQuestCompletion();
        UpdateQuestUI();
    }

    private void CheckQuestCompletion()
    {
        if (slimesKilled >= targetSlimes && sentinelsKilled >= targetSentinels)
        {
            isQuestComplete = true;
        }
    }

    // --- NEW: Mini Boss Methods ---
    public void EncounterMiniBoss()
    {
        if (currentQuest == QuestType.FindLever && !isQuestComplete && !miniBossDefeated)
        {
            miniBossEncountered = true;
            UpdateQuestUI();
        }
    }

    public void DefeatMiniBoss()
    {
        if (currentQuest == QuestType.FindLever && !isQuestComplete)
        {
            miniBossDefeated = true;
            UpdateQuestUI();
        }
    }
    // ------------------------------

    public void CompleteLeverQuest()
    {
        if (currentQuest == QuestType.FindLever && !isQuestComplete)
        {
            isQuestComplete = true;
            
            if (questText != null)
                questText.text = "<b>— QUEST —</b>\nHidden room unlocked! Enter the teleporter.";
        }
    }

    private void UpdateQuestUI()
    {
        if (questText == null) return;

        if (currentQuest == QuestType.FindLever)
        {
            // Update text based on boss status!
            if (miniBossDefeated)
                questText.text = "<b>— QUEST —</b>\nPull the lever to unlock the hidden room.";
            else if (miniBossEncountered)
                questText.text = "<b>— QUEST —</b>\nDefeat the mini boss to proceed.";
            else
                questText.text = "<b>— QUEST —</b>\nFind the lever at the bottom to unlock the hidden room.";
        }
        else if (currentQuest == QuestType.KillMobs)
        {
            if (isQuestComplete)
                questText.text = "<b>— QUEST —</b>\nProceed to next level";
            else
                questText.text = $"<b>— QUEST —</b>\n" +
                                 $"Kill Slime: {slimesKilled} / {targetSlimes}\n" +
                                 $"Kill Sentinel: {sentinelsKilled} / {targetSentinels}";
        }
    }
}