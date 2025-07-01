// using System;
// using System.Collections.Generic;
// using UnityEngine;
// using Unity.Services.Authentication;
// using Unity.Services.Core;
// using Unity.Services.CloudSave;
// using System.Threading.Tasks;

// public class PromoCloudManager : MonoBehaviour
// {
//     public static PromoCloudManager Instance;

//     private const string PROMO_CLOUD_KEY = "PROMO_CODES"; // Ключ для промокодов в облаке

//     [SerializeField] private List<PromoCodeData> _promoCodes; // Промокоды из инспектора
//     private Dictionary<string, int> _cloudPromoCodes = new Dictionary<string, int>(); // Хранилище промокодов

//     private async void Awake()
//     {
//         // Singleton
//         if (Instance == null)
//         {
//             Instance = this;
//             DontDestroyOnLoad(gameObject); // Объект сохраняется при смене сцен
//         }
//         else
//         {
//             Destroy(gameObject);
//         }

//         await InitializeCloudService();
//     }

//     // Инициализация Unity Services
//     private async Task InitializeCloudService()
//     {
//         try
//         {
//             await UnityServices.InitializeAsync();  // Инициализация Unity Services
//             Debug.Log("Unity Services Initialized.");

//             await AuthenticationService.Instance.SignInAnonymouslyAsync();  // Аутентификация пользователя
//             Debug.Log("Signed in anonymously.");

//             if (AuthenticationService.Instance.IsSignedIn)
//             {
//                 await LoadPromoCodesFromCloud();  // Загрузка промокодов из облака
//             }
//             else
//             {
//                 Debug.LogError("Ошибка аутентификации.");
//             }
//         }
//         catch (Exception e)
//         {
//             Debug.LogError($"Ошибка инициализации: {e.Message}");
//         }
//     }

//     /// <summary>
//     /// Сохраняет промокоды в облако.
//     /// </summary>
//     private async Task SavePromoCodesToCloud()
//     {
//         try
//         {
//             // Преобразуем словарь промокодов в объект, который можно сохранить
//             Dictionary<string, object> data = new Dictionary<string, object>() { { PROMO_CLOUD_KEY, _cloudPromoCodes } };

//             // Сохраняем данные в облаке
//             await CloudSaveService.Instance.Data.ForceSaveAsync(data);
//             Debug.Log("Промокоды успешно сохранены в облаке.");
//         }
//         catch (Exception e)
//         {
//             Debug.LogError($"Ошибка сохранения промокодов в облаке: {e.Message}");
//         }
//     }

//     /// <summary>
//     /// Загружает промокоды из облака. Если данные отсутствуют, инициализирует их.
//     /// </summary>
//     private async Task LoadPromoCodesFromCloud()
//     {
//         try
//         {
//             var data = await CloudSaveService.Instance.Data.LoadAsync();  // Загружаем данные из облака

//             if (data.TryGetValue(PROMO_CLOUD_KEY, out var promoDataJson))
//             {
//                 // Загружаем промокоды из облака
//                 _cloudPromoCodes = JsonUtility.FromJson<PromoCodeDictionary>(promoDataJson).ToDictionary();
//                 Debug.Log($"Данные промокодов загружены из облака: {promoDataJson}");
//             }
//             else
//             {
//                 Debug.LogWarning("Данные промокодов отсутствуют в облаке. Инициализация из инспектора.");

//                 // Инициализируем данные локальными промокодами
//                 foreach (var promo in _promoCodes)
//                 {
//                     if (!_cloudPromoCodes.ContainsKey(promo.Code))
//                     {
//                         _cloudPromoCodes[promo.Code] = 0; // 0 - не использован
//                     }
//                 }

//                 // Сохраняем инициализированные данные в облако
//                 await SavePromoCodesToCloud();
//             }
//         }
//         catch (Exception e)
//         {
//             Debug.LogError($"Ошибка загрузки промокодов из облака: {e.Message}");
//         }
//     }

//     /// <summary>
//     /// Помечает промокод как использованный.
//     /// </summary>
//     public async Task MarkPromoCodeAsUsed(string code)
//     {
//         try
//         {
//             if (_cloudPromoCodes.ContainsKey(code))
//             {
//                 _cloudPromoCodes[code] = 1; // 1 - использован
//                 await SavePromoCodesToCloud();
//                 Debug.Log($"Промокод {code} помечен как использованный.");
//             }
//             else
//             {
//                 Debug.LogError($"Промокод {code} не найден.");
//             }
//         }
//         catch (Exception e)
//         {
//             Debug.LogError($"Ошибка при пометке промокода {code} как использованный: {e.Message}");
//         }
//     }

//     /// <summary>
//     /// Проверяет, использован ли промокод.
//     /// </summary>
//     public bool IsPromoCodeUsed(string code)
//     {
//         return _cloudPromoCodes.TryGetValue(code, out var status) && status == 1;
//     }

//     /// <summary>
//     /// Обработка нажатия кнопки Ok (проверка и добавление награды).
//     /// </summary>
//     public async void Ok(string code)
//     {
//         if (_cloudPromoCodes.ContainsKey(code))
//         {
//             // Проверяем, использован ли промокод
//             if (_cloudPromoCodes[code] == 0) // Если не использован
//             {
//                 // Получаем данные для эликсиров и рубинов
//                 var promo = _promoCodes.Find(p => p.Code == code);
//                 if (promo != null)
//                 {
//                     // Добавляем эликсиры и рубины через соответствующие менеджеры
//                     AddElixirs(promo.ElixirAmount);
//                     AddRubies(promo.RubyAmount);

//                     // Помечаем промокод как использованный
//                     await MarkPromoCodeAsUsed(code);
//                     Debug.Log($"Промокод {code} использован. Эликсиры: {promo.ElixirAmount}, Рубины: {promo.RubyAmount}");
//                 }
//             }
//             else
//             {
//                 Debug.Log($"Промокод {code} уже использован.");
//             }
//         }
//         else
//         {
//             Debug.LogError($"Промокод {code} не найден.");
//         }
//     }

//     /// <summary>
//     /// Добавляет эликсиры через ElixirManagerShop.
//     /// </summary>
//     private void AddElixirs(int amount)
//     {
//         ElixirManagerShop.Instance.AddElixir(amount);  // Используем метод для добавления эликсиров
//         Debug.Log($"Эликсиры обновлены: {amount}");
//     }

//     /// <summary>
//     /// Добавляет рубины через IAPManager.
//     /// </summary>
//     private void AddRubies(int amount)
//     {
//         IAPManager.Instance.AddRuby(amount);  // Используем метод для добавления рубинов
//         Debug.Log($"Рубины обновлены: {amount}");
//     }

//     // Метод для тестирования сохранения в облако, вызовите его при необходимости
//     public async void TestSavePromoCodes()
//     {
//         await SavePromoCodesToCloud();
//     }

//     // Метод для тестирования загрузки из облака, вызовите его при необходимости
//     public async void TestLoadPromoCodes()
//     {
//         await LoadPromoCodesFromCloud();
//     }
// }

// [Serializable]
// public class PromoCodeData
// {
//     public string Code; // Промокод
//     public int ElixirAmount; // Количество эликсиров
//     public int RubyAmount; // Количество рубинов
// }

// [Serializable]
// public class PromoCodeDictionary
// {
//     public List<string> Keys = new List<string>();
//     public List<int> Values = new List<int>();

//     public PromoCodeDictionary() { }

//     public PromoCodeDictionary(Dictionary<string, int> dictionary)
//     {
//         foreach (var kvp in dictionary)
//         {
//             Keys.Add(kvp.Key);
//             Values.Add(kvp.Value);
//         }
//     }

//     public Dictionary<string, int> ToDictionary()
//     {
//         var dictionary = new Dictionary<string, int>();
//         for (int i = 0; i < Keys.Count; i++)
//         {
//             dictionary[Keys[i]] = Values[i];
//         }
//         return dictionary;
//     }
// }