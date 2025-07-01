using UnityEngine;

public class LightSwitcher : MonoBehaviour
{
    [SerializeField] private GameObject light1; // Объект Light1
    [SerializeField] private GameObject light2; // Объект Light2
    [SerializeField] private int light1Cost = 3; // Стоимость при горящем Light1
    [SerializeField] private int light2Cost = 11; // Стоимость при горящем Light2

    private async void Start()
    {
        // Устанавливаем изначальное состояние
        if (light1 != null) light1.SetActive(true); // Light1 включён
        if (light2 != null) light2.SetActive(false); // Light2 выключен

        // Загружаем данные CatMoney из облака
        if (GameCloud.Instance != null)
        {
            int catMoneyCount = GameCloud.Instance.GetCurrencyAmount("CATMONEY_ELIXIR");
            Debug.Log($"CatMoney успешно загружена: {catMoneyCount}");
            
        }
        else
        {
            Debug.LogError("CatMoneyManager.Instance не найден! Проверьте Singleton.");
        }
    }


    // Метод для активации Light1 и деактивации Light2
    public void ActivateLight1()
    {
        if (light1 != null && light2 != null)
        {
            light1.SetActive(true); // Включаем Light1
            light2.SetActive(false); // Отключаем Light2
        }
        else
        {
            Debug.LogError("Light1 или Light2 не привязаны в инспекторе!");
        }
    }

    // Метод для активации Light2 и деактивации Light1
    public void ActivateLight2()
    {
        if (light1 != null && light2 != null)
        {
            light1.SetActive(false); // Отключаем Light1
            light2.SetActive(true); // Включаем Light2
        }
        else
        {
            Debug.LogError("Light1 или Light2 не привязаны в инспекторе!");
        }
    }

    // Метод, вызываемый при нажатии кнопки "button"
    public async void OnButtonClick()
    {
        if (GameCloud.Instance == null)
        {
            Debug.LogError("GameCloud.Instance не найден!");
            return;
        }

        // Получаем текущее количество CatMoney из PlayerPrefs (после загрузки из облака)
        int catMoneyCount = GameCloud.Instance.GetCurrencyAmount("CATMONEY_ELIXIR");
        if (catMoneyCount == -1)
        {
            Debug.LogError("Не удалось получить данные CatMoney из PlayerPrefs. Убедитесь, что данные загружаются из облака.");
            return;
        }

        int foodCount = GameCloud.Instance.GetCurrencyAmount("PLAYER_FOOD"); // Получаем количество корма из PlayerPrefs

        if (light1.activeSelf) // Если горит Light1
        {
            if (catMoneyCount >= light1Cost) // Проверяем, хватает ли монет
            {
                GameCloud.Instance.UpdateResource("CATMONEY_ELIXIR", -light1Cost); // Вычитаем стоимость
                GameCloud.Instance.UpdateResource("PLAYER_FOOD", 1);
                Debug.Log($"Light1: Добавлен 1 корм, вычтено {light1Cost} монет.");
            }
            else
            {
                Debug.LogWarning("Недостаточно монет для Light1!");
            }
        }
        else if (light2.activeSelf) // Если горит Light2
        {
            if (catMoneyCount >= light2Cost) // Проверяем, хватает ли монет
            {
                GameCloud.Instance.UpdateResource("CATMONEY_ELIXIR", -light2Cost); // Вычитаем стоимость
                GameCloud.Instance.UpdateResource("PLAYER_FOOD", 4);
                Debug.Log($"Light2: Добавлен 1 корм, вычтено {light2Cost} монет.");

                // Обновляем PlayerPrefs
                PlayerPrefs.SetInt("FoodCount", foodCount);

                // Сохраняем обновлённые данные в облако
                //await CatMoneyManager.Instance.SaveCatMoneyData(catMoneyCount);
            }
            else
            {
                Debug.LogWarning("Недостаточно монет для Light2!");
            }
        }
        else
        {
            Debug.LogWarning("Не горит ни один свет. Нажатие кнопки невозможно!");
        }

        // Сохраняем данные локально
        PlayerPrefs.Save();
        Debug.Log($"Данные сохранены локально. Корм: {foodCount}, монеты: {catMoneyCount}");
    }

}




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