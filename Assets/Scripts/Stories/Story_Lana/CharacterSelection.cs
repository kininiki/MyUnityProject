using UnityEngine;
using UnityEngine.UI;
using Fungus;
using System.Collections;

using Unity.Services.CloudSave;
using Unity.Services.Core;
using Unity.Services.Authentication;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

using TMPro;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.ResourceManagement.AsyncOperations;




[CommandInfo("Character",
    "Character Selection",
    "Displays character selection UI with left/right arrows, a select button, and up/down buttons to hide/show the panel. Includes localized text for character types.")]
public class CharacterSelectionCommand : Command
{
    [Tooltip("Indexes of character types to cycle through")]
    [SerializeField]
    private int[] characterTypeIndexes;

    [Tooltip("Wardrobe panel (parent of all UI elements)")]
    [SerializeField]
    private GameObject wardrobePanel; // Панель Wardrobe, которая включает все UI-элементы

    [Tooltip("Panel to hide/show (child of WardrobePanel)")]
    [SerializeField]
    private GameObject panelToHide; // Объект Panel, который будет скрываться/показываться

    [Tooltip("Left arrow button")]
    [SerializeField]
    private Button leftArrowButton;

    [Tooltip("Right arrow button")]
    [SerializeField]
    private Button rightArrowButton;

    [Tooltip("Select button")]
    [SerializeField]
    private Button selectButton;

    [Tooltip("Button to hide the panel (move down)")]
    [SerializeField]
    private Button buttonDown;

    [Tooltip("Button to show the panel (move up)")]
    [SerializeField]
    private Button buttonUp;

    [Tooltip("Position on the screen where the character should appear")]
    [SerializeField]
    private int positionIndex;

    [Tooltip("Duration of the slide animation")]
    [SerializeField]
    private float slideDuration = 0.5f;

    [Tooltip("Distance to slide the panel up/down")]
    [SerializeField]
    private float slideDistance = 200f;

    [Tooltip("TextMeshPro component to display character type description")]
    [SerializeField]
    private TextMeshProUGUI characterDescriptionText;

    [Tooltip("Localized text keys for character type descriptions")]
    [SerializeField]
    private LocalizedString[] characterTypeDescriptions;

    private int currentIndex = 0;
    private SimpleCharacterManager characterManager;
    private Flowchart flowchart;

    private Vector2 originalPosition; // Исходная позиция панели
    private bool isSliding = false; // Флаг для предотвращения повторных нажатий

    public override void OnEnter()
    {
        // Инициализация
        characterManager = FindObjectOfType<SimpleCharacterManager>();
        flowchart = GetFlowchart();

        if (characterManager == null || flowchart == null)
        {
            Debug.LogError("SimpleCharacterManager или Flowchart не найдены!");
            Continue();
            return;
        }

        // Проверка на наличие индексов
        if (characterTypeIndexes == null || characterTypeIndexes.Length == 0)
        {
            Debug.LogError("Не указаны индексы типов персонажей!");
            Continue();
            return;
        }

        // Активируем панель Wardrobe
        wardrobePanel.SetActive(true);

        // Сохраняем исходную позицию панели
        originalPosition = panelToHide.GetComponent<RectTransform>().anchoredPosition;

        // Устанавливаем начального персонажа
        SetCharacter(currentIndex);

        // Назначаем обработчики для кнопок
        leftArrowButton.onClick.RemoveAllListeners();
        leftArrowButton.onClick.AddListener(OnLeftArrowClicked);

        rightArrowButton.onClick.RemoveAllListeners();
        rightArrowButton.onClick.AddListener(OnRightArrowClicked);

        selectButton.onClick.RemoveAllListeners();
        selectButton.onClick.AddListener(OnSelectClicked);

        buttonDown.onClick.RemoveAllListeners();
        buttonDown.onClick.AddListener(HidePanel);

        buttonUp.onClick.RemoveAllListeners();
        buttonUp.onClick.AddListener(ShowPanel);

        Debug.Log("CharacterSelectionCommand инициализирован.");
    }

    public override void OnExit()
    {
        // Деактивируем панель Wardrobe при завершении команды
        wardrobePanel.SetActive(false);
    }

    private void SetCharacter(int index)
    {
        if (index < 0 || index >= characterTypeIndexes.Length)
        {
            Debug.LogError("Неверный индекс персонажа!");
            return;
        }

        // Получаем текущий тип персонажа
        int characterTypeIndex = characterTypeIndexes[index];

        // Получаем значения переменных из Flowchart для всех слоев
        int hairIndex = flowchart.GetVariable<IntegerVariable>("Hair").Value;
        int makeupIndex = flowchart.GetVariable<IntegerVariable>("Makeup").Value;
        int dressIndex = flowchart.GetVariable<IntegerVariable>("Dress").Value;
        int ukrashenieIndex = flowchart.GetVariable<IntegerVariable>("Ukrashenie").Value;
        int accessoriseIndex = flowchart.GetVariable<IntegerVariable>("Accessorise").Value;

        // Устанавливаем спрайты для всех слоев
        characterManager.SetCharacterSprite(
            positionIndex,
            characterTypeIndex,
            CharacterEmotion.Нейтральное, // Эмоция по умолчанию
            hairIndex,
            makeupIndex,
            dressIndex,
            ukrashenieIndex,
            accessoriseIndex
        );

        // Обновляем текстовое описание персонажа
        UpdateCharacterDescription(characterTypeIndex);
    }

    private void UpdateCharacterDescription(int characterTypeIndex)
    {
        if (characterDescriptionText == null || characterTypeDescriptions == null || characterTypeIndex < 0 || characterTypeIndex >= characterTypeDescriptions.Length)
        {
            Debug.LogError("Не удалось обновить описание персонажа: проверьте настройки TextMeshPro и локализованных строк.");
            return;
        }

        // Получаем локализованное описание для текущего типа персонажа
        var localizedDescription = characterTypeDescriptions[characterTypeIndex];
        StartCoroutine(LoadLocalizedText(localizedDescription));
    }

    private IEnumerator LoadLocalizedText(LocalizedString localizedString)
    {
        // Ждем завершения инициализации системы локализации
        yield return LocalizationSettings.InitializationOperation;

        // Получаем локализованный текст асинхронно
        var operation = localizedString.GetLocalizedStringAsync();
        yield return operation;

        // Проверяем статус операции
        if (operation.Status == AsyncOperationStatus.Succeeded)
        {
            characterDescriptionText.text = operation.Result;
        }
        else if (operation.Status == AsyncOperationStatus.Failed)
        {
            Debug.LogError("Ошибка загрузки локализованного текста: " + operation.OperationException.Message);
            characterDescriptionText.text = "Error: Localization Failed";
        }
        else
        {
            Debug.LogError("Не удалось загрузить локализованный текст: операция не завершена.");
            characterDescriptionText.text = "Error: Localization Failed";
        }
    }

    private void OnLeftArrowClicked()
    {
        currentIndex--;
        if (currentIndex < 0)
        {
            currentIndex = characterTypeIndexes.Length - 1;
        }
        SetCharacter(currentIndex);
    }

    private void OnRightArrowClicked()
    {
        currentIndex++;
        if (currentIndex >= characterTypeIndexes.Length)
        {
            currentIndex = 0;
        }
        SetCharacter(currentIndex);
    }

    private void OnSelectClicked()
    {
        // Устанавливаем выбранный тип персонажа в переменную Flowchart
        IntegerVariable typeVariable = flowchart.GetVariable<IntegerVariable>("Type");
        if (typeVariable != null)
        {
            typeVariable.Value = characterTypeIndexes[currentIndex];
            Debug.Log($"Выбран тип персонажа: {typeVariable.Value}");
        }
        else
        {
            Debug.LogError("Переменная 'Type' не найдена в Flowchart!");
        }

        // Завершаем команду
        Continue();
    }

    private void HidePanel()
    {
        if (isSliding) return; // Если уже идет анимация, пропускаем
        isSliding = true;

        // Скрываем панель (перемещаем вниз)
        StartCoroutine(SlidePanel(Vector2.down * slideDistance, () =>
        {
            buttonDown.gameObject.SetActive(false); // Скрываем ButtonDown
            buttonUp.gameObject.SetActive(true); // Показываем ButtonUp
            isSliding = false;
            Debug.Log("Panel hidden.");
        }));
    }

    private void ShowPanel()
    {
        if (isSliding) return; // Если уже идет анимация, пропускаем
        isSliding = true;

        // Показываем панель (перемещаем вверх)
        StartCoroutine(SlidePanel(Vector2.zero, () =>
        {
            buttonDown.gameObject.SetActive(true); // Показываем ButtonDown
            buttonUp.gameObject.SetActive(false); // Скрываем ButtonUp
            isSliding = false;
            Debug.Log("Panel shown.");
        }));
    }

    private IEnumerator SlidePanel(Vector2 targetOffset, System.Action onComplete)
    {
        Debug.Log("SlidePanel started.");
        Vector2 startPosition = panelToHide.GetComponent<RectTransform>().anchoredPosition;
        Vector2 targetPosition = originalPosition + targetOffset;

        Debug.Log($"Start position: {startPosition}, Target position: {targetPosition}");

        float elapsedTime = 0f;
        while (elapsedTime < slideDuration)
        {
            elapsedTime += Time.deltaTime;
            panelToHide.GetComponent<RectTransform>().anchoredPosition = Vector2.Lerp(startPosition, targetPosition, elapsedTime / slideDuration);
            Debug.Log($"Current position: {panelToHide.GetComponent<RectTransform>().anchoredPosition}");
            yield return null;
        }

        panelToHide.GetComponent<RectTransform>().anchoredPosition = targetPosition;
        Debug.Log("SlidePanel finished.");
        onComplete?.Invoke();
    }

    public override string GetSummary()
    {
        return "Displays character selection UI with left/right arrows, a select button, and up/down buttons to hide/show the panel. Includes localized text for character types.";
    }

    public override Color GetButtonColor()
    {
        return new Color32(255, 200, 150, 255);
    }
}





[CommandInfo("Cloud", 
    "Save Cloud Wardrobe", 
    "Saves multiple category indexes to Unity Cloud Save.")]
public class SaveCloudVariableCommand : Command
{
    [Tooltip("The cloud section name, for example 'LANA_WARDROBE_DATA'.")]
    public string cloudSection;

    // Пары ключ-значение для каждой категории и переменной Flowchart
    [Tooltip("The category 'Type' key and associated Flowchart variable.")]
    public string typeCategoryKey;
    [VariableProperty(typeof(IntegerVariable))]
    public IntegerVariable typeCategoryValue;

    [Tooltip("The category 'Makeup' key and associated Flowchart variable.")]
    public string makeupCategoryKey;
    [VariableProperty(typeof(IntegerVariable))]
    public IntegerVariable makeupCategoryValue;

    [Tooltip("The category 'Hair' key and associated Flowchart variable.")]
    public string hairCategoryKey;
    [VariableProperty(typeof(IntegerVariable))]
    public IntegerVariable hairCategoryValue;

    [Tooltip("The category 'Dress' key and associated Flowchart variable.")]
    public string dressCategoryKey;
    [VariableProperty(typeof(IntegerVariable))]
    public IntegerVariable dressCategoryValue;

    [Tooltip("The category 'Ukrashenie' key and associated Flowchart variable.")]
    public string ukrashenieCategoryKey;
    [VariableProperty(typeof(IntegerVariable))]
    public IntegerVariable ukrashenieCategoryValue;

    [Tooltip("The category 'Accessorise' key and associated Flowchart variable.")]
    public string accessoriseCategoryKey;
    [VariableProperty(typeof(IntegerVariable))]
    public IntegerVariable accessoriseCategoryValue;

    public override void OnEnter()
    {
        // Запуск асинхронного процесса сохранения
        SaveDataToCloud();
    }

    private async void SaveDataToCloud()
    {
        // Инициализация Unity Services, если еще не инициализировано
        if (!UnityServices.State.Equals(ServicesInitializationState.Initialized))
        {
            try
            {
                await UnityServices.InitializeAsync();
                await SignInAnonymously();
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to initialize Unity Services: {e.Message}");
                Continue();
                return;
            }
        }

        // Создаем JSON-объект для сохранения
        JObject jsonData = new JObject
        {
            ["categories"] = new JArray
            {
                new JObject
                {
                    ["categoryName"] = typeCategoryKey,
                    ["index"] = typeCategoryValue.Value
                },
                new JObject
                {
                    ["categoryName"] = makeupCategoryKey,
                    ["index"] = makeupCategoryValue.Value
                },
                new JObject
                {
                    ["categoryName"] = hairCategoryKey,
                    ["index"] = hairCategoryValue.Value
                },
                new JObject
                {
                    ["categoryName"] = dressCategoryKey,
                    ["index"] = dressCategoryValue.Value
                },
                new JObject
                {
                    ["categoryName"] = ukrashenieCategoryKey,
                    ["index"] = ukrashenieCategoryValue.Value
                },
                new JObject
                {
                    ["categoryName"] = accessoriseCategoryKey,
                    ["index"] = accessoriseCategoryValue.Value
                }
            }
        };

        // Сохраняем данные в облако
        try
        {
            var dataToSave = new Dictionary<string, object>
            {
                { cloudSection, jsonData.ToString() }
            };

            await CloudSaveService.Instance.Data.ForceSaveAsync(dataToSave);
            Debug.Log($"Data saved to cloud section '{cloudSection}': {jsonData}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error saving cloud variable: {e.Message}");
        }

        Continue();
    }

    private async System.Threading.Tasks.Task SignInAnonymously()
    {
        try
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log("Sign-in successful");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to sign in: {e.Message}");
        }
    }

    public override string GetSummary()
    {
        return $"Save multiple category indexes to '{cloudSection}'";
    }
}









[CommandInfo("UI",
    "Hide Wardrobe UI",
    "Hides all wardrobe related UI elements")]
public class HideWardrobeUICommand : Command
{
    [Header("UI References")]
    [SerializeField] private GameObject wardrobePanel;
    [SerializeField] private GameObject panelToHide;
    [SerializeField] private RawImage[] characterParts;

    [Header("Fade Settings")]
    [SerializeField] private float fadeDuration = 0.3f;
    [SerializeField] private float targetAlpha = 0f;

    public override void OnEnter()
    {
        StartCoroutine(HideUI());
    }

    private IEnumerator HideUI()
    {
        // Плавное исчезновение частей персонажа
        if (characterParts != null && characterParts.Length > 0)
        {
            float elapsed = 0f;
            float startAlpha = characterParts[0].color.a;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float currentAlpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);
                SetCharacterAlpha(currentAlpha);
                yield return null;
            }
        }

        // Деактивация UI элементов
        if (wardrobePanel != null) wardrobePanel.SetActive(false);
        if (panelToHide != null) panelToHide.SetActive(false);

        Continue();
    }

    private void SetCharacterAlpha(float alpha)
    {
        foreach (var part in characterParts)
        {
            if (part != null)
            {
                Color color = part.color;
                color.a = alpha;
                part.color = color;
            }
        }
    }

    public override string GetSummary()
    {
        return $"Hides wardrobe UI in {fadeDuration}s";
    }

    public override Color GetButtonColor()
    {
        return new Color32(160, 255, 160, 255);
    }
}