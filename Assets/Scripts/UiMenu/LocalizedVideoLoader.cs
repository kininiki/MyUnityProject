using UnityEngine;
using UnityEngine.Video;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class LocalizedVideoLoader : MonoBehaviour
{
    public VideoPlayer videoPlayer; // Ссылка на VideoPlayer
    public string videoKey = "introVideoLana"; // Ключ для видео в таблице локализации

    private void Start()
    {
        // Подписываемся на событие смены языка
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;

        // Загружаем видео при запуске сцены
        LoadLocalizedVideo();
    }

    private async void LoadLocalizedVideo()
    {
        // Ждём инициализацию локализации
        await LocalizationSettings.InitializationOperation.Task;

        // Получаем путь к локализованному видео
        string localizedPath = LocalizationSettings.StringDatabase.GetLocalizedString("VideoPaths", videoKey);

        // Загружаем видео из папки Resources
        VideoClip videoClip = Resources.Load<VideoClip>(localizedPath);

        if (videoClip != null)
        {
            videoPlayer.clip = videoClip;
            videoPlayer.Play();
        }
        else
        {
            Debug.LogError("Не удалось загрузить локализованное видео по пути: " + localizedPath);
        }
    }

    // Метод вызывается при смене языка
    private void OnLocaleChanged(UnityEngine.Localization.Locale locale)
    {
        Debug.Log("Язык был изменён. Загружаем новое видео...");
        LoadLocalizedVideo(); // Перезагружаем локализованное видео
    }

    private void OnDestroy()
    {
        // Отписываемся от события при уничтожении объекта
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }
}
