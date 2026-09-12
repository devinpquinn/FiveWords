using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

public class MessageManager : MonoBehaviour
{
    public GameObject messagePrefab;
    public GameObject outgoingMessagePrefab;
    public Transform messageContainer;
    
    public GameObject typingIndicator;
    public int maxTypingDurationCharacters = 100;
    public float minTypingDuration = 1f;
    public float maxTypingDuration = 5f;
    public float crossfadeDuration = 0.25f;
    public float defaultMessageHeight = 164f;
    public float slideDuration = 0.1f;
    public Ease slideEase = Ease.OutCubic;
    public float delayBetweenMessages = 0.5f;

    public RectTransform inputAreaBackground;
    public RectTransform draftLabelBackground;
    public Button restartButton;
    public float collapseDuration = 0.35f;
    public float expandDuration = 0.35f;
    public Ease collapseEase = Ease.InOutCubic;
    public float delayBeforeCollapse = 0.5f;
    [Tooltip("Multiplier on the typing duration the last received message would have used.")]
    public float expandDelayTypingMultiplier = 1f;
    public float expandAmount = 164f;

    private MessageHandler currentIncomingMessage;
    private MessageHandler currentOutgoingMessage;
    private readonly Queue<PendingMessage> pendingMessages = new Queue<PendingMessage>();
    private Coroutine processRoutine;
    private Tween containerSlideTween;
    private bool inputCollapsed;
    private bool inputExpanded;
    private string lastIncomingText;

    private struct PendingMessage
    {
        public string text;
        public bool outgoing;
    }

    void Awake()
    {
        if (typingIndicator != null)
        {
            typingIndicator.SetActive(false);
        }

        if (restartButton != null)
        {
            restartButton.gameObject.SetActive(false);
            restartButton.onClick.AddListener(RestartScene);
        }
    }

    public void RestartScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void AddMessage(string message)
    {
        Enqueue(message, false);
    }

    public void AddOutgoingMessage(string message)
    {
        Enqueue(message, true);
    }

    private void Enqueue(string message, bool outgoing)
    {
        pendingMessages.Enqueue(new PendingMessage { text = message, outgoing = outgoing });

        if (processRoutine == null)
        {
            processRoutine = StartCoroutine(ProcessQueue());
        }
    }

    private IEnumerator ProcessQueue()
    {
        while (pendingMessages.Count > 0)
        {
            PendingMessage next = pendingMessages.Dequeue();
            yield return AddMessageRoutine(next.text, next.outgoing);

            if (next.outgoing && !inputCollapsed)
            {
                yield return WaitForSlideToSettle();

                if (delayBeforeCollapse > 0f)
                {
                    yield return new WaitForSeconds(delayBeforeCollapse);
                }

                yield return CollapseInputAreaRoutine();
            }

            if (pendingMessages.Count > 0 && delayBetweenMessages > 0f)
            {
                yield return new WaitForSeconds(delayBetweenMessages);
            }
        }

        if (inputCollapsed && !inputExpanded)
        {
            yield return WaitForSlideToSettle();

            float expandDelay = lastIncomingText != null
                ? GetTypingDuration(lastIncomingText) * expandDelayTypingMultiplier
                : 0f;
            if (expandDelay > 0f)
            {
                yield return new WaitForSeconds(expandDelay);
            }

            yield return ExpandInputAreaRoutine();
        }

        processRoutine = null;
    }

    private IEnumerator WaitForSlideToSettle()
    {
        while (containerSlideTween != null && containerSlideTween.IsActive() && !containerSlideTween.IsComplete())
        {
            yield return null;
        }

        containerSlideTween = null;
    }

    private IEnumerator AddMessageRoutine(string message, bool outgoing)
    {
        CanvasGroup indicatorGroup = null;
        LayoutElement indicatorLayout = null;
        bool useIndicator = !outgoing && typingIndicator != null;

        if (useIndicator)
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

        // Each direction only ages its own previous bubble.
        MessageHandler previous = outgoing ? currentOutgoingMessage : currentIncomingMessage;
        if (previous != null)
        {
            previous.SetOld();
        }

        GameObject instance = Instantiate(outgoing ? outgoingMessagePrefab : messagePrefab, messageContainer);
        MessageHandler handler = instance.GetComponent<MessageHandler>();
        handler.SetMessage(message);

        if (outgoing)
        {
            currentOutgoingMessage = handler;
        }
        else
        {
            currentIncomingMessage = handler;
            lastIncomingText = message;
        }

        CanvasGroup messageGroup = instance.GetComponent<CanvasGroup>();
        if (messageGroup != null)
        {
            messageGroup.alpha = 0f;
        }

        if (useIndicator)
        {
            // Take the indicator out of the layout so the new message occupies its slot during the fade.
            if (indicatorLayout != null)
            {
                indicatorLayout.ignoreLayout = true;
            }
            typingIndicator.transform.SetAsLastSibling();
        }

        ResolveLayout(instance);
        SlideIn(instance.GetComponent<RectTransform>(), outgoing ? 0f : defaultMessageHeight);

        yield return CrossfadeRoutine(indicatorGroup, messageGroup);

        if (useIndicator)
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

    // WidthSetter and HeightMatcher normally run in LateUpdate, which would leave the bubble at its
    // prefab size for the spawn frame. Drive the same chain immediately instead.
    private void ResolveLayout(GameObject instance)
    {
        foreach (WidthSetter widthSetter in instance.GetComponentsInChildren<WidthSetter>(true))
        {
            widthSetter.Apply();
        }

        // ForceRebuildLayoutImmediate skips the whole subtree when the rect it's given has no
        // ILayoutController, and the message root has none, so rebuild from each controller instead.
        ILayoutController[] controllers = instance.GetComponentsInChildren<ILayoutController>(true);
        for (int i = controllers.Length - 1; i >= 0; i--)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(((Component)controllers[i]).GetComponent<RectTransform>());
        }

        foreach (HeightMatcher matcher in instance.GetComponentsInChildren<HeightMatcher>(true))
        {
            matcher.Match();
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(messageContainer.GetComponent<RectTransform>());
    }

    // Drops the container's bottom margin by the new message's height and eases it back, so the
    // stack appears to slide up into place.
    private void SlideIn(RectTransform messageRect, float defaultHeight)
    {
        RectTransform container = messageContainer as RectTransform;
        if (container == null || messageRect == null)
            return;

        container.DOKill(true);

        float restingBottom = container.offsetMin.y;
        container.offsetMin = new Vector2(container.offsetMin.x, restingBottom - (messageRect.rect.height - defaultHeight));

        containerSlideTween = DOTween.To(
                () => container.offsetMin.y,
                y => container.offsetMin = new Vector2(container.offsetMin.x, y),
                restingBottom,
                slideDuration)
            .SetEase(slideEase)
            .SetTarget(container);
    }

    // Runs once, after the sent message has settled: the composer folds away and the thread
    // reclaims the space before any reply is shown.
    private IEnumerator CollapseInputAreaRoutine()
    {
        inputCollapsed = true;
        yield return ResizeInputAreaRoutine(0f, collapseDuration);
    }

    // Mirror of the collapse, reopening just enough room for the restart button.
    private IEnumerator ExpandInputAreaRoutine()
    {
        inputExpanded = true;
        restartButton.gameObject.SetActive(true);
        yield return ResizeInputAreaRoutine(expandAmount, expandDuration);
    }

    private IEnumerator ResizeInputAreaRoutine(float amount, float duration)
    {
        containerSlideTween = null;

        Sequence resize = DOTween.Sequence();

        if (inputAreaBackground != null)
        {
            resize.Join(inputAreaBackground
                .DOSizeDelta(new Vector2(inputAreaBackground.sizeDelta.x, amount), duration)
                .SetEase(collapseEase));
        }

        RectTransform container = messageContainer as RectTransform;
        if (container != null)
        {
            float targetBottom = (draftLabelBackground != null ? draftLabelBackground.rect.height : 0f) + amount;
            resize.Join(DOTween.To(
                    () => container.offsetMin.y,
                    y => container.offsetMin = new Vector2(container.offsetMin.x, y),
                    targetBottom,
                    duration)
                .SetEase(collapseEase)
                .SetTarget(container));
        }

        if (draftLabelBackground != null)
        {
            resize.Join(draftLabelBackground
                .DOAnchorPosY(amount, duration)
                .SetEase(collapseEase));
        }

        yield return resize.WaitForCompletion();
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
