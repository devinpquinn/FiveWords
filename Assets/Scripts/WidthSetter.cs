using TMPro;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(TMP_Text))]
public class WidthSetter : MonoBehaviour
{
    private float maxWidthPercent = 0.75f;

    private RectTransform rectTransform;
    private TMP_Text text;
    private float lastPreferredWidth = -1f;
    private float lastEffectiveMaxWidth = -1f;

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

        RectTransform rootRectTransform = rectTransform.root as RectTransform;
        float rootWidth = rootRectTransform != null ? rootRectTransform.rect.width : rectTransform.rect.width;
        float effectiveMaxWidth = rootWidth * Mathf.Clamp01(maxWidthPercent);

        // Measured unconstrained so the rect's own width doesn't feed back into the value.
        float preferredWidth = text.GetPreferredValues(Mathf.Infinity, Mathf.Infinity).x;
        if (Mathf.Approximately(preferredWidth, lastPreferredWidth)
            && Mathf.Approximately(effectiveMaxWidth, lastEffectiveMaxWidth))
            return;

        lastPreferredWidth = preferredWidth;
        lastEffectiveMaxWidth = effectiveMaxWidth;
        rectTransform.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Horizontal,
            Mathf.Min(preferredWidth, effectiveMaxWidth));
    }
}
