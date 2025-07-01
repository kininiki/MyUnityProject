// using System.Threading.Tasks;
// using UnityEngine;
// using UnityEngine.UI;
// using TMPro;

// public class WardrobeUI : MonoBehaviour
// {
//     [SerializeField] private Button nextOutfitButton;
//     [SerializeField] private Button previousOutfitButton;
//     [SerializeField] private TMP_Text outfitLabel;

//     private int currentOutfitIndex;
//     private const int totalOutfits = 5; // Количество доступных нарядов

//     private async void Start()
//     {
//         // Убедимся, что гардероб загружен перед взаимодействием
//         await WardrobeManager.Instance.LoadWardrobeData();
//         currentOutfitIndex = WardrobeManager.Instance.GetSelectedOutfit();
//         UpdateUI();

//         // Подписываем кнопки на методы
//         nextOutfitButton.onClick.AddListener(() => ChangeOutfit(1));
//         previousOutfitButton.onClick.AddListener(() => ChangeOutfit(-1));
//     }

//     /// <summary>
//     /// Меняет текущий наряд и обновляет UI.
//     /// </summary>
//     private async void ChangeOutfit(int direction)
//     {
//         currentOutfitIndex += direction;

//         // Зацикливаем наряды
//         if (currentOutfitIndex >= totalOutfits) currentOutfitIndex = 0;
//         if (currentOutfitIndex < 0) currentOutfitIndex = totalOutfits - 1;

//         WardrobeManager.Instance.SetSelectedOutfit(currentOutfitIndex);
//         UpdateUI();
//     }

//     /// <summary>
//     /// Обновляет текстовый индикатор текущего наряда.
//     /// </summary>
//     private void UpdateUI()
//     {
//         outfitLabel.text = $"Outfit: {currentOutfitIndex + 1}";
//     }
// }
