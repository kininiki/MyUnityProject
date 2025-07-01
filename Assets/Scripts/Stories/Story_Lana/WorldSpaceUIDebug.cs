using UnityEngine;
using UnityEngine.UI;
using System.Collections;  // Добавлен импорт для IEnumerator
using System.Collections.Generic;

public class WorldSpaceUIDebug : MonoBehaviour
{
    private Canvas canvas;
    private Camera eventCamera;
    private RectTransform canvasRect;

    void Start()
    {
        canvas = GetComponent<Canvas>();
        canvasRect = GetComponent<RectTransform>();
        eventCamera = canvas.worldCamera;

        if (canvas.renderMode != RenderMode.WorldSpace)
        {
            Debug.LogError("Canvas должен быть в режиме World Space!");
        }

        StartCoroutine(CheckUIVisibility());
    }

    private IEnumerator CheckUIVisibility()
    {
        yield return new WaitForSeconds(0.1f); // Ждём полной инициализации

        if (eventCamera == null)
        {
            Debug.LogError("Event Camera не назначена в Canvas!");
            yield break;
        }

        // Проверяем, находится ли Canvas в поле зрения камеры
        Vector3[] corners = new Vector3[4];
        canvasRect.GetWorldCorners(corners);
        
        bool isVisible = false;
        foreach (Vector3 corner in corners)
        {
            Vector3 viewportPoint = eventCamera.WorldToViewportPoint(corner);
            Debug.Log($"Угол Canvas в viewport: {viewportPoint}");
            
            // Точка видима, если она находится в пределах от 0 до 1 по X и Y, и перед камерой (Z > 0)
            if (viewportPoint.x >= 0 && viewportPoint.x <= 1 &&
                viewportPoint.y >= 0 && viewportPoint.y <= 1 &&
                viewportPoint.z > 0)
            {
                isVisible = true;
                break;
            }
        }

        if (!isVisible)
        {
            Debug.LogError("Canvas находится вне поля зрения камеры!");
            Debug.Log($"Canvas position: {canvasRect.position}");
            Debug.Log($"Camera position: {eventCamera.transform.position}");
            Debug.Log($"Camera rotation: {eventCamera.transform.rotation.eulerAngles}");
        }
        else
        {
            Debug.Log("Canvas виден в камере");
        }

        // Проверяем настройки GraphicRaycaster для World Space
        var raycaster = GetComponent<GraphicRaycaster>();
        if (raycaster != null)
        {
            Debug.Log($"Graphic Raycaster найден: {raycaster.enabled}");
            Debug.Log($"Blocking Objects: {raycaster.blockingObjects}");
            Debug.Log($"Ignore Reversed Graphics: {raycaster.ignoreReversedGraphics}");
        }
        else
        {
            Debug.LogError("GraphicRaycaster отсутствует на Canvas!");
        }
    }

    void OnDrawGizmos()
    {
        if (canvasRect != null)
        {
            // Рисуем границы Canvas в сцене
            Vector3[] corners = new Vector3[4];
            canvasRect.GetWorldCorners(corners);
            
            Gizmos.color = Color.yellow;
            for (int i = 0; i < 4; i++)
            {
                Gizmos.DrawLine(corners[i], corners[(i + 1) % 4]);
            }

            // Рисуем направление нормали Canvas
            Gizmos.color = Color.blue;
            Vector3 center = (corners[0] + corners[2]) * 0.5f;
            Gizmos.DrawRay(center, canvasRect.forward * 2);
        }
    }
}