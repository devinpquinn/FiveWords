using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class HeightMatcher : MonoBehaviour
{
    [SerializeField] private RectTransform target;

    private RectTransform rectTransform;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    void LateUpdate()
    {
        Match();
    }

    public void Match()
    {
        if (target == null)
            return;

        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        float targetHeight = target.rect.height;
        if (targetHeight <= 0f)
            return;

        if (Mathf.Approximately(rectTransform.rect.height, targetHeight))
            return;

        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetHeight);
    }
}
