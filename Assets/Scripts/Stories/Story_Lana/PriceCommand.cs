using UnityEngine;
using Fungus;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
using Unity.Services.CloudSave;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

[CommandInfo("Custom", 
             "Price", 
             "Отображает количество рубинов на кнопке, обновляет каждые 3 секунды и открывает магазин при нажатии.")]
public class PriceCommand : Command
{
    [Tooltip("Ключ кнопки в ButtonManager.")]
    public string buttonKey;

    [Tooltip("Текст по умолчанию, если количество рубинов не удалось получить.")]
    public string defaultText = "0";

    [Tooltip("Имя сцены магазина, которую нужно открыть при нажатии.")]
    public string shopSceneName = "shopRubin";

    [Tooltip("Интервал обновления цены в секундах.")]
    public float updateInterval = 2f;

    private ButtonManager buttonManager;
    private Coroutine updateCoroutine;
    private bool isActive = true;
    private const string CURRENCY_KEY = "PLAYER_RUBY";

    public override void OnEnter()
    {
        buttonManager = FindObjectOfType<ButtonManager>();

        if (buttonManager == null)
        {
            Debug.LogError("ButtonManager не найден на сцене.");
            Continue();
            return;
        }

        // Инициализация Unity Services
        StartCoroutine(InitializeAndStartUpdating());
    }

    private IEnumerator InitializeAndStartUpdating()
    {
        yield return InitializeUnityServices();

        // Запускаем корутину для периодического обновления
        updateCoroutine = StartCoroutine(UpdatePricePeriodically());

        // Первоначальное обновление
        yield return UpdatePrice();

        // Назначаем обработчик клика для кнопки
        ButtonStyle buttonStyle = buttonManager.buttonStyles.Find(b => b.buttonKey == buttonKey);
        if (buttonStyle != null && buttonStyle.button != null)
        {
            buttonStyle.button.onClick.RemoveAllListeners();
            buttonStyle.button.onClick.AddListener(OpenShopScene);
        }

        Continue();
    }

    private IEnumerator InitializeUnityServices()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            var initialization = UnityServices.InitializeAsync();
            yield return new WaitUntil(() => initialization.IsCompleted);
        }

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            var signIn = AuthenticationService.Instance.SignInAnonymouslyAsync();
            yield return new WaitUntil(() => signIn.IsCompleted);

            if (signIn.IsFaulted)
            {
                Debug.LogError("Failed to sign in anonymously");
            }
        }
    }

    private IEnumerator UpdatePricePeriodically()
    {
        while (isActive)
        {
            yield return new WaitForSeconds(updateInterval);
            yield return UpdatePrice();
        }
    }

    private IEnumerator UpdatePrice()
    {
        if (!isActive) yield break;

        // Загружаем данные из облака
        var loadOperation = CloudSaveService.Instance.Data.LoadAsync(new HashSet<string> { CURRENCY_KEY });
        yield return new WaitUntil(() => loadOperation.IsCompleted);

        int rubyCount = 0;

        if (loadOperation.IsCompleted && !loadOperation.IsFaulted)
        {
            if (loadOperation.Result.TryGetValue(CURRENCY_KEY, out var currencyData))
            {
                try
                {
                    string currencyJson = currencyData.ToString();
                    var currencyDict = JsonConvert.DeserializeObject<Dictionary<string, int>>(currencyJson);
                    if (currencyDict != null && currencyDict.ContainsKey("ruby"))
                    {
                        rubyCount = currencyDict["ruby"];
                    }
                }
                catch
                {
                    Debug.LogWarning("Не удалось распарсить данные о валюте");
                }
            }
        }
        else
        {
            Debug.LogWarning("Не удалось загрузить данные о валюте из облака");
        }

        // Устанавливаем текст на кнопке
        buttonManager.ApplyStyle(buttonKey, rubyCount.ToString());
    }

    private void OpenShopScene()
    {
        if (!string.IsNullOrEmpty(shopSceneName))
        {
            // Останавливаем обновление при переходе в магазин
            isActive = false;
            if (updateCoroutine != null)
            {
                StopCoroutine(updateCoroutine);
            }

            // Сохраняем текущую сцену в истории
            SceneHistoryManager.AddScene(SceneManager.GetActiveScene().name);

            // Загружаем сцену магазина
            SceneManager.LoadScene(shopSceneName);
            Debug.Log($"Сцена '{shopSceneName}' загружена.");
        }
        else
        {
            Debug.LogError("Имя сцены магазина не указано.");
        }
    }

    public override void OnExit()
    {
        // Останавливаем обновление при выходе из команды
        isActive = false;
        if (updateCoroutine != null)
        {
            StopCoroutine(updateCoroutine);
        }
    }

    public override string GetSummary()
    {
        return $"Отображает количество рубинов на кнопке '{buttonKey}' (обновление каждые {updateInterval} сек) и открывает сцену '{shopSceneName}' при нажатии.";
    }
}



[CommandInfo("Custom", "Price Fade Out", "Fades out the price button")]
public class PriceFadeOutCommand : Command
{
    [Tooltip("Key of the button in ButtonManager")]
    public string buttonKey;

    [Tooltip("Fade duration in seconds")]
    public float fadeDuration = 1f;

    private ButtonManager buttonManager;

    public override void OnEnter()
    {
        buttonManager = FindObjectOfType<ButtonManager>();
        if (buttonManager == null)
        {
            Debug.LogError("ButtonManager not found in the scene.");
            Continue();
            return;
        }

        ButtonStyle buttonStyle = buttonManager.buttonStyles.Find(b => b.buttonKey == buttonKey);
        if (buttonStyle == null || buttonStyle.button == null)
        {
            Debug.LogError($"Button with key '{buttonKey}' not found.");
            Continue();
            return;
        }

        GameObject target = buttonStyle.button.gameObject;
        CanvasGroup canvasGroup = target.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = target.AddComponent<CanvasGroup>();
        }

        float startAlpha = canvasGroup.alpha;
        LeanTween.value(target, startAlpha, 0f, fadeDuration)
            .setOnUpdate((float value) =>
            {
                canvasGroup.alpha = value;
            })
            .setOnComplete(() =>
            {
                target.SetActive(false);
                Continue();
            });
    }

    public override string GetSummary()
    {
        return $"Fade out price button '{buttonKey}' over {fadeDuration}s";
    }

    public override Color GetButtonColor()
    {
        return new Color32(221, 184, 169, 255);
    }
}