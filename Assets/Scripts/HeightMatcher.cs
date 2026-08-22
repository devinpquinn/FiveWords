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

    void Update()
    {
        if (target == null)
            return;

        float targetHeight = target.rect.height;
        if (Mathf.Approximately(rectTransform.rect.height, targetHeight))
            return;

        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetHeight);
    }
}
