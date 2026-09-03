using UnityEngine;
using System.Collections.Generic;
using System.Collections;
public class MessageTester : MonoBehaviour
{
    public MessageManager messageManager;
    public string outgoingMessage;
    public List<string> messages;
    public float messageInterval = 2f;
    
    void Start()
    {
        StartCoroutine(AddMessages());
    }
    
    IEnumerator AddMessages()
    {
        yield return new WaitForSeconds(1f); // Wait for 1 second before starting to add messages
        
        messageManager.AddOutgoingMessage(outgoingMessage);
        
        yield return new WaitForSeconds(3f);
        
        foreach (string message in messages)
        {
            messageManager.AddMessage(message);
            yield return new WaitForSeconds(messageInterval);
        }
    }
}
