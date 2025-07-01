using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using Unity.Services.CloudSave;
using Unity.Services.Authentication;

public class HungerManager : MonoBehaviour
{
    [Header("UI Elements")]
    public Slider hungerSlider;               // Ползунок
    public Image sliderFillImage;             // Заливка слайдера
//    public TextMeshProUGUI foodText;          // Текст количества корма
    public TextMeshProUGUI timerText;         // Текст таймера
    public TextMeshProUGUI feedingDayText;    // Текст дней кормления
    public Button feedButton;                 // Кнопка "Накормить"

    [Header("Hunger Settings")]
    private int hungerLevel;                  // Текущее значение шкалы
    public int maxHungerLevel = 14;           // Максимум делений
    public int initialHungerLevel = 7;        // Начальное значение шкалы

    [Header("Food Settings")]
    private int food;                    // Количество корма
    private DateTime nextFoodTime;            // Время начисления нового корма
    private DateTime lastLoginTime;           // Время последнего входа в игру
    private const int HoursToWait = 24;       // Таймер на 24 часа для корма
    private const int DaysToDecrease = 2;     // Таймер на 2 дня для уменьшения шкалы

    private int feedingDays;                  // Количество дней, когда кота покормили

    private Gradient gradient;                // Градиент для цветов

    [Header("Cat Images")]
    public RawImage happyCatImage;    // Изображение счастливого кота
    public RawImage heartsImage;      // Изображение с сердечками
    public RawImage angryCatImage;    // Изображение злого кота
    public RawImage sadCatImage;    // Изображение грустного кота
    public RawImage catImage;       // Базовое изображение кота (подложка)

    [Header("Second Cat Appearance")]
    public RawImage happyCatImage2;
    public RawImage heartsImage2;
    public RawImage angryCatImage2;
    public RawImage sadCatImage2;
    public RawImage catImage2;

    private int currentCatAppearance = 1; // 1 - первая внешность, 2 - вторая

    public GameObject pinkZoneViewport; // Viewport для розовой зоны
    public GameObject pinkZoneViewportOnFeed; // Viewport для розовой зоны при кормлении


    public void SwitchCatAppearance()
    {
        // Переключаем внешность
        currentCatAppearance = currentCatAppearance == 1 ? 2 : 1;

        // Сохраняем выбор в PlayerPrefs
        PlayerPrefs.SetInt("CurrentCatAppearance", currentCatAppearance);
        PlayerPrefs.Save();

        // Обновляем UI с учётом новой внешности
        UpdateCatImages();
    }



    private void Start()
    {
        
        InitializeGame();
        UpdateUI();

        // Добавляем слушатель кнопки "Накормить"
        feedButton.onClick.AddListener(FeedTamagotchi);
    }

    private void Update()
    {
 //       UpdateTimer();
        CheckIdleTime();
    }

    private bool isTimerActive = false; // Флаг активности таймера
    
    private void InitializeGame()
    {
        hungerSlider.interactable = false;

        // Загружаем сохранённые данные
        food = GameCloud.Instance.GetCurrencyAmount("PLAYER_FOOD");
        hungerLevel = PlayerPrefs.GetInt("HungerLevel", initialHungerLevel);
        feedingDays = PlayerPrefs.GetInt("FeedingDays", 0);

        string savedTime = PlayerPrefs.GetString("NextFoodTime", "");
        if (!string.IsNullOrEmpty(savedTime))
        {
            nextFoodTime = DateTime.Parse(savedTime);
            isTimerActive = PlayerPrefs.GetInt("IsTimerActive", 0) == 1; // Загружаем состояние таймера
        }

        string lastLogin = PlayerPrefs.GetString("LastLoginTime", "");
        if (!string.IsNullOrEmpty(lastLogin))
            lastLoginTime = DateTime.Parse(lastLogin);
        else
            lastLoginTime = DateTime.Now;

        // Загружаем текущую внешность кота
        currentCatAppearance = PlayerPrefs.GetInt("CurrentCatAppearance", 1);

        hungerSlider.maxValue = maxHungerLevel;
        hungerSlider.value = hungerLevel;
        CreateGradient();
        UpdateUI();

        // Проверяем, нужно ли выдавать награду за розовую зону
        CheckPinkZoneReward();
    }

    private void CheckPinkZoneReward()
    {
        float normalizedValue = hungerSlider.value / hungerSlider.maxValue;

        // Проверяем, находится ли шкала в розовой зоне
        if (normalizedValue >= 0.75f)
        {
            string lastRewardDate = PlayerPrefs.GetString("LastPinkZoneReward", "");

            // Проверяем, выдавался ли бонус сегодня
            if (lastRewardDate != DateTime.Now.Date.ToString("yyyy-MM-dd"))
            {
                // Начисляем награды
                GameCloud.Instance.UpdateResource("CATMONEY_ELIXIR", 7);
                GameCloud.Instance.UpdateResource("PLAYER_RUBY", 15);
                GameCloud.Instance.UpdateResource("PLAYER_ELIXIR", 6);

                // Запоминаем дату последнего бонуса
                PlayerPrefs.SetString("LastPinkZoneReward", DateTime.Now.Date.ToString("yyyy-MM-dd"));
                PlayerPrefs.Save();

                // Показываем viewport
                if (pinkZoneViewport != null)
                {
                    pinkZoneViewport.SetActive(true);
                }

                Debug.Log("Выдана награда за розовую зону!");
            }
        }
    }


private void CheckIfEnteredPinkZone(int previousHungerLevel, int newHungerLevel)
{
    float prevNormalized = (float)previousHungerLevel / maxHungerLevel;
    float newNormalized = (float)newHungerLevel / maxHungerLevel;

    // Если раньше был НЕ в розовой зоне, а теперь в неё попал
    if (prevNormalized < 0.75f && newNormalized >= 0.75f)
    {
        string lastRewardDate = PlayerPrefs.GetString("LastPinkZoneRewardOnFeed", "");

        if (lastRewardDate != DateTime.Now.Date.ToString("yyyy-MM-dd"))
        {
            // Начисляем награды
            GameCloud.Instance.UpdateResource("CATMONEY_ELIXIR", 7);
            GameCloud.Instance.UpdateResource("PLAYER_RUBY", 15);
            GameCloud.Instance.UpdateResource("PLAYER_ELIXIR", 6);

            // Запоминаем дату последнего бонуса
            PlayerPrefs.SetString("LastPinkZoneRewardOnFeed", DateTime.Now.Date.ToString("yyyy-MM-dd"));
            PlayerPrefs.Save();

            // Показываем viewport
            if (pinkZoneViewportOnFeed != null)
            {
                pinkZoneViewportOnFeed.SetActive(true);
            }

            Debug.Log("Игрок впервые достиг розовой зоны при кормлении! Выдана награда.");
        }
    }
}


    private void FeedTamagotchi()
    {
        int foodNeeded = GetFoodCost();
        int food = GameCloud.Instance.GetCurrencyAmount("PLAYER_FOOD");
        if (food > 0)
        {
            if (food >= foodNeeded)
            {
                int previousHungerLevel = hungerLevel; // Запоминаем прошлый уровень
                hungerLevel = Mathf.Clamp(hungerLevel + 1, 0, maxHungerLevel);

                // Проверяем, достиг ли игрок розовой зоны
                CheckIfEnteredPinkZone(previousHungerLevel, hungerLevel);
            }
            // Если корма достаточно
            //foodCount -= foodCount;
            GameCloud.Instance.UpdateResource("PLAYER_FOOD", -food);

            // Проверяем первое кормление за сутки
            if (PlayerPrefs.GetString("LastFeedingDay", "") != DateTime.Now.Date.ToString("yyyy-MM-dd"))
            {
                feedingDays++;
                PlayerPrefs.SetString("LastFeedingDay", DateTime.Now.Date.ToString("yyyy-MM-dd"));
                Debug.Log("Первое кормление за день! Дней кормления: " + feedingDays);
            }

            // Запускаем таймер, если он ещё не активен
            if (!isTimerActive)
            {
                isTimerActive = true;
                nextFoodTime = DateTime.Now.AddHours(HoursToWait);
                PlayerPrefs.SetString("NextFoodTime", nextFoodTime.ToString());
                PlayerPrefs.SetInt("IsTimerActive", 1);
            }

            // Обновляем время последнего входа
            lastLoginTime = DateTime.Now;
            PlayerPrefs.SetString("LastLoginTime", lastLoginTime.ToString());

            SaveData();
            UpdateUI();
        }
        else if (food <= 0)
        {
            // Если корма недостаточно, загружаем магазин с вкладкой Catmoney
            Debug.Log("Недостаточно корма! Переход в магазин на вкладку Catmoney.");

            // Сохраняем текущую сцену как предыдущую
            PlayerPrefs.SetString("PreviousScene", SceneManager.GetActiveScene().name);
            PlayerPrefs.Save();

            SceneLoaderShop.selectedScrollView = "Catmoney"; // Устанавливаем вкладку
            SceneLoaderShop sceneLoader = FindObjectOfType<SceneLoaderShop>();
            if (sceneLoader != null)
            {
                sceneLoader.LoadShopSceneWithCatmoney(); // Загружаем магазин
            }
            else
            {
                Debug.LogError("SceneLoaderShop не найден в текущей сцене!");
            }
        }
    }



    private int GetFoodCost()
    {
        float normalizedValue = hungerSlider.value / hungerSlider.maxValue;

        if (normalizedValue < 0.25f) // Красная зона
            return 1;
        else if (normalizedValue < 0.5f) // Жёлтая зона и левая половина зелёной
            return 2;
        else if (normalizedValue < 0.75f) // Правая половина зелёной зоны
            return 4;
        else // Розовая зона
            return 5;
    }

    private void CheckIdleTime()
    {
        TimeSpan idleTime = DateTime.Now - lastLoginTime;
        if (idleTime.TotalDays >= DaysToDecrease)
        {
            hungerLevel = Mathf.Clamp(hungerLevel - 1, 0, maxHungerLevel);
            lastLoginTime = DateTime.Now;
            SaveData();
            UpdateUI();
        }
    }

    private void UpdateUI()
    {
        hungerSlider.value = hungerLevel;
//        foodText.text = $"{foodCount}";
        feedingDayText.text = $"{feedingDays}/150";
        UpdateSliderColor();
        UpdateCatImages(); // Новый метод для отображения изображений
    }

    private void UpdateCatImages()
    {
        float normalizedValue = hungerSlider.value / hungerSlider.maxValue;

        // Скрываем все эмоции по умолчанию
        happyCatImage.gameObject.SetActive(false);
        heartsImage.gameObject.SetActive(false);
        angryCatImage.gameObject.SetActive(false);
        sadCatImage.gameObject.SetActive(false);
        catImage.gameObject.SetActive(false);

        happyCatImage2.gameObject.SetActive(false);
        heartsImage2.gameObject.SetActive(false);
        angryCatImage2.gameObject.SetActive(false);
        sadCatImage2.gameObject.SetActive(false);
        catImage2.gameObject.SetActive(false);

        // Логика отображения эмоций для текущей внешности
        if (currentCatAppearance == 1)
        {
            catImage.gameObject.SetActive(true);

            if (normalizedValue >= 0.75f) // Розовая зона
            {
                happyCatImage.gameObject.SetActive(true);
                heartsImage.gameObject.SetActive(true);
            }
            else if (normalizedValue >= 0.25f && normalizedValue < 0.5f) // Жёлтая зона
            {
                sadCatImage.gameObject.SetActive(true);
            }
            else if (normalizedValue < 0.25f) // Красная зона
            {
                angryCatImage.gameObject.SetActive(true);
            }
        }
        else if (currentCatAppearance == 2)
        {
            catImage2.gameObject.SetActive(true);

            if (normalizedValue >= 0.75f) // Розовая зона
            {
                happyCatImage2.gameObject.SetActive(true);
                heartsImage2.gameObject.SetActive(true);
            }
            else if (normalizedValue >= 0.25f && normalizedValue < 0.5f) // Жёлтая зона
            {
                sadCatImage2.gameObject.SetActive(true);
            }
            else if (normalizedValue < 0.25f) // Красная зона
            {
                angryCatImage2.gameObject.SetActive(true);
            }
        }
    }


    private void SaveData()
    {
        PlayerPrefs.SetInt("FoodCount", food);
        PlayerPrefs.SetInt("HungerLevel", hungerLevel);
        PlayerPrefs.SetInt("FeedingDays", feedingDays);
        PlayerPrefs.SetString("NextFoodTime", nextFoodTime.ToString());
        PlayerPrefs.SetInt("IsTimerActive", isTimerActive ? 1 : 0);
        PlayerPrefs.SetString("LastLoginTime", lastLoginTime.ToString());
        PlayerPrefs.Save();
    }

    private void CreateGradient()
    {
        gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(Color.red, 0f),
                new GradientColorKey(Color.yellow, 0.25f),
                new GradientColorKey(Color.green, 0.5f),
                new GradientColorKey(Color.magenta, 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
            }
        );
        UpdateSliderColor();
    }

    private void UpdateSliderColor()
    {
        float normalizedValue = hungerSlider.value / hungerSlider.maxValue;
        sliderFillImage.color = gradient.Evaluate(normalizedValue);
    }
}








// using System;
// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.UI;
// using TMPro;
// using UnityEngine.SceneManagement;
// using Unity.Services.CloudSave;
// using Unity.Services.Authentication;
// using System.Threading.Tasks;
// using Newtonsoft.Json;

// public class HungerManager : MonoBehaviour
// {
//     [Header("UI Elements")]
//     public Slider hungerSlider;               // Ползунок
//     public Image sliderFillImage;             // Заливка слайдера
//     public TextMeshProUGUI timerText;         // Текст таймера
//     public TextMeshProUGUI feedingDayText;    // Текст дней кормления
//     public Button feedButton;                 // Кнопка "Накормить"

//     [Header("Hunger Settings")]
//     private int hungerLevel;                  // Текущее значение шкалы
//     public int maxHungerLevel = 14;           // Максимум делений
//     public int initialHungerLevel = 7;        // Начальное значение шкалы

//     [Header("Food Settings")]
//     private int food;                         // Количество корма
//     private DateTime nextFoodTime;            // Время начисления нового корма
//     private DateTime lastLoginTime;           // Время последнего входа в игру
//     private const int HoursToWait = 24;       // Таймер на 24 часа для корма
//     private const int DaysToDecrease = 2;     // Таймер на 2 дня для уменьшения шкалы

//     private int feedingDays;                  // Количество дней, когда кота покормили

//     private Gradient gradient;                // Градиент для цветов

//     [Header("Cat Images")]
//     public RawImage happyCatImage;    // Изображение счастливого кота
//     public RawImage heartsImage;      // Изображение с сердечками
//     public RawImage angryCatImage;    // Изображение злого кота
//     public RawImage sadCatImage;      // Изображение грустного кота
//     public RawImage catImage;         // Базовое изображение кота (подложка)

//     [Header("Second Cat Appearance")]
//     public RawImage happyCatImage2;
//     public RawImage heartsImage2;
//     public RawImage angryCatImage2;
//     public RawImage sadCatImage2;
//     public RawImage catImage2;

//     [Header("Loading Screen")]
//     public GameObject loadingScreen; // Панель загрузки
//     public TextMeshProUGUI loadingText; // Текст загрузки

//     private int currentCatAppearance = 1; // 1 - первая внешность, 2 - вторая

//     public GameObject pinkZoneViewport; // Viewport для розовой зоны
//     public GameObject pinkZoneViewportOnFeed; // Viewport для розовой зоны при кормлении

//     private bool isSaving = false;




//     private async void Start()
//     {
//         // Показываем экран загрузки
//         ShowLoadingScreen();

//         // Загружаем данные
//         await LoadPetDataFromCloud();

//         // // Проверяем, нужно ли обновить lastLoginTime
//         // if (lastLoginTime.Date != DateTime.Now.Date)
//         // {
//         //     Debug.Log($"🕒 lastLoginTime обновлён: {lastLoginTime} -> {DateTime.Now}");
//         //     lastLoginTime = DateTime.Now;
//         //     await SavePetDataToCloud(); // Сохраняем обновлённое время
//         // }
        
//         //CheckIdleTime();

//         // Инициализируем игру
//         InitializeGame();

//         // Обновляем UI
//         UpdateUI();

//         // Скрываем экран загрузки
//         HideLoadingScreen();

//         // Добавляем слушатель кнопки "Накормить"
//         feedButton.onClick.AddListener(FeedTamagotchi);
//     }


//     public async void SwitchCatAppearance()
//     {
//         // Переключаем внешность
//         currentCatAppearance = currentCatAppearance == 1 ? 2 : 1;

//         // Сохраняем выбор в облако
//         await SavePetDataToCloud();

//         // Обновляем UI с учётом новой внешности
//         UpdateCatImages();
//     }

//     private void ShowLoadingScreen()
//     {
//         if (loadingScreen != null)
//         {
//             loadingScreen.SetActive(true);
//             loadingText.text = "Загрузка данных...";
//         }

//         // Отключаем интерактивные элементы
//         feedButton.interactable = false;
//         hungerSlider.interactable = false;
//     }

//     private void HideLoadingScreen()
//     {
//         if (loadingScreen != null)
//         {
//             loadingScreen.SetActive(false);
//         }

//         // Включаем интерактивные элементы
//         feedButton.interactable = true;
//         hungerSlider.interactable = true;
//     }

//     private async Task LoadPetDataFromCloud()
//     {
//         try
//         {
//             Debug.Log("Загрузка данных питомца из облака...");

//             var data = await CloudSaveService.Instance.Data.LoadAsync(new HashSet<string> { "PET" });

//             if (data.TryGetValue("PET", out var petData))
//             {
//                 Debug.Log($"PET Data JSON: {petData}");

//                 var petDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(petData.ToString());
//                 foreach (var pair in petDict)
//                 {
//                     Debug.Log($"Loaded Key: {pair.Key}, Value: {pair.Value}");
//                 }

//                 if (petDict != null)
//                 {
//                     hungerLevel = int.Parse(petDict.GetValueOrDefault("HungerLevel", initialHungerLevel.ToString()));
//                     feedingDays = int.Parse(petDict.GetValueOrDefault("FeedingDays", "0"));
//                     currentCatAppearance = int.Parse(petDict.GetValueOrDefault("CurrentCatAppearance", "1"));

//                     if (DateTime.TryParse(petDict.GetValueOrDefault("NextFoodTime", ""), out var parsedNextFoodTime))
//                         nextFoodTime = parsedNextFoodTime;

//                     if (DateTime.TryParse(petDict.GetValueOrDefault("LastLoginTime", ""), out var parsedLastLoginTime))
//                     {
//                         // Проверяем, что lastLoginTime не в будущем
//                         if (parsedLastLoginTime > DateTime.Now)
//                         {
//                             Debug.LogWarning("lastLoginTime в будущем! Сбрасываем на текущее время.");
//                             lastLoginTime = DateTime.Now;
//                         }
//                         else
//                         {
//                             lastLoginTime = parsedLastLoginTime;
//                         }
//                     }

//                     Debug.Log("Данные питомца успешно загружены.");
//                     return;
//                 }
//             }

//             // Если данные не найдены, устанавливаем значения по умолчанию
//             Debug.LogWarning("Данные питомца не найдены, устанавливаем значения по умолчанию.");
//             SetDefaultValues();
//             await SavePetDataToCloud(); // Сохраняем значения по умолчанию в облако
//         }
//         catch (Exception e)
//         {
//             Debug.LogError($"Ошибка загрузки данных: {e.Message}");
//             // В случае ошибки также устанавливаем значения по умолчанию
//             SetDefaultValues();
//             await SavePetDataToCloud(); // Сохраняем значения по умолчанию в облако
//         }
//     }


//     private void SetDefaultValues()
//     {
//         Debug.Log("Установка дефолтных значений...");

//         hungerLevel = initialHungerLevel; // Устанавливаем шкалу голода на середину зелёного уровня
//         feedingDays = 0; // Дней кормления: 0
//         currentCatAppearance = 1; // Внешность кота: 1 (первая)
//         nextFoodTime = DateTime.Now.AddHours(24); // Время следующего кормления: через 24 часа
//         lastLoginTime = DateTime.Now; // Время последнего входа: сейчас

//         Debug.Log($"Дефолтные значения установлены: HungerLevel={hungerLevel}, FeedingDays={feedingDays}, NextFoodTime={nextFoodTime}, LastLoginTime={lastLoginTime}");
//     }

//     private async Task SavePetDataToCloud()
//     {
//         if (isSaving) return;
//         isSaving = true;

//         try
//         {
//             Debug.Log("Сохранение данных питомца на облако...");

//             var petData = new Dictionary<string, string>
//             {
//                 { "HungerLevel", hungerLevel.ToString() },
//                 { "FeedingDays", feedingDays.ToString() },
//                 { "CurrentCatAppearance", currentCatAppearance.ToString() },
//                 { "NextFoodTime", nextFoodTime.ToString() },
//                 { "LastLoginTime", lastLoginTime.ToString() }
//             };

//             var data = new Dictionary<string, object>
//             {
//                 { "PET", JsonConvert.SerializeObject(petData) }
//             };

//             await CloudSaveService.Instance.Data.ForceSaveAsync(data);
//             Debug.Log("Данные питомца сохранены на облако.");
//         }
//         catch (Exception e)
//         {
//             Debug.LogError($"Ошибка сохранения данных питомца на облако: {e.Message}");
//         }
//         finally
//         {
//             isSaving = false;
//         }
//     }
    

//     private void InitializeGame()
//     {
//         Debug.Log("Инициализация игры...");

//         // Проверяем, что sliderFillImage инициализирован
//         if (sliderFillImage == null)
//         {
//             Debug.LogError("sliderFillImage не присвоен в инспекторе!");
//             return;
//         }

//         // Создаём градиент
//         CreateGradient();

//         hungerSlider.interactable = false;

//         // Используем данные, загруженные из облака
//         hungerSlider.maxValue = maxHungerLevel;
//         hungerSlider.value = hungerLevel; // Значение из облака

//         Debug.Log("Игра инициализирована.");
//     }

//     private void CreateGradient()
//     {
//         gradient = new Gradient();
//         gradient.SetKeys(
//             new GradientColorKey[]
//             {
//                 new GradientColorKey(Color.red, 0f),
//                 new GradientColorKey(Color.yellow, 0.25f),
//                 new GradientColorKey(Color.green, 0.5f),
//                 new GradientColorKey(Color.magenta, 1f)
//             },
//             new GradientAlphaKey[]
//             {
//                 new GradientAlphaKey(1f, 0f),
//                 new GradientAlphaKey(1f, 1f)
//             }
//         );
//     }

//     private void UpdateUI()
//     {
//         Debug.Log("Обновление UI...");

//         if (hungerSlider == null)
//         {
//             Debug.LogError("hungerSlider не инициализирован!");
//             return;
//         }

//         hungerSlider.value = hungerLevel;
//         feedingDayText.text = $"{feedingDays}/150";

//         // Обновляем цвет слайдера
//         UpdateSliderColor();

//         // Обновляем изображения кота
//         UpdateCatImages();

//         Debug.Log("UI обновлён.");
//     }

//     private void UpdateSliderColor()
//     {
//         Debug.Log("Обновление цвета слайдера...");

//         if (sliderFillImage == null)
//         {
//             Debug.LogError("sliderFillImage не инициализирован!");
//             return;
//         }

//         if (gradient == null)
//         {
//             Debug.LogError("gradient не инициализирован!");
//             return;
//         }

//         float normalizedValue = hungerSlider.value / hungerSlider.maxValue;
//         sliderFillImage.color = gradient.Evaluate(normalizedValue);

//         Debug.Log($"Цвет слайдера обновлён. Нормализованное значение: {normalizedValue}, Цвет: {sliderFillImage.color}");
//     }

//     private void UpdateCatImages()
//     {
//         float normalizedValue = hungerSlider.value / hungerSlider.maxValue;

//         // Скрываем все эмоции по умолчанию
//         happyCatImage.gameObject.SetActive(false);
//         heartsImage.gameObject.SetActive(false);
//         angryCatImage.gameObject.SetActive(false);
//         sadCatImage.gameObject.SetActive(false);
//         catImage.gameObject.SetActive(false);

//         happyCatImage2.gameObject.SetActive(false);
//         heartsImage2.gameObject.SetActive(false);
//         angryCatImage2.gameObject.SetActive(false);
//         sadCatImage2.gameObject.SetActive(false);
//         catImage2.gameObject.SetActive(false);

//         // Логика отображения эмоций для текущей внешности
//         if (currentCatAppearance == 1)
//         {
//             catImage.gameObject.SetActive(true);

//             if (normalizedValue >= 0.75f) // Розовая зона
//             {
//                 happyCatImage.gameObject.SetActive(true);
//                 heartsImage.gameObject.SetActive(true);
//             }
//             else if (normalizedValue >= 0.25f && normalizedValue < 0.5f) // Жёлтая зона
//             {
//                 sadCatImage.gameObject.SetActive(true);
//             }
//             else if (normalizedValue < 0.25f) // Красная зона
//             {
//                 angryCatImage.gameObject.SetActive(true);
//             }
//         }
//         else if (currentCatAppearance == 2)
//         {
//             catImage2.gameObject.SetActive(true);

//             if (normalizedValue >= 0.75f) // Розовая зона
//             {
//                 happyCatImage2.gameObject.SetActive(true);
//                 heartsImage2.gameObject.SetActive(true);
//             }
//             else if (normalizedValue >= 0.25f && normalizedValue < 0.5f) // Жёлтая зона
//             {
//                 sadCatImage2.gameObject.SetActive(true);
//             }
//             else if (normalizedValue < 0.25f) // Красная зона
//             {
//                 angryCatImage2.gameObject.SetActive(true);
//             }
//         }
//     }

//     private bool isTimerActive = false; // Флаг активности таймера
//     private async void FeedTamagotchi()
//     {
//         int foodNeeded = GetFoodCost();
//         int food = GameCloud.Instance.GetCurrencyAmount("PLAYER_FOOD");
//         if (food > 0)
//         {
//             if (food >= foodNeeded)
//             {
//                 int previousHungerLevel = hungerLevel; // Запоминаем прошлый уровень
//                 hungerLevel = Mathf.Clamp(hungerLevel + 1, 0, maxHungerLevel);
                
//                 // Проверяем, достиг ли игрок розовой зоны
//                 CheckIfEnteredPinkZone(previousHungerLevel, hungerLevel);
//             }

//             // Если корма достаточно
//             GameCloud.Instance.UpdateResource("PLAYER_FOOD", -food);


//             // Debug.Log($"lastLoginTime: {lastLoginTime}, Current Date: {DateTime.Now.Date}");
//             // // Проверяем первое кормление за сутки
//             // if (lastLoginTime.Date != DateTime.Now.Date)
//             // {
//             //     feedingDays++;
//             //     Debug.Log($"✅ Новый день! FeedingDays увеличен: {feedingDays}");
//             //     lastLoginTime = DateTime.Now; // Обновляем время последнего входа только после увеличения feedingDays
//             // }
//                     // Проверяем, прошло ли 24 часа с момента последнего кормления
//             TimeSpan timeSinceLastLogin = DateTime.Now - lastLoginTime;
//             if (timeSinceLastLogin.TotalHours >= 24)
//             {
//                 feedingDays++;
//                 Debug.Log($"✅ Новый день! FeedingDays увеличен: {feedingDays}");
//                 lastLoginTime = DateTime.Now; // Обновляем время последнего входа
//             }

//             // Запускаем таймер, если он ещё не активен
//             if (!isTimerActive)
//             {
//                 isTimerActive = true;
//                 nextFoodTime = DateTime.Now.AddHours(HoursToWait);
//             }

//             // Сохраняем данные на облако
//             SaveData();

//             // Обновляем UI
//             UpdateUI();
//         }
//         else if (food <= 0)
//         {
//             // Если корма недостаточно, загружаем магазин с вкладкой Catmoney
//             Debug.Log("Недостаточно корма! Переход в магазин на вкладку Catmoney.");

//             // Сохраняем текущую сцену как предыдущую
//             SceneLoaderShop.selectedScrollView = "Catmoney"; // Устанавливаем вкладку
//             SceneLoaderShop sceneLoader = FindObjectOfType<SceneLoaderShop>();
//             if (sceneLoader != null)
//             {
//                 sceneLoader.LoadShopSceneWithCatmoney(); // Загружаем магазин
//             }
//             else
//             {
//                 Debug.LogError("SceneLoaderShop не найден в текущей сцене!");
//             }
//         }
//     }

//     private int GetFoodCost()
//     {
//         float normalizedValue = hungerSlider.value / hungerSlider.maxValue;

//         if (normalizedValue < 0.25f) // Красная зона
//             return 1;
//         else if (normalizedValue < 0.5f) // Жёлтая зона и левая половина зелёной
//             return 2;
//         else if (normalizedValue < 0.75f) // Правая половина зелёной зоны
//             return 4;
//         else // Розовая зона
//             return 5;
//     }

//     private void CheckIdleTime()
//     {
//         TimeSpan idleTime = DateTime.Now - lastLoginTime;
//         if (idleTime.TotalDays >= DaysToDecrease)
//         {
//             hungerLevel = Mathf.Clamp(hungerLevel - 1, 0, maxHungerLevel);
//             lastLoginTime = DateTime.Now;
//             SaveData();
//             UpdateUI();
//         }
//     }

//     private async void SaveData()
//     {
//         await SavePetDataToCloud();
//     }

//     private async void CheckIfEnteredPinkZone(int previousHungerLevel, int newHungerLevel)
//     {
//         float prevNormalized = (float)previousHungerLevel / maxHungerLevel;
//         float newNormalized = (float)newHungerLevel / maxHungerLevel;

//         // Если раньше был НЕ в розовой зоне, а теперь в неё попал
//         if (prevNormalized < 0.75f && newNormalized >= 0.75f)
//         {
//             if (lastLoginTime.Date != DateTime.Now.Date)
//             {
//                 // Начисляем награды
//                 GameCloud.Instance.UpdateResource("CATMONEY_ELIXIR", 7);
//                 GameCloud.Instance.UpdateResource("PLAYER_RUBY", 15);
//                 GameCloud.Instance.UpdateResource("PLAYER_ELIXIR", 6);

//                 // Обновляем время последнего входа
//                 lastLoginTime = DateTime.Now;

//                 // Сохраняем данные на облако
//                 SaveData();

//                 // Показываем viewport
//                 if (pinkZoneViewportOnFeed != null)
//                 {
//                     pinkZoneViewportOnFeed.SetActive(true);
//                 }

//                 Debug.Log("Игрок впервые достиг розовой зоны при кормлении! Выдана награда.");
//             }
//         }
//     }
// }