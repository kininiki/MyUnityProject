using UnityEngine;
using UnityEngine.UI;
using Fungus;
using System.Collections;
using TMPro;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.ResourceManagement.AsyncOperations;
using Unity.Services.CloudSave;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine.SceneManagement;

[CommandInfo("Character",
    "Hair Selection",
    "Displays hair selection UI with prices, buy options and navigation")]
public class HairSelectionCommand : Command
{
    [Header("Hair Settings")]
    [SerializeField] private int[] hairTypeIndexes;
    [SerializeField] private string[] buyKeys;
    [SerializeField] private int[] hairPrices;
    [SerializeField] private int buyAllPrice = 1000;
    [SerializeField] private int buyAllIndex = -1;
    [SerializeField] private bool buyAllEnabled = true;

    [Header("UI References")]
    [SerializeField] private GameObject wardrobePanel;
    [SerializeField] private GameObject panelToHide;
    [SerializeField] private Button leftArrowButton;
    [SerializeField] private Button rightArrowButton;
    [SerializeField] private Button selectButton;
    [SerializeField] private Button buttonDown;
    [SerializeField] private Button buttonUp;
    [SerializeField] private Button buyButton;
    [SerializeField] private TextMeshProUGUI hairDescriptionText;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private LocalizedString[] hairTypeDescriptions;
    [SerializeField] private RawImage buyAllPreviewImage;

    [Header("Character Parts")]
    [SerializeField] private RawImage[] characterParts = new RawImage[6]; // Массив из 6 RawImage

    [Header("Fade Settings")]
    [SerializeField] private float fadeDuration = 0.3f;
    [SerializeField] private float visibleAlpha = 1f;
    [SerializeField] private float hiddenAlpha = 0f;

    [Header("Configuration")]
    [SerializeField] private int positionIndex;
    [SerializeField] private float slideDuration = 0.5f;
    [SerializeField] private float slideDistance = 200f;

    private int currentIndex = 0;
    private SimpleCharacterManager characterManager;
    private Flowchart flowchart;
    private Vector2 originalPosition;
    private bool isSliding = false;
    private bool isBuyAllPage = false;
    private Dictionary<string, string> unlockVariables = new Dictionary<string, string>();
    private Dictionary<string, int> playerCurrency = new Dictionary<string, int>();
    private const string UNLOCK_KEY = "LANA_UNLOCK_VARIABLES";
    private const string CURRENCY_KEY = "PLAYER_RUBY";

    public override void OnEnter()
    {
        characterManager = FindObjectOfType<SimpleCharacterManager>();
        flowchart = GetFlowchart();

        if (characterManager == null || flowchart == null)
        {
            Debug.LogError("Required components not found!");
            Continue();
            return;
        }

        wardrobePanel.SetActive(true);
        originalPosition = panelToHide.GetComponent<RectTransform>().anchoredPosition;
        
        if (buyAllPreviewImage != null)
        {
            buyAllPreviewImage.gameObject.SetActive(false);
        }

        SetCharacterAlpha(visibleAlpha);

        if (!buyAllEnabled && buyAllIndex == currentIndex)
        {
            currentIndex = (currentIndex + 1) % hairTypeIndexes.Length;
        }
        
        StartCoroutine(InitializeAndLoadData());
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

    private IEnumerator FadeCharacter(float targetAlpha)
    {
        if (characterParts == null || characterParts.Length == 0 || characterParts[0] == null) 
            yield break;

        float startAlpha = characterParts[0].color.a;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            float currentAlpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);
            SetCharacterAlpha(currentAlpha);
            elapsed += Time.deltaTime;
            yield return null;
        }

        SetCharacterAlpha(targetAlpha);
    }

    private void SetHair(int index)
    {
        if (index < 0 || index >= hairTypeIndexes.Length) return;

        isBuyAllPage = buyAllEnabled && (index == buyAllIndex);
        selectButton.gameObject.SetActive(!isBuyAllPage);

        if (!isBuyAllPage)
        {
            if (buyAllPreviewImage != null)
            {
                buyAllPreviewImage.gameObject.SetActive(false);
            }

            StartCoroutine(FadeCharacter(visibleAlpha));

            int hairTypeIndex = hairTypeIndexes[index];
       
            characterManager.SetCharacterSprite(
                positionIndex,
                flowchart.GetVariable<IntegerVariable>("Type").Value,
                CharacterEmotion.Нейтральное,
                hairTypeIndex,
                flowchart.GetVariable<IntegerVariable>("Makeup").Value,
                flowchart.GetVariable<IntegerVariable>("Dress").Value,
                flowchart.GetVariable<IntegerVariable>("Ukrashenie").Value,
                flowchart.GetVariable<IntegerVariable>("Accessorise").Value
            );

            UpdateHairDescription(index);
            UpdatePriceDisplay(index);
        }
        else
        {
            if (buyAllPreviewImage != null)
            {
                buyAllPreviewImage.gameObject.SetActive(true);
            }

            StartCoroutine(FadeCharacter(hiddenAlpha));

            UpdateHairDescriptionForBuyAll();
            UpdateBuyAllPriceDisplay();
        }
    }

    private IEnumerator InitializeAndLoadData()
    {
        yield return InitializeUnityServices();
        
        var loadOperation = CloudSaveService.Instance.Data.LoadAsync(new HashSet<string> { UNLOCK_KEY, CURRENCY_KEY });
        yield return new WaitUntil(() => loadOperation.IsCompleted);

        if (loadOperation.IsCompleted && !loadOperation.IsFaulted)
        {
            if (loadOperation.Result.TryGetValue(UNLOCK_KEY, out var savedData))
            {
                try
                {
                    string jsonString = savedData.ToString();
                    unlockVariables = IsValidJson(jsonString) 
                        ? JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString)
                        : InitializeDefaultUnlockVariables();
                }
                catch
                {
                    unlockVariables = InitializeDefaultUnlockVariables();
                }
                
                if (unlockVariables.Count == 0)
                {
                    InitializeDefaultUnlockVariables();
                    yield return SaveUnlockVariables();
                }
            }
            else
            {
                InitializeDefaultUnlockVariables();
                yield return SaveUnlockVariables();
            }

            Dictionary<string, int> loadedCurrency = null;
            bool currencyLoadFailed = false;
            
            if (loadOperation.Result.TryGetValue(CURRENCY_KEY, out var currencyData))
            {
                try
                {
                    string currencyJson = currencyData.ToString();
                    if (IsValidJson(currencyJson))
                    {
                        loadedCurrency = JsonConvert.DeserializeObject<Dictionary<string, int>>(currencyJson);
                    }
                    else
                    {
                        currencyLoadFailed = true;
                    }
                }
                catch
                {
                    currencyLoadFailed = true;
                }
            }
            else
            {
                currencyLoadFailed = true;
            }

            if (currencyLoadFailed || loadedCurrency == null || !loadedCurrency.ContainsKey("ruby"))
            {
                InitializeDefaultCurrency();
                yield return SaveCurrency();
            }
            else
            {
                playerCurrency = loadedCurrency;
            }
        }
        else
        {
            InitializeDefaultUnlockVariables();
            InitializeDefaultCurrency();
        }

        SetHair(currentIndex);
        SetupButtonListeners();
    }

    private IEnumerator InitializeUnityServices()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            var initialization = UnityServices.InitializeAsync();
            yield return new WaitUntil(() => initialization.IsCompleted);
        }

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            var signIn = AuthenticationService.Instance.SignInAnonymouslyAsync();
            yield return new WaitUntil(() => signIn.IsCompleted);

            if (signIn.IsFaulted)
            {
                Debug.LogError("Failed to sign in anonymously");
            }
        }
    }

    private Dictionary<string, string> InitializeDefaultUnlockVariables()
    {
        var defaults = new Dictionary<string, string>();
        foreach (var key in buyKeys)
        {
            if (!string.IsNullOrEmpty(key))
            {
                defaults[key] = "0";
            }
        }
        unlockVariables = defaults;
        return defaults;
    }

    private void InitializeDefaultCurrency()
    {
        playerCurrency = new Dictionary<string, int> { { "ruby", 0 } };
    }

    private IEnumerator SaveUnlockVariables()
    {
        var dataToSave = new Dictionary<string, object>
        {
            { UNLOCK_KEY, JsonConvert.SerializeObject(unlockVariables) }
        };

        var saveOperation = CloudSaveService.Instance.Data.ForceSaveAsync(dataToSave);
        yield return new WaitUntil(() => saveOperation.IsCompleted);

        if (saveOperation.IsFaulted)
        {
            Debug.LogError("Error saving unlock variables");
        }
    }

    private IEnumerator SaveCurrency()
    {
        var dataToSave = new Dictionary<string, object>
        {
            { CURRENCY_KEY, JsonConvert.SerializeObject(playerCurrency) }
        };

        var saveOperation = CloudSaveService.Instance.Data.ForceSaveAsync(dataToSave);
        yield return new WaitUntil(() => saveOperation.IsCompleted);

        if (saveOperation.IsFaulted)
        {
            Debug.LogError("Error saving currency data");
        }

        PriceCommand.RefreshPrice();
    }

    private bool IsValidJson(string jsonString)
    {
        try
        {
            JsonConvert.DeserializeObject(jsonString);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void SetupButtonListeners()
    {
        leftArrowButton.onClick.RemoveAllListeners();
        rightArrowButton.onClick.RemoveAllListeners();
        selectButton.onClick.RemoveAllListeners();
        buttonDown.onClick.RemoveAllListeners();
        buttonUp.onClick.RemoveAllListeners();
        buyButton.onClick.RemoveAllListeners();

        leftArrowButton.onClick.AddListener(OnLeftArrowClicked);
        rightArrowButton.onClick.AddListener(OnRightArrowClicked);
        selectButton.onClick.AddListener(OnSelectClicked);
        buttonDown.onClick.AddListener(HidePanel);
        buttonUp.onClick.AddListener(ShowPanel);
        buyButton.onClick.AddListener(OnBuyClicked);
    }

    private void UpdatePriceDisplay(int index)
    {
        bool isPaid = index < buyKeys.Length && !string.IsNullOrEmpty(buyKeys[index]);
        bool isUnlocked = isPaid && unlockVariables.ContainsKey(buyKeys[index]) && unlockVariables[buyKeys[index]] == "1";

        buyButton.gameObject.SetActive(isPaid && !isUnlocked);
        priceText.gameObject.SetActive(isPaid && !isUnlocked && index < hairPrices.Length);

        if (priceText.gameObject.activeSelf)
        {
            priceText.text = hairPrices[index].ToString();
        }
    }

    private void UpdateBuyAllPriceDisplay()
    {
        bool anyUnlocked = buyKeys.Any(key => 
            !string.IsNullOrEmpty(key) && 
            (!unlockVariables.ContainsKey(key) || unlockVariables[key] != "1"));

        buyButton.gameObject.SetActive(anyUnlocked);
        priceText.gameObject.SetActive(anyUnlocked);
        priceText.text = anyUnlocked ? buyAllPrice.ToString() : "";
    }

    private void UpdateHairDescription(int index)
    {
        if (index >= 0 && index < hairTypeDescriptions.Length)
        {
            StartCoroutine(LoadLocalizedText(hairTypeDescriptions[index]));
        }
    }

    private void UpdateHairDescriptionForBuyAll()
    {
        if (buyAllIndex >= 0 && buyAllIndex < hairTypeDescriptions.Length)
        {
            StartCoroutine(LoadLocalizedText(hairTypeDescriptions[buyAllIndex]));
        }
        else
        {
            hairDescriptionText.text = "Купить все платные причёски";
        }
    }

    private IEnumerator LoadLocalizedText(LocalizedString localizedString)
    {
        yield return LocalizationSettings.InitializationOperation;
        var operation = localizedString.GetLocalizedStringAsync();
        yield return operation;

        if (operation.Status == AsyncOperationStatus.Succeeded)
        {
            hairDescriptionText.text = operation.Result;
        }
        else
        {
            hairDescriptionText.text = "Error: Localization Failed";
        }
    }

    private void OnLeftArrowClicked()
    {
        do
        {
            currentIndex--;
            if (currentIndex < 0) currentIndex = hairTypeIndexes.Length - 1;
        }
        while (!buyAllEnabled && currentIndex == buyAllIndex);
        SetHair(currentIndex);
    }

    private void OnRightArrowClicked()
    {
        do
        {
            currentIndex++;
            if (currentIndex >= hairTypeIndexes.Length) currentIndex = 0;
        }
        while (!buyAllEnabled && currentIndex == buyAllIndex);
        SetHair(currentIndex);
    }

    private void OnSelectClicked()
    {
        IntegerVariable hairVariable = flowchart.GetVariable<IntegerVariable>("Hair");
        if (hairVariable != null)
        {
            hairVariable.Value = hairTypeIndexes[currentIndex];
        }
        Continue();
    }

    private void OnBuyClicked()
    {
        if (isBuyAllPage)
        {
            StartCoroutine(UnlockAllHair());
        }
        else if (currentIndex < buyKeys.Length && !string.IsNullOrEmpty(buyKeys[currentIndex]))
        {
            StartCoroutine(UnlockHair(buyKeys[currentIndex]));
        }
    }

    private IEnumerator UnlockAllHair()
    {
        if (playerCurrency["ruby"] < buyAllPrice)
        {
            Debug.Log("Not enough rubies to buy all hair");
            SceneHistoryManager.AddScene(SceneManager.GetActiveScene().name);
            SceneManager.LoadScene("shopRubin");
            yield break;
        }

        playerCurrency["ruby"] -= buyAllPrice;
        yield return SaveCurrency();

        foreach (var key in buyKeys)
        {
            if (!string.IsNullOrEmpty(key))
            {
                unlockVariables[key] = "1";
            }
        }
        yield return SaveUnlockVariables();
        
        buyButton.gameObject.SetActive(false);
        priceText.gameObject.SetActive(false);
    }

    private IEnumerator UnlockHair(string buyKey)
    {
        int price = hairPrices[currentIndex];
        
        if (playerCurrency["ruby"] < price)
        {
            Debug.Log("Not enough rubies to buy this hair");
            SceneHistoryManager.AddScene(SceneManager.GetActiveScene().name);
            SceneManager.LoadScene("shopRubin");
            yield break;
        }

        playerCurrency["ruby"] -= price;
        yield return SaveCurrency();

        unlockVariables[buyKey] = "1";
        yield return SaveUnlockVariables();
        
        buyButton.gameObject.SetActive(false);
        priceText.gameObject.SetActive(false);
    }

    private void HidePanel()
    {
        if (isSliding) return;
        isSliding = true;
        StartCoroutine(SlidePanel(Vector2.down * slideDistance, () =>
        {
            buttonDown.gameObject.SetActive(false);
            buttonUp.gameObject.SetActive(true);
            isSliding = false;
        }));
    }

    private void ShowPanel()
    {
        if (isSliding) return;
        isSliding = true;
        StartCoroutine(SlidePanel(Vector2.zero, () =>
        {
            buttonDown.gameObject.SetActive(true);
            buttonUp.gameObject.SetActive(false);
            isSliding = false;
        }));
    }

    private IEnumerator SlidePanel(Vector2 targetOffset, Action onComplete)
    {
        Vector2 startPos = panelToHide.GetComponent<RectTransform>().anchoredPosition;
        Vector2 targetPos = originalPosition + targetOffset;
        float elapsed = 0f;

        while (elapsed < slideDuration)
        {
            panelToHide.GetComponent<RectTransform>().anchoredPosition = 
                Vector2.Lerp(startPos, targetPos, elapsed / slideDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        panelToHide.GetComponent<RectTransform>().anchoredPosition = targetPos;
        onComplete?.Invoke();
    }

    public override string GetSummary()
    {
        return "Hair selection with prices and buy options";
    }

    public override Color GetButtonColor()
    {
        return new Color32(255, 200, 150, 255);
    }
}









// using UnityEngine;
// using UnityEngine.UI;
// using Fungus;
// using System.Collections;
// using TMPro;
// using UnityEngine.Localization;
// using UnityEngine.Localization.Settings;
// using UnityEngine.ResourceManagement.AsyncOperations;
// using Unity.Services.CloudSave;
// using Unity.Services.Core;
// using Unity.Services.Authentication;
// using Newtonsoft.Json;
// using System.Collections.Generic;
// using System.Linq;
// using System;

// [CommandInfo("Character",
//     "Hair Selection",
//     "Displays hair selection UI with prices, buy options and navigation")]
// public class HairSelectionCommand : Command
// {
//     [Header("Hair Settings")]
//     [SerializeField] private int[] hairTypeIndexes;
//     [SerializeField] private string[] buyKeys;
//     [SerializeField] private int[] hairPrices;
//     [SerializeField] private int buyAllPrice = 1000;
//     [SerializeField] private int buyAllIndex = -1;

//     [Header("UI References")]
//     [SerializeField] private GameObject wardrobePanel;
//     [SerializeField] private GameObject panelToHide;
//     [SerializeField] private Button leftArrowButton;
//     [SerializeField] private Button rightArrowButton;
//     [SerializeField] private Button selectButton;
//     [SerializeField] private Button buttonDown;
//     [SerializeField] private Button buttonUp;
//     [SerializeField] private Button buyButton;
//     [SerializeField] private TextMeshProUGUI hairDescriptionText;
//     [SerializeField] private TextMeshProUGUI priceText;
//     [SerializeField] private LocalizedString[] hairTypeDescriptions;

//     [Header("Configuration")]
//     [SerializeField] private int positionIndex;
//     [SerializeField] private float slideDuration = 0.5f;
//     [SerializeField] private float slideDistance = 200f;

//     private int currentIndex = 0;
//     private SimpleCharacterManager characterManager;
//     private Flowchart flowchart;
//     private Vector2 originalPosition;
//     private bool isSliding = false;
//     private bool isBuyAllPage = false;
//     private Dictionary<string, string> unlockVariables = new Dictionary<string, string>();
//     private Dictionary<string, int> playerCurrency = new Dictionary<string, int>();
//     private const string UNLOCK_KEY = "LANA_UNLOCK_VARIABLES";
//     private const string CURRENCY_KEY = "PLAYER_RUBY";

//     public override void OnEnter()
//     {
//         characterManager = FindObjectOfType<SimpleCharacterManager>();
//         flowchart = GetFlowchart();

//         if (characterManager == null || flowchart == null)
//         {
//             Debug.LogError("Required components not found!");
//             Continue();
//             return;
//         }

//         wardrobePanel.SetActive(true);
//         originalPosition = panelToHide.GetComponent<RectTransform>().anchoredPosition;
//         StartCoroutine(InitializeAndLoadData());
//     }

// private IEnumerator InitializeAndLoadData()
// {
//     yield return InitializeUnityServices();
    
//     var loadOperation = CloudSaveService.Instance.Data.LoadAsync(new HashSet<string> { UNLOCK_KEY, CURRENCY_KEY });
//     yield return new WaitUntil(() => loadOperation.IsCompleted);

//     if (loadOperation.IsCompleted && !loadOperation.IsFaulted)
//     {
//         // Обработка данных о разблокированных предметах
//         if (loadOperation.Result.TryGetValue(UNLOCK_KEY, out var savedData))
//         {
//             try
//             {
//                 string jsonString = savedData.ToString();
//                 unlockVariables = IsValidJson(jsonString) 
//                     ? JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString)
//                     : InitializeDefaultUnlockVariables();
//             }
//             catch
//             {
//                 unlockVariables = InitializeDefaultUnlockVariables();
//             }
            
//             // Перенесли сохранение за пределы try-catch
//             if (unlockVariables.Count == 0)
//             {
//                 InitializeDefaultUnlockVariables();
//                 yield return SaveUnlockVariables();
//             }
//         }
//         else
//         {
//             InitializeDefaultUnlockVariables();
//             yield return SaveUnlockVariables();
//         }

//         // Обработка данных о валюте
//         Dictionary<string, int> loadedCurrency = null;
//         bool currencyLoadFailed = false;
        
//         if (loadOperation.Result.TryGetValue(CURRENCY_KEY, out var currencyData))
//         {
//             try
//             {
//                 string currencyJson = currencyData.ToString();
//                 if (IsValidJson(currencyJson))
//                 {
//                     loadedCurrency = JsonConvert.DeserializeObject<Dictionary<string, int>>(currencyJson);
//                 }
//                 else
//                 {
//                     currencyLoadFailed = true;
//                 }
//             }
//             catch
//             {
//                 currencyLoadFailed = true;
//             }
//         }
//         else
//         {
//             currencyLoadFailed = true;
//         }

//         // Обработка результата загрузки валюты
//         if (currencyLoadFailed || loadedCurrency == null || !loadedCurrency.ContainsKey("ruby"))
//         {
//             InitializeDefaultCurrency();
//             yield return SaveCurrency();
//         }
//         else
//         {
//             playerCurrency = loadedCurrency;
//         }
//     }
//     else
//     {
//         InitializeDefaultUnlockVariables();
//         InitializeDefaultCurrency();
//     }

//     SetHair(currentIndex);
//     SetupButtonListeners();
// }

//     private IEnumerator InitializeUnityServices()
//     {
//         if (UnityServices.State != ServicesInitializationState.Initialized)
//         {
//             var initialization = UnityServices.InitializeAsync();
//             yield return new WaitUntil(() => initialization.IsCompleted);
//         }

//         if (!AuthenticationService.Instance.IsSignedIn)
//         {
//             var signIn = AuthenticationService.Instance.SignInAnonymouslyAsync();
//             yield return new WaitUntil(() => signIn.IsCompleted);

//             if (signIn.IsFaulted)
//             {
//                 Debug.LogError("Failed to sign in anonymously");
//             }
//         }
//     }

//     private Dictionary<string, string> InitializeDefaultUnlockVariables()
//     {
//         var defaults = new Dictionary<string, string>();
//         foreach (var key in buyKeys)
//         {
//             if (!string.IsNullOrEmpty(key))
//             {
//                 defaults[key] = "0";
//             }
//         }
//         unlockVariables = defaults; // Сохраняем в поле класса
//         return defaults;
//     }

//     private void InitializeDefaultCurrency()
//     {
//         playerCurrency = new Dictionary<string, int> { { "ruby", 0 } };
//     }

//     private IEnumerator SaveUnlockVariables()
//     {
//         var dataToSave = new Dictionary<string, object>
//         {
//             { UNLOCK_KEY, JsonConvert.SerializeObject(unlockVariables) }
//         };

//         var saveOperation = CloudSaveService.Instance.Data.ForceSaveAsync(dataToSave);
//         yield return new WaitUntil(() => saveOperation.IsCompleted);

//         if (saveOperation.IsFaulted)
//         {
//             Debug.LogError("Error saving unlock variables");
//         }
//     }

//     private IEnumerator SaveCurrency()
//     {
//         var dataToSave = new Dictionary<string, object>
//         {
//             { CURRENCY_KEY, JsonConvert.SerializeObject(playerCurrency) }
//         };

//         var saveOperation = CloudSaveService.Instance.Data.ForceSaveAsync(dataToSave);
//         yield return new WaitUntil(() => saveOperation.IsCompleted);

//         if (saveOperation.IsFaulted)
//         {
//             Debug.LogError("Error saving currency data");
//         }
//     }

//     private bool IsValidJson(string jsonString)
//     {
//         try
//         {
//             JsonConvert.DeserializeObject(jsonString);
//             return true;
//         }
//         catch
//         {
//             return false;
//         }
//     }

//     private void SetupButtonListeners()
//     {
//         leftArrowButton.onClick.RemoveAllListeners();
//         rightArrowButton.onClick.RemoveAllListeners();
//         selectButton.onClick.RemoveAllListeners();
//         buttonDown.onClick.RemoveAllListeners();
//         buttonUp.onClick.RemoveAllListeners();
//         buyButton.onClick.RemoveAllListeners();

//         leftArrowButton.onClick.AddListener(OnLeftArrowClicked);
//         rightArrowButton.onClick.AddListener(OnRightArrowClicked);
//         selectButton.onClick.AddListener(OnSelectClicked);
//         buttonDown.onClick.AddListener(HidePanel);
//         buttonUp.onClick.AddListener(ShowPanel);
//         buyButton.onClick.AddListener(OnBuyClicked);
//     }

//     private void SetHair(int index)
//     {
//         if (index < 0 || index >= hairTypeIndexes.Length) return;

//         isBuyAllPage = (index == buyAllIndex);
//         selectButton.gameObject.SetActive(!isBuyAllPage);

//         if (!isBuyAllPage)
//         {
//             int hairTypeIndex = hairTypeIndexes[index];
            
//             characterManager.SetCharacterSprite(
//                 positionIndex,
//                 flowchart.GetVariable<IntegerVariable>("Type").Value,
//                 CharacterEmotion.Нейтральное,
//                 hairTypeIndex,
//                 flowchart.GetVariable<IntegerVariable>("Makeup").Value,
//                 flowchart.GetVariable<IntegerVariable>("Dress").Value,
//                 flowchart.GetVariable<IntegerVariable>("Ukrashenie").Value,
//                 flowchart.GetVariable<IntegerVariable>("Accessorise").Value
//             );

//             UpdateHairDescription(hairTypeIndex);
//             UpdatePriceDisplay(index);
//         }
//         else
//         {
//             UpdateHairDescriptionForBuyAll();
//             UpdateBuyAllPriceDisplay();
//         }
//     }

//     private void UpdatePriceDisplay(int index)
//     {
//         bool isPaid = index < buyKeys.Length && !string.IsNullOrEmpty(buyKeys[index]);
//         bool isUnlocked = isPaid && unlockVariables.ContainsKey(buyKeys[index]) && unlockVariables[buyKeys[index]] == "1";

//         buyButton.gameObject.SetActive(isPaid && !isUnlocked);
//         priceText.gameObject.SetActive(isPaid && !isUnlocked && index < hairPrices.Length);

//         if (priceText.gameObject.activeSelf)
//         {
//             priceText.text = hairPrices[index].ToString();
//         }
//     }

//     private void UpdateBuyAllPriceDisplay()
//     {
//         bool anyUnlocked = buyKeys.Any(key => 
//             !string.IsNullOrEmpty(key) && 
//             (!unlockVariables.ContainsKey(key) || unlockVariables[key] != "1"));

//         buyButton.gameObject.SetActive(anyUnlocked);
//         priceText.gameObject.SetActive(anyUnlocked);
//         priceText.text = anyUnlocked ? buyAllPrice.ToString() : "";
//     }

//     private void UpdateHairDescription(int hairTypeIndex)
//     {
//         if (hairTypeIndex >= 0 && hairTypeIndex < hairTypeDescriptions.Length)
//         {
//             StartCoroutine(LoadLocalizedText(hairTypeDescriptions[hairTypeIndex]));
//         }
//     }

//     private void UpdateHairDescriptionForBuyAll()
//     {
//         hairDescriptionText.text = "Купить все платные причёски";
//     }

//     private IEnumerator LoadLocalizedText(LocalizedString localizedString)
//     {
//         yield return LocalizationSettings.InitializationOperation;
//         var operation = localizedString.GetLocalizedStringAsync();
//         yield return operation;

//         if (operation.Status == AsyncOperationStatus.Succeeded)
//         {
//             hairDescriptionText.text = operation.Result;
//         }
//         else
//         {
//             hairDescriptionText.text = "Error: Localization Failed";
//         }
//     }

//     private void OnLeftArrowClicked()
//     {
//         currentIndex--;
//         if (currentIndex < 0) currentIndex = hairTypeIndexes.Length - 1;
//         SetHair(currentIndex);
//     }

//     private void OnRightArrowClicked()
//     {
//         currentIndex++;
//         if (currentIndex >= hairTypeIndexes.Length) currentIndex = 0;
//         SetHair(currentIndex);
//     }

//     private void OnSelectClicked()
//     {
//         IntegerVariable hairVariable = flowchart.GetVariable<IntegerVariable>("Hair");
//         if (hairVariable != null)
//         {
//             hairVariable.Value = hairTypeIndexes[currentIndex];
//         }
//         Continue();
//     }

//     private void OnBuyClicked()
//     {
//         if (isBuyAllPage)
//         {
//             StartCoroutine(UnlockAllHair());
//         }
//         else if (currentIndex < buyKeys.Length && !string.IsNullOrEmpty(buyKeys[currentIndex]))
//         {
//             StartCoroutine(UnlockHair(buyKeys[currentIndex]));
//         }
//     }

//     private IEnumerator UnlockAllHair()
//     {
//         if (playerCurrency["ruby"] < buyAllPrice)
//         {
//             Debug.Log("Not enough rubies to buy all hair");
//             yield break;
//         }

//         playerCurrency["ruby"] -= buyAllPrice;
//         yield return SaveCurrency();

//         foreach (var key in buyKeys)
//         {
//             if (!string.IsNullOrEmpty(key))
//             {
//                 unlockVariables[key] = "1";
//             }
//         }
//         yield return SaveUnlockVariables();
        
//         buyButton.gameObject.SetActive(false);
//         priceText.gameObject.SetActive(false);
//     }

//     private IEnumerator UnlockHair(string buyKey)
//     {
//         int price = hairPrices[currentIndex];
        
//         if (playerCurrency["ruby"] < price)
//         {
//             Debug.Log("Not enough rubies to buy this hair");
//             yield break;
//         }

//         playerCurrency["ruby"] -= price;
//         yield return SaveCurrency();

//         unlockVariables[buyKey] = "1";
//         yield return SaveUnlockVariables();
        
//         buyButton.gameObject.SetActive(false);
//         priceText.gameObject.SetActive(false);
//     }

//     private void HidePanel()
//     {
//         if (isSliding) return;
//         isSliding = true;
//         StartCoroutine(SlidePanel(Vector2.down * slideDistance, () =>
//         {
//             buttonDown.gameObject.SetActive(false);
//             buttonUp.gameObject.SetActive(true);
//             isSliding = false;
//         }));
//     }

//     private void ShowPanel()
//     {
//         if (isSliding) return;
//         isSliding = true;
//         StartCoroutine(SlidePanel(Vector2.zero, () =>
//         {
//             buttonDown.gameObject.SetActive(true);
//             buttonUp.gameObject.SetActive(false);
//             isSliding = false;
//         }));
//     }

//     private IEnumerator SlidePanel(Vector2 targetOffset, Action onComplete)
//     {
//         Vector2 startPos = panelToHide.GetComponent<RectTransform>().anchoredPosition;
//         Vector2 targetPos = originalPosition + targetOffset;
//         float elapsed = 0f;

//         while (elapsed < slideDuration)
//         {
//             panelToHide.GetComponent<RectTransform>().anchoredPosition = 
//                 Vector2.Lerp(startPos, targetPos, elapsed / slideDuration);
//             elapsed += Time.deltaTime;
//             yield return null;
//         }

//         panelToHide.GetComponent<RectTransform>().anchoredPosition = targetPos;
//         onComplete?.Invoke();
//     }

//     public override string GetSummary()
//     {
//         return "Hair selection with prices and buy options";
//     }

//     public override Color GetButtonColor()
//     {
//         return new Color32(255, 200, 150, 255);
//     }
// }








// using UnityEngine;
// using UnityEngine.UI;
// using Fungus;
// using System.Collections;
// using TMPro;
// using UnityEngine.Localization;
// using UnityEngine.Localization.Settings;
// using UnityEngine.ResourceManagement.AsyncOperations;
// using Unity.Services.CloudSave;
// using Unity.Services.Core;
// using Unity.Services.Authentication;
// using Newtonsoft.Json;
// using System.Collections.Generic;
// using System.Linq;
// using System;

// [CommandInfo("Character",
//     "Hair Selection",
//     "Displays hair selection UI with prices, buy options and navigation")]
// public class HairSelectionCommand : Command
// {
//     [Header("Hair Settings")]
//     [SerializeField] private int[] hairTypeIndexes;
//     [SerializeField] private string[] buyKeys;
//     [SerializeField] private int[] hairPrices;
//     [SerializeField] private int buyAllPrice = 1000; // Фиксированная цена для "Купить всё"
//     [SerializeField] private int buyAllIndex = -1;

//     [Header("UI References")]
//     [SerializeField] private GameObject wardrobePanel;
//     [SerializeField] private GameObject panelToHide;
//     [SerializeField] private Button leftArrowButton;
//     [SerializeField] private Button rightArrowButton;
//     [SerializeField] private Button selectButton;
//     [SerializeField] private Button buttonDown;
//     [SerializeField] private Button buttonUp;
//     [SerializeField] private Button buyButton;
//     [SerializeField] private TextMeshProUGUI hairDescriptionText;
//     [SerializeField] private TextMeshProUGUI priceText;
//     [SerializeField] private LocalizedString[] hairTypeDescriptions;

//     [Header("Configuration")]
//     [SerializeField] private int positionIndex;
//     [SerializeField] private float slideDuration = 0.5f;
//     [SerializeField] private float slideDistance = 200f;

//     private int currentIndex = 0;
//     private SimpleCharacterManager characterManager;
//     private Flowchart flowchart;
//     private Vector2 originalPosition;
//     private bool isSliding = false;
//     private bool isBuyAllPage = false;
//     private Dictionary<string, string> unlockVariables = new Dictionary<string, string>();
//     private const string UNLOCK_KEY = "LANA_UNLOCK_VARIABLES";

//     public override void OnEnter()
//     {
//         characterManager = FindObjectOfType<SimpleCharacterManager>();
//         flowchart = GetFlowchart();

//         if (characterManager == null || flowchart == null)
//         {
//             Debug.LogError("Required components not found!");
//             Continue();
//             return;
//         }

//         wardrobePanel.SetActive(true);
//         originalPosition = panelToHide.GetComponent<RectTransform>().anchoredPosition;
//         StartCoroutine(LoadUnlockVariables());
//     }

//     private IEnumerator LoadUnlockVariables()
//     {
//         yield return InitializeUnityServices();

//         var loadOperation = CloudSaveService.Instance.Data.LoadAsync(new HashSet<string> { UNLOCK_KEY });
//         yield return new WaitUntil(() => loadOperation.IsCompleted);

//         if (loadOperation.IsCompleted && !loadOperation.IsFaulted)
//         {
//             if (loadOperation.Result.TryGetValue(UNLOCK_KEY, out var savedData))
//             {
//                 try
//                 {
//                     string jsonString = savedData.ToString();
//                     if (IsValidJson(jsonString))
//                     {
//                         unlockVariables = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString);
//                     }
//                     else
//                     {
//                         InitializeDefaultUnlockVariables();
//                     }
//                 }
//                 catch
//                 {
//                     InitializeDefaultUnlockVariables();
//                 }
//             }
//             else
//             {
//                 InitializeDefaultUnlockVariables();
//                 yield return SaveUnlockVariables();
//             }
//         }
//         else
//         {
//             InitializeDefaultUnlockVariables();
//         }

//         SetHair(currentIndex);
//         SetupButtonListeners();
//     }

//     private void SetupButtonListeners()
//     {
//         leftArrowButton.onClick.RemoveAllListeners();
//         rightArrowButton.onClick.RemoveAllListeners();
//         selectButton.onClick.RemoveAllListeners();
//         buttonDown.onClick.RemoveAllListeners();
//         buttonUp.onClick.RemoveAllListeners();
//         buyButton.onClick.RemoveAllListeners();

//         leftArrowButton.onClick.AddListener(OnLeftArrowClicked);
//         rightArrowButton.onClick.AddListener(OnRightArrowClicked);
//         selectButton.onClick.AddListener(OnSelectClicked);
//         buttonDown.onClick.AddListener(HidePanel);
//         buttonUp.onClick.AddListener(ShowPanel);
//         buyButton.onClick.AddListener(OnBuyClicked);
//     }

//     private void SetHair(int index)
//     {
//         if (index < 0 || index >= hairTypeIndexes.Length) return;

//         isBuyAllPage = (index == buyAllIndex);
//         selectButton.gameObject.SetActive(!isBuyAllPage);

//         if (!isBuyAllPage)
//         {
//             int hairTypeIndex = hairTypeIndexes[index];
            
//             // Установка спрайтов персонажа
//             characterManager.SetCharacterSprite(
//                 positionIndex,
//                 flowchart.GetVariable<IntegerVariable>("Type").Value,
//                 CharacterEmotion.Нейтральное,
//                 hairTypeIndex,
//                 flowchart.GetVariable<IntegerVariable>("Makeup").Value,
//                 flowchart.GetVariable<IntegerVariable>("Dress").Value,
//                 flowchart.GetVariable<IntegerVariable>("Ukrashenie").Value,
//                 flowchart.GetVariable<IntegerVariable>("Accessorise").Value
//             );

//             UpdateHairDescription(hairTypeIndex);
//             UpdatePriceDisplay(index);
//         }
//         else
//         {
//             UpdateHairDescriptionForBuyAll();
//             UpdateBuyAllPriceDisplay();
//         }
//     }

//     private void UpdatePriceDisplay(int index)
//     {
//         bool isPaid = index < buyKeys.Length && !string.IsNullOrEmpty(buyKeys[index]);
//         bool isUnlocked = isPaid && unlockVariables.ContainsKey(buyKeys[index]) && unlockVariables[buyKeys[index]] == "1";

//         buyButton.gameObject.SetActive(isPaid && !isUnlocked);
//         priceText.gameObject.SetActive(isPaid && !isUnlocked && index < hairPrices.Length);

//         if (priceText.gameObject.activeSelf)
//         {
//             priceText.text = hairPrices[index].ToString();
//         }
//     }

//     private void UpdateBuyAllPriceDisplay()
//     {
//         bool anyUnlocked = buyKeys.Any(key => 
//             !string.IsNullOrEmpty(key) && 
//             (!unlockVariables.ContainsKey(key) || unlockVariables[key] != "1"));

//         buyButton.gameObject.SetActive(anyUnlocked);
//         priceText.gameObject.SetActive(anyUnlocked);
//         priceText.text = anyUnlocked ? buyAllPrice.ToString() : "";
//     }

//     private void UpdateHairDescription(int hairTypeIndex)
//     {
//         if (hairTypeIndex >= 0 && hairTypeIndex < hairTypeDescriptions.Length)
//         {
//             StartCoroutine(LoadLocalizedText(hairTypeDescriptions[hairTypeIndex]));
//         }
//     }

//     private void UpdateHairDescriptionForBuyAll()
//     {
//         hairDescriptionText.text = "Купить все платные причёски";
//     }

//     private IEnumerator LoadLocalizedText(LocalizedString localizedString)
//     {
//         yield return LocalizationSettings.InitializationOperation;
//         var operation = localizedString.GetLocalizedStringAsync();
//         yield return operation;

//         if (operation.Status == AsyncOperationStatus.Succeeded)
//         {
//             hairDescriptionText.text = operation.Result;
//         }
//         else
//         {
//             hairDescriptionText.text = "Error: Localization Failed";
//         }
//     }

//     private IEnumerator InitializeUnityServices()
//     {
//         var initialization = UnityServices.InitializeAsync();
//         yield return new WaitUntil(() => initialization.IsCompleted);

//         var signIn = AuthenticationService.Instance.SignInAnonymouslyAsync();
//         yield return new WaitUntil(() => signIn.IsCompleted);

//         if (!signIn.IsCompleted || signIn.IsFaulted)
//         {
//             Debug.LogError("Ошибка входа в Unity Services");
//         }
//     }

//     private void InitializeDefaultUnlockVariables()
//     {
//         unlockVariables = new Dictionary<string, string>();
//         foreach (var key in buyKeys)
//         {
//             if (!string.IsNullOrEmpty(key))
//             {
//                 unlockVariables[key] = "0";
//             }
//         }
//     }

//     private IEnumerator SaveUnlockVariables()
//     {
//         var dataToSave = new Dictionary<string, object>
//         {
//             { UNLOCK_KEY, JsonConvert.SerializeObject(unlockVariables) }
//         };

//         var saveOperation = CloudSaveService.Instance.Data.ForceSaveAsync(dataToSave);
//         yield return new WaitUntil(() => saveOperation.IsCompleted);

//         if (saveOperation.IsFaulted)
//         {
//             Debug.LogError("Error saving unlock variables");
//         }
//     }

//     private bool IsValidJson(string jsonString)
//     {
//         try
//         {
//             JsonConvert.DeserializeObject(jsonString);
//             return true;
//         }
//         catch
//         {
//             return false;
//         }
//     }

//     private void OnLeftArrowClicked()
//     {
//         currentIndex--;
//         if (currentIndex < 0) currentIndex = hairTypeIndexes.Length - 1;
//         SetHair(currentIndex);
//     }

//     private void OnRightArrowClicked()
//     {
//         currentIndex++;
//         if (currentIndex >= hairTypeIndexes.Length) currentIndex = 0;
//         SetHair(currentIndex);
//     }

//     private void OnSelectClicked()
//     {
//         IntegerVariable hairVariable = flowchart.GetVariable<IntegerVariable>("Hair");
//         if (hairVariable != null)
//         {
//             hairVariable.Value = hairTypeIndexes[currentIndex];
//         }
//         Continue();
//     }

//     private void OnBuyClicked()
//     {
//         if (isBuyAllPage)
//         {
//             StartCoroutine(UnlockAllHair());
//         }
//         else if (currentIndex < buyKeys.Length && !string.IsNullOrEmpty(buyKeys[currentIndex]))
//         {
//             StartCoroutine(UnlockHair(buyKeys[currentIndex]));
//         }
//     }

//     private IEnumerator UnlockAllHair()
//     {
//         foreach (var key in buyKeys)
//         {
//             if (!string.IsNullOrEmpty(key))
//             {
//                 unlockVariables[key] = "1";
//             }
//         }
//         yield return SaveUnlockVariables();
//         buyButton.gameObject.SetActive(false);
//         priceText.gameObject.SetActive(false);
//     }

//     private IEnumerator UnlockHair(string buyKey)
//     {
//         unlockVariables[buyKey] = "1";
//         yield return SaveUnlockVariables();
//         buyButton.gameObject.SetActive(false);
//         priceText.gameObject.SetActive(false);
//     }

//     private void HidePanel()
//     {
//         if (isSliding) return;
//         isSliding = true;
//         StartCoroutine(SlidePanel(Vector2.down * slideDistance, () =>
//         {
//             buttonDown.gameObject.SetActive(false);
//             buttonUp.gameObject.SetActive(true);
//             isSliding = false;
//         }));
//     }

//     private void ShowPanel()
//     {
//         if (isSliding) return;
//         isSliding = true;
//         StartCoroutine(SlidePanel(Vector2.zero, () =>
//         {
//             buttonDown.gameObject.SetActive(true);
//             buttonUp.gameObject.SetActive(false);
//             isSliding = false;
//         }));
//     }

//     private IEnumerator SlidePanel(Vector2 targetOffset, Action onComplete)
//     {
//         Vector2 startPos = panelToHide.GetComponent<RectTransform>().anchoredPosition;
//         Vector2 targetPos = originalPosition + targetOffset;
//         float elapsed = 0f;

//         while (elapsed < slideDuration)
//         {
//             panelToHide.GetComponent<RectTransform>().anchoredPosition = 
//                 Vector2.Lerp(startPos, targetPos, elapsed / slideDuration);
//             elapsed += Time.deltaTime;
//             yield return null;
//         }

//         panelToHide.GetComponent<RectTransform>().anchoredPosition = targetPos;
//         onComplete?.Invoke();
//     }

//     public override string GetSummary()
//     {
//         return "Hair selection with prices and buy options";
//     }

//     public override Color GetButtonColor()
//     {
//         return new Color32(255, 200, 150, 255);
//     }
// }






// using UnityEngine;
// using UnityEngine.UI;
// using Fungus;
// using System.Collections;
// using TMPro;
// using UnityEngine.Localization;
// using UnityEngine.Localization.Settings;
// using UnityEngine.ResourceManagement.AsyncOperations;

// using Unity.Services.CloudSave;
// using Unity.Services.Core;
// using Unity.Services.Authentication;
// using System.Threading.Tasks;
// using Newtonsoft.Json;
// using System.Collections.Generic;
// using System.Linq;
// using System;

// [CommandInfo("Character",
//     "Hair Selection",
//     "Displays hair selection UI with left/right arrows, a select button, and up/down buttons to hide/show the panel. Includes localized text for hair types and paid hair options.")]
// public class HairSelectionCommand : Command
// {
//     [Tooltip("Indexes of hair types to cycle through")]
//     [SerializeField]
//     private int[] hairTypeIndexes;

//     [Tooltip("Wardrobe panel (parent of all UI elements)")]
//     [SerializeField]
//     private GameObject wardrobePanel; // Панель Wardrobe, которая включает все UI-элементы

//     [Tooltip("Panel to hide/show (child of WardrobePanel)")]
//     [SerializeField]
//     private GameObject panelToHide; // Объект Panel, который будет скрываться/показываться

//     [Tooltip("Left arrow button")]
//     [SerializeField]
//     private Button leftArrowButton;

//     [Tooltip("Right arrow button")]
//     [SerializeField]
//     private Button rightArrowButton;

//     [Tooltip("Select button")]
//     [SerializeField]
//     private Button selectButton;

//     [Tooltip("Button to hide the panel (move down)")]
//     [SerializeField]
//     private Button buttonDown;

//     [Tooltip("Button to show the panel (move up)")]
//     [SerializeField]
//     private Button buttonUp;

//     [Tooltip("Button to buy the selected hair")]
//     [SerializeField]
//     private Button buyButton; // Кнопка "Купить"

//     [Tooltip("Position on the screen where the character should appear")]
//     [SerializeField]
//     private int positionIndex;

//     [Tooltip("Duration of the slide animation")]
//     [SerializeField]
//     private float slideDuration = 0.5f;

//     [Tooltip("Distance to slide the panel up/down")]
//     [SerializeField]
//     private float slideDistance = 200f;

//     [Tooltip("TextMeshPro component to display hair type description")]
//     [SerializeField]
//     private TextMeshProUGUI hairDescriptionText;

//     [Tooltip("Localized text keys for hair type descriptions")]
//     [SerializeField]
//     private LocalizedString[] hairTypeDescriptions;

//     [Tooltip("Buy keys for paid hair types (e.g., buy1, buy2, etc.)")]
//     [SerializeField]
//     private string[] buyKeys; // Ключи для платных причёсок (например, buy1, buy2 и т.д.)

//     private int currentIndex = 0;
//     private SimpleCharacterManager characterManager;
//     private Flowchart flowchart;

//     private Vector2 originalPosition; // Исходная позиция панели
//     private bool isSliding = false; // Флаг для предотвращения повторных нажатий

//     private Dictionary<string, string> unlockVariables = new Dictionary<string, string>(); // Словарь для хранения разблокированных причёсок

//     private const string UNLOCK_KEY = "LANA_UNLOCK_VARIABLES"; // Ключ для данных в облаке

//     public override void OnEnter()
//     {
//         // Инициализация
//         characterManager = FindObjectOfType<SimpleCharacterManager>();
//         flowchart = GetFlowchart();

//         if (characterManager == null || flowchart == null)
//         {
//             Debug.LogError("SimpleCharacterManager или Flowchart не найдены!");
//             Continue();
//             return;
//         }

//         // Проверка на наличие индексов
//         if (hairTypeIndexes == null || hairTypeIndexes.Length == 0)
//         {
//             Debug.LogError("Не указаны индексы типов причёсок!");
//             Continue();
//             return;
//         }

//         // Активируем панель Wardrobe
//         wardrobePanel.SetActive(true);

//         // Сохраняем исходную позицию панели
//         originalPosition = panelToHide.GetComponent<RectTransform>().anchoredPosition;

//         // Загружаем разблокированные причёски из облака
//         StartCoroutine(LoadUnlockVariables());
//     }

// private IEnumerator LoadUnlockVariables()
// {
//     // Инициализация Unity Services
//     yield return InitializeUnityServices();

//     // Загружаем данные из облака
//     var loadOperation = CloudSaveService.Instance.Data.LoadAsync(new HashSet<string> { UNLOCK_KEY });
//     yield return new WaitUntil(() => loadOperation.IsCompleted);

//     if (loadOperation.IsCompleted && !loadOperation.IsFaulted)
//     {
//         var data = loadOperation.Result;
//         if (data.TryGetValue(UNLOCK_KEY, out var savedData))
//         {
//             Debug.Log($"Raw data from cloud: {savedData}");

//             try
//             {
//                 // Преобразуем данные в строку (если это объект)
//                 string jsonString = savedData.ToString();

//                 // Проверяем, что данные представляют собой валидный JSON
//                 if (IsValidJson(jsonString))
//                 {
//                     // Десериализуем данные в словарь
//                     unlockVariables = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString);

//                     if (unlockVariables != null)
//                     {
//                         Debug.Log($"Parsed unlock variables: {string.Join(", ", unlockVariables.Select(kv => $"{kv.Key}={kv.Value}"))}");
//                     }
//                     else
//                     {
//                         Debug.LogError("Failed to deserialize data into dictionary. Initializing defaults.");
//                         InitializeDefaultUnlockVariables();
//                     }
//                 }
//                 else
//                 {
//                     Debug.LogError("Invalid JSON format in the cloud data. Initializing defaults.");
//                     InitializeDefaultUnlockVariables();
//                 }
//             }
//             catch (Exception jsonEx)
//             {
//                 Debug.LogError($"Parsing error: {jsonEx.Message}");
//                 InitializeDefaultUnlockVariables();
//             }
//         }
//         else
//         {
//             Debug.Log("No unlock variables found in cloud, initializing defaults");
//             InitializeDefaultUnlockVariables();
//             yield return SaveUnlockVariables();
//         }
//     }
//     else
//     {
//         Debug.LogError("Error loading cloud data");
//         InitializeDefaultUnlockVariables();
//     }

//     // Устанавливаем начальную причёску
//     SetHair(currentIndex);

//     // Назначаем обработчики для кнопок
//     leftArrowButton.onClick.RemoveAllListeners();
//     leftArrowButton.onClick.AddListener(OnLeftArrowClicked);

//     rightArrowButton.onClick.RemoveAllListeners();
//     rightArrowButton.onClick.AddListener(OnRightArrowClicked);

//     selectButton.onClick.RemoveAllListeners();
//     selectButton.onClick.AddListener(OnSelectClicked);

//     buttonDown.onClick.RemoveAllListeners();
//     buttonDown.onClick.AddListener(HidePanel);

//     buttonUp.onClick.RemoveAllListeners();
//     buttonUp.onClick.AddListener(ShowPanel);

//     buyButton.onClick.RemoveAllListeners();
//     buyButton.onClick.AddListener(OnBuyClicked);

//     Debug.Log("HairSelectionCommand инициализирован.");
// }

// private IEnumerator InitializeUnityServices()
// {
//     // Инициализация Unity Services
//     var initialization = UnityServices.InitializeAsync();
//     yield return new WaitUntil(() => initialization.IsCompleted);

//     // Анонимный вход
//     var signIn = AuthenticationService.Instance.SignInAnonymouslyAsync();
//     yield return new WaitUntil(() => signIn.IsCompleted);

//     if (signIn.IsCompleted && !signIn.IsFaulted)
//     {
//         Debug.Log("Успешный вход в Unity Services.");
//     }
//     else
//     {
//         Debug.LogError("Ошибка входа в Unity Services");
//     }
// }

//     private void InitializeDefaultUnlockVariables()
//     {
//         // Инициализация словаря значениями по умолчанию
//         unlockVariables = new Dictionary<string, string>();
//         foreach (var key in buyKeys)
//         {
//             unlockVariables[key] = "0"; // По умолчанию все причёски не разблокированы
//         }
//     }

// private IEnumerator SaveUnlockVariables()
// {
//     // Сохраняем данные в облако
//     var dataToSave = new Dictionary<string, object>
//     {
//         { UNLOCK_KEY, JsonConvert.SerializeObject(unlockVariables) }
//     };

//     var saveOperation = CloudSaveService.Instance.Data.ForceSaveAsync(dataToSave);
//     yield return new WaitUntil(() => saveOperation.IsCompleted);

//     if (saveOperation.IsCompleted && !saveOperation.IsFaulted)
//     {
//         Debug.Log("Unlock variables saved to cloud.");
//     }
//     else
//     {
//         Debug.LogError("Error saving unlock variables");
//     }
// }

//     private bool IsValidJson(string jsonString)
//     {
//         try
//         {
//             JsonConvert.DeserializeObject(jsonString);
//             return true;
//         }
//         catch
//         {
//             return false;
//         }
//     }

//     private void SetHair(int index)
//     {
//         if (index < 0 || index >= hairTypeIndexes.Length)
//         {
//             Debug.LogError("Неверный индекс причёски!");
//             return;
//         }

//         // Получаем текущий тип причёски
//         int hairTypeIndex = hairTypeIndexes[index];

//         // Получаем значения переменных из Flowchart для всех слоев
//         int typeIndex = flowchart.GetVariable<IntegerVariable>("Type").Value;
//         int makeupIndex = flowchart.GetVariable<IntegerVariable>("Makeup").Value;
//         int dressIndex = flowchart.GetVariable<IntegerVariable>("Dress").Value;
//         int ukrashenieIndex = flowchart.GetVariable<IntegerVariable>("Ukrashenie").Value;
//         int accessoriseIndex = flowchart.GetVariable<IntegerVariable>("Accessorise").Value;

//         // Устанавливаем спрайты для всех слоев
//         characterManager.SetCharacterSprite(
//             positionIndex,
//             typeIndex,
//             CharacterEmotion.Нейтральное, // Эмоция по умолчанию
//             hairTypeIndex,
//             makeupIndex,
//             dressIndex,
//             ukrashenieIndex,
//             accessoriseIndex
//         );

//         // Обновляем текстовое описание причёски
//         UpdateHairDescription(hairTypeIndex);

//         // Проверяем, является ли причёска платной и разблокирована ли она
//         if (index < buyKeys.Length && !string.IsNullOrEmpty(buyKeys[index]))
//         {
//             bool isUnlocked = unlockVariables.ContainsKey(buyKeys[index]) && unlockVariables[buyKeys[index]] == "1";
//             buyButton.gameObject.SetActive(!isUnlocked); // Показываем кнопку "Купить", если причёска не разблокирована
//         }
//         else
//         {
//             buyButton.gameObject.SetActive(false); // Скрываем кнопку "Купить" для бесплатных причёсок
//         }
//     }

//     private void UpdateHairDescription(int hairTypeIndex)
//     {
//         if (hairDescriptionText == null || hairTypeDescriptions == null || hairTypeIndex < 0 || hairTypeIndex >= hairTypeDescriptions.Length)
//         {
//             Debug.LogError("Не удалось обновить описание причёски: проверьте настройки TextMeshPro и локализованных строк.");
//             return;
//         }

//         // Получаем локализованное описание для текущего типа причёски
//         var localizedDescription = hairTypeDescriptions[hairTypeIndex];
//         StartCoroutine(LoadLocalizedText(localizedDescription));
//     }

//     private IEnumerator LoadLocalizedText(LocalizedString localizedString)
//     {
//         // Ждем завершения инициализации системы локализации
//         yield return LocalizationSettings.InitializationOperation;

//         // Получаем локализованный текст асинхронно
//         var operation = localizedString.GetLocalizedStringAsync();
//         yield return operation;

//         // Проверяем статус операции
//         if (operation.Status == AsyncOperationStatus.Succeeded)
//         {
//             hairDescriptionText.text = operation.Result;
//         }
//         else if (operation.Status == AsyncOperationStatus.Failed)
//         {
//             Debug.LogError("Ошибка загрузки локализованного текста: " + operation.OperationException.Message);
//             hairDescriptionText.text = "Error: Localization Failed";
//         }
//         else
//         {
//             Debug.LogError("Не удалось загрузить локализованный текст: операция не завершена.");
//             hairDescriptionText.text = "Error: Localization Failed";
//         }
//     }

//     private void OnLeftArrowClicked()
//     {
//         currentIndex--;
//         if (currentIndex < 0)
//         {
//             currentIndex = hairTypeIndexes.Length - 1;
//         }
//         SetHair(currentIndex);
//     }

//     private void OnRightArrowClicked()
//     {
//         currentIndex++;
//         if (currentIndex >= hairTypeIndexes.Length)
//         {
//             currentIndex = 0;
//         }
//         SetHair(currentIndex);
//     }

//     private void OnSelectClicked()
//     {
//         // Устанавливаем выбранный тип причёски в переменную Flowchart
//         IntegerVariable hairVariable = flowchart.GetVariable<IntegerVariable>("Hair");
//         if (hairVariable != null)
//         {
//             hairVariable.Value = hairTypeIndexes[currentIndex];
//             Debug.Log($"Выбран тип причёски: {hairVariable.Value}");
//         }
//         else
//         {
//             Debug.LogError("Переменная 'Hair' не найдена в Flowchart!");
//         }

//         // Завершаем команду
//         Continue();
//     }

//     private void OnBuyClicked()
//     {
//         // Покупка причёски
//         if (currentIndex < buyKeys.Length && !string.IsNullOrEmpty(buyKeys[currentIndex]))
//         {
//             StartCoroutine(UnlockHair(buyKeys[currentIndex]));
//         }
//     }

//     private IEnumerator UnlockHair(string buyKey)
//     {
//         // Обновляем локальный словарь
//         unlockVariables[buyKey] = "1";

//         // Сохраняем данные в облако
//         yield return SaveUnlockVariables();

//         // Скрываем кнопку "Купить"
//         buyButton.gameObject.SetActive(false);

//         Debug.Log($"Причёска {buyKey} успешно разблокирована.");
//     }

//     private void HidePanel()
//     {
//         if (isSliding) return; // Если уже идет анимация, пропускаем
//         isSliding = true;

//         // Скрываем панель (перемещаем вниз)
//         StartCoroutine(SlidePanel(Vector2.down * slideDistance, () =>
//         {
//             buttonDown.gameObject.SetActive(false); // Скрываем ButtonDown
//             buttonUp.gameObject.SetActive(true); // Показываем ButtonUp
//             isSliding = false;
//             Debug.Log("Panel hidden.");
//         }));
//     }

//     private void ShowPanel()
//     {
//         if (isSliding) return; // Если уже идет анимация, пропускаем
//         isSliding = true;

//         // Показываем панель (перемещаем вверх)
//         StartCoroutine(SlidePanel(Vector2.zero, () =>
//         {
//             buttonDown.gameObject.SetActive(true); // Показываем ButtonDown
//             buttonUp.gameObject.SetActive(false); // Скрываем ButtonUp
//             isSliding = false;
//             Debug.Log("Panel shown.");
//         }));
//     }

//     private IEnumerator SlidePanel(Vector2 targetOffset, System.Action onComplete)
//     {
//         Debug.Log("SlidePanel started.");
//         Vector2 startPosition = panelToHide.GetComponent<RectTransform>().anchoredPosition;
//         Vector2 targetPosition = originalPosition + targetOffset;

//         Debug.Log($"Start position: {startPosition}, Target position: {targetPosition}");

//         float elapsedTime = 0f;
//         while (elapsedTime < slideDuration)
//         {
//             elapsedTime += Time.deltaTime;
//             panelToHide.GetComponent<RectTransform>().anchoredPosition = Vector2.Lerp(startPosition, targetPosition, elapsedTime / slideDuration);
//             Debug.Log($"Current position: {panelToHide.GetComponent<RectTransform>().anchoredPosition}");
//             yield return null;
//         }

//         panelToHide.GetComponent<RectTransform>().anchoredPosition = targetPosition;
//         Debug.Log("SlidePanel finished.");
//         onComplete?.Invoke();
//     }

//     public override string GetSummary()
//     {
//         return "Displays hair selection UI with left/right arrows, a select button, and up/down buttons to hide/show the panel. Includes localized text for hair types and paid hair options.";
//     }

//     public override Color GetButtonColor()
//     {
//         return new Color32(255, 200, 150, 255);
//     }
// }