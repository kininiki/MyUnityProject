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
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

[CommandInfo("Character",
    "Dress Selection",
    "Displays dress selection UI with prices, buy options and navigation")]
public class DressSelectionCommand : Command
{
    [Header("Dress Settings")]
    [SerializeField] private int[] dressTypeIndexes;
    [SerializeField] private string[] buyKeys;
    [SerializeField] private int[] dressPrices;
    [SerializeField] private int buyAllPrice = 1000;
    [SerializeField] private int buyAllIndex = -1;

    [Header("UI References")]
    [SerializeField] private GameObject wardrobePanel;
    [SerializeField] private GameObject panelToHide;
    [SerializeField] private Button leftArrowButton;
    [SerializeField] private Button rightArrowButton;
    [SerializeField] private Button selectButton;
    [SerializeField] private Button buttonDown;
    [SerializeField] private Button buttonUp;
    [SerializeField] private Button buyButton;
    [SerializeField] private TextMeshProUGUI dressDescriptionText;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private LocalizedString[] dressTypeDescriptions;
    [SerializeField] private RawImage buyAllPreviewImage;

    [Header("Character Parts")]
    [SerializeField] private RawImage[] characterParts = new RawImage[6];

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

    private void SetDress(int index)
    {
        Debug.Log($"Setting dress: index={index}, dressTypeIndex={(index >= 0 && index < dressTypeIndexes.Length ? dressTypeIndexes[index].ToString() : "invalid")}");
        
        if (index < 0 || index >= dressTypeIndexes.Length) return;

        isBuyAllPage = (index == buyAllIndex);
        selectButton.gameObject.SetActive(!isBuyAllPage);

        if (!isBuyAllPage)
        {
            if (buyAllPreviewImage != null)
            {
                buyAllPreviewImage.gameObject.SetActive(false);
            }

            StartCoroutine(FadeCharacter(visibleAlpha));

            int dressTypeIndex = dressTypeIndexes[index];

            if (characterManager.dressSprites[dressTypeIndex].OperationHandle.IsValid())
            {
                Addressables.Release(characterManager.dressSprites[dressTypeIndex].OperationHandle);
            }

            Debug.Log($"Updating character with dress type: {dressTypeIndex}");
            
            // Исправленный порядок параметров:
            characterManager.SetCharacterSprite(
                positionIndex,
                flowchart.GetVariable<IntegerVariable>("Type").Value,
                CharacterEmotion.Нейтральное,
                flowchart.GetVariable<IntegerVariable>("Hair").Value,       // hairIndex
                flowchart.GetVariable<IntegerVariable>("Makeup").Value,    // makeupIndex
                dressTypeIndex,                                            // dressIndex
                flowchart.GetVariable<IntegerVariable>("Ukrashenie").Value,
                flowchart.GetVariable<IntegerVariable>("Accessorise").Value
            );
            characterManager.ForceUpdateDress(positionIndex, dressTypeIndex); // Дополнительный принудительный вызов

            UpdateDressDescription(index);
            UpdatePriceDisplay(index);
        }
        else
        {
            if (buyAllPreviewImage != null)
            {
                buyAllPreviewImage.gameObject.SetActive(true);
            }

            StartCoroutine(FadeCharacter(hiddenAlpha));

            UpdateDressDescriptionForBuyAll();
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

        SetDress(currentIndex);
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
        priceText.gameObject.SetActive(isPaid && !isUnlocked && index < dressPrices.Length);

        if (priceText.gameObject.activeSelf)
        {
            priceText.text = dressPrices[index].ToString();
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

    private void UpdateDressDescription(int index)
    {
        if (index >= 0 && index < dressTypeDescriptions.Length)
        {
            StartCoroutine(LoadLocalizedText(dressTypeDescriptions[index]));
        }
    }

    private void UpdateDressDescriptionForBuyAll()
    {
        if (buyAllIndex >= 0 && buyAllIndex < dressTypeDescriptions.Length)
        {
            StartCoroutine(LoadLocalizedText(dressTypeDescriptions[buyAllIndex]));
        }
        else
        {
            dressDescriptionText.text = "Купить всю платную одежду";
        }
    }

    private IEnumerator LoadLocalizedText(LocalizedString localizedString)
    {
        yield return LocalizationSettings.InitializationOperation;
        var operation = localizedString.GetLocalizedStringAsync();
        yield return operation;

        if (operation.Status == AsyncOperationStatus.Succeeded)
        {
            dressDescriptionText.text = operation.Result;
        }
        else
        {
            dressDescriptionText.text = "Error: Localization Failed";
        }
    }

    private void OnLeftArrowClicked()
    {
        currentIndex--;
        if (currentIndex < 0) currentIndex = dressTypeIndexes.Length - 1;
        SetDress(currentIndex);
    }

    private void OnRightArrowClicked()
    {
        currentIndex++;
        if (currentIndex >= dressTypeIndexes.Length) currentIndex = 0;
        SetDress(currentIndex);
    }

    private void OnSelectClicked()
    {
        IntegerVariable dressVariable = flowchart.GetVariable<IntegerVariable>("Dress");
        Debug.Log($"Before setting Dress: {dressVariable.Value}"); // Логируем текущее значение
        dressVariable.Value = dressTypeIndexes[currentIndex];
        Debug.Log($"After setting Dress: {dressVariable.Value}"); // Логируем новое значение
        Continue();
    }

    private void OnBuyClicked()
    {
        if (isBuyAllPage)
        {
            StartCoroutine(UnlockAllDresses());
        }
        else if (currentIndex < buyKeys.Length && !string.IsNullOrEmpty(buyKeys[currentIndex]))
        {
            StartCoroutine(UnlockDress(buyKeys[currentIndex]));
        }
    }

    private IEnumerator UnlockAllDresses()
    {
        if (playerCurrency["ruby"] < buyAllPrice)
        {
            Debug.Log("Not enough rubies to buy all dresses");
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

    private IEnumerator UnlockDress(string buyKey)
    {
        int price = dressPrices[currentIndex];
        
        if (playerCurrency["ruby"] < price)
        {
            Debug.Log("Not enough rubies to buy this dress");
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
        return "Dress selection with prices and buy options";
    }

    public override Color GetButtonColor()
    {
        return new Color32(255, 200, 150, 255);
    }
}