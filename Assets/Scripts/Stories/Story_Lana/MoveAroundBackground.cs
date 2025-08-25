using UnityEngine;
using UnityEngine.UI;
using Fungus;
using System.Collections;

[CommandInfo("Background", "Move Around Background", "Pans the background left and right then zooms in and out, keeping the image within screen bounds.")]
public class MoveAroundBackgroundCommand : Command
{
    [SerializeField] protected Graphic targetImage;

    [Header("Pan Durations")]
    [SerializeField] protected float moveToLeftDuration = 2f;
    [SerializeField] protected float moveToRightDuration = 2f;
    [SerializeField] protected float moveToCenterDuration = 2f;

    [Header("Zoom Settings")]
    [SerializeField] protected float zoomScale = 1.2f;
    [SerializeField] protected float zoomInDuration = 2f;
    [SerializeField] protected float zoomOutDuration = 2f;

    [Header("Hold Times")]
    [SerializeField] protected float leftHoldDuration = 1f;
    [SerializeField] protected float rightHoldDuration = 1f;
    [SerializeField] protected float zoomInHoldDuration = 1f;
    [SerializeField] protected float zoomOutHoldDuration = 1f;

    [SerializeField] protected float initialDelay = 0.5f;

    [Header("Edges")]
    [Tooltip("Pixels of padding to keep the camera from touching the image edges.")]
    [SerializeField] protected float edgePadding = 25f;

    RectTransform imageTransform;
    Vector2 centerPos;
    Vector3 originalScale;

    public override void OnEnter()
    {
        if (targetImage == null)
        {
            Debug.LogError("Target Image is not set in MoveAroundBackgroundCommand");
            Continue();
            return;
        }

        imageTransform = targetImage.rectTransform;
        centerPos = imageTransform.anchoredPosition;
        originalScale = imageTransform.localScale;

        StartCoroutine(MoveSequence());
    }

    IEnumerator MoveSequence()
    {
        Canvas canvas = targetImage.canvas;
        float canvasScaleFactor = canvas != null ? canvas.scaleFactor : 1f;
        float screenWidth = Screen.width / canvasScaleFactor;
        float imageWidth = imageTransform.rect.width;
        float maxOffset = (imageWidth - screenWidth) / 2f;
        if (maxOffset > 0f)
        {
            maxOffset = Mathf.Max(0f, maxOffset - edgePadding);
        }

        yield return new WaitForSeconds(initialDelay);

        if (maxOffset > 0f)
        {
            float startX = centerPos.x;
            float targetX = -maxOffset;
            float elapsedTime = 0f;
            while (elapsedTime < moveToLeftDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsedTime / moveToLeftDuration);
                float newX = Mathf.Lerp(startX, targetX, t);
                imageTransform.anchoredPosition = new Vector2(newX, centerPos.y);
                yield return null;
            }
            imageTransform.anchoredPosition = new Vector2(targetX, centerPos.y);
            yield return new WaitForSeconds(leftHoldDuration);

            startX = -maxOffset;
            targetX = maxOffset;
            elapsedTime = 0f;
            while (elapsedTime < moveToRightDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsedTime / moveToRightDuration);
                float newX = Mathf.Lerp(startX, targetX, t);
                imageTransform.anchoredPosition = new Vector2(newX, centerPos.y);
                yield return null;
            }
            imageTransform.anchoredPosition = new Vector2(targetX, centerPos.y);
            yield return new WaitForSeconds(rightHoldDuration);

            startX = maxOffset;
            targetX = centerPos.x;
            elapsedTime = 0f;
            while (elapsedTime < moveToCenterDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsedTime / moveToCenterDuration);
                float newX = Mathf.Lerp(startX, targetX, t);
                imageTransform.anchoredPosition = new Vector2(newX, centerPos.y);
                yield return null;
            }
            imageTransform.anchoredPosition = centerPos;
        }

        float elapsed = 0f;
        Vector3 startScale = originalScale;
        Vector3 targetScale = originalScale * zoomScale;
        while (elapsed < zoomInDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / zoomInDuration);
            imageTransform.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }
        imageTransform.localScale = targetScale;
        yield return new WaitForSeconds(zoomInHoldDuration);

        elapsed = 0f;
        startScale = targetScale;
        targetScale = originalScale;
        while (elapsed < zoomOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / zoomOutDuration);
            imageTransform.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }
        imageTransform.localScale = originalScale;
        yield return new WaitForSeconds(zoomOutHoldDuration);

        imageTransform.anchoredPosition = centerPos;
        imageTransform.localScale = originalScale;

        Continue();
    }

    public override string GetSummary()
    {
        if (targetImage == null)
        {
            return "Error: No target image set";
        }

        return "Pan left/right and zoom";
    }

    public override Color GetButtonColor()
    {
        return new Color32(221, 184, 169, 255);
    }
}
