using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Cyclic horizontal carousel for RawImages in the main menu.
/// Shows one image in the center and two neighbours scaled down on the sides.
/// </summary>
public class StoriesCarousel : MonoBehaviour, IBeginDragHandler, IEndDragHandler
{
    [SerializeField] private List<RawImage> images = new List<RawImage>();
    [SerializeField] private RectTransform centerPoint;
    [SerializeField] private float sideOffset = 400f;
    [SerializeField] private float centerScale = 1f;
    [SerializeField] private float sideScale = 0.7f;
    [SerializeField] private float swipeThreshold = 50f;
    [SerializeField] private Button leftButton;
    [SerializeField] private Button rightButton;

    private bool dragging;

    private Vector2 dragStart;

    private int centerIndex = 0;

    private void Start()
    {
        if (centerPoint == null)
        {
            centerPoint = transform as RectTransform;
        }
        if (leftButton != null)
        {
            leftButton.onClick.AddListener(ShowPrevious);
        }
        if (rightButton != null)
        {
            rightButton.onClick.AddListener(ShowNext);
        }

        UpdateLayout();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        dragStart = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        float deltaX = eventData.position.x - dragStart.x;
        if (Mathf.Abs(deltaX) >= swipeThreshold)
        {
            if (deltaX < 0)
            {
                ShowNext();
            }
            else
            {
                ShowPrevious();
            }
        }
    }

    /// <summary>
    /// Show next image in the carousel.
    /// </summary>
    public void ShowNext()
    {
        if (images.Count == 0) return;
        centerIndex = (centerIndex + 1) % images.Count;
        UpdateLayout();
    }

    /// <summary>
    /// Show previous image in the carousel.
    /// </summary>
    public void ShowPrevious()
    {
        if (images.Count == 0) return;
        centerIndex = (centerIndex - 1 + images.Count) % images.Count;
        UpdateLayout();
    }

    private void UpdateLayout()
    {
        if (centerPoint == null) return;

        for (int i = 0; i < images.Count; i++)
        {
            var rect = images[i].rectTransform;
            int diff = (i - centerIndex + images.Count) % images.Count;
            if (diff > images.Count / 2) diff -= images.Count;

            if (diff == 0)
            {
                rect.anchoredPosition = centerPoint.anchoredPosition;
                rect.localScale = Vector3.one * centerScale;
                rect.SetAsLastSibling();
                images[i].gameObject.SetActive(true);
            }
            else if (diff == 1 || diff == -1)
            {
                float sign = diff > 0 ? 1f : -1f;
                rect.anchoredPosition = centerPoint.anchoredPosition + new Vector2(sign * sideOffset, 0f);
                rect.localScale = Vector3.one * sideScale;
                rect.SetSiblingIndex(0);
                images[i].gameObject.SetActive(true);
            }
            else
            {
                images[i].gameObject.SetActive(false);
            }
        }
    }

    private void Update()
    {
        if (Input.touchSupported && Input.touchCount > 0)
        {
            Touch t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Began)
            {
                dragging = true;
                dragStart = t.position;
            }
            else if (t.phase == TouchPhase.Ended && dragging)
            {
                HandleSwipe(t.position.x - dragStart.x);
                dragging = false;
            }
        }
        else
        {
            if (Input.GetMouseButtonDown(0))
            {
                dragging = true;
                dragStart = Input.mousePosition;
            }
            else if (Input.GetMouseButtonUp(0) && dragging)
            {
                HandleSwipe(Input.mousePosition.x - dragStart.x);
                dragging = false;
            }
        }
    }

    private void HandleSwipe(float deltaX)
    {
        if (Mathf.Abs(deltaX) >= swipeThreshold)
        {
            if (deltaX < 0)
            {
                ShowNext();
            }
            else
            {
                ShowPrevious();
            }
        }
    }

    private void OnDestroy()
    {
        if (leftButton != null)
        {
            leftButton.onClick.RemoveListener(ShowPrevious);
        }
        if (rightButton != null)
        {
            rightButton.onClick.RemoveListener(ShowNext);
        }
    }
}