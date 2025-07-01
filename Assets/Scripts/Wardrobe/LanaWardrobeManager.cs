using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using Unity.Services.CloudSave;
using Unity.Services.Core;
using Unity.Services.Authentication;
using System.Threading.Tasks;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using System.Collections;


[Serializable]
public class UnlockCondition
{
    public string variableName;  // Будет содержать значения типа "w1", "w2" и т.д.
    public int requiredValue = 1;
}

[Serializable]
public class WardrobeSprite
{
    public Sprite sprite;
    public string localizationKey; // Ключ для локализации

    [TextArea(1, 3)]
    public string description;
    public UnlockCondition[] unlockConditions;
    public bool isPaid = false;  // Галочка для определения, является ли спрайт платным
    public int price = 0;  // Цена
    public string buyIndex; // Индекс покупки (например, "buy1", "buy2" и т.д.)
    public bool isSelectAll = false; // Является ли элемент "Взять всё"

    public void SetPurchased()
    {
        // Сохраняем состояние покупки в PlayerPrefs
        PlayerPrefs.SetInt(buyIndex, 1);  // 1 означает, что куплено
        PlayerPrefs.Save();
    }

    public bool IsPurchased()
    {
        // Проверяем, куплен ли спрайт
        return PlayerPrefs.GetInt(buyIndex, 0) == 1;
    }

}



[Serializable]
public class WardrobeFavCategory
{
    public string categoryName; // Название категории (например, "Персонаж 1")
    public WardrobeSprite[] sprites; // Спрайты для этой категории
    public Image displayImageFav; // DisplayImageFav для отображения спрайтов
    public TMP_Text itemNameText; // Текст для названия элемента
    public int currentSpriteIndex; // Текущий индекс спрайта
    public int defaultSpriteIndex; // Индекс спрайта по умолчанию
}

[Serializable]
public class WardrobeCategory
{
    public string categoryName;
    public WardrobeSprite[] sprites;
    public Image displayImage;
    public Image displayImageAll; // Изображение для "Взять всё"
    public TMP_Text itemNameText;
    public int currentSpriteIndex;
    public int defaultSpriteIndex;
}

[Serializable]
public class CategoryIndex
{
    public string categoryName;
    public int index;
}

[Serializable]
public class WardrobeSaveData
{
    public List<CategoryIndex> categories = new List<CategoryIndex>();

    public Dictionary<string, int> ToDictionary()
    {
        var dict = new Dictionary<string, int>();
        foreach (var category in categories)
        {
            dict[category.categoryName] = category.index;
        }
        return dict;
    }

    public void FromDictionary(Dictionary<string, int> dict)
    {
        categories.Clear();
        foreach (var kvp in dict)
        {
            categories.Add(new CategoryIndex { categoryName = kvp.Key, index = kvp.Value });
        }
    }
}

[Serializable]
public class FavWardrobeSaveData
{
    public List<CategoryIndex> categories = new List<CategoryIndex>();

    public Dictionary<string, int> ToDictionary()
    {
        var dict = new Dictionary<string, int>();
        foreach (var category in categories)
        {
            dict[category.categoryName] = category.index;
        }
        return dict;
    }

    public void FromDictionary(Dictionary<string, int> dict)
    {
        categories.Clear();
        foreach (var kvp in dict)
        {
            categories.Add(new CategoryIndex { categoryName = kvp.Key, index = kvp.Value });
        }
    }
}



[Serializable]
public class UnlockVariables
{
    public Dictionary<string, string> variables = new Dictionary<string, string>();

    public int GetIntValue(string key, int defaultValue = 0)
    {
        if (variables.TryGetValue(key, out string value))
        {
            if (int.TryParse(value, out int result))
            {
                return result;
            }
        }
        return defaultValue;
    }
}

public class LanaWardrobeManager : MonoBehaviour
{
    [Header("Wardrobe Categories")]
    public WardrobeCategory[] categories;
    public WardrobeFavCategory[] favCategories; // Категории для второстепенных персонажей

    [Header("UI Elements")]
    public Button leftButton;
    public Button rightButton;
    public Button[] categoryButtons;
    public Button[] favCategoryButtons; // Кнопки для категорий второстепенных персонажей
    public GameObject loadingText;
    public GameObject lockedOverlay;
    public TMP_Text lockMessageText;

    public TMP_Text priceText; // Текст для отображения цены
    public Button buyButton;   // Кнопка "Купить"
    public int playerCoins = 1000; // Валюта игрока, надо подключить к общей системе

    private WardrobeCategory currentCategory;
    private WardrobeFavCategory currentFavCategory; // Текущая категория второстепенного персонажа
    private const string SAVE_KEY = "LANA_WARDROBE_DATA";
    private const string UNLOCK_KEY = "LANA_UNLOCK_VARIABLES";
    private const string BUY_FAV_KEY = "BUY_FAV"; // Ключ для покупок второстепенных персонажей
    private const string FAV_SAVE_KEY = "LANA_FAV_DATA"; // Для второстепенных персонажей
    private const string FAV_BUY_KEY = "LANA_FAV_BUY"; // Для покупок второстепенных персонажей
    private int currentCategoryIndex = -1;
    private int currentFavCategoryIndex = -1;
    private bool isInitialized = false;
    private bool isLoading = true;
    private Dictionary<string, string> unlockVariables = new Dictionary<string, string>();

    private void OnEnable()
    {
        SetUIInteractable(false);
    }

    private void SetUIInteractable(bool interactable)
    {
        if (leftButton != null) leftButton.interactable = interactable;
        if (rightButton != null) rightButton.interactable = interactable;
        foreach (var button in categoryButtons)
        {
            if (button != null) button.interactable = interactable;
        }

        if (loadingText != null)
        {
            loadingText.SetActive(!interactable);
        }
    }

    private async void Start()
    {
        Debug.Log("Starting initialization...");
        await InitializeUnityServices();
        
        Debug.Log("Loading unlock variables...");
        await LoadUnlockVariables();
        
        Debug.Log("Loading wardrobe data...");
        await LoadWardrobeData();

        await LoadFavWardrobeData(); // Загружаем данные для второстепенных персонажей
        
        Debug.Log("Loading purchases from cloud...");
        await LoadPurchasesFromCloud(); // Загружаем данные о покупках

        await LoadFavPurchasesFromCloud(); // Загружаем покупки для второстепенных персонажей
        
        Debug.Log("Updating display...");
        UpdateAllCategoriesDisplay();
        
        InitializeUI();
        SetUIInteractable(true);
        isLoading = false;

        InitializeWardrobeState();  // Инициализация состояния гардероба

        // Убедимся, что все спрайты из всех категорий отображаются
        ApplyAllCategorySprites();
        
        if (categories.Length > 0)
        {
            SelectCategory(0);
        }

        InvokeRepeating("CheckCloudVariables", 5f, 5f);
    }



private void ApplyAllCategorySprites()
{
    foreach (var category in categories)
    {
        // Получаем текущий спрайт для категории
        var currentSprite = category.sprites[category.currentSpriteIndex];
        
        // Применяем спрайт к соответствующему изображению (если оно есть)
        if (category.displayImage != null)
        {
            category.displayImage.sprite = currentSprite.sprite;
            category.displayImage.enabled = true;
        }

        // Обновляем текстовое поле с названием спрайта (если оно есть)
        if (category.itemNameText != null)
        {
            //category.itemNameText.text = currentSprite.spriteName;
            category.itemNameText.text = currentSprite.localizationKey;
            category.itemNameText.enabled = true;
        }
    }
}



    private async void CheckCloudVariables()
    {
        await LoadUnlockVariables();
        UpdateAllCategoriesDisplay();
    }

    private async Task InitializeUnityServices()
    {
        try
        {
            await UnityServices.InitializeAsync();
            await SignInAnonymously();
            isInitialized = true;
            Debug.Log("Unity Services initialized successfully");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to initialize Unity Services: {e.Message}");
            SetDefaultSprites();
        }
    }

    private async Task SignInAnonymously()
    {
        try
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log("Sign in successful");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to sign in: {e.Message}");
        }
    }


    private bool IsValidJson(string jsonData)
    {
        try
        {
            // Попытка десериализовать строку, если не удается, то это не валидный JSON
            JsonConvert.DeserializeObject(jsonData);
            return true;
        }
        catch
        {
            return false;
        }
    }

private async Task LoadUnlockVariables()
{
    try
    {
        // Загружаем данные из облака
        var data = await CloudSaveService.Instance.Data.LoadAsync(new HashSet<string> { UNLOCK_KEY });

        if (data.TryGetValue(UNLOCK_KEY, out var savedData))
        {
            Debug.Log($"Raw data from cloud: {savedData}");

            try
            {
                // Преобразуем данные в строку (если это объект)
                string jsonString = savedData.ToString();

                // Проверяем, что данные представляют собой валидный JSON
                if (IsValidJson(jsonString))
                {
                    // Десериализуем данные в словарь
                    var parsedData = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString);

                    if (parsedData != null)
                    {
                        // Сохраняем данные в переменную
                        unlockVariables = parsedData;
                        Debug.Log($"Parsed unlock variables: {string.Join(", ", unlockVariables.Select(kv => $"{kv.Key}={kv.Value}"))}");

                        // Проверяем значения w_dress, w_hair, w_makeup, w_ukrashenie, w_accessorise
                        CheckAndHideCanvasObjects();

                        //AutoPressCategoryButtonWithDelay();
                        // Автоматически нажимаем кнопку на основе unlockVariables
                        StartCoroutine(AutoPressCategoryButtonWithDelay());
                    }
                    else
                    {
                        Debug.LogError("Failed to deserialize data into dictionary. Initializing defaults.");
                        InitializeDefaultUnlockVariables();
                    }
                }
                else
                {
                    Debug.LogError("Invalid JSON format in the cloud data. Initializing defaults.");
                    InitializeDefaultUnlockVariables();
                }
            }
            catch (Exception jsonEx)
            {
                Debug.LogError($"Parsing error: {jsonEx.Message}");
                InitializeDefaultUnlockVariables();
            }
        }
        else
        {
            Debug.Log("No unlock variables found in cloud, initializing defaults");
            InitializeDefaultUnlockVariables();
            await SaveUnlockVariables();
        }
    }
    catch (Exception e)
    {
        Debug.LogError($"Error loading unlock variables: {e.Message}");
        InitializeDefaultUnlockVariables();
    }
}

private IEnumerator AutoPressCategoryButtonWithDelay()
{
    // Ждём один кадр, чтобы UI успел инициализироваться
    yield return null;

{
    Debug.Log("Waiting for UI to initialize...");
    yield return new WaitForSeconds(2f); // Задержка в 2 секунды

    Debug.Log("Checking unlock variables...");
    if (unlockVariables.ContainsKey("w_dress") && unlockVariables["w_dress"] == "1")
    {
        Debug.Log("w_dress is 1, selecting Dress category...");
        int dressCategoryIndex = FindCategoryIndexByName("Dress");
        if (dressCategoryIndex != -1)
        {
            Debug.Log($"Auto-selecting Dress category at index {dressCategoryIndex}");
            SelectCategory(dressCategoryIndex);
        }
        else
        {
            Debug.LogError("Category 'Dress' not found!");
        }
    }
    else if (unlockVariables.ContainsKey("w_hair") && unlockVariables["w_hair"] == "1")
    {
        Debug.Log("w_hair is 1, selecting Hair category...");
        int hairCategoryIndex = FindCategoryIndexByName("Hair");
        if (hairCategoryIndex != -1)
        {
            Debug.Log($"Auto-selecting Hair category at index {hairCategoryIndex}");
            SelectCategory(hairCategoryIndex);
        }
        else
        {
            Debug.LogError("Category 'Hair' not found!");
        }
    }
    else
    {
        Debug.Log("No matching unlock variables found.");
    }
}


}



private void CheckAndHideCanvasObjects()
{
    Debug.Log("Unlock variables:");
    foreach (var kvp in unlockVariables)
    {
        Debug.Log($"{kvp.Key} = {kvp.Value}");
    }
    // Проверяем значения переменных
    bool shouldHideObjects = false;

    // Проверяем каждую переменную, если хотя бы одна равна "1", скрываем объекты
    if (unlockVariables.ContainsKey("w_dress") && unlockVariables["w_dress"] == "1" ||
        unlockVariables.ContainsKey("w_hair") && unlockVariables["w_hair"] == "1" ||
        unlockVariables.ContainsKey("w_makeup") && unlockVariables["w_makeup"] == "1" ||
        unlockVariables.ContainsKey("w_ukrashenie") && unlockVariables["w_ukrashenie"] == "1" ||
        unlockVariables.ContainsKey("w_accessorise") && unlockVariables["w_accessorise"] == "1")
    {
        shouldHideObjects = true;
        //AutoSelectCategoryWithDelay();
    }

    // Если нужно скрыть объекты
    if (shouldHideObjects)
    {
        HideCanvasObjects();
    }
    else
    {
        ShowCanvasObjects();
    }

    Debug.Log($"Canvas objects hidden: {shouldHideObjects}");
}






    public GameObject[] canvasObjects;

    // Метод для скрытия объектов
    private void HideCanvasObjects()
    {
        foreach (GameObject obj in canvasObjects)
        {
            if (obj != null)
            {
                obj.SetActive(false);
                Debug.Log($"Hidden: {obj.name}");
            }
        }
    }


    // Метод для показа объектов
    private void ShowCanvasObjects()
    {
        foreach (GameObject obj in canvasObjects)
        {
            if (obj != null)
            {
                obj.SetActive(true);
                Debug.Log($"Shown: {obj.name}");
            }
        }
    }


    private void InitializeDefaultUnlockVariables()
    {
        unlockVariables.Clear();
        for (int i = 1; i <= 40; i++)
        {
            unlockVariables[$"w{i}"] = "0";
        }
    }

    private async Task LoadWardrobeData()
    {
        if (!isInitialized)
        {
            Debug.LogError("Unity Services not initialized!");
            return;
        }

        try
        {
            Debug.Log("Starting to load wardrobe data...");
            var data = await CloudSaveService.Instance.Data.LoadAsync(new HashSet<string> { SAVE_KEY });
            
            if (data != null && data.TryGetValue(SAVE_KEY, out var savedData))
            {
                Debug.Log($"Raw saved data: {savedData?.ToString()}");
                
                if (!string.IsNullOrEmpty(savedData?.ToString()))
                {
                    var wardrobeData = JsonUtility.FromJson<WardrobeSaveData>(savedData.ToString());

                    if (wardrobeData?.categories != null)
                    {
                        var categoryDict = wardrobeData.ToDictionary();
                        Debug.Log($"Found {categoryDict.Count} saved categories");
                        
                        foreach (var category in categories)
                        {
                            if (categoryDict.TryGetValue(category.categoryName, out int spriteIndex))
                            {
                                Debug.Log($"Found saved index {spriteIndex} for category {category.categoryName}");
                                
                                if (spriteIndex >= 0 && spriteIndex < category.sprites.Length)
                                {
                                    category.currentSpriteIndex = spriteIndex;
                                    Debug.Log($"Successfully set index {spriteIndex} for category {category.categoryName}");
                                }
                                else
                                {
                                    Debug.LogError($"Invalid sprite index {spriteIndex} for category {category.categoryName}");
                                }
                            }
                            else
                            {
                                Debug.Log($"No saved data found for category {category.categoryName}");
                            }
                        }
                    }
                    else
                    {
                        Debug.LogError("Wardrobe data was null or invalid");
                    }
                }
                else
                {
                    Debug.Log("No saved data string found");
                }
            }
            else
            {
                Debug.Log("No saved data found in cloud");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error loading wardrobe data: {e.Message}\n{e.StackTrace}");
        }
    }


private async Task LoadFavWardrobeData()
{
    if (!isInitialized)
    {
        Debug.LogError("Unity Services not initialized!");
        return;
    }

    try
    {
        Debug.Log("Starting to load fav wardrobe data...");
        var data = await CloudSaveService.Instance.Data.LoadAsync(new HashSet<string> { FAV_SAVE_KEY });

        if (data != null && data.TryGetValue(FAV_SAVE_KEY, out var savedData))
        {
            Debug.Log($"Raw saved data: {savedData?.ToString()}");

            if (!string.IsNullOrEmpty(savedData?.ToString()))
            {
                var wardrobeData = JsonUtility.FromJson<FavWardrobeSaveData>(savedData.ToString());

                if (wardrobeData?.categories != null)
                {
                    var categoryDict = wardrobeData.ToDictionary();
                    Debug.Log($"Found {categoryDict.Count} saved fav categories");

                    foreach (var category in favCategories)
                    {
                        if (categoryDict.TryGetValue(category.categoryName, out int spriteIndex))
                        {
                            Debug.Log($"Found saved index {spriteIndex} for fav category {category.categoryName}");

                            if (spriteIndex >= 0 && spriteIndex < category.sprites.Length)
                            {
                                category.currentSpriteIndex = spriteIndex;
                                Debug.Log($"Successfully set index {spriteIndex} for fav category {category.categoryName}");
                            }
                            else
                            {
                                Debug.LogError($"Invalid sprite index {spriteIndex} for fav category {category.categoryName}");
                            }
                        }
                        else
                        {
                            Debug.Log($"No saved data found for fav category {category.categoryName}");
                        }
                    }
                }
                else
                {
                    Debug.LogError("Fav wardrobe data was null or invalid");
                }
            }
            else
            {
                Debug.Log("No saved data string found");
            }
        }
        else
        {
            Debug.Log("No saved data found in cloud");
        }
    }
    catch (Exception e)
    {
        Debug.LogError($"Error loading fav wardrobe data: {e.Message}\n{e.StackTrace}");
    }
}



    private async Task SaveUnlockVariables()
    {
        try
        {
            var unlockData = new UnlockVariables { variables = unlockVariables };
            var jsonData = JsonUtility.ToJson(unlockData);

            var data = new Dictionary<string, object>
            {
                { UNLOCK_KEY, jsonData }
            };

            await CloudSaveService.Instance.Data.ForceSaveAsync(data);
            Debug.Log("Unlock variables saved successfully");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error saving unlock variables: {e.Message}");
        }
    }

    private async void SaveWardrobeData()
    {
        if (!isInitialized)
        {
            Debug.LogError("Cannot save - services not initialized");
            return;
        }

        try
        {
            var saveData = new WardrobeSaveData();
            var dict = new Dictionary<string, int>();
            
            foreach (var category in categories)
            {
                dict[category.categoryName] = category.currentSpriteIndex;
                Debug.Log($"Preparing to save category {category.categoryName} with index {category.currentSpriteIndex}");
            }

            saveData.FromDictionary(dict);
            var jsonData = JsonUtility.ToJson(saveData);
            Debug.Log($"Saving JSON data: {jsonData}");

            var data = new Dictionary<string, object>
            {
                { SAVE_KEY, jsonData }
            };

            await CloudSaveService.Instance.Data.ForceSaveAsync(data);
            Debug.Log("Wardrobe data saved successfully");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error saving wardrobe data: {e.Message}\n{e.StackTrace}");
        }
    }


private async void SaveFavWardrobeData()
{
    if (!isInitialized)
    {
        Debug.LogError("Cannot save - services not initialized");
        return;
    }

    try
    {
        var saveData = new FavWardrobeSaveData();
        var dict = new Dictionary<string, int>();

        foreach (var category in favCategories)
        {
            dict[category.categoryName] = category.currentSpriteIndex;
            Debug.Log($"Preparing to save fav category {category.categoryName} with index {category.currentSpriteIndex}");
        }

        saveData.FromDictionary(dict);
        var jsonData = JsonUtility.ToJson(saveData);
        Debug.Log($"Saving JSON data: {jsonData}");

        var data = new Dictionary<string, object>
        {
            { FAV_SAVE_KEY, jsonData }
        };

        await CloudSaveService.Instance.Data.ForceSaveAsync(data);
        Debug.Log("Fav wardrobe data saved successfully");
    }
    catch (Exception e)
    {
        Debug.LogError($"Error saving fav wardrobe data: {e.Message}\n{e.StackTrace}");
    }
}



private void InitializeUI()
{
    if (leftButton != null)
    {
        leftButton.onClick.AddListener(() =>
        {
            if (currentCategory != null)
            {
                CycleItem(-1); // Переключение для главной героини
            }
            else if (currentFavCategory != null)
            {
                CycleFavItem(-1); // Переключение для второстепенного персонажа
            }
        });
    }
    else
    {
        Debug.LogError("LeftButton is not assigned.");
    }

    if (rightButton != null)
    {
        rightButton.onClick.AddListener(() =>
        {
            if (currentCategory != null)
            {
                CycleItem(1); // Переключение для главной героини
            }
            else if (currentFavCategory != null)
            {
                CycleFavItem(1); // Переключение для второстепенного персонажа
            }
        });
    }
    else
    {
        Debug.LogError("RightButton is not assigned.");
    }

    // Привязка кнопок для категорий главной героини
    if (categoryButtons != null)
    {
        for (int i = 0; i < categoryButtons.Length; i++)
        {
            if (categoryButtons[i] != null)
            {
                int index = i;
                categoryButtons[i].onClick.AddListener(() => SelectCategory(index));
            }
            else
            {
                Debug.LogError($"CategoryButton at index {i} is not assigned.");
            }
        }
    }
    else
    {
        Debug.LogError("CategoryButtons array is not assigned.");
    }

    // Привязка кнопок для категорий второстепенных персонажей
    if (favCategoryButtons != null)
    {
        for (int i = 0; i < favCategoryButtons.Length; i++)
        {
            if (favCategoryButtons[i] != null)
            {
                int index = i;
                favCategoryButtons[i].onClick.AddListener(() => SelectFavCategory(index));
            }
            else
            {
                Debug.LogError($"FavCategoryButton at index {i} is not assigned.");
            }
        }
    }
    else
    {
        Debug.LogError("FavCategoryButtons array is not assigned.");
    }
}

private void SelectCategory(int index)
{
    if (index < 0 || index >= categories.Length || isLoading)
    {
        Debug.LogError("Invalid category index or isLoading is true.");
        return;
    }

    // Сбрасываем текущую категорию второстепенного персонажа
    currentFavCategory = null;
    currentFavCategoryIndex = -1;

    // Скрываем текстовые поля второстепенных персонажей
    HideFavItemNameTexts();

    // Устанавливаем текущую категорию главной героини
    currentCategoryIndex = index;
    currentCategory = categories[index];
    if (currentCategory == null)
    {
        Debug.LogError("Current category is null.");
        return;
    }

    // Скрываем изображения второстепенных персонажей
    HideFavCharacterImages();

    // Показываем изображения главной героини
    ShowMainCharacterImages();

    // Обновляем отображение текущей категории
    UpdateCategoryDisplay(currentCategory);
    UpdatePriceDisplay(currentCategory); // Обновляем цену и кнопку "Купить"
}



private int FindCategoryIndexByName(string categoryName)
{
    if (categories == null || categories.Length == 0)
    {
        Debug.LogError("Categories array is null or empty!");
        return -1;
    }

    for (int i = 0; i < categories.Length; i++)
    {
        if (categories[i] != null && categories[i].categoryName == categoryName)
        {
            return i; // Возвращаем индекс категории
        }
    }

    Debug.LogError($"Category with name '{categoryName}' not found!");
    return -1; // Если категория не найдена
}





    private bool IsItemUnlocked(WardrobeSprite sprite)
    {
        if (sprite.unlockConditions == null || sprite.unlockConditions.Length == 0)
            return true;

        foreach (var condition in sprite.unlockConditions)
        {
            int value = GetUnlockValue(condition.variableName);
            if (value < condition.requiredValue)
            {
                Debug.Log($"Condition not met for {condition.variableName}: required {condition.requiredValue}, but got {value}");
                return false;
            }
            else
            {
                Debug.Log($"Condition met for {condition.variableName}: required {condition.requiredValue}, got {value}");
            }
        }
        return true;
    }

    private int GetUnlockValue(string variableName)
    {
        if (unlockVariables.TryGetValue(variableName, out string strValue))
        {
            if (int.TryParse(strValue, out int value))
            {
                return value;
            }
        }
        return 0;
    }

private void CycleItem(int direction)
{
    if (isLoading) return;

    // Если активна категория главной героини
    if (currentCategory != null)
    {
        int newIndex = currentCategory.currentSpriteIndex;
        int attempts = 0;
        int totalSprites = currentCategory.sprites.Length;

        do
        {
            newIndex = (newIndex + direction + totalSprites) % totalSprites;
            attempts++;

            if (attempts >= totalSprites)
            {
                newIndex = currentCategory.defaultSpriteIndex;
                break;
            }
        }
        while (!IsItemUnlocked(currentCategory.sprites[newIndex]));

        if (IsItemUnlocked(currentCategory.sprites[newIndex]))
        {
            currentCategory.currentSpriteIndex = newIndex;
            UpdateCategoryDisplay(currentCategory); // Обновляем только текущую категорию
            SaveWardrobeData();
        }
    }
    // Если активна категория второстепенного персонажа
    else if (currentFavCategory != null)
    {
        int newIndex = currentFavCategory.currentSpriteIndex;
        int attempts = 0;
        int totalSprites = currentFavCategory.sprites.Length;

        do
        {
            newIndex = (newIndex + direction + totalSprites) % totalSprites;
            attempts++;

            if (attempts >= totalSprites)
            {
                newIndex = currentFavCategory.defaultSpriteIndex;
                break;
            }
        }
        while (!IsItemUnlocked(currentFavCategory.sprites[newIndex]));

        if (IsItemUnlocked(currentFavCategory.sprites[newIndex]))
        {
            currentFavCategory.currentSpriteIndex = newIndex;
            UpdateFavCategoryDisplay(currentFavCategory); // Обновляем только текущую категорию
            SaveWardrobeData();
        }
    }
}

private void CycleFavItem(int direction)
{
    if (currentFavCategory == null || isLoading) return;

    int newIndex = currentFavCategory.currentSpriteIndex;
    int attempts = 0;
    int totalSprites = currentFavCategory.sprites.Length;

    do
    {
        newIndex = (newIndex + direction + totalSprites) % totalSprites;
        attempts++;

        if (attempts >= totalSprites)
        {
            newIndex = currentFavCategory.defaultSpriteIndex;
            break;
        }

        var currentSprite = currentFavCategory.sprites[newIndex];

        // Если элемент разблокирован и это не чёрный силуэт, выбираем его
        if (IsItemUnlocked(currentSprite))
        {
            // Если есть другие разблокированные элементы, пропускаем чёрный силуэт
            if (HasOtherUnlockedItems(currentFavCategory) && IsBlackSilhouette(currentSprite))
            {
                continue; // Пропускаем чёрный силуэт
            }

            // Если это допустимый элемент, выбираем его
            currentFavCategory.currentSpriteIndex = newIndex;
            UpdateFavCategoryDisplay(currentFavCategory); // Обновляем отображение
            SaveFavWardrobeData(); // Сохраняем данные на облаке
            return;
        }
    }
    while (true); // Выход из цикла через break
}



public void OnBuyButtonClick()
{
    if (currentCategory != null)
    {
        // Покупка для главной героини
        var currentSprite = categories[currentCategoryIndex].sprites[categories[currentCategoryIndex].currentSpriteIndex];

        if (currentSprite.isPaid)
        {
            currentSprite.SetPurchased();  // Сохраняем покупку в PlayerPrefs
            currentSprite.isPaid = false;  // Снимаем галочку
            buyButton.interactable = false;  // Отключаем кнопку "Купить"

            // Обновляем UI
            UpdatePriceDisplay(categories[currentCategoryIndex]);  // Обновляем отображение
            SaveWardrobeData();  // Сохраняем данные гардероба
        }
    }
    else if (currentFavCategory != null)
    {
        // Покупка для второстепенного персонажа
        //BuyCurrentFavItem();  // Вызываем метод для покупки нарядов второстепенных персонажей
    }
    else
    {
        Debug.LogError("No active category.");
    }
}



public void OnBuyFavButtonClick()
{
    if (currentFavCategory == null)
    {
        Debug.LogError("Current fav category is null.");
        return;
    }

    var currentSprite = currentFavCategory.sprites[currentFavCategory.currentSpriteIndex];
    if (currentSprite == null)
    {
        Debug.LogError("Current sprite is null.");
        return;
    }

    if (currentSprite.isPaid)
    {
        // Снимаем галочку, спрайт куплен
        currentSprite.SetPurchased();  // Сохраняем покупку в PlayerPrefs
        currentSprite.isPaid = false;  // Снимаем галочку
        buyButton.interactable = false;  // Отключаем кнопку "Купить"

        // Обновляем UI
        UpdatePriceDisplayFav(currentFavCategory);  // Обновляем отображение
        SaveFavWardrobeData();  // Сохраняем данные гардероба
    }
}


private void UpdateAllCategoriesDisplay()
{
    foreach (var category in categories)
    {
        if (category == currentCategory) // Обновляем только текущую категорию
        {
            UpdateCategoryDisplay(category);
        }
    }
}

// Ссылки на объекты TextMeshPro в иерархии сцены
public GameObject textMainType;
public GameObject textMainHair;
public GameObject textMainDress;

private void UpdateCategoryDisplay(WardrobeCategory category)
{
    if (category.sprites.Length > 0)
    {
        var currentSprite = category.sprites[category.currentSpriteIndex];
        bool isUnlocked = IsItemUnlocked(currentSprite);

        if (!isUnlocked)
        {
            category.currentSpriteIndex = category.defaultSpriteIndex;
            currentSprite = category.sprites[category.defaultSpriteIndex];
        }

        SetCurrentSprite(category, currentSprite, isUnlocked);
        UpdateTextVisibility(category.categoryName); // Включаем/отключаем нужные объекты
    }
}

// Метод для включения/отключения объектов TextMeshPro
private void UpdateTextVisibility(string categoryName)
{
    if (textMainType != null) textMainType.SetActive(categoryName == "Type");
    if (textMainHair != null) textMainHair.SetActive(categoryName == "Hair");
    if (textMainDress != null) textMainDress.SetActive(categoryName == "Dress");
}




private void HideAllDisplayImages()
{
    foreach (var category in categories)
    {
        if (category.displayImage != null)
        {
            category.displayImage.enabled = false; // Скрываем DisplayImage
        }
        if (category.displayImageAll != null)
        {
            category.displayImageAll.enabled = false; // Скрываем DisplayImageAll (если нужно)
        }
    }
}


// // Метод, который обновляет и спрайт, и текст одновременно
private void SetCurrentSprite(WardrobeCategory category, WardrobeSprite sprite, bool isUnlocked)
{
    if (category == null || sprite == null)
    {
        Debug.LogError("Category or sprite is null.");
        return;
    }

    // Если это элемент "Взять всё"
    if (sprite.isSelectAll)
    {
        // Скрываем все DisplayImage
        HideAllDisplayImages();

        // Показываем DisplayImageAll текущей категории
        if (category.displayImageAll != null)
        {
            category.displayImageAll.sprite = sprite.sprite;
            category.displayImageAll.enabled = true;
        }
    }
    else
    {
        // Восстанавливаем DisplayImage всех категорий
        foreach (var cat in categories)
        {
            if (cat.displayImage != null)
            {
                cat.displayImage.enabled = true;
            }
            if (cat.displayImageAll != null)
            {
                cat.displayImageAll.enabled = false; // Скрываем DisplayImageAll
            }
        }

        // Отображаем текущий спрайт
        if (category.displayImage != null)
        {
            category.displayImage.sprite = sprite.sprite;
            category.displayImage.enabled = true;
        }
    }

    // Обновляем текстовое поле с названием спрайта
    if (category.itemNameText != null)
    {
        var localizedString = new LocalizedString("WardrobeStrings", sprite.localizationKey);
        localizedString.StringChanged += (localizedText) =>
        {
            string displayText = localizedText;
            if (!isUnlocked)
            {
                displayText += " <color=#FF4444></color>";
            }
            category.itemNameText.text = displayText;
            category.itemNameText.enabled = true;
        };
    }

    // Обновляем overlay блокировки
    if (lockedOverlay != null)
    {
        lockedOverlay.SetActive(!isUnlocked);
    }

    // Обновляем отображение цены и кнопки "Купить"
    UpdatePriceDisplay(category);
    UpdateLockMessage(sprite);
}


private void UpdatePriceDisplay(WardrobeCategory category)
{
    if (category == null)
    {
        Debug.LogError("Category is null.");
        return;
    }

    if (category.sprites == null || category.sprites.Length == 0)
    {
        Debug.LogError("No sprites in category.");
        return;
    }

    var currentSprite = category.sprites[category.currentSpriteIndex];
    if (currentSprite == null)
    {
        Debug.LogError("Current sprite is null.");
        return;
    }

    if (priceText == null)
    {
        Debug.LogError("PriceText is not assigned.");
        return;
    }

    if (buyButton == null)
    {
        Debug.LogError("BuyButton is not assigned.");
        return;
    }

    Debug.Log($"Updating price display for sprite: {currentSprite.localizationKey}, isPaid: {currentSprite.isPaid}, isPurchased: {currentSprite.IsPurchased()}");

    // Получаем количество рубинов
    int playerRubies = 100; // Значение по умолчанию
    if (GameCloud.Instance != null)
    {
        playerRubies = GameCloud.Instance.GetCurrencyAmount("PLAYER_RUBY");
    }
    else
    {
        Debug.LogWarning("GameCloud.Instance is null. Using default rubies value: 100.");
    }

    // Если спрайт платный
    if (currentSprite.isPaid)
    {
        // Если спрайт уже куплен, скрываем цену и кнопку "Купить"
        if (currentSprite.IsPurchased())
        {
            Debug.Log("Sprite is already purchased.");
            priceText.text = string.Empty;  // Скрываем цену
            priceText.enabled = false;  // Отключаем отображение цены
            buyButton.interactable = false;  // Отключаем кнопку "Купить"
            buyButton.gameObject.SetActive(false); // Скрываем кнопку "Купить"
        }
        else
        {
            // Отображаем цену и проверяем, достаточно ли рубинов
            Debug.Log($"Player rubies: {playerRubies}, required: {currentSprite.price}");
            priceText.text = $"{currentSprite.price}";  
            priceText.enabled = true;  // Включаем отображение цены

            // Кнопка всегда активна, но при недостатке рубинов не выполняет действий
            buyButton.interactable = true;
            buyButton.gameObject.SetActive(true); // Показываем кнопку "Купить"
        }
    }
    else
    {
        Debug.Log("Sprite is not paid.");
        priceText.text = string.Empty;  // Скрываем цену, если спрайт бесплатный
        priceText.enabled = false;  // Скрываем текст
        buyButton.interactable = false;  // Деактивируем кнопку "Купить"
        buyButton.gameObject.SetActive(false); // Скрываем кнопку "Купить"
    }
}


private void InitializeWardrobeState()
{
    foreach (var category in categories)
    {
        foreach (var sprite in category.sprites)
        {
            // Проверяем, был ли куплен спрайт через PlayerPrefs
            if (sprite.IsPurchased())
            {
                sprite.isPaid = false;  // Если куплен, снимаем галочку
            }
        }
    }
    

    UpdateAllCategoriesDisplay();  // Обновляем отображение всех категорий
}



private bool ShouldHideSelectAll(WardrobeCategory category, WardrobeSprite selectAllSprite)
{
    if (category == null || selectAllSprite == null)
    {
        Debug.LogError("Category or selectAllSprite is null.");
        return false;
    }

    // Получаем значение w1 из UnlockConditions элемента "Взять всё"
    string targetW1 = selectAllSprite.unlockConditions
        .FirstOrDefault(condition => condition.variableName == "w1")?.requiredValue.ToString();

    if (string.IsNullOrEmpty(targetW1))
    {
        Debug.LogError("No w1 condition found in selectAllSprite.");
        return false;
    }

    // Проверяем, куплены ли все платные элементы с таким же значением w1
    foreach (var sprite in category.sprites)
    {
        if (sprite.isPaid && !sprite.isSelectAll) // Игнорируем сам элемент "Взять всё"
        {
            var spriteW1 = sprite.unlockConditions
                .FirstOrDefault(condition => condition.variableName == "w1")?.requiredValue.ToString();

            if (spriteW1 == targetW1 && !sprite.IsPurchased())
            {
                return false; // Нашли некупленный элемент, не скрываем "Взять всё"
            }
        }
    }

    return true; // Все элементы куплены, скрываем "Взять всё"
}




private void RemoveSelectAllItem(WardrobeCategory category)
{
    if (category == null)
    {
        Debug.LogError("Category is null.");
        return;
    }

    // Убираем элемент "Выбрать всё" из списка спрайтов
    var newSprites = category.sprites.Where(sprite => !sprite.isSelectAll).ToArray();
    category.sprites = newSprites;

    // Корректируем индекс, если он вышел за пределы массива
    if (category.currentSpriteIndex >= newSprites.Length)
    {
        category.currentSpriteIndex = newSprites.Length - 1; // Переключаемся на последний элемент
    }

    // Обновляем отображение
    UpdateCategoryDisplay(category);
}

public async void BuyCurrentItem()
{
    if (currentCategory == null)
    {
        BuyCurrentFavItem();
        //Debug.LogError("Current category is nullaaa.");
        return;
    }

    var currentSprite = currentCategory.sprites[currentCategory.currentSpriteIndex];
    if (currentSprite == null)
    {
        Debug.LogError("Current sprite is null.");
        return;
    }

    // Проверяем, является ли наряд платным и не куплен ли он уже
    if (currentSprite.isPaid && !currentSprite.IsPurchased())
    {
        int playerRubies = 100; // Значение по умолчанию
        if (GameCloud.Instance != null)
        {
            playerRubies = GameCloud.Instance.GetCurrencyAmount("PLAYER_RUBY");
        }
        else
        {
            Debug.LogWarning("GameCloud.Instance is null. Using default rubies value: 100.");
        }

        // Проверяем, достаточно ли рубинов для покупки
        if (playerRubies >= currentSprite.price)
        {
            // Вычитаем рубины
            if (GameCloud.Instance != null)
            {
                GameCloud.Instance.UpdateResource("PLAYER_RUBY", -currentSprite.price);
            }
            else
            {
                Debug.LogWarning("GameCloud.Instance is null. Skipping rubies update.");
            }

            // Сохраняем состояние покупки
            currentSprite.SetPurchased();
            currentSprite.isPaid = false; // Снимаем галочку "платный"

            // Если это элемент "Выбрать всё", покупаем все подходящие наряды
            List<string> boughtIndexes = new List<string>();
            if (currentSprite.isSelectAll)
            {
                boughtIndexes = BuyAllMatchingItems(currentCategory, currentSprite);

                // Удаляем элемент "Выбрать всё" из списка
                RemoveSelectAllItem(currentCategory);

                // Переключаемся на предыдущий элемент
                if (currentCategory.sprites.Length > 0)
                {
                    currentCategory.currentSpriteIndex = Mathf.Clamp(
                        currentCategory.currentSpriteIndex - 1, 
                        0, 
                        currentCategory.sprites.Length - 1
                    );
                    UpdateCategoryDisplay(currentCategory); // Обновляем отображение
                }
            }
            else
            {
                // Если это не "Выбрать всё", добавляем текущий buyIndex в список
                boughtIndexes.Add(currentSprite.buyIndex);
            }

            // Сохраняем покупку на облаке
            await SavePurchaseToCloud(boughtIndexes); // Передаём список buyIndexes

            // Обновляем UI
            UpdatePriceDisplay(currentCategory);
            UpdateCategoryDisplay(currentCategory); // Обновляем отображение текущей категории

            Debug.Log($"Item {currentSprite.localizationKey} purchased!");
        }
        else
        {
            Debug.Log("Not enough rubies to buy this item.");
        }
    }
}

private void HideSelectAllItem(WardrobeCategory category)
{
    if (category == null)
    {
        Debug.LogError("Category is null.");
        return;
    }

    // Убираем элемент "Взять всё" из списка спрайтов
    category.sprites = category.sprites.Where(sprite => !sprite.isSelectAll).ToArray();

    // Обновляем отображение
    UpdateCategoryDisplay(category);
}

private List<string> BuyAllMatchingItems(WardrobeCategory category, WardrobeSprite selectAllSprite)
{
    var boughtIndexes = new List<string>();

    if (category == null || selectAllSprite == null)
    {
        Debug.LogError("Category or selectAllSprite is null.");
        return boughtIndexes;
    }

    // Получаем значение w1 из UnlockConditions элемента "Взять всё"
    string targetW1 = selectAllSprite.unlockConditions
        .FirstOrDefault(condition => condition.variableName == "w1")?.requiredValue.ToString();

    if (string.IsNullOrEmpty(targetW1))
    {
        Debug.LogError("No w1 condition found in selectAllSprite.");
        return boughtIndexes;
    }

    // Покупаем все наряды с таким же значением w1
    foreach (var sprite in category.sprites)
    {
        if (sprite.isPaid && !sprite.IsPurchased())
        {
            var spriteW1 = sprite.unlockConditions
                .FirstOrDefault(condition => condition.variableName == "w1")?.requiredValue.ToString();

            if (spriteW1 == targetW1)
            {
                sprite.SetPurchased();
                sprite.isPaid = false; // Снимаем галочку "платный"
                if (!string.IsNullOrEmpty(sprite.buyIndex))
                {
                    boughtIndexes.Add(sprite.buyIndex); // Добавляем Buy Index в список
                }
                Debug.Log($"Item {sprite.localizationKey} purchased as part of 'Select All'.");
            }
        }
    }

    return boughtIndexes;
}



    private void UpdateLockMessage(WardrobeSprite sprite)
    {
        if (lockMessageText != null)
        {
            if (!IsItemUnlocked(sprite) && sprite.unlockConditions != null && sprite.unlockConditions.Length > 0)
            {
                string conditions = string.Join(", ", sprite.unlockConditions.Select(c => 
                {
                    int currentValue = GetUnlockValue(c.variableName);
                    return $"{c.variableName} ({currentValue}/{c.requiredValue})";
                }));
                lockMessageText.text = $"Unlock requires: {conditions}";
                lockMessageText.gameObject.SetActive(true);
            }
            else
            {
                lockMessageText.gameObject.SetActive(false);
            }
        }
    }


    private void SetDefaultSprites()
    {
        foreach (var category in categories)
        {
            category.currentSpriteIndex = category.defaultSpriteIndex;
        }
    }

    public async void UpdateUnlockVariable(string variableName, int value)
    {
        string stringValue = value.ToString();
        if (unlockVariables.ContainsKey(variableName))
        {
            unlockVariables[variableName] = stringValue;
            await SaveUnlockVariables();
            UpdateAllCategoriesDisplay();
        }
    }


private const string BUY_CLOTHES_KEY = "BUY_CLOTHES";
private async Task SavePurchaseToCloud(List<string> buyIndexes)
{
    try
    {
        // Загружаем текущие данные из облака
        var data = await CloudSaveService.Instance.Data.LoadAsync(new HashSet<string> { BUY_CLOTHES_KEY });

        // Создаём или обновляем словарь с покупками
        Dictionary<string, int> buyClothesData = new Dictionary<string, int>();
        if (data.TryGetValue(BUY_CLOTHES_KEY, out var savedData))
        {
            // Если данные уже есть, десериализуем их
            buyClothesData = JsonConvert.DeserializeObject<Dictionary<string, int>>(savedData.ToString());
        }

        // Добавляем текущие покупки
        foreach (var buyIndex in buyIndexes)
        {
            buyClothesData[buyIndex] = 1; // 1 означает, что наряд куплен
        }

        // Сохраняем обновлённые данные в облако
        var updatedData = new Dictionary<string, object>
        {
            { BUY_CLOTHES_KEY, JsonConvert.SerializeObject(buyClothesData) }
        };

        await CloudSaveService.Instance.Data.ForceSaveAsync(updatedData);
        Debug.Log($"Purchases saved to cloud: {string.Join(", ", buyIndexes)}");
    }
    catch (Exception e)
    {
        Debug.LogError($"Error saving purchase to cloud: {e.Message}");
    }
}

private async Task LoadPurchasesFromCloud()
{
    try
    {
        // Загружаем данные из облака
        var data = await CloudSaveService.Instance.Data.LoadAsync(new HashSet<string> { BUY_CLOTHES_KEY });

        if (data.TryGetValue(BUY_CLOTHES_KEY, out var savedData))
        {
            // Десериализуем данные
            var buyClothesData = JsonConvert.DeserializeObject<Dictionary<string, int>>(savedData.ToString());

            // Обновляем состояние покупок в гардеробе
            foreach (var category in categories)
            {
                foreach (var sprite in category.sprites)
                {
                    if (sprite.isPaid && buyClothesData.ContainsKey(sprite.buyIndex))
                    {
                        sprite.SetPurchased();
                        sprite.isPaid = false; // Снимаем галочку "платный"
                    }
                }

                // Проверяем, нужно ли скрыть элемент "Выбрать всё"
                var selectAllSprite = category.sprites.FirstOrDefault(sprite => sprite.isSelectAll);
                if (selectAllSprite != null && ShouldHideSelectAll(category, selectAllSprite))
                {
                    HideSelectAllItem(category);
                }
            }

            Debug.Log("Purchases loaded from cloud.");
        }
        else
        {
            Debug.Log("No purchase data found in cloud.");
        }
    }
    catch (Exception e)
    {
        Debug.LogError($"Error loading purchases from cloud: {e.Message}");
    }
}




private void HideMainCharacterImages()
{
    foreach (var category in categories)
    {
        if (category.displayImage != null)
        {
            category.displayImage.enabled = false;
        }
        if (category.displayImageAll != null)
        {
            category.displayImageAll.enabled = false;
        }
    }
}

private void HideFavCharacterImages()
{
    foreach (var favCategory in favCategories)
    {
        if (favCategory.displayImageFav != null)
        {
            favCategory.displayImageFav.enabled = false;
        }
    }
}

private void ShowCurrentFavImage()
{
    if (currentFavCategory != null && currentFavCategory.displayImageFav != null)
    {
        currentFavCategory.displayImageFav.enabled = true;
    }
}

private void SelectFavCategory(int index)
{
    if (index < 0 || index >= favCategories.Length || isLoading)
    {
        Debug.LogError("Invalid fav category index or isLoading is true.");
        return;
    }

    // Сбрасываем текущую категорию главной героини
    currentCategory = null;
    currentCategoryIndex = -1;

    // Скрываем текстовые поля главной героини
    HideMainItemNameTexts();

    // Устанавливаем текущую категорию второстепенного персонажа
    currentFavCategoryIndex = index;
    currentFavCategory = favCategories[index];
    if (currentFavCategory == null)
    {
        Debug.LogError("Current fav category is null.");
        return;
    }

    // Скрываем изображения главной героини
    HideMainCharacterImages();

    // Показываем текущий DisplayImageFav
    ShowCurrentFavImage();

    // Обновляем отображение текущей категории
    UpdateFavCategoryDisplay(currentFavCategory);
    UpdatePriceDisplayFav(currentFavCategory); // Обновляем цену и кнопку "Купить"
}

private void UpdateFavCategoryDisplay(WardrobeFavCategory category)
{
    if (category.sprites.Length > 0)
    {
        var currentSprite = category.sprites[category.currentSpriteIndex];
        bool isUnlocked = IsItemUnlocked(currentSprite);

        if (!isUnlocked)
        {
            category.currentSpriteIndex = category.defaultSpriteIndex;
            currentSprite = category.sprites[category.defaultSpriteIndex];
        }

        // Устанавливаем спрайт и текст
        if (category.displayImageFav != null)
        {
            category.displayImageFav.sprite = currentSprite.sprite;
            category.displayImageFav.enabled = true;
        }

        if (category.itemNameText != null)
        {
            var localizedString = new LocalizedString("WardrobeStrings", currentSprite.localizationKey);
            localizedString.StringChanged += (localizedText) =>
            {
                string displayText = localizedText;
                if (!isUnlocked)
                {
                    displayText += " <color=#FF4444></color>";
                }
                category.itemNameText.text = displayText;
                category.itemNameText.enabled = true;
            };
        }

        // Обновляем overlay блокировки
        if (lockedOverlay != null)
        {
            lockedOverlay.SetActive(!isUnlocked);
        }

        // Обновляем отображение цены и кнопки "Купить"
        UpdatePriceDisplayFav(category);
        UpdateLockMessage(currentSprite);
    }
}

public async void BuyCurrentFavItem()
{
    if (currentFavCategory == null)
    {
        Debug.LogError("Current fav category is null.");
        return;
    }

    var currentSprite = currentFavCategory.sprites[currentFavCategory.currentSpriteIndex];
    if (currentSprite == null)
    {
        Debug.LogError("Current sprite is null.");
        return;
    }

    // Проверяем, является ли наряд платным и не куплен ли он уже
    if (currentSprite.isPaid && !currentSprite.IsPurchased())
    {
        int playerRubies = 0;

        // Проверяем, авторизован ли пользователь
        if (AuthenticationService.Instance.IsSignedIn)
        {
            // Используем рубины из GameCloud
            if (GameCloud.Instance != null)
            {
                playerRubies = GameCloud.Instance.GetCurrencyAmount("PLAYER_RUBY");
            }
            else
            {
                Debug.LogWarning("GameCloud.Instance is null. Using default rubies value: 100.");
                playerRubies = 100; // Значение по умолчанию
            }
        }
        else
        {
            // Используем временную валюту для неавторизованных пользователей
            playerRubies = playerCoins;
        }

        // Проверяем, достаточно ли рубинов для покупки
        if (playerRubies >= currentSprite.price)
        {
            // Вычитаем рубины
            if (AuthenticationService.Instance.IsSignedIn)
            {
                // Используем GameCloud для авторизованных пользователей
                if (GameCloud.Instance != null)
                {
                    GameCloud.Instance.UpdateResource("PLAYER_RUBY", -currentSprite.price);
                }
                else
                {
                    Debug.LogWarning("GameCloud.Instance is null. Skipping rubies update.");
                }
            }
            else
            {
                // Используем временную валюту для неавторизованных пользователей
                playerCoins -= currentSprite.price;
                Debug.Log($"Temporary rubies updated: {playerCoins}");
            }

            // Сохраняем состояние покупки
            currentSprite.SetPurchased();
            currentSprite.isPaid = false; // Снимаем галочку "платный"

            // Сохраняем покупку на облаке
            await SaveFavPurchaseToCloud(currentSprite.buyIndex);

            // Обновляем UI
            UpdatePriceDisplayFav(currentFavCategory);
            UpdateFavCategoryDisplay(currentFavCategory); // Обновляем отображение текущей категории

            Debug.Log($"Item {currentSprite.localizationKey} purchased!");
        }
        else
        {
            Debug.Log("Not enough rubies to buy this item.");
        }
    }
}

private async Task SaveFavPurchaseToCloud(string buyIndex)
{
    try
    {
        // Загружаем текущие данные из облака
        var data = await CloudSaveService.Instance.Data.LoadAsync(new HashSet<string> { FAV_BUY_KEY });

        // Создаём или обновляем словарь с покупками
        Dictionary<string, int> buyFavData = new Dictionary<string, int>();
        if (data.TryGetValue(FAV_BUY_KEY, out var savedData))
        {
            // Если данные уже есть, десериализуем их
            buyFavData = JsonConvert.DeserializeObject<Dictionary<string, int>>(savedData.ToString());
        }

        // Добавляем текущую покупку
        buyFavData[buyIndex] = 1; // 1 означает, что наряд куплен

        // Сохраняем обновлённые данные в облако
        var updatedData = new Dictionary<string, object>
        {
            { FAV_BUY_KEY, JsonConvert.SerializeObject(buyFavData) }
        };

        await CloudSaveService.Instance.Data.ForceSaveAsync(updatedData);
        Debug.Log($"Fav purchase saved to cloud: {buyIndex}");
    }
    catch (Exception e)
    {
        Debug.LogError($"Error saving fav purchase to cloud: {e.Message}");
    }
}

private async Task LoadFavPurchasesFromCloud()
{
    try
    {
        // Загружаем данные из облака
        var data = await CloudSaveService.Instance.Data.LoadAsync(new HashSet<string> { FAV_BUY_KEY });

        if (data.TryGetValue(FAV_BUY_KEY, out var savedData))
        {
            // Десериализуем данные
            var buyFavData = JsonConvert.DeserializeObject<Dictionary<string, int>>(savedData.ToString());

            // Обновляем состояние покупок в гардеробе
            foreach (var category in favCategories)
            {
                foreach (var sprite in category.sprites)
                {
                    if (sprite.isPaid && buyFavData.ContainsKey(sprite.buyIndex))
                    {
                        sprite.SetPurchased();
                        sprite.isPaid = false; // Снимаем галочку "платный"
                    }
                }
            }

            Debug.Log("Fav purchases loaded from cloud.");
        }
        else
        {
            Debug.Log("No fav purchase data found in cloud.");
        }
    }
    catch (Exception e)
    {
        Debug.LogError($"Error loading fav purchases from cloud: {e.Message}");
    }
}



private void UpdatePriceDisplayFav(WardrobeFavCategory category)
{
    if (category == null)
    {
        Debug.LogError("Category is null.");
        return;
    }

    if (category.sprites == null || category.sprites.Length == 0)
    {
        Debug.LogError("No sprites in category.");
        return;
    }

    var currentSprite = category.sprites[category.currentSpriteIndex];
    if (currentSprite == null)
    {
        Debug.LogError("Current sprite is null.");
        return;
    }

    if (priceText == null)
    {
        Debug.LogError("PriceText is not assigned.");
        return;
    }

    if (buyButton == null)
    {
        Debug.LogError("BuyButton is not assigned.");
        return;
    }

    Debug.Log($"Updating price display for sprite: {currentSprite.localizationKey}, isPaid: {currentSprite.isPaid}, isPurchased: {currentSprite.IsPurchased()}");

    // Получаем количество рубинов
    int playerRubies = 100; // Значение по умолчанию
    if (GameCloud.Instance != null)
    {
        playerRubies = GameCloud.Instance.GetCurrencyAmount("PLAYER_RUBY");
    }
    else
    {
        Debug.LogWarning("GameCloud.Instance is null. Using default rubies value: 100.");
    }

    // Если спрайт платный
    if (currentSprite.isPaid)
    {
        // Если спрайт уже куплен, скрываем цену и кнопку "Купить"
        if (currentSprite.IsPurchased())
        {
            Debug.Log("Sprite is already purchased.");
            priceText.text = string.Empty;  // Скрываем цену
            priceText.enabled = false;  // Отключаем отображение цены
            buyButton.interactable = false;  // Отключаем кнопку "Купить"
            buyButton.gameObject.SetActive(false); // Скрываем кнопку "Купить"
        }
        else
        {
            // Отображаем цену и проверяем, достаточно ли рубинов
            Debug.Log($"Player rubies: {playerRubies}, required: {currentSprite.price}");
            priceText.text = $"{currentSprite.price}";  
            priceText.enabled = true;  // Включаем отображение цены

            // Кнопка всегда активна, но при недостатке рубинов не выполняет действий
            buyButton.interactable = true;
            buyButton.gameObject.SetActive(true); // Показываем кнопку "Купить"
        }
    }
    else
    {
        Debug.Log("Sprite is not paid.");
        priceText.text = string.Empty;  // Скрываем цену, если спрайт бесплатный
        priceText.enabled = false;  // Скрываем текст
        buyButton.interactable = false;  // Деактивируем кнопку "Купить"
        buyButton.gameObject.SetActive(false); // Скрываем кнопку "Купить"
    }
}

private void ShowMainCharacterImages()
{
    foreach (var category in categories)
    {
        if (category.displayImage != null)
        {
            category.displayImage.enabled = true;
        }
        if (category.displayImageAll != null)
        {
            category.displayImageAll.enabled = true;
        }
    }
}

private void HideFavItemNameTexts()
{
    foreach (var favCategory in favCategories)
    {
        if (favCategory.itemNameText != null)
        {
            favCategory.itemNameText.enabled = false; // Скрываем текстовое поле
        }
    }
}

private void HideMainItemNameTexts()
{
    foreach (var category in categories)
    {
        if (category.itemNameText != null)
        {
            category.itemNameText.enabled = false; // Скрываем текстовое поле
        }
    }


    // Дополнительно скрываем текстовые элементы, если они заданы отдельно
    if (textMainType != null) textMainType.SetActive(false);
    if (textMainHair != null) textMainHair.SetActive(false);
    if (textMainDress != null) textMainDress.SetActive(false);
}


private bool HasOtherUnlockedItems(WardrobeFavCategory category)
{
    foreach (var sprite in category.sprites)
    {
        // Проверяем, разблокирован ли элемент и не является ли он чёрным силуэтом
        if (IsItemUnlocked(sprite) && !IsBlackSilhouette(sprite))
        {
            return true; // Нашли другой разблокированный элемент
        }
    }
    return false; // Других разблокированных элементов нет
}

private bool IsBlackSilhouette(WardrobeSprite sprite)
{
    // Проверяем, является ли элемент чёрным силуэтом (f1=0, f2=0, и т.д.)
    foreach (var condition in sprite.unlockConditions)
    {
        if (condition.variableName.StartsWith("f") && condition.requiredValue == 0)
        {
            return true;
        }
    }
    return false;
}

}