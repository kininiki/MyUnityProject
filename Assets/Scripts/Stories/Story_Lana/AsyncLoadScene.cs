using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Fungus;
using UnityEngine.UI;
using UnityEngine.AddressableAssets; // Для очистки Addressables
using UnityEngine.ResourceManagement.AsyncOperations; // Для работы с Addressables

[CommandInfo("Scene",
             "Async Load Scene",
             "Loads a scene asynchronously while showing a loading sprite.")]
public class AsyncLoadSceneCommand : Command
{
    [Tooltip("The name of the scene to load asynchronously")]
    [SerializeField] private string sceneName;

    [Tooltip("Sprite to display while loading the scene")]
    [SerializeField] private Sprite loadingSprite;

    [Tooltip("The Canvas where the loading sprite will be displayed")]
    [SerializeField] private Canvas loadingCanvasPrefab;

    private GameObject loadingCanvasInstance;

    public override void OnEnter()
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            StartCoroutine(LoadSceneAsync());
        }
        else
        {
            Debug.LogWarning("Scene name is empty!");
            Continue();
        }
    }

    private IEnumerator LoadSceneAsync()
    {
        // Очистка ресурсов перед загрузкой новой сцены
        yield return StartCoroutine(UnloadUnusedResources());

        // Создаём и показываем панель загрузки
        SetupLoadingCanvas();

        // Асинхронная загрузка сцены
        AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(sceneName);
        asyncOperation.allowSceneActivation = false;

        // Отображаем прогресс загрузки
        while (!asyncOperation.isDone)
        {
            float progress = Mathf.Clamp01(asyncOperation.progress / 0.9f);
            Debug.Log($"Loading progress: {progress * 100}%");

            // Минимальная нагрузка на CPU
            yield return null;

            if (asyncOperation.progress >= 0.9f)
            {
                // Завершаем загрузку
                asyncOperation.allowSceneActivation = true;
            }
        }

        // Убираем панель загрузки
        DestroyLoadingCanvas();
        Continue();
    }

    private IEnumerator UnloadUnusedResources()
    {
        Debug.Log("Unloading unused assets and clearing memory...");

        // Очистка Addressables Cache
        AsyncOperationHandle handle = Addressables.CleanBundleCache();
        yield return handle;

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            Debug.Log("Addressables cache cleared successfully.");
        }
        else
        {
            Debug.LogWarning("Failed to clear Addressables cache.");
        }

        // Очистка неиспользуемых ресурсов
        yield return Resources.UnloadUnusedAssets();

        // Принудительный вызов сборщика мусора
        System.GC.Collect();
        Debug.Log("Unused assets unloaded and garbage collector triggered.");
    }

    private void SetupLoadingCanvas()
    {
        if (loadingCanvasPrefab != null)
        {
            Canvas canvasInstance = Instantiate(loadingCanvasPrefab);
            loadingCanvasInstance = canvasInstance.gameObject;

            loadingCanvasInstance.name = "LoadingCanvas";

            Image loadingImage = loadingCanvasInstance.GetComponentInChildren<Image>();
            if (loadingImage != null && loadingSprite != null)
            {
                loadingImage.sprite = loadingSprite;
                loadingImage.preserveAspect = true;
            }
        }
        else
        {
            Debug.LogWarning("Loading Canvas Prefab is not assigned!");
        }
    }

    private void DestroyLoadingCanvas()
    {
        if (loadingCanvasInstance != null)
        {
            Destroy(loadingCanvasInstance);
        }
    }

    public override string GetSummary()
    {
        return !string.IsNullOrEmpty(sceneName) ? $"Load Scene: {sceneName}" : "No scene selected.";
    }

    public override Color GetButtonColor()
    {
        return new Color32(184, 255, 184, 255); // Зеленоватый цвет для кнопки команды
    }
}
