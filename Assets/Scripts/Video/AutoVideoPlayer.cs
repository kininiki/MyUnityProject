// using Sttplay.MediaPlayer;
// using UnityEngine;

// public class SimpleVideoPlayer : MonoBehaviour
// {
//     public UnitySCPlayerPro player;
//     public string videoFileName = "фонзвёзды.mp4";

//     void Start()
//     {
//         // Настройка параметров
//         player.loop = true;
//         player.openAndPlay = true;
//         player.enableHWAccel = true;

//         // Установка пути к видео
//         string videoPath = System.IO.Path.Combine(Application.streamingAssetsPath, videoFileName);

//         // Запуск воспроизведения с явным указанием параметров
//         player.Open(
//             MediaType.VIDEO, // Тип медиа (уточните в документации SCPlayerPro)
//             videoPath             // Полный путь к файлу
//         );

//         // Настройка размера (пример для плоскости 16:9)
//         transform.localScale = new Vector3(16, 9, 1);
//     }

//     void OnDestroy()
//     {
//         player.Close();
//     }
// }