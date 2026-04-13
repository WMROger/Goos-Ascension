using UnityEngine;
using UnityEngine.Rendering;
using TMPro;

public class LeverToggle : MonoBehaviour, IInteractable
{
    [Header("Interaction")]
    [SerializeField] private KeyCode interactionKey = KeyCode.F;
    [SerializeField] private string playerTag = "Player";

    [Header("Toggle Targets")]
    [SerializeField] private GameObject[] enableOnPull;
    [SerializeField] private GameObject[] disableOnPull;

    [Header("Behavior")]
    [SerializeField] private bool oneShot = true;

    [Header("Timing")]
    [SerializeField] private int toggleDelayFrames = 0;

    [Header("Lever Animation (Optional)")]
    [SerializeField] private Animator leverAnimator;
    [SerializeField] private string pullTriggerName = "Pull";
    [SerializeField] private string returnTriggerName = "Return";

    [Header("Prompt UI")]
    [SerializeField] private Vector3 promptOffset = new Vector3(0f, 1.5f, 0f);

    private bool playerNearby;
    private bool isOn;
    private bool isApplying;

    private GameObject promptPanel;

    private void Awake()
    {
        CreatePromptUI();
    }

    private void CreatePromptUI()
    {
        promptPanel = new GameObject("LeverPrompt");
        promptPanel.transform.SetParent(transform, false);
        promptPanel.transform.localPosition = promptOffset;
        promptPanel.transform.localScale = Vector3.one;

        var sortGroup = promptPanel.AddComponent<SortingGroup>();
        sortGroup.sortingLayerName = "Background_Lights";
        sortGroup.sortingOrder = 100;

        var tmp = promptPanel.AddComponent<TextMeshPro>();
        tmp.text = $"Press <b>{interactionKey}</b> to Activate";
        tmp.fontSize = 4f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/lithosbold SDF");
        if (font != null) tmp.font = font;

        var rt = promptPanel.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(4f, 1f);

        promptPanel.SetActive(false);
    }

    public bool CanInteract()
    {
        if (isApplying) return false;
        if (oneShot && isOn) return false; 
        return true;
    }

    private void Update()
    {
        if (!playerNearby) return;
        if (!CanInteract()) return;
        if (PauseController.IsGamePaused) return;

        if (Input.GetKeyDown(interactionKey))
            Interact();
    }

    public void Interact()
    {
        if (!CanInteract()) return;

        isOn = !isOn;

        if (promptPanel != null && oneShot && isOn)
            promptPanel.SetActive(false);

        StartCoroutine(ApplyToggleAfterDelay());
    }

    private System.Collections.IEnumerator ApplyToggleAfterDelay()
    {
        isApplying = true;

        for (int i = 0; i < Mathf.Max(0, toggleDelayFrames); i++)
            yield return null;

        if (isOn)
        {
            if (leverAnimator != null && !string.IsNullOrEmpty(pullTriggerName))
                leverAnimator.SetTrigger(pullTriggerName);

            if (enableOnPull != null)
            {
                for (int i = 0; i < enableOnPull.Length; i++)
                {
                    GameObject obj = enableOnPull[i];
                    if (obj != null) obj.SetActive(true);
                }
            }

            if (disableOnPull != null)
            {
                for (int i = 0; i < disableOnPull.Length; i++)
                {
                    GameObject obj = disableOnPull[i];
                    if (obj != null) obj.SetActive(false);
                }
            }

            // --- NEW: TELL THE QUEST MANAGER THE LEVER WAS PULLED! ---
            if (KillQuestManager.Instance != null)
            {
                KillQuestManager.Instance.CompleteLeverQuest();
            }
            // ---------------------------------------------------------
        }
        else
        {
            if (leverAnimator != null && !string.IsNullOrEmpty(returnTriggerName))
                leverAnimator.SetTrigger(returnTriggerName);

            if (enableOnPull != null)
            {
                for (int i = 0; i < enableOnPull.Length; i++)
                {
                    GameObject obj = enableOnPull[i];
                    if (obj != null) obj.SetActive(false);
                }
            }

            if (disableOnPull != null)
            {
                for (int i = 0; i < disableOnPull.Length; i++)
                {
                    GameObject obj = disableOnPull[i];
                    if (obj != null) obj.SetActive(true);
                }
            }
        }

        isApplying = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsPlayerCollider(other))
        {
            playerNearby = true;
            if (promptPanel != null && CanInteract())
                promptPanel.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (IsPlayerCollider(other))
        {
            playerNearby = false;
            if (promptPanel != null)
                promptPanel.SetActive(false);
        }
    }

    private bool IsPlayerCollider(Collider2D other)
    {
        if (other == null) return false;
        if (other.CompareTag(playerTag)) return true;
        return other.GetComponentInParent<PlayerMovement>() != null;
    }
}