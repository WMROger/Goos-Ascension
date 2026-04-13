using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Place on the CaveEntrance trigger collider.
/// - Player enters range → "Press R" prompt appears.
/// - Player presses R   → checks for living enemies.
///     • Enemies alive  → brief "Defeat all enemies!" warning.
///     • No enemies     → confirmation panel appears (pauses game).
///         - Confirm    → load next level.
///         - Cancel     → close panel, resume.
/// </summary>
public class LevelEntrance : MonoBehaviour
{
    [Header("Level")]
    [SerializeField] private string nextLevelName;

    [Header("Prompt UI")]
    [Tooltip("Small label shown when the player is nearby (e.g. 'Press R to enter').")]
    [SerializeField] private GameObject promptPanel;
    [SerializeField] private TMP_Text promptText;

    [Header("Enemy Warning UI")]
    [Tooltip("Panel / text shown briefly when enemies are still alive.")]
    [SerializeField] private GameObject enemyWarningPanel;
    [SerializeField] private TMP_Text enemyWarningText;
    [SerializeField] private float warningDuration = 2.5f;

    [Header("Confirmation UI")]
    [Tooltip("Confirmation panel shown when the player is allowed to proceed.")]
    [SerializeField] private GameObject confirmationPanel;
    [SerializeField] private TMP_Text confirmationText;
    [SerializeField] private Button confirmYesButton;
    [SerializeField] private Button confirmNoButton;

    [Header("Confirmation Message")]
    [TextArea(3, 6)]
    [SerializeField] private string confirmMessage =
        "Are you sure you want to proceed?\n\n" +
        "You cannot go back — you might miss some special things!";

    private bool playerInRange = false;
    private bool confirmationOpen = false;
    private Coroutine warningRoutine;

    private void Start()
    {
        HideAll();

        if (confirmationText != null)
            confirmationText.text = confirmMessage;

        if (confirmYesButton != null)
            confirmYesButton.onClick.AddListener(OnConfirmYes);

        if (confirmNoButton != null)
            confirmNoButton.onClick.AddListener(OnConfirmNo);
    }

    private void Update()
    {
        if (!playerInRange) return;

        if (Input.GetKeyDown(KeyCode.R))
            TryEnter();
    }

    // -------------------------------------------------------------------------
    // Trigger detection
    // -------------------------------------------------------------------------

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerMovement>() == null) return;

        playerInRange = true;
        ShowPrompt("Press  R  to Enter");
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerMovement>() == null) return;

        playerInRange = false;

        if (!confirmationOpen)
            HideAll();
    }

    // -------------------------------------------------------------------------
    // Entry logic
    // -------------------------------------------------------------------------

    private void TryEnter()
    {
        if (confirmationOpen) return;

        if (EnemiesRemaining())
        {
            ShowEnemyWarning();
        }
        else
        {
            OpenConfirmation();
        }
    }

    private bool EnemiesRemaining()
    {
        // Check regular enemies
        foreach (EnemyHealth e in FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None))
        {
            if (e != null && e.gameObject.activeInHierarchy)
                return true;
        }

        // Check boss
        foreach (MechBossHealth b in FindObjectsByType<MechBossHealth>(FindObjectsSortMode.None))
        {
            if (b != null && !b.IsDead())
                return true;
        }

        return false;
    }

    // -------------------------------------------------------------------------
    // UI helpers
    // -------------------------------------------------------------------------

    private void ShowPrompt(string text)
    {
        if (promptPanel != null) promptPanel.SetActive(true);
        if (promptText  != null) promptText.text = text;
    }

    private void ShowEnemyWarning()
    {
        if (warningRoutine != null) StopCoroutine(warningRoutine);
        warningRoutine = StartCoroutine(EnemyWarningRoutine());
    }

    private IEnumerator EnemyWarningRoutine()
    {
        // Hide prompt while warning is shown
        if (promptPanel       != null) promptPanel.SetActive(false);
        if (enemyWarningPanel != null) enemyWarningPanel.SetActive(true);
        if (enemyWarningText  != null) enemyWarningText.text = "Defeat all enemies before proceeding!";

        yield return new WaitForSeconds(warningDuration);

        if (enemyWarningPanel != null) enemyWarningPanel.SetActive(false);

        // Restore prompt if player is still in range
        if (playerInRange)
            ShowPrompt("Press  R  to Enter");
    }

    private void OpenConfirmation()
    {
        confirmationOpen = true;

        if (promptPanel       != null) promptPanel.SetActive(false);
        if (confirmationPanel != null) confirmationPanel.SetActive(true);

        PauseController.SetPause(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
    }

    private void CloseConfirmation()
    {
        confirmationOpen = false;

        if (confirmationPanel != null) confirmationPanel.SetActive(false);

        PauseController.SetPause(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;

        if (playerInRange)
            ShowPrompt("Press  R  to Enter");
    }

    private void HideAll()
    {
        if (promptPanel       != null) promptPanel.SetActive(false);
        if (enemyWarningPanel != null) enemyWarningPanel.SetActive(false);
        if (confirmationPanel != null) confirmationPanel.SetActive(false);
    }

    // -------------------------------------------------------------------------
    // Button callbacks
    // -------------------------------------------------------------------------

    private void OnConfirmYes()
    {
        HideAll();
        PauseController.SetPause(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;

        // Load the next scene directly so we don't depend on SceneController.instance
        // being initialized (it can be null depending on scene setup/order).
        if (!string.IsNullOrEmpty(nextLevelName))
        {
            SceneManager.LoadScene(nextLevelName);
        }
        else
        {
            Debug.LogError("LevelEntrance: nextLevelName is empty. Set it in the inspector.");
        }
    }

    private void OnConfirmNo()
    {
        CloseConfirmation();
    }
}
