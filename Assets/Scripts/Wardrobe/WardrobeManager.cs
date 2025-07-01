// using System.Collections.Generic;
// using UnityEngine;

// public class WardrobeManager : MonoBehaviour
// {
//     public static WardrobeManager Instance { get; private set; }

//     private Dictionary<string, int> selectedItems = new Dictionary<string, int>();
//     private HashSet<string> unlockedItems = new HashSet<string>(); // Набор разблокированных предметов

//     private void Awake()
//     {
//         if (Instance == null)
//         {
//             Instance = this;
//         }
//         else
//         {
//             Destroy(gameObject);
//         }

//         LoadWardrobeData();
//     }

//     /// <summary>
//     /// Проверяет, разблокирован ли предмет в данной категории.
//     /// </summary>
//     public bool IsItemUnlocked(string categoryName, int itemIndex)
//     {
//         string key = $"{categoryName}_{itemIndex}";
//         return unlockedItems.Contains(key);
//     }

//     /// <summary>
//     /// Разблокировать предмет одежды.
//     /// </summary>
//     public void UnlockItem(string categoryName, int itemIndex)
//     {
//         string key = $"{categoryName}_{itemIndex}";
//         if (!unlockedItems.Contains(key))
//         {
//             unlockedItems.Add(key);
//             SaveWardrobeData();
//         }
//     }

//     /// <summary>
//     /// Получить индекс выбранного предмета в категории.
//     /// </summary>
//     public int GetSelectedItemIndex(string categoryName)
//     {
//         return selectedItems.ContainsKey(categoryName) ? selectedItems[categoryName] : 0;
//     }

//     /// <summary>
//     /// Установить выбранный предмет в категории.
//     /// </summary>
//     public void SetSelectedItemIndex(string categoryName, int itemIndex)
//     {
//         selectedItems[categoryName] = itemIndex;
//         SaveWardrobeData();
//     }

//     /// <summary>
//     /// Сохранение данных гардероба.
//     /// </summary>
//     private void SaveWardrobeData()
//     {
//         PlayerPrefs.SetString("UnlockedItems", string.Join(",", unlockedItems));
//         foreach (var pair in selectedItems)
//         {
//             PlayerPrefs.SetInt($"Selected_{pair.Key}", pair.Value);
//         }
//         PlayerPrefs.Save();
//     }

//     /// <summary>
//     /// Загрузка данных гардероба.
//     /// </summary>
//     private void LoadWardrobeData()
//     {
//         string unlockedData = PlayerPrefs.GetString("UnlockedItems", "");
//         unlockedItems = new HashSet<string>(unlockedData.Split(','));

//         selectedItems.Clear();
//         // Пример категорий (можно переделать под динамическую загрузку)
//         string[] categories = { "Hair", "Dress", "Shoes", "Hat" };

//         foreach (string category in categories)
//         {
//             selectedItems[category] = PlayerPrefs.GetInt($"Selected_{category}", 0);
//         }
//     }
// }
