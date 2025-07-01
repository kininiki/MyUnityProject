using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class HandlePositionLimiter : MonoBehaviour
{
    private RectTransform handleTransform;
    private RectTransform slidingAreaTransform;

    void Start()
    {
        handleTransform = GetComponent<RectTransform>();
        slidingAreaTransform = transform.parent.GetComponent<RectTransform>();
    }

    void Update()
    {
        Vector3 localPosition = handleTransform.localPosition;

        // Ограничиваем положение внутри Sliding Area
        float minY = slidingAreaTransform.rect.yMin;
        float maxY = slidingAreaTransform.rect.yMax;

        localPosition.y = Mathf.Clamp(localPosition.y, minY, maxY);
        handleTransform.localPosition = localPosition;
    }
}
