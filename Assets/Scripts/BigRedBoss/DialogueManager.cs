using System.Collections;
using UnityEngine;
using TMPro;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;
    public GameObject dialoguePanel;
    [Header("UI Transition References")]
    public RectTransform dialoguePanelRect;
    public CanvasGroup dialogueTextCanvasGroup;
    // Optionally, add character image here if needed
    public TMP_Text dialogueText;
    [Header("Typewriter Settings")]
    public float typingSpeed = 0.04f;

    [Header("Sound Effect Settings")]
    public AudioSource soundEffectAudioSource;
    public AudioClip typeSound;
    public bool playVoiceWithTyping = true;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }


    public IEnumerator ShowDialogue(string text, float holdDuration = 1.0f)
    {
        dialoguePanel.SetActive(true);
        // Slide in panel from bottom
        yield return StartCoroutine(SlideInPanel());
        // Fade in text
        yield return StartCoroutine(FadeInText());
        // Typewriter effect
        yield return StartCoroutine(TypeLine(text));
        yield return new WaitForSeconds(holdDuration);
        // Optionally, fade out text and slide out panel here
        dialoguePanel.SetActive(false);
    }

    private IEnumerator SlideInPanel()
    {
        if (dialoguePanelRect == null)
            yield break;
        // Slide from offscreen bottom (anchoredPosition.y = -Screen.height) to anchoredPosition.y = 0
        float duration = 0.4f;
        float elapsed = 0f;
        Vector2 start = new Vector2(dialoguePanelRect.anchoredPosition.x, -Screen.height);
        Vector2 end = new Vector2(dialoguePanelRect.anchoredPosition.x, 188);
        dialoguePanelRect.anchoredPosition = start;
        while (elapsed < duration)
        {
            dialoguePanelRect.anchoredPosition = Vector2.Lerp(start, end, elapsed / duration);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        dialoguePanelRect.anchoredPosition = end;
    }

    private IEnumerator FadeInText()
    {
        if (dialogueTextCanvasGroup == null)
            yield break;
        float duration = 0.3f;
        float elapsed = 0f;
        dialogueTextCanvasGroup.alpha = 0f;
        while (elapsed < duration)
        {
            dialogueTextCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        dialogueTextCanvasGroup.alpha = 1f;
    }

    private IEnumerator TypeLine(string text)
    {
        dialogueText.text = "";
        foreach (char letter in text)
        {
            dialogueText.text += letter;
            if (playVoiceWithTyping && soundEffectAudioSource != null && typeSound != null && !char.IsWhiteSpace(letter))
            {
                soundEffectAudioSource.PlayOneShot(typeSound);
            }
            yield return new WaitForSecondsRealtime(typingSpeed);
        }
    }

    // Optional: Show dialogue and wait for player input to continue
    public IEnumerator ShowDialogueWaitForInput(string text)
    {
        dialoguePanel.SetActive(true);
        yield return StartCoroutine(TypeLine(text));
        bool proceed = false;
        while (!proceed)
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
                proceed = true;
            yield return null;
        }
        dialoguePanel.SetActive(false);
    }
}
