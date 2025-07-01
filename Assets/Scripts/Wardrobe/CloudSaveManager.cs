// using System;
// using System.Collections.Generic;
// using System.Threading.Tasks;
// using UnityEngine;
// using Newtonsoft.Json;
// using Unity.Services.CloudSave;

// public class CloudSaveManager : MonoBehaviour
// {
//     private const string UNLOCK_KEY = "unlock_variables";
//     private const string SAVE_KEY = "wardrobe_data";

//     public static CloudSaveManager Instance { get; private set; }

//     private Dictionary<string, string> unlockVariables = new Dictionary<string, string>();

//     private void Awake()
//     {
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

//     /// <summary>
//     /// Загружает переменные разблокировки из облака.
//     /// </summary>
//     public async Task LoadUnlockVariables()
//     {
//         try
//         {
//             var data = await CloudSaveService.Instance.Data.LoadAsync(new HashSet<string> { UNLOCK_KEY });

//             if (data.TryGetValue(UNLOCK_KEY, out var savedData) && IsValidJson(savedData.ToString()))
//             {
//                 unlockVariables = JsonConvert.DeserializeObject<Dictionary<string, string>>(savedData.ToString()) ?? new Dictionary<string, string>();
//                 Debug.Log($"Loaded unlock variables: {string.Join(", ", unlockVariables)}");
//             }
//             else
//             {
//                 Debug.Log("No valid unlock variables found, initializing defaults.");
//                 InitializeDefaultUnlockVariables();
//                 await SaveUnlockVariables();
//             }
//         }
//         catch (Exception e)
//         {
//             Debug.LogError($"Error loading unlock variables: {e.Message}");
//             InitializeDefaultUnlockVariables();
//         }
//     }

//     /// <summary>
//     /// Сохраняет переменные разблокировки в облако.
//     /// </summary>
//     public async Task SaveUnlockVariables()
//     {
//         try
//         {
//             string jsonData = JsonConvert.SerializeObject(unlockVariables);
//             var data = new Dictionary<string, object> { { UNLOCK_KEY, jsonData } };

//             await CloudSaveService.Instance.Data.ForceSaveAsync(data);
//             Debug.Log("Unlock variables saved successfully.");
//         }
//         catch (Exception e)
//         {
//             Debug.LogError($"Error saving unlock variables: {e.Message}");
//         }
//     }

//     /// <summary>
//     /// Инициализирует переменные разблокировки значениями по умолчанию.
//     /// </summary>
//     private void InitializeDefaultUnlockVariables()
//     {
//         unlockVariables.Clear();
//         for (int i = 1; i <= 40; i++)
//         {
//             unlockVariables[$"w{i}"] = "0";
//         }
//     }

//     /// <summary>
//     /// Проверяет, является ли строка корректным JSON.
//     /// </summary>
//     private bool IsValidJson(string jsonString)
//     {
//         try
//         {
//             JsonConvert.DeserializeObject<object>(jsonString);
//             return true;
//         }
//         catch
//         {
//             return false;
//         }
//     }

//     /// <summary>
//     /// Получает значение переменной разблокировки.
//     /// </summary>
//     public int GetUnlockValue(string variableName)
//     {
//         return unlockVariables.TryGetValue(variableName, out string value) && int.TryParse(value, out int result) ? result : 0;
//     }

//     /// <summary>
//     /// Обновляет переменную разблокировки и сохраняет изменения.
//     /// </summary>
//     public async void UpdateUnlockVariable(string variableName, int value)
//     {
//         unlockVariables[variableName] = value.ToString();
//         await SaveUnlockVariables();
//     }
// }
