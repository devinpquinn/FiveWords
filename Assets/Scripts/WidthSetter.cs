using TMPro;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(TMP_Text))]
public class WidthSetter : MonoBehaviour
{
    [SerializeField] private float maxWidth = 500f;

    private RectTransform rectTransform;
    private TMP_Text text;
    private float lastPreferredWidth = -1f;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        text = GetComponent<TMP_Text>();
    }

    void LateUpdate()
    {
        Apply();
    }

    public void Apply()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();
        if (text == null)
            text = GetComponent<TMP_Text>();

        // Measured unconstrained so the rect's own width doesn't feed back into the value.
        float preferredWidth = text.GetPreferredValues(Mathf.Infinity, Mathf.Infinity).x;
        if (Mathf.Approximately(preferredWidth, lastPreferredWidth))
            return;

        lastPreferredWidth = preferredWidth;
        rectTransform.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Horizontal,
            Mathf.Min(preferredWidth, maxWidth));
    }
}
