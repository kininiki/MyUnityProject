// using Unity.Services.Authentication;
// using Unity.Services.Core;
// using UnityEngine;
// using Unity.Services.CloudSave;
// using System.Collections.Generic;
// using UnityEngine.SceneManagement;
// using Fungus;

// public class GameCloud : MonoBehaviour
// {
//     public static GameCloud Instance;  // Добавляем Singleton

//     private const string PLAYER_CLOUD_KEY = "PLAYER_DATA";
//     private const string ALMAZ_KEY = "PLAYER_ALMAZ";

//     private void Awake()
//     {
//         // Реализация Singleton
//         if (Instance == null)
//         {
//             Instance = this;
//             DontDestroyOnLoad(gameObject);  // Оставляем объект при смене сцен
//         }
//         else
//         {
//             Destroy(gameObject);
//         }
//     }

//     private async void Start()
//     {
//         await UnityServices.InitializeAsync();

//         await AuthenticationService.Instance.SignInAnonymouslyAsync();

//         // После успешного входа вызываем сохранение данных
//         SaveData();

//         // После успешного входа переходим в основное меню
//         if (AuthenticationService.Instance.IsSignedIn)
//         {
//             // После успешного входа вызываем сохранение данных
//             SaveData();

//             // Переход в сцену mainMenu
//             SceneManager.LoadScene("mainMenu");
//         }
//     }

//     public async void SaveData()
//     {
//         PlayerData playerData = new()
//         {
//             Notification = 1,
//             Language = "Русский",
//             Sound = 1
//         };

//         Dictionary<string, object> data = new Dictionary<string, object>() { { PLAYER_CLOUD_KEY, playerData } };
//         await CloudSaveService.Instance.Data.ForceSaveAsync(data);
//     }

//     // Метод для обновления количества алмазов
//     public async void UpdateAlmaz(int amount)
//     {
//         int currentAlmaz = PlayerPrefs.GetInt(ALMAZ_KEY, 0);
//         currentAlmaz += amount;

//         // Сохраняем обновленные данные локально
//         PlayerPrefs.SetInt(ALMAZ_KEY, currentAlmaz);

//         // Сохраняем обновленные данные в облаке
//         Dictionary<string, object> data = new Dictionary<string, object>() { { ALMAZ_KEY, currentAlmaz } };
//         await CloudSaveService.Instance.Data.ForceSaveAsync(data);

//         Debug.Log("Алмазы обновлены: " + currentAlmaz);
//     }

//     // Новый метод для сохранения настроек уведомлений и языка при изменении
//     public async void SaveSettings(int notificationStatus, string language, int soundStatus)
//     {
//         PlayerData playerData = new()
//         {
//             Notification = notificationStatus, // Сохраняем состояние уведомлений
//             Language = language,               // Сохраняем выбранный язык
//             Sound = soundStatus
//         };

//         Dictionary<string, object> data = new Dictionary<string, object>()
//         {
//             { PLAYER_CLOUD_KEY, playerData }
//         };

//         // Сохраняем настройки в облако
//         await CloudSaveService.Instance.Data.ForceSaveAsync(data);
//         Debug.Log("Player settings saved: Notifications - " + notificationStatus + ", Language - " + language);
//     }

//     public async void LoadData()
//     {
//         // Реализация загрузки данных будет здесь
//     }
// }

// public class PlayerData
// {
//     public int Notification; // 0 (Off) или 1 (On)
//     public string Language;
//     public int Sound; // 0 (Off) или 1 (On)
// }


// using Unity.Services.Authentication;
// using Unity.Services.Core;
// using UnityEngine;
// using Unity.Services.CloudSave;
// using System.Collections.Generic;
// using UnityEngine.SceneManagement;
// using System.Threading.Tasks;

// public class GameCloud : MonoBehaviour
// {
//     public static GameCloud Instance;  // Singleton

//     private const string PLAYER_CLOUD_KEY = "PLAYER_DATA";
//     private const string ALMAZ_KEY = "PLAYER_ALMAZ";

//     private void Awake()
//     {
//         // Singleton реализация
//         if (Instance == null)
//         {
//             Instance = this;
//             DontDestroyOnLoad(gameObject);
//         }
//         else
//         {
//             Destroy(gameObject);
//         }
//     }

//     private async void Start()
//     {
//         await UnityServices.InitializeAsync();

//         await AuthenticationService.Instance.SignInAnonymouslyAsync();

//         if (AuthenticationService.Instance.IsSignedIn)
//         {
//             // После успешного входа проверяем и обновляем данные
//             await CheckAndSaveData();

//             // Переход в сцену mainMenu
//             SceneManager.LoadScene("mainMenu");
//         }
//     }

// public async Task CheckAndSaveData()
// {
//     try
//     {
//         // Загружаем данные из облака
//         var data = await CloudSaveService.Instance.Data.LoadAsync(new HashSet<string> { PLAYER_CLOUD_KEY });

//         PlayerData playerData;

//         if (data.ContainsKey(PLAYER_CLOUD_KEY))
//         {
//             // Данные существуют, десериализуем их
//             string jsonData = data[PLAYER_CLOUD_KEY].ToString();
//             playerData = JsonUtility.FromJson<PlayerData>(jsonData);

//             // Проверяем и добавляем недостающие разделы
//             if (!data[PLAYER_CLOUD_KEY].ToString().Contains("\"Notification\""))
//             {
//                 playerData.Notification = 1; // Устанавливаем значение по умолчанию
//             }
//             if (string.IsNullOrEmpty(playerData.Language))
//             {
//                 playerData.Language = "Русский"; // Значение по умолчанию
//             }
//             if (!data[PLAYER_CLOUD_KEY].ToString().Contains("\"Sound\""))
//             {
//                 playerData.Sound = 1; // Устанавливаем значение по умолчанию
//             }
//         }
//         else
//         {
//             // Если данных нет, создаём их с дефолтными значениями
//             playerData = new PlayerData
//             {
//                 Notification = 1,
//                 Language = "Русский",
//                 Sound = 1
//             };
//         }

//         // Применяем настройки звука на основе значения Sound
//         ApplySoundSettings(playerData.Sound);

//         // Сохраняем обновленные или дефолтные данные обратно в облако
//         Dictionary<string, object> updatedData = new Dictionary<string, object>()
//         {
//             { PLAYER_CLOUD_KEY, JsonUtility.ToJson(playerData) }
//         };
//         await CloudSaveService.Instance.Data.ForceSaveAsync(updatedData);

//         Debug.Log("Player data checked and saved.");
//     }
//     catch (System.Exception e)
//     {
//         Debug.LogError("Error checking and saving player data: " + e.Message);
//     }
// }

// // Метод для применения настройки звука
// private void ApplySoundSettings(int soundStatus)
// {
//     if (soundStatus == 0)
//     {
//         AudioListener.pause = true; // Отключаем весь звук
//         Debug.Log("Sound is OFF");
//     }
//     else
//     {
//         AudioListener.pause = false; // Включаем звук
//         Debug.Log("Sound is ON");
//     }
// }



//     public async void UpdateAlmaz(int amount)
//     {
//         int currentAlmaz = PlayerPrefs.GetInt(ALMAZ_KEY, 0);
//         currentAlmaz += amount;

//         // Сохраняем обновленные данные локально
//         PlayerPrefs.SetInt(ALMAZ_KEY, currentAlmaz);

//         // Сохраняем обновленные данные в облаке
//         Dictionary<string, object> data = new Dictionary<string, object>() { { ALMAZ_KEY, currentAlmaz } };
//         await CloudSaveService.Instance.Data.ForceSaveAsync(data);

//         Debug.Log("Алмазы обновлены: " + currentAlmaz);
//     }

//     public async void SaveSettings(int notificationStatus, string language, int soundStatus)
//     {
//         PlayerData playerData = new()
//         {
//             Notification = notificationStatus,
//             Language = language,
//             Sound = soundStatus
//         };

//         Dictionary<string, object> data = new Dictionary<string, object>()
//         {
//             { PLAYER_CLOUD_KEY, playerData }
//         };

//         // Сохраняем настройки в облако
//         await CloudSaveService.Instance.Data.ForceSaveAsync(data);
//         Debug.Log("Player settings saved: Notifications - " + notificationStatus + ", Language - " + language);
//     }

//     public async void LoadData()
//     {
//         try
//         {
//             var data = await CloudSaveService.Instance.Data.LoadAsync(new HashSet<string> { PLAYER_CLOUD_KEY });

//             if (data.ContainsKey(PLAYER_CLOUD_KEY))
//             {
//                 string jsonData = data[PLAYER_CLOUD_KEY].ToString();
//                 PlayerData playerData = JsonUtility.FromJson<PlayerData>(jsonData);

//                 Debug.Log("Loaded player data: " +
//                           "Notifications - " + playerData.Notification +
//                           ", Language - " + playerData.Language +
//                           ", Sound - " + playerData.Sound);

//                 // Применяем данные
//                 PlayerPrefs.SetInt("NotificationsEnabled", playerData.Notification);
//                 PlayerPrefs.SetString("SelectedLanguage", playerData.Language);
//                 PlayerPrefs.SetInt("SoundEnabled", playerData.Sound);
//                 PlayerPrefs.Save();
//             }
//             else
//             {
//                 Debug.Log("No player data found in the cloud.");
//             }
//         }
//         catch (System.Exception e)
//         {
//             Debug.LogError("Error loading player data: " + e.Message);
//         }
//     }
// }

// [System.Serializable]
// public class PlayerData
// {
//     public int Notification; // 0 (Off) или 1 (On)
//     public string Language;
//     public int Sound; // 0 (Off) или 1 (On)
// }



using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using Unity.Services.CloudSave;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TMPro;

public class GameCloud : MonoBehaviour
{
    public static GameCloud Instance;  // Singleton

    private const string PLAYER_CLOUD_KEY = "PLAYER_DATA";
    private const string ALMAZ_KEY = "PLAYER_ALMAZ";
    
    // Валюты и ресурсы
    private const string FOOD_KEY = "PLAYER_FOOD";
    private const string ELIXIR_KEY = "PLAYER_ELIXIR";
    private const string RUBY_KEY = "PLAYER_RUBY";
    private const string CATMONEY_KEY = "CATMONEY_ELIXIR";

    public TMP_Text foodText;
    public TMP_Text elixirText;
    public TMP_Text rubyText;
    public TMP_Text catMoneyText;
    public TMP_Text almazText;

    private readonly Dictionary<string, string> defaultData = new Dictionary<string, string>
    {
        { FOOD_KEY, "{\"food\":0}" },
        { ELIXIR_KEY, "{\"elixir\":3}" },
        { RUBY_KEY, "{\"ruby\":75}" },
        { CATMONEY_KEY, "{\"catmoney\":3}" },
        { ALMAZ_KEY, "0" }
    };

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private async void Start()
    {
        await UnityServices.InitializeAsync();
        await AuthenticationService.Instance.SignInAnonymouslyAsync();

        if (AuthenticationService.Instance.IsSignedIn)
        {
            Debug.Log("Пользователь успешно вошел в систему!");
            await LoadAllData(); // Ожидаем завершения загрузки данных
            InvokeRepeating(nameof(UpdateCurrencyDisplay), 0, 15f);
            
            //SceneManager.LoadScene("mainMenu");
            // Загружаем сцену только после полной инициализации
            SceneManager.LoadSceneAsync("mainMenu", LoadSceneMode.Single);
        }
    }

private async Task LoadAllData()
{
    try
    {
        var keys = new HashSet<string> { PLAYER_CLOUD_KEY, FOOD_KEY, ELIXIR_KEY, RUBY_KEY, CATMONEY_KEY };
        var data = await CloudSaveService.Instance.Data.LoadAsync(keys);

        await EnsureKeyExists(PLAYER_CLOUD_KEY, "{\"Notification\":1, \"Language\":\"Русский\", \"Sound\":1}");
        await EnsureKeyExists(FOOD_KEY, "{\"food\":0}");
        await EnsureKeyExists(ELIXIR_KEY, "{\"elixir\":3}");
        await EnsureKeyExists(RUBY_KEY, "{\"ruby\":75}");
        await EnsureKeyExists(CATMONEY_KEY, "{\"catmoney\":3}");

        await LoadPlayerSettings();
    }
    catch (System.Exception ex)
    {
        Debug.LogError($"❌ Ошибка загрузки данных: {ex.Message}");
    }
}


private async Task EnsureKeyExists(string key, string defaultValue)
{
    var data = await CloudSaveService.Instance.Data.LoadAsync(new HashSet<string> { key });

    if (!data.ContainsKey(key))
    {
        await SaveData(key, defaultValue);
        Debug.Log($"🟢 Ключ {key} создан с дефолтным значением: {defaultValue}");
    }
}



    private async Task LoadPlayerSettings()
    {
        try
        {
            var data = await CloudSaveService.Instance.Data.LoadAsync(new HashSet<string> { PLAYER_CLOUD_KEY });

            if (data.ContainsKey(PLAYER_CLOUD_KEY))
            {
                string jsonData = data[PLAYER_CLOUD_KEY].ToString();
                PlayerData playerData = JsonUtility.FromJson<PlayerData>(jsonData);

                if (string.IsNullOrEmpty(playerData.Language)) playerData.Language = "Русский";
                if (!data[PLAYER_CLOUD_KEY].ToString().Contains("\"Notification\"")) playerData.Notification = 1;
                if (!data[PLAYER_CLOUD_KEY].ToString().Contains("\"Sound\"")) playerData.Sound = 1;

                ApplySoundSettings(playerData.Sound);

                await SaveData(PLAYER_CLOUD_KEY, JsonUtility.ToJson(playerData));

                PlayerPrefs.SetInt("NotificationsEnabled", playerData.Notification);
                PlayerPrefs.SetString("SelectedLanguage", playerData.Language);
                PlayerPrefs.SetInt("SoundEnabled", playerData.Sound);
                PlayerPrefs.Save();
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Ошибка загрузки настроек: " + ex.Message);
        }
    }

    public async Task SaveData(string key, string jsonData)
    {
        try
        {
            var data = new Dictionary<string, object> { { key, jsonData } };
            await CloudSaveService.Instance.Data.ForceSaveAsync(data);
            PlayerPrefs.SetString(key, jsonData);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Ошибка сохранения {key}: {ex.Message}");
        }
    }

    public async void UpdateResource(string key, int amount)
    {
        if (PlayerPrefs.HasKey(key))
        {
            string jsonData = PlayerPrefs.GetString(key);
            var resourceData = JsonConvert.DeserializeObject<Dictionary<string, int>>(jsonData);

            if (resourceData != null)
            {
                string resourceName = key switch
                {
                    FOOD_KEY => "food",
                    ELIXIR_KEY => "elixir",
                    RUBY_KEY => "ruby",
                    CATMONEY_KEY => "catmoney",
                    _ => null
                };

                if (!string.IsNullOrEmpty(resourceName) && resourceData.ContainsKey(resourceName))
                {
                    resourceData[resourceName] += amount;
                    string newJson = JsonConvert.SerializeObject(resourceData);
                    await SaveData(key, newJson);
                    UpdateCurrencyDisplay();
                }
            }
        }
    }

    public async void UpdateAlmaz(int amount)
    {
        int currentAlmaz = PlayerPrefs.GetInt(ALMAZ_KEY, 0) + amount;
        PlayerPrefs.SetInt(ALMAZ_KEY, currentAlmaz);

        Dictionary<string, object> data = new Dictionary<string, object>() { { ALMAZ_KEY, currentAlmaz } };
        await CloudSaveService.Instance.Data.ForceSaveAsync(data);

        UpdateCurrencyDisplay();
    }

    private void UpdateCurrencyDisplay()
    {
        if (foodText) foodText.text = GetCurrencyAmount(FOOD_KEY).ToString();
        if (elixirText) elixirText.text = GetCurrencyAmount(ELIXIR_KEY).ToString();
        if (rubyText) rubyText.text = GetCurrencyAmount(RUBY_KEY).ToString();
        if (catMoneyText) catMoneyText.text = GetCurrencyAmount(CATMONEY_KEY).ToString();
        if (almazText) almazText.text = PlayerPrefs.GetInt(ALMAZ_KEY, 0).ToString();
    }

    public int GetCurrencyAmount(string key)
    {
        if (PlayerPrefs.HasKey(key))
        {
            string jsonData = PlayerPrefs.GetString(key);
            var resourceData = JsonConvert.DeserializeObject<Dictionary<string, int>>(jsonData);
            string resourceName = key switch
            {
                FOOD_KEY => "food",
                ELIXIR_KEY => "elixir",
                RUBY_KEY => "ruby",
                CATMONEY_KEY => "catmoney",
                _ => null
            };

            if (!string.IsNullOrEmpty(resourceName) && resourceData.ContainsKey(resourceName))
            {
                return resourceData[resourceName];
            }
        }
        return 0;
    }

    public async void SaveSettings(int notificationStatus, string language, int soundStatus)
    {
        PlayerData playerData = new()
        {
            Notification = notificationStatus,
            Language = language,
            Sound = soundStatus
        };

        await SaveData(PLAYER_CLOUD_KEY, JsonUtility.ToJson(playerData));
    }

    private void ApplySoundSettings(int soundStatus)
    {
        AudioListener.pause = soundStatus == 0;
    }
}

[System.Serializable]
public class PlayerData
{
    public int Notification;
    public string Language;
    public int Sound;
}




// добавить еду GameCloud.Instance.UpdateResource("PLAYER_FOOD", 5);
// добавить эликсир GameCloud.Instance.UpdateResource("PLAYER_ELIXIR", 1);
// добавить рубины GameCloud.Instance.UpdateResource("PLAYER_RUBY", 10);
// добавить котомани GameCloud.Instance.UpdateResource("CATMONEY_ELIXIR", 2);
// уменьшить что-либо: GameCloud.Instance.UpdateResource("PLAYER_ELIXIR", -1);