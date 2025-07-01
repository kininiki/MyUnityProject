// using UnityEngine;
// using Fungus;
// using System.Collections;

// [CommandInfo("Camera", "Move Around", "Moves the camera sequentially to three specified positions with a 1-second delay at the first two positions.")]
// public class MoveAroundCommand : Command
// {
//     public Camera mainCamera;

//     [Tooltip("Position 1 RectTransform (1-7)")]
//     public RectTransform position1Rect;
//     [Tooltip("Position 2 RectTransform (1-7)")]
//     public RectTransform position2Rect;
//     [Tooltip("Position 3 RectTransform (1-7)")]
//     public RectTransform position3Rect;

//     [Tooltip("Speed of camera movement")]
//     public float moveSpeed = 10f;

//     public override void OnEnter()
//     {
//         if (mainCamera == null)
//         {
//             mainCamera = Camera.main;
//         }

//         if (mainCamera == null)
//         {
//             Debug.LogError("Main camera not assigned and could not find Camera.main.");
//             Continue();
//             return;
//         }

//         // Запускаем корутину для последовательного перемещения камеры
//         StartCoroutine(MoveAroundSequence());
//     }

//     private IEnumerator MoveAroundSequence()
//     {
//         Debug.Log("Starting MoveAroundSequence");

//         // Перемещаем камеру к первой позиции и ждем 1 секунду
//         yield return MoveCameraToPosition(position1Rect);
//         yield return new WaitForSeconds(1f);
//         Debug.Log("Arrived at Position 1");

//         // Перемещаем камеру ко второй позиции и ждем 1 секунду
//         yield return MoveCameraToPosition(position2Rect);
//         yield return new WaitForSeconds(1f);
//         Debug.Log("Arrived at Position 2");

//         // Перемещаем камеру к третьей позиции
//         yield return MoveCameraToPosition(position3Rect);
//         Debug.Log("Arrived at Position 3");

//         // Завершаем выполнение команды
//         Debug.Log("Finished MoveAroundSequence");
//         Continue();
//     }

//     private IEnumerator MoveCameraToPosition(RectTransform targetRect)
//     {
//         if (targetRect == null)
//         {
//             Debug.LogError("Target position RectTransform is null.");
//             yield break;
//         }

//         Vector3 targetPosition = RectTransformToWorldPosition(targetRect);
//         Debug.Log($"Moving to target position: {targetPosition}");

//         // Плавное перемещение камеры к целевой позиции с MoveTowards
//         while (Vector3.Distance(mainCamera.transform.position, targetPosition) > 0.05f) // Ослаблено условие
//         {
//             mainCamera.transform.position = Vector3.MoveTowards(mainCamera.transform.position, targetPosition, Time.deltaTime * moveSpeed);

//             // Лог, чтобы отслеживать текущую позицию камеры
//             Debug.Log($"Camera position: {mainCamera.transform.position}");

//             yield return null;
//         }

//         // Убедимся, что камера точно достигла позиции, даже если расстояние слишком маленькое
//         mainCamera.transform.position = targetPosition;

//         // Лог после достижения позиции
//         Debug.Log($"Reached target position: {targetPosition}");
//     }

//     // Метод для преобразования позиции RectTransform в мировые координаты
//     private Vector3 RectTransformToWorldPosition(RectTransform rectTransform)
//     {
//         Vector3[] corners = new Vector3[4];
//         rectTransform.GetWorldCorners(corners);
//         return (corners[0] + corners[2]) / 2; // Берем центр
//     }
// }

