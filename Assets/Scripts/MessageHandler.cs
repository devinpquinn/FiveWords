using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MessageHandler : MonoBehaviour
{
    public TextMeshProUGUI messageText;
    
    public Image bubbleImage;
    public Sprite oldBubbleSprite;
    
    public VerticalLayoutGroup layoutGroup;
    public int oldBottomPadding = 38;
    
    public void SetMessage(string message)
    {
        messageText.text = message;
    }
    
    public void SetOld(string message)
    {
        messageText.text = message;
        bubbleImage.sprite = oldBubbleSprite;
        layoutGroup.padding.bottom = oldBottomPadding;
    }
}
