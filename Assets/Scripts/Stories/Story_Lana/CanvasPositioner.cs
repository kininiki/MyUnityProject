using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class CanvasPositioner : MonoBehaviour
{
    public Camera targetCamera;
    public float distanceFromCamera = 2f;
    public float canvasScale = 0.01f;

    void Reset()
    {
        // Находим камеру автоматически при добавлении скрипта
        targetCamera = Camera.main;
        if(targetCamera == null)
        {
            Debug.LogWarning("Main Camera не найдена!");
        }
    }

    public void PositionInFrontOfCamera()
    {
        if(targetCamera == null)
        {
            Debug.LogError("Назначьте камеру в инспекторе!");
            return;
        }

        // Получаем RectTransform канваса
        RectTransform rectTransform = GetComponent<RectTransform>();
        if(rectTransform == null)
        {
            Debug.LogError("RectTransform не найден!");
            return;
        }

        // Позиционируем Canvas перед камерой
        transform.position = targetCamera.transform.position + targetCamera.transform.forward * distanceFromCamera;
        
        // Поворачиваем Canvas лицом к камере
        transform.rotation = targetCamera.transform.rotation;

        // Устанавливаем масштаб
        transform.localScale = new Vector3(canvasScale, canvasScale, canvasScale);

        // Проверяем Canvas компонент
        Canvas canvas = GetComponent<Canvas>();
        if(canvas != null)
        {
            canvas.worldCamera = targetCamera;
            canvas.planeDistance = distanceFromCamera;
        }

        Debug.Log("Canvas позиционирован перед камерой");
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(CanvasPositioner))]
public class CanvasPositionerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        CanvasPositioner positioner = (CanvasPositioner)target;
        if(GUILayout.Button("Позиционировать перед камерой"))
        {
            positioner.PositionInFrontOfCamera();
        }
    }
}
#endif