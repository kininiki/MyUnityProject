using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    public Camera mainCamera; // Ссылка на главную камеру
    private Vector3 offset;  // Смещение относительно камеры

    void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main; // Находим главную камеру, если она не назначена
        }

        // Рассчитываем начальное смещение
        offset = transform.position - mainCamera.transform.position;
    }

    void LateUpdate()
    {
        // Обновляем позицию канваса в соответствии с камерой
        transform.position = mainCamera.transform.position + offset;
    }
}
