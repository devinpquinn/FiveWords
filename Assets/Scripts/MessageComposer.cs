using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MessageComposer : MonoBehaviour
{
    public MessageManager messageManager;
    public MessageNode rootNode;
    public Button[] choiceButtons = new Button[MessageNode.ChoiceCount];
    public Button sendButton;
    public TextMeshProUGUI draftLabel;

    public GameObject placeholderText;
    public Image sendButtonImage;
    public Sprite sendInactiveSprite;
    public Sprite sendActiveSprite;

    [Tooltip("Send automatically once the fifth word is chosen.")]
    public bool autoSendAtMaxWords = true;

    private MessageNode current;
    private bool sent;

    void Start()
    {
        for (int i = 0; i < choiceButtons.Length; i++)
        {
            int index = i;
            if (choiceButtons[i] != null)
            {
                choiceButtons[i].onClick.AddListener(() => Choose(index));
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
        if (sent || current == null)
            return;

        MessageNode next = current.GetChoice(index);
        if (next == null)
        {
            Debug.LogWarning($"Missing choice {index + 1} on node '{current.PathId}'.", current);
            return;
        }

        current = next;

        if (current.IsComplete && autoSendAtMaxWords)
        {
            Send();
            return;
        }

        Refresh();
    }

    public void Send()
    {
        if (sent || current == null || current.IsRoot)
            return;

        sent = true;
        SetChoicesInteractable(false);
        if (sendButton != null)
        {
            sendButton.interactable = false;
        }

        messageManager.AddOutgoingMessage(current.MessageText);

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

        if (sendButtonImage != null)
        {
            Sprite sprite = canSend ? sendActiveSprite : sendInactiveSprite;
            if (sprite != null)
            {
                sendButtonImage.sprite = sprite;
            }
        }

        for (int i = 0; i < choiceButtons.Length; i++)
        {
            Button button = choiceButtons[i];
            if (button == null)
                continue;

            MessageNode choice = current.GetChoice(i);
            button.gameObject.SetActive(choice != null);

            if (choice == null)
                continue;

            button.interactable = true;
            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                label.text = choice.DisplayWord;
            }
        }
    }

    private void SetChoicesInteractable(bool value)
    {
        foreach (Button button in choiceButtons)
        {
            if (button != null)
            {
                button.interactable = value;
            }
        }
    }
}
