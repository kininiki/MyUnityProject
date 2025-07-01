using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class VideoBackground : MonoBehaviour
{
    [SerializeField] private RawImage rawImage;
    private VideoPlayer videoPlayer;
    private RenderTexture renderTexture;

    void Start()
    {
        videoPlayer = GetComponent<VideoPlayer>();
        
        // Получаем реальные размеры видео
        float videoWidth = (float)videoPlayer.clip.width;
        float videoHeight = (float)videoPlayer.clip.height;
        float videoRatio = videoWidth / videoHeight;
        
        // Создаём RenderTexture
        renderTexture = new RenderTexture((int)(1080 * videoRatio), 1080, 24);
        videoPlayer.targetTexture = renderTexture;
        videoPlayer.isLooping = true;
        rawImage.texture = renderTexture;

        // Настраиваем RawImage на весь экран
        RectTransform rt = rawImage.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        
        // Настраиваем AspectRatioFitter
        AspectRatioFitter fitter = rawImage.GetComponent<AspectRatioFitter>();
        if (fitter == null)
            fitter = rawImage.gameObject.AddComponent<AspectRatioFitter>();
        
        // Важно: меняем на EnvelopeParent для заполнения всей высоты
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        
        // Используем соотношение сторон экрана
        float screenRatio = (float)Screen.width / Screen.height;
        fitter.aspectRatio = screenRatio;
        
        // Настраиваем UV для центрирования видео
        float scale = videoRatio / screenRatio;
        rawImage.uvRect = new Rect(0.5f - 0.5f, 0, 1, 1);
        
        videoPlayer.Play();
    }

    void Update()
    {
        // Обновляем UV каждый кадр для поддержки разных ориентаций экрана
        float screenRatio = (float)Screen.width / Screen.height;
        float videoRatio = (float)videoPlayer.clip.width / videoPlayer.clip.height;
        float scale = videoRatio / screenRatio;
        rawImage.uvRect = new Rect(0.5f - 0.5f/scale, 0, 1f/scale, 1);
    }
}