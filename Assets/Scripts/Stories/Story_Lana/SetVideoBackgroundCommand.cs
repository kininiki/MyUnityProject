using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using Fungus;
using System.Collections;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class VideoBackgroundManager : MonoBehaviour
{
    private static VideoBackgroundManager instance;
    public static VideoBackgroundManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<VideoBackgroundManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("VideoBackgroundManager");
                    instance = go.AddComponent<VideoBackgroundManager>();
                }
            }
            return instance;
        }
    }

    private CanvasGroup fadeOverlay;
    private bool isTransitionComplete = false;
    private bool isCameraAnimationRunning = false;

    public bool IsTransitionComplete => isTransitionComplete;
    public bool IsCameraAnimationRunning => isCameraAnimationRunning;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            CreateFadeOverlay();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void CreateFadeOverlay()
    {
        GameObject overlayObj = new GameObject("FadeOverlay");
        overlayObj.transform.SetParent(FindObjectOfType<Canvas>().transform, false);
        
        RawImage overlay = overlayObj.AddComponent<RawImage>();
        overlay.color = Color.black;
        overlay.raycastTarget = false;
        
        RectTransform rectTransform = overlay.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.sizeDelta = Vector2.zero;
        rectTransform.localScale = Vector3.one;
        
        fadeOverlay = overlayObj.AddComponent<CanvasGroup>();
        fadeOverlay.alpha = 0;
    }

    public void ResetState()
    {
        isTransitionComplete = false;
        isCameraAnimationRunning = false;
        if (fadeOverlay != null)
        {
            fadeOverlay.alpha = 0;
        }
    }

    public void SetFadeOverlayAlpha(float alpha)
    {
        if (fadeOverlay != null)
        {
            fadeOverlay.alpha = alpha;
        }
    }

    public void SetTransitionComplete(bool complete)
    {
        isTransitionComplete = complete;
    }

    public void SetCameraAnimationRunning(bool running)
    {
        isCameraAnimationRunning = running;
    }
}



[CommandInfo("Background",
    "Set Video Background",
    "Sets a video background with fade in effect and camera movement")]
public class SetVideoBackgroundCommand : Command
{
    [SerializeField] protected RawImage targetImage;
    [SerializeField] protected VideoPlayer videoPlayer;
    [SerializeField] protected string videoClipAddress; // Addressables ключ для видео
    [SerializeField] protected float fadeDuration = 1f;
    [SerializeField] protected bool loop = true;
    [SerializeField] protected RenderTexture renderTexture;

    [Header("Camera Movement Settings")]
    [SerializeField] protected bool enableCameraMovement = false;
    [SerializeField] protected float moveToLeftDuration = 2f;
    [SerializeField] protected float moveToRightDuration = 2f;
    [SerializeField] protected float moveToCenterDuration = 2f;
    [SerializeField] protected float pauseAtLeftDuration = 1f;
    [SerializeField] protected float pauseAtRightDuration = 1f;
    [SerializeField] protected float initialDelay = 0.5f;

    protected RectTransform imageTransform;
    private Coroutine fadeCoroutine;
    private Coroutine cameraCoroutine;

    private VideoClip loadedVideoClip; // Сохранение загруженного клипа

    protected virtual void Awake()
    {
        VideoBackgroundManager.Instance.ResetState();
    }

    void CreateRenderTexture(int width, int height)
    {
        if (renderTexture == null || renderTexture.width != width || renderTexture.height != height)
        {
            if (renderTexture != null)
                renderTexture.Release();

            renderTexture = new RenderTexture(width, height, 24);
            renderTexture.name = "VideoRenderTexture";
        }
    }

    public override void OnEnter()
    {
        if (targetImage == null || videoPlayer == null || string.IsNullOrEmpty(videoClipAddress))
        {
            Debug.LogError("Required components not set in SetVideoBackgroundCommand");
            Continue();
            return;
        }

        // Остановить предыдущие корутины
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        if (cameraCoroutine != null) StopCoroutine(cameraCoroutine);

        VideoBackgroundManager.Instance.ResetState();

        // Начать загрузку видео через Addressables
        StartCoroutine(LoadVideoClipAndPlay());
    }

    IEnumerator LoadVideoClipAndPlay()
    {
        // Загружаем видео через Addressables
        AsyncOperationHandle<VideoClip> handle = Addressables.LoadAssetAsync<VideoClip>(videoClipAddress);
        yield return handle;

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            loadedVideoClip = handle.Result;

            // Создаём рендер-текстуру
            CreateRenderTexture((int)loadedVideoClip.width, (int)loadedVideoClip.height);

            imageTransform = targetImage.GetComponent<RectTransform>();

            // Настройка видео плеера
            videoPlayer.clip = loadedVideoClip;
            videoPlayer.isLooping = loop;
            videoPlayer.playOnAwake = false;
            videoPlayer.targetTexture = renderTexture;
            targetImage.texture = renderTexture;

            AdjustImageSize(loadedVideoClip);

            VideoBackgroundManager.Instance.SetFadeOverlayAlpha(1);
            videoPlayer.Play();

            fadeCoroutine = StartCoroutine(FadeIn());
        }
        else
        {
            Debug.LogError($"Failed to load video clip from Addressables: {videoClipAddress}");
            Continue();
        }
    }

    void AdjustImageSize(VideoClip clip)
    {
        // Настраиваем размер изображения
        float videoAspect = (float)clip.width / clip.height;
        float screenAspect = (float)Screen.width / Screen.height;

        if (videoAspect > screenAspect)
        {
            float height = imageTransform.rect.height;
            float width = height * videoAspect;
            imageTransform.sizeDelta = new Vector2(width, height);
        }
        else
        {
            float width = imageTransform.rect.width;
            float height = width / videoAspect;
            imageTransform.sizeDelta = new Vector2(width, height);
        }

        imageTransform.anchoredPosition = Vector2.zero;
    }

    IEnumerator FadeIn()
    {
        float elapsedTime = 0;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeDuration);
            VideoBackgroundManager.Instance.SetFadeOverlayAlpha(alpha);
            yield return null;
        }
        VideoBackgroundManager.Instance.SetFadeOverlayAlpha(0);
        Continue();
        
    }

    public override string GetSummary()
    {
        if (string.IsNullOrEmpty(videoClipAddress))
            return "Error: No video clip address set";
        return videoClipAddress;
    }

    public override Color GetButtonColor()
    {
        return new Color32(221, 184, 169, 255);
    }

    void OnDestroy()
    {
        if (renderTexture != null)
        {
            renderTexture.Release();
        }

        // Выгружаем видео из памяти
        if (loadedVideoClip != null)
        {
            Addressables.Release(loadedVideoClip);
            loadedVideoClip = null;
        }
    }
}







[CommandInfo("Background",
            "Fade Out Video",
            "Fades out the current video background")]
public class FadeOutVideoCommand : Command
{
    [SerializeField] protected float fadeDuration = 1f;
    [SerializeField] protected VideoPlayer videoPlayer;

    private Coroutine fadeCoroutine;

    IEnumerator FadeOut()
    {
        // Ждем завершения анимации камеры, если она выполняется
        while (VideoBackgroundManager.Instance.IsCameraAnimationRunning)
        {
            yield return null;
        }

        float elapsedTime = 0;
        float startAlpha = 0f;

        // Плавное затемнение
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, 1f, elapsedTime / fadeDuration);
            VideoBackgroundManager.Instance.SetFadeOverlayAlpha(alpha);
            yield return null;
        }

        // После завершения затемнения, устанавливаем полный непрозрачный слой
        VideoBackgroundManager.Instance.SetFadeOverlayAlpha(1);

        // Останавливаем видео
        if (videoPlayer != null)
        {
            videoPlayer.Stop();
        }

        // Выполняем асинхронную очистку памяти с использованием ResourceManager
        yield return PerformMemoryCleanup();

        // Продолжаем выполнение команды Fungus
        Continue();
    }

    public override void OnEnter()
    {
        if (videoPlayer == null)
        {
            Debug.LogError("Video Player not set in FadeOutVideoCommand");
            Continue();
            return;
        }

        // Остановить предыдущий корутин, если он выполняется
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        // Запуск корутины для плавного затемнения видео
        fadeCoroutine = StartCoroutine(FadeOut());
    }

    private IEnumerator PerformMemoryCleanup()
    {
        // Находим ResourceManager в сцене
        ResourceManager resourceManager = FindObjectOfType<ResourceManager>();

        if (resourceManager == null)
        {
            Debug.LogError("ResourceManager not found in the scene.");
            yield break;
        }

        bool cleanupComplete = false;

        // Запускаем асинхронную очистку памяти и используем обратный вызов
        resourceManager.CleanupUnusedAssets(() =>
        {
            cleanupComplete = true;
        });

        // Ждем завершения очистки
        while (!cleanupComplete)
        {
            yield return null;
        }
    }

    public override string GetSummary()
    {
        if (videoPlayer == null)
            return "Error: No video player set";

        return $"Fade out video ({fadeDuration}s)";
    }

    public override Color GetButtonColor()
    {
        return new Color32(221, 184, 169, 255);
    }
}







[CommandInfo("Camera", "Move Around", "Moves the camera sequentially (left, right, center)")]
public class MoveAroundCommand : Command
{
    [SerializeField] protected RawImage targetImage;  // RawImage для видео
    [SerializeField] protected VideoPlayer videoPlayer; // Видео плеер
    [SerializeField] protected float moveToLeftDuration = 2f;
    [SerializeField] protected float moveToRightDuration = 2f;
    [SerializeField] protected float moveToCenterDuration = 2f;
    [SerializeField] protected float initialDelay = 0.5f;
    
    [Header("Camera Movement Settings")]
    [SerializeField] protected bool enableCameraMovement = false;

    protected RectTransform imageTransform;
    private Coroutine cameraCoroutine;

    protected virtual void Awake()
    {
        VideoBackgroundManager.Instance.ResetState();
    }

    void CreateRenderTexture()
    {
        // Используем существующий видео плеер и создаём RenderTexture для видео
        if (videoPlayer.targetTexture == null)
        {
            var renderTexture = new RenderTexture((int)videoPlayer.clip.width, (int)videoPlayer.clip.height, 24);
            renderTexture.name = "VideoRenderTexture";
            videoPlayer.targetTexture = renderTexture;
            targetImage.texture = renderTexture;
        }
    }

    IEnumerator MoveCamera()
    {
        if (!enableCameraMovement)
        {
            VideoBackgroundManager.Instance.SetTransitionComplete(true);
            VideoBackgroundManager.Instance.SetCameraAnimationRunning(false);
            Continue();
            yield break;
        }

        VideoBackgroundManager.Instance.SetCameraAnimationRunning(true);

        // Получаем начальную позицию (центр)
        Vector2 centerPos = imageTransform.anchoredPosition;

        // Получаем Canvas и его масштаб
        Canvas canvas = targetImage.canvas;
        float canvasScaleFactor = canvas.scaleFactor;

        // Получаем реальные размеры на экране с учетом масштаба
        float screenWidth = Screen.width / canvasScaleFactor;
        float imageWidth = imageTransform.rect.width;

        // Расчет максимального смещения с учетом масштаба UI
        float maxOffset = (imageWidth - screenWidth) / 2;

        // Если видео недостаточно широкое для движения камеры
        if (maxOffset <= 0)
        {
            VideoBackgroundManager.Instance.SetTransitionComplete(true);
            VideoBackgroundManager.Instance.SetCameraAnimationRunning(false);
            Continue();
            yield break;
        }

        // Добавляем небольшой отступ для избежания пустых краев
        maxOffset = Mathf.Max(0, maxOffset - 25);

        // Начальная пауза
        yield return new WaitForSeconds(initialDelay);

        // 1. Движение из центра влево
        float startX = centerPos.x;
        float targetX = -maxOffset;
        float elapsedTime = 0;

        while (elapsedTime < moveToLeftDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / moveToLeftDuration;
            float smoothT = Mathf.SmoothStep(0, 1, t);
            float newX = Mathf.Lerp(startX, targetX, smoothT);
            imageTransform.anchoredPosition = new Vector2(newX, centerPos.y);
            yield return null;
        }

        imageTransform.anchoredPosition = new Vector2(targetX, centerPos.y);
        yield return new WaitForSeconds(1f); // Пауза влево

        // 2. Движение слева вправо
        startX = -maxOffset;
        targetX = maxOffset;
        elapsedTime = 0;

        while (elapsedTime < moveToRightDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / moveToRightDuration;
            float smoothT = Mathf.SmoothStep(0, 1, t);
            float newX = Mathf.Lerp(startX, targetX, smoothT);
            imageTransform.anchoredPosition = new Vector2(newX, centerPos.y);
            yield return null;
        }

        imageTransform.anchoredPosition = new Vector2(targetX, centerPos.y);
        yield return new WaitForSeconds(1f); // Пауза вправо

        // 3. Возврат в центр
        startX = maxOffset;
        targetX = centerPos.x;
        elapsedTime = 0;

        while (elapsedTime < moveToCenterDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / moveToCenterDuration;
            float smoothT = Mathf.SmoothStep(0, 1, t);
            float newX = Mathf.Lerp(startX, targetX, smoothT);
            imageTransform.anchoredPosition = new Vector2(newX, centerPos.y);
            yield return null;
        }

        imageTransform.anchoredPosition = centerPos;

        VideoBackgroundManager.Instance.SetCameraAnimationRunning(false);
        VideoBackgroundManager.Instance.SetTransitionComplete(true);
        Continue();
    }

    public override void OnEnter()
    {
        if (targetImage == null || videoPlayer == null)
        {
            Debug.LogError("Required components not set in MoveAroundCommand");
            Continue();
            return;
        }

        // Остановить предыдущие корутины, если они выполняются
        if (cameraCoroutine != null)
            StopCoroutine(cameraCoroutine);

        VideoBackgroundManager.Instance.ResetState();
        imageTransform = targetImage.GetComponent<RectTransform>();

        // Создаем RenderTexture только если его нет
        CreateRenderTexture();

        // Запускаем движение камеры
        cameraCoroutine = StartCoroutine(MoveCamera());
    }

    public override string GetSummary()
    {
        if (targetImage == null)
            return "Error: No target image set";

        if (videoPlayer == null)
            return "Error: No video player set";

        string summary = "Camera Move (Left, Right, Center)";
        return summary;
    }

    public override Color GetButtonColor()
    {
        return new Color32(221, 184, 169, 255);
    }
}
