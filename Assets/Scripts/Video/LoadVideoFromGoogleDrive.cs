using UnityEngine;
using UnityEngine.Video;
using UnityEngine.Networking;
using System.Collections;
using System.IO;

public class LoadVideoFromGoogleDrive : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public string videoUrl = "https://drive.google.com/uc?export=download&id=1VGQbjpQ7R5pJZbrhDWeXT3MpTcBSPm_s";

    IEnumerator Start()
    {
        string tempPath = null;

        // Загрузка видео
        UnityWebRequest request = UnityWebRequest.Get(videoUrl);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            tempPath = Path.Combine(Application.temporaryCachePath, "video.mp4");
            
            // Удаляем старый файл, если он существует
            if (File.Exists(tempPath)) File.Delete(tempPath);

            // Сохраняем видео
            File.WriteAllBytes(tempPath, request.downloadHandler.data);

            // Воспроизводим видео
            videoPlayer.url = tempPath;
            videoPlayer.Play();
        }
        else
        {
            Debug.LogError("Ошибка загрузки видео: " + request.error);
            yield break;
        }

        // Ждём окончания воспроизведения
        yield return new WaitWhile(() => videoPlayer.isPlaying);

        // Добавляем задержку для освобождения файла
        yield return new WaitForSeconds(1f);

        // Удаляем файл с обработкой исключений
        if (tempPath != null)
        {
            try
            {
                File.Delete(tempPath);
                Debug.Log("Видео удалено из кеша");
            }
            catch (System.Exception ex)
            {
                Debug.LogError("Ошибка удаления файла: " + ex.Message);
            }
        }
    }
}