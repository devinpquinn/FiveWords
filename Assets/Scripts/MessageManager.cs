using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class MessageManager : MonoBehaviour
{
    public GameObject messagePrefab;
    public Transform messageContainer;
    
    public GameObject typingIndicator;
    public int maxTypingDurationCharacters = 100;
    public float minTypingDuration = 1f;
    public float maxTypingDuration = 5f;

    private MessageHandler currentMessage;
    private string currentMessageText;
    private readonly Queue<string> pendingMessages = new Queue<string>();
    private Coroutine processRoutine;

    void Awake()
    {
        if (typingIndicator != null)
        {
            typingIndicator.SetActive(false);
        }
    }

    public void AddMessage(string message)
    {
        pendingMessages.Enqueue(message);

        if (processRoutine == null)
        {
            processRoutine = StartCoroutine(ProcessQueue());
        }
    }

    private IEnumerator ProcessQueue()
    {
        while (pendingMessages.Count > 0)
        {
            yield return AddMessageRoutine(pendingMessages.Dequeue());
        }

        processRoutine = null;
    }

    private IEnumerator AddMessageRoutine(string message)
    {
        if (typingIndicator != null)
        {
            typingIndicator.transform.SetAsLastSibling();
            typingIndicator.SetActive(true);
            yield return new WaitForSeconds(GetTypingDuration(message));
            typingIndicator.SetActive(false);
        }

        if (currentMessage != null)
        {
            currentMessage.SetOld();
        }

        GameObject instance = Instantiate(messagePrefab, messageContainer);
        MessageHandler handler = instance.GetComponent<MessageHandler>();
        handler.SetMessage(message);

        currentMessage = handler;
        currentMessageText = message;

        // Rebuild now so HeightMatcher doesn't read a zero height in this frame's LateUpdate.
        LayoutRebuilder.ForceRebuildLayoutImmediate(instance.GetComponent<RectTransform>());
        LayoutRebuilder.ForceRebuildLayoutImmediate(messageContainer.GetComponent<RectTransform>());
    }

    private float GetTypingDuration(string message)
    {
        int characters = Mathf.Clamp(message.Length, 1, Mathf.Max(1, maxTypingDurationCharacters));
        float t = maxTypingDurationCharacters > 1 ? (characters - 1f) / (maxTypingDurationCharacters - 1f) : 0f;
        return Mathf.Lerp(minTypingDuration, maxTypingDuration, t);
    }
}
