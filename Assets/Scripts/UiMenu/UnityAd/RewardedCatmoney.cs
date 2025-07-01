using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Advertisements;
using Unity.Services.Core;
using Unity.Services.CloudSave;
using Unity.Services.Authentication;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;

public class RewardedCatmoney : MonoBehaviour, IUnityAdsLoadListener, IUnityAdsShowListener
{
    [SerializeField] Button _showAdButton;
    [SerializeField] string _androidAdUnitId = "Rewarded_Android";
    [SerializeField] string _iOSAdUnitId = "Rewarded_iOS";
    [SerializeField] private TMPro.TMP_Text _timerText; // TextMeshPro для таймера
    [SerializeField] private TMPro.TMP_Text _adCountText; // TextMeshPro для счетчика рекламы

    private string _adUnitId = null;

    private float _timeRemaining = 0f; // Таймер в секундах
    private bool _isTimerRunning = false; // Флаг для проверки, активен ли таймер

    private const float _timerDuration = 86400f; // Таймер 24 часа (в секундах)
    private const int _maxAdViews = 1; // Максимальное количество просмотров рекламы
    private int _currentAdViews = 0; // Текущее количество просмотров рекламы
    private int _adViewsForOneRuby = 1; // Количество просмотров рекламы для начисления 1 эликсира

    void Awake()
    {
#if UNITY_IOS
        _adUnitId = _iOSAdUnitId;
#elif UNITY_ANDROID
        _adUnitId = _androidAdUnitId;
#endif

        _showAdButton.interactable = false; // Отключаем кнопку
        UpdateTimerText(); // Устанавливаем текст таймера изначально
        UpdateAdCountText(); // Устанавливаем текст счетчика рекламы
    }

    void Update()
    {
        // Если таймер активен, обновляем его каждую секунду
        if (_isTimerRunning)
        {
            _timeRemaining -= Time.deltaTime;
            if (_timeRemaining <= 0)
            {
                _timeRemaining = 0;
                _isTimerRunning = false;

                // Сбрасываем счетчик рекламы
                _currentAdViews = 0;
                UpdateAdCountText();

                // Делаем кнопку активной снова
                _showAdButton.interactable = true;
                _timerText.text = ""; // Очищаем текст таймера
            }
            else
            {
                UpdateTimerText();
            }
        }
        else if (!_showAdButton.interactable)
        {
            // Если реклама не подгружена, показываем сообщение
            _timerText.text = "The ad is loading...";
        }
    }

    // Метод для обновления отображения таймера
    private void UpdateTimerText()
    {
        if (_timeRemaining > 0)
        {
            int hours = Mathf.FloorToInt(_timeRemaining / 3600f);
            int minutes = Mathf.FloorToInt((_timeRemaining % 3600) / 60f);
            int seconds = Mathf.FloorToInt(_timeRemaining % 60);

            // Форматируем время и обновляем текст
            _timerText.text = string.Format("{0:D2}:{1:D2}:{2:D2}", hours, minutes, seconds);
        }
        else
        {
            _timerText.text = "";
        }
    }

    // Метод для обновления отображения счетчика рекламы
    private void UpdateAdCountText()
    {
        _adCountText.text = $"{_currentAdViews}/{_maxAdViews}";
    }

    public void LoadAd()
    {
        Debug.Log("Loading Ad: " + _adUnitId);
        Advertisement.Load(_adUnitId, this);
    }

    public void OnUnityAdsAdLoaded(string adUnitId)
    {
        Debug.Log("Ad Loaded: " + adUnitId);

        if (adUnitId.Equals(_adUnitId))
        {
            _showAdButton.onClick.AddListener(ShowAd);
            _showAdButton.interactable = true; // Включаем кнопку
        }
    }

    public void ShowAd()
    {
        _showAdButton.interactable = false; // Отключаем кнопку
        //Advertisement.Show(_adUnitId, this);
        if (Advertisement.isInitialized)
        {
            Advertisement.Load(_adUnitId);
            Advertisement.Show(_adUnitId, this);
        }
            else
        {
            HandleAdFailure(); // Если реклама недоступна, выполняем логику начисления
        }
    }

    private void HandleAdFailure()
    {
        Debug.Log("Ad not available. Granting bonus progress.");

        // Увеличиваем счетчик просмотров рекламы
        _currentAdViews++;
        UpdateAdCountText();

        // Начисляем рубины за каждый 1 просмотр
        if (_currentAdViews % _adViewsForOneRuby == 0)
        {
            GameCloud.Instance.UpdateResource("CATMONEY_ELIXIR", 3);
        }

        // Если счетчик рекламы достигает 10/10, запускаем таймер
        if (_currentAdViews >= _maxAdViews)
        {
            _timeRemaining = _timerDuration;
            _isTimerRunning = true;
            _showAdButton.interactable = false; // Отключаем кнопку до окончания таймера
        }
    }


    public void OnUnityAdsShowComplete(string adUnitId, UnityAdsShowCompletionState showCompletionState)
    {
        if (adUnitId.Equals(_adUnitId) && showCompletionState.Equals(UnityAdsShowCompletionState.COMPLETED))
        {
            Debug.Log("Unity Ads Rewarded Ad Completed");

            // Увеличиваем счетчик рекламы
            _currentAdViews++;
            UpdateAdCountText();

            // Начисляем котомани за каждый 1 просмотр
            if (_currentAdViews % _adViewsForOneRuby == 0)
            {
                GameCloud.Instance.UpdateResource("CATMONEY_ELIXIR", 1);
            }

            // Если счетчик рекламы достигает 1/1, запускаем таймер
            if (_currentAdViews >= _maxAdViews)
            {
                _timeRemaining = _timerDuration;
                _isTimerRunning = true;
                _showAdButton.interactable = false; // Отключаем кнопку до окончания таймера
            }
        }
    }

    public void OnUnityAdsFailedToLoad(string adUnitId, UnityAdsLoadError error, string message) { }
    public void OnUnityAdsShowFailure(string adUnitId, UnityAdsShowError error, string message) { }
    public void OnUnityAdsShowStart(string adUnitId) { }
    public void OnUnityAdsShowClick(string adUnitId) { }
}
