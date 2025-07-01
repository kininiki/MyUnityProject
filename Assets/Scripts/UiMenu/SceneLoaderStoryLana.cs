// using UnityEngine;
// using UnityEngine.UI;
// using UnityEngine.SceneManagement;

// public class SceneLoaderStoryLana : MonoBehaviour
// {
//     private void Start()
//     {
//         Button button = GetComponent<Button>();
//         button.onClick.AddListener(LoadStoryLanaScene);
//     }

//     private void LoadStoryLanaScene()
//     {
//         // Сохраняем имя текущей сцены перед переходом
//         PlayerPrefs.SetString("PreviousScene", SceneManager.GetActiveScene().name);
//         SceneManager.LoadScene("Chapter1_Lana");
//     }
// }

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Localization.Settings;

public class AsyncSceneLoaderWithPanel : MonoBehaviour
{
    public GameObject loadingPanel; // Панель загрузки
    public Slider progressBar; // Ползунок для отображения прогресса
    public TextMeshProUGUI progressText; // Текст для отображения процентов загрузки
    public AudioSource menuMusic; // Музыка главного меню
    public float minimumLoadingTime = 15f; // Минимальное время загрузки (в секундах)

    private static bool suppressLogsInNextScene = false;

    // Метод для запуска загрузки сцены
    public void StartSceneLoad(string sceneName)
    {
        if (menuMusic != null)
        {
            menuMusic.Stop(); // Отключаем музыку
        }

        loadingPanel.SetActive(true); // Включаем панель загрузки
        suppressLogsInNextScene = true;

        StartCoroutine(LoadLocalizationAndScene(sceneName));
    }

    /// <summary>
    /// Загружает локализацию и затем асинхронно загружает сцену.
    /// </summary>
    /// <param name="sceneName">Имя сцены для загрузки</param>
    private IEnumerator LoadLocalizationAndScene(string sceneName)
    {
        // Шаг 1: Загружаем локализацию
        yield return StartCoroutine(LoadLocalization());

        // Шаг 2: Начинаем асинхронную загрузку сцены
        yield return StartCoroutine(LoadSceneAsync(sceneName));
    }

    /// <summary>
    /// Загружает и инициализирует локализационные таблицы Unity Localization.
    /// </summary>
    private IEnumerator LoadLocalization()
    {
        Debug.Log("Начало загрузки локализации...");

        var localizationOperation = LocalizationSettings.InitializationOperation;

        while (!localizationOperation.IsDone)
        {
            // Показываем текст о загрузке локализации (опционально)
            progressText.text = "Загрузка локализации...";
            yield return null;
        }

        if (localizationOperation.IsDone)
        {
            Debug.Log("Локализация загружена успешно.");
        }
        else
        {
            Debug.LogError("Ошибка при загрузке локализации!");
        }
    }

    /// <summary>
    /// Асинхронно загружает сцену с прогрессом.
    /// </summary>
    private IEnumerator LoadSceneAsync(string sceneName)
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;

        float elapsedTime = 0f;
        float artificialProgress = 0f;

        Resources.UnloadUnusedAssets();
        System.GC.Collect();

        while (!operation.isDone)
        {
            float realProgress = Mathf.Clamp01(operation.progress / 0.9f);

            if (artificialProgress < realProgress)
            {
                artificialProgress = Mathf.MoveTowards(artificialProgress, realProgress, Time.deltaTime / minimumLoadingTime);
            }

            progressBar.value = artificialProgress;
            progressText.text = $"Загрузка: {(artificialProgress * 100):0}%";

            if (realProgress >= 0.9f && elapsedTime >= minimumLoadingTime)
            {
                yield return new WaitForSeconds(0.5f);
                operation.allowSceneActivation = true;
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }

    public static bool ShouldSuppressLogs()
    {
        return suppressLogsInNextScene;
    }

    public static void ResetSuppressLogsFlag()
    {
        suppressLogsInNextScene = false;
    }
}
