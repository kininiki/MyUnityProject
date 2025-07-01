// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.UI;

// public class WardrobeRenderer : MonoBehaviour
// {
//     [SerializeField] private Image baseCharacterImage; // Базовое изображение персонажа
//     [SerializeField] private List<WardrobeCategory> categories; // Категории одежды (волосы, платье, аксессуары и т. д.)
    
    
//     private void Start()
//     {
//         // Загружаем сохранённое состояние гардероба
//         ApplySavedOutfit();
//     }

//     /// <summary>
//     /// Применяет сохранённое состояние гардероба и обновляет внешний вид персонажа.
//     /// </summary>
//     public void ApplySavedOutfit()
//     {
//         foreach (var category in categories)
//         {
//             int selectedIndex = WardrobeManager.Instance.GetSelectedItemIndex(category.categoryName);
//             category.currentSpriteIndex = selectedIndex;
//             UpdateCategoryDisplay(category);
//         }
//     }

//     /// <summary>
//     /// Обновляет отображение одежды в заданной категории.
//     /// </summary>
//     private void UpdateCategoryDisplay(WardrobeCategory category)
//     {
//         if (category.sprites.Length == 0) return;

//         var selectedSprite = category.sprites[category.currentSpriteIndex];
//         category.displayImage.sprite = selectedSprite.sprite;
//         category.displayImage.enabled = true;
//     }

//     /// <summary>
//     /// Меняет текущий предмет одежды в указанной категории и обновляет отображение.
//     /// </summary>
//     public void ChangeItemInCategory(string categoryName, int direction)
//     {
//         var category = categories.Find(c => c.categoryName == categoryName);
//         if (category == null) return;

//         int newIndex = (category.currentSpriteIndex + direction + category.sprites.Length) % category.sprites.Length;
        
//         if (WardrobeManager.Instance.IsItemUnlocked(categoryName, newIndex))
//         {
//             category.currentSpriteIndex = newIndex;
//             WardrobeManager.Instance.SetSelectedItemIndex(categoryName, newIndex);
//             UpdateCategoryDisplay(category);
//         }
//     }
// }