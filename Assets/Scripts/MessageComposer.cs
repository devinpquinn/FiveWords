using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class MessageComposer : MonoBehaviour
{
    public MessageManager messageManager;
    public MessageNode rootNode;
    public Button[] choiceButtons = new Button[MessageNode.ChoiceCount];
    public Button sendButton;
    public TextMeshProUGUI draftLabel;

    public GameObject placeholderText;

    [Tooltip("Send automatically once the fifth word is chosen.")]
    public bool autoSendAtMaxWords = true;

    [Tooltip("Duration for fading out all choice button labels before text updates.")]
    public float choiceTextFadeOutDuration = 0.08f;

    [Tooltip("Duration for fading in all choice button labels after text updates.")]
    public float choiceTextFadeInDuration = 0.12f;

    private MessageNode current;
    private bool sent;
    private bool choiceTransitionInProgress;
    private TextMeshProUGUI[] choiceLabels;
    private CanvasGroup[] choiceLabelGroups;
    private MessageNode[] displayedChoices;

    void OnEnable()
    {
        if (messageManager != null)
        {
            messageManager.InputAreaCollapsed += DisableChoiceButtonObjects;
        }
    }

    void OnDisable()
    {
        if (messageManager != null)
        {
            messageManager.InputAreaCollapsed -= DisableChoiceButtonObjects;
        }
    }

    void Start()
    {
        choiceLabels = new TextMeshProUGUI[choiceButtons.Length];
        choiceLabelGroups = new CanvasGroup[choiceButtons.Length];
        displayedChoices = new MessageNode[choiceButtons.Length];

        for (int i = 0; i < choiceButtons.Length; i++)
        {
            int index = i;
            if (choiceButtons[i] != null)
            {
                choiceButtons[i].onClick.AddListener(() => Choose(index));
                choiceLabels[i] = choiceButtons[i].GetComponentInChildren<TextMeshProUGUI>(true);
                if (choiceLabels[i] != null)
                {
                    choiceLabelGroups[i] = choiceLabels[i].GetComponent<CanvasGroup>();
                }
            }
        }

        if (sendButton != null)
        {
            sendButton.onClick.AddListener(Send);
        }

        current = rootNode;
        Refresh();
    }

    public void Choose(int index)
    {
        if (sent || current == null || choiceTransitionInProgress)
            return;

        MessageNode next = (displayedChoices != null && index >= 0 && index < displayedChoices.Length)
            ? displayedChoices[index]
            : null;
        if (next == null)
        {
            Debug.LogWarning($"Missing displayed choice at button index {index + 1} on node '{current.PathId}'.", current);
            return;
        }

        SoundManager.PlaySound("TypingLight", 0.35f);

        StartCoroutine(ChooseWithCrossfadeRoutine(next));
    }

    private IEnumerator ChooseWithCrossfadeRoutine(MessageNode next)
    {
        choiceTransitionInProgress = true;
        SetChoiceButtonsInteractable(false);

        yield return FadeChoiceLabelGroups(1f, 0f, choiceTextFadeOutDuration);

        current = next;

        if (current.IsComplete && autoSendAtMaxWords)
        {
            Send();
        }
        else
        {
            Refresh();
            // Keep choices locked for the full crossfade duration.
            SetChoiceButtonsInteractable(false);
        }

        yield return FadeChoiceLabelGroups(0f, 1f, choiceTextFadeInDuration);

        if (!sent)
        {
            RestoreChoiceButtonInteractability();
        }

        choiceTransitionInProgress = false;
    }

    private IEnumerator FadeChoiceLabelGroups(float from, float to, float duration)
    {
        if (choiceLabelGroups == null || choiceLabelGroups.Length == 0)
        {
            yield break;
        }

        if (duration <= 0f)
        {
            for (int i = 0; i < choiceLabelGroups.Length; i++)
            {
                if (choiceLabelGroups[i] != null)
                {
                    choiceLabelGroups[i].alpha = to;
                }
            }
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float alpha = Mathf.Lerp(from, to, t);

            for (int i = 0; i < choiceLabelGroups.Length; i++)
            {
                if (choiceLabelGroups[i] != null)
                {
                    choiceLabelGroups[i].alpha = alpha;
                }
            }

            yield return null;
        }

        for (int i = 0; i < choiceLabelGroups.Length; i++)
        {
            if (choiceLabelGroups[i] != null)
            {
                choiceLabelGroups[i].alpha = to;
            }
        }
    }

    public void Send()
    {
        if (sent || current == null || current.IsRoot)
            return;

        sent = true;
        HideChoices();
        if (sendButton != null)
        {
            sendButton.interactable = false;
        }

        if (draftLabel != null)
        {
            draftLabel.text = string.Empty;
        }

        if (placeholderText != null)
        {
            placeholderText.SetActive(true);
        }

        sendButton.interactable = false;

        string outgoingText = current.MessageText;
        if (outgoingText != null && outgoingText.Length < 2)
        {
            outgoingText += "...";
        }

        messageManager.AddOutgoingMessage(outgoingText);

        if (current.Response.Count == 0)
        {
            Debug.LogWarning($"No response authored for node '{current.PathId}' ({current.MessageText}).", current);
            return;
        }

        for (int i = 0; i < current.Response.Count; i++)
        {
            messageManager.AddMessage(current.Response[i]);
        }
    }

    private void Refresh()
    {
        bool canSend = !current.IsRoot;

        if (draftLabel != null)
        {
            draftLabel.text = current.MessageText;
        }

        if (placeholderText != null)
        {
            placeholderText.SetActive(!canSend);
        }

        if (sendButton != null)
        {
            sendButton.interactable = canSend;
        }
        
        if(canSend)
        {
            sendButton.interactable = true;
        }
        else
        {
            sendButton.interactable = false;
        }

        List<MessageNode> availableChoices = new List<MessageNode>(MessageNode.ChoiceCount);
        for (int i = 0; i < MessageNode.ChoiceCount; i++)
        {
            MessageNode choice = current.GetChoice(i);
            if (choice != null)
            {
                availableChoices.Add(choice);
            }
        }

        for (int i = availableChoices.Count - 1; i > 0; i--)
        {
            int swapIndex = Random.Range(0, i + 1);
            MessageNode temp = availableChoices[i];
            availableChoices[i] = availableChoices[swapIndex];
            availableChoices[swapIndex] = temp;
        }

        for (int i = 0; i < choiceButtons.Length; i++)
        {
            Button button = choiceButtons[i];
            if (button == null)
                continue;

            MessageNode choice = i < availableChoices.Count ? availableChoices[i] : null;
            displayedChoices[i] = choice;
            button.gameObject.SetActive(choice != null);

            if (choice == null)
            {
                button.interactable = false;
                continue;
            }

            button.interactable = true;
            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                label.text = choice.DisplayWord;
            }
        }
    }

    private void HideChoices()
    {
        foreach (Button button in choiceButtons)
        {
            if (button != null)
            {
                button.interactable = false;
                TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
                if (label != null)
                {
                    label.text = "...";
                }
            }
        }
    }

    private void SetChoiceButtonsInteractable(bool value)
    {
        foreach (Button button in choiceButtons)
        {
            if (button != null)
            {
                button.interactable = value;
            }
        }
    }

    private void RestoreChoiceButtonInteractability()
    {
        for (int i = 0; i < choiceButtons.Length; i++)
        {
            Button button = choiceButtons[i];
            if (button == null)
            {
                continue;
            }

            button.interactable = button.gameObject.activeSelf
                && displayedChoices != null
                && i >= 0
                && i < displayedChoices.Length
                && displayedChoices[i] != null;
        }
    }

    private void DisableChoiceButtonObjects()
    {
        foreach (Button button in choiceButtons)
        {
            if (button != null)
            {
                button.gameObject.SetActive(false);
            }
        }
    }
}
