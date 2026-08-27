using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class MessageManager : MonoBehaviour
{
    public GameObject messagePrefab;
    public Transform messageContainer;
    public GameObject typingIndicator;
    public float typingDuration = 1f;

    private MessageHandler currentMessage;
    private string currentMessageText;

    void Awake()
    {
        if (typingIndicator != null)
        {
            typingIndicator.SetActive(false);
        }
    }

    public void AddMessage(string message)
    {
        StartCoroutine(AddMessageRoutine(message));
    }

    private IEnumerator AddMessageRoutine(string message)
    {
        if (typingIndicator != null)
        {
            typingIndicator.transform.SetAsLastSibling();
            typingIndicator.SetActive(true);
            yield return new WaitForSeconds(typingDuration);
            typingIndicator.SetActive(false);
        }

        if (currentMessage != null)
        {
            currentMessage.SetOld(currentMessageText);
        }

        GameObject instance = Instantiate(messagePrefab, messageContainer);
        MessageHandler handler = instance.GetComponent<MessageHandler>();
        handler.SetMessage(message);

        currentMessage = handler;
        currentMessageText = message;
    }
}
