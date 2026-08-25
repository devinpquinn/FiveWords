using UnityEngine;
using UnityEngine.UI;

public class MessageManager : MonoBehaviour
{
    public GameObject messagePrefab;
    public Transform messageContainer;

    private MessageHandler currentMessage;
    private string currentMessageText;

    public MessageHandler AddMessage(string message)
    {
        if (currentMessage != null)
        {
            currentMessage.SetOld(currentMessageText);
        }

        GameObject instance = Instantiate(messagePrefab, messageContainer);
        MessageHandler handler = instance.GetComponent<MessageHandler>();
        handler.SetMessage(message);

        currentMessage = handler;
        currentMessageText = message;

        return handler;
    }
}
