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
    public float crossfadeDuration = 0.25f;

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
        CanvasGroup indicatorGroup = null;
        LayoutElement indicatorLayout = null;

        if (typingIndicator != null)
        {
            indicatorGroup = typingIndicator.GetComponent<CanvasGroup>();
            indicatorLayout = typingIndicator.GetComponent<LayoutElement>();

            typingIndicator.transform.SetAsLastSibling();
            if (indicatorGroup != null)
            {
                indicatorGroup.alpha = 1f;
            }
            typingIndicator.SetActive(true);
            yield return new WaitForSeconds(GetTypingDuration(message));
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

        CanvasGroup messageGroup = instance.GetComponent<CanvasGroup>();
        if (messageGroup != null)
        {
            messageGroup.alpha = 0f;
        }

        if (typingIndicator != null)
        {
            // Take the indicator out of the layout so the new message occupies its slot during the fade.
            if (indicatorLayout != null)
            {
                indicatorLayout.ignoreLayout = true;
            }
            typingIndicator.transform.SetAsLastSibling();
        }

        // Rebuild now so HeightMatcher doesn't read a zero height in this frame's LateUpdate.
        LayoutRebuilder.ForceRebuildLayoutImmediate(instance.GetComponent<RectTransform>());
        LayoutRebuilder.ForceRebuildLayoutImmediate(messageContainer.GetComponent<RectTransform>());

        yield return CrossfadeRoutine(indicatorGroup, messageGroup);

        if (typingIndicator != null)
        {
            typingIndicator.SetActive(false);
            if (indicatorLayout != null)
            {
                indicatorLayout.ignoreLayout = false;
            }
            if (indicatorGroup != null)
            {
                indicatorGroup.alpha = 1f;
            }
        }
    }

    private IEnumerator CrossfadeRoutine(CanvasGroup fadeOut, CanvasGroup fadeIn)
    {
        if (crossfadeDuration > 0f && (fadeOut != null || fadeIn != null))
        {
            float elapsed = 0f;
            while (elapsed < crossfadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / crossfadeDuration);

                if (fadeOut != null)
                {
                    fadeOut.alpha = 1f - t;
                }
                if (fadeIn != null)
                {
                    fadeIn.alpha = t;
                }

                yield return null;
            }
        }

        if (fadeOut != null)
        {
            fadeOut.alpha = 0f;
        }
        if (fadeIn != null)
        {
            fadeIn.alpha = 1f;
        }
    }

    private float GetTypingDuration(string message)
    {
        int characters = Mathf.Clamp(message.Length, 1, Mathf.Max(1, maxTypingDurationCharacters));
        float t = maxTypingDurationCharacters > 1 ? (characters - 1f) / (maxTypingDurationCharacters - 1f) : 0f;
        return Mathf.Lerp(minTypingDuration, maxTypingDuration, t);
    }
}
