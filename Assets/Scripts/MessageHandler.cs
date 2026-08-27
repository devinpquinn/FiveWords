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
        // TMP otherwise reports stale bounds until its next update, giving a wrong height for one frame.
        messageText.ForceMeshUpdate();
    }
    
    public void SetOld()
    {
        bubbleImage.sprite = oldBubbleSprite;
        layoutGroup.padding.bottom = oldBottomPadding;
    }
}
