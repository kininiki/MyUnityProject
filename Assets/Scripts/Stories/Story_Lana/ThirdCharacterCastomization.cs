using UnityEngine;
using UnityEngine.UI;
using Fungus;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Unity.Services.CloudSave;
using Unity.Services.Core;
using Unity.Services.Authentication;

public static class ThirdCharacterCloudUtils
{
    public const string SAVE_KEY = "LANA_WARDROBE_DATA";

    public static IEnumerator InitializeServices()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            var init = UnityServices.InitializeAsync();
            yield return new WaitUntil(() => init.IsCompleted);
        }
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            var signIn = AuthenticationService.Instance.SignInAnonymouslyAsync();
            yield return new WaitUntil(() => signIn.IsCompleted);
        }
    }

    public static IEnumerator LoadData(System.Action<JObject> onLoaded)
    {
        var loadOperation = CloudSaveService.Instance.Data.LoadAsync(new HashSet<string> { SAVE_KEY });
        yield return new WaitUntil(() => loadOperation.IsCompleted);

        JObject json = new JObject();
        if (loadOperation.Result != null && loadOperation.Result.TryGetValue(SAVE_KEY, out var savedData) && !string.IsNullOrEmpty(savedData.ToString()))
        {
            json = JObject.Parse(savedData.ToString());
        }
        onLoaded?.Invoke(json);
    }

    public static IEnumerator SaveData(JObject json)
    {
        var dataToSave = new Dictionary<string, object> { { SAVE_KEY, json.ToString() } };
        var saveOperation = CloudSaveService.Instance.Data.ForceSaveAsync(dataToSave);
        yield return new WaitUntil(() => saveOperation.IsCompleted);
    }
}

[CommandInfo("Character", "Set Character Sprite 3", "Displays a third-type character using the saved type selection")]
public class SetThirdCharacterSpriteCommand : Command
{
    [SerializeField] private string characterName;
    [SerializeField] private int positionIndex;
    [SerializeField] private string emotionKey;

    public override void OnEnter()
    {
        StartCoroutine(LoadAndShow());
    }

    private IEnumerator LoadAndShow()
    {
        yield return ThirdCharacterCloudUtils.InitializeServices();

        JObject json = null;
        yield return ThirdCharacterCloudUtils.LoadData(j => json = j);

        ThirdCharacterManager manager = Object.FindObjectOfType<ThirdCharacterManager>();
        if (manager == null)
        {
            Debug.LogError("ThirdCharacterManager not found");
            Continue();
            yield break;
        }

        int typeIndex = 0;
        if (json != null && json.TryGetValue(characterName, out JToken typeToken))
        {
            typeIndex = typeToken.ToObject<int>();
        }

        // Ensure layers are visible before setting sprites
        if (positionIndex >= 0 && positionIndex < manager.characterPositions.Length)
        {
            manager.characterPositions[positionIndex].color = new Color(1, 1, 1, 1);
            manager.emotionLayers[positionIndex].color = new Color(1, 1, 1, 1);
            manager.dressLayers[positionIndex].color = new Color(1, 1, 1, 1);
        }

        manager.SetCharacterSprite(characterName, positionIndex, typeIndex, emotionKey, 0);
        Continue();
    }

    public override string GetSummary()
    {
        return $"Show third-type character '{characterName}' with saved type";
    }
}

[CommandInfo("Character", "Type Selection 3", "Selects a character type for a third-type character")]
public class ThirdTypeSelectionCommand : Command
{
    [SerializeField] private string characterName;
    [SerializeField] private int positionIndex;
    [SerializeField] private string emotionKey;

    [Header("UI References")]
    [SerializeField] private GameObject wardrobePanel;
    [SerializeField] private GameObject panelToHide;
    [SerializeField] private Button leftButton;
    [SerializeField] private Button rightButton;
    [SerializeField] private Button selectButton;
    [SerializeField] private Button buttonDown;
    [SerializeField] private Button buttonUp;

    [Header("Slide Settings")]
    [SerializeField] private float slideDuration = 0.5f;
    [SerializeField] private float slideDistance = 200f;
    [SerializeField] private float fadeDuration = 0.5f;

    private int currentIndex;
    private ThirdCharacterManager manager;
    private JObject cloudData;
    private Vector2 originalPosition;
    private bool isSliding = false;

    public override void OnEnter()
    {
        manager = Object.FindObjectOfType<ThirdCharacterManager>();
        if (manager == null)
        {
            Debug.LogError("ThirdCharacterManager not found");
            Continue();
            return;
        }

        wardrobePanel.SetActive(true);
        originalPosition = panelToHide.GetComponent<RectTransform>().anchoredPosition;

        leftButton.gameObject.SetActive(true);
        rightButton.gameObject.SetActive(true);
        selectButton.gameObject.SetActive(true);
        buttonDown.gameObject.SetActive(true);
        buttonUp.gameObject.SetActive(false);

        StartCoroutine(Setup());
    }

    public override void OnExit()
    {
        wardrobePanel.SetActive(false);
    }

    private IEnumerator Setup()
    {
        yield return ThirdCharacterCloudUtils.InitializeServices();
        yield return ThirdCharacterCloudUtils.LoadData(j => cloudData = j);

        currentIndex = cloudData != null && cloudData.TryGetValue(characterName, out JToken token) ? token.ToObject<int>() : 0;
        UpdatePreview();

        leftButton.onClick.RemoveAllListeners();
        rightButton.onClick.RemoveAllListeners();
        selectButton.onClick.RemoveAllListeners();
        buttonDown.onClick.RemoveAllListeners();
        buttonUp.onClick.RemoveAllListeners();

        leftButton.onClick.AddListener(Prev);
        rightButton.onClick.AddListener(Next);
        selectButton.onClick.AddListener(Select);
        buttonDown.onClick.AddListener(HidePanel);
        buttonUp.onClick.AddListener(ShowPanel);
    }

    private void Prev()
    {
        currentIndex--;
        int total = manager.GetCharacterByName(characterName).characterTypes.Length;
        if (currentIndex < 0) currentIndex = total - 1;
        UpdatePreview();
    }

    private void Next()
    {
        currentIndex++;
        int total = manager.GetCharacterByName(characterName).characterTypes.Length;
        if (currentIndex >= total) currentIndex = 0;
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        if (positionIndex >= 0 && positionIndex < manager.characterPositions.Length)
        {
            manager.characterPositions[positionIndex].color = new Color(1, 1, 1, 1);
            manager.emotionLayers[positionIndex].color = new Color(1, 1, 1, 1);
            manager.dressLayers[positionIndex].color = new Color(1, 1, 1, 1);
        }
        manager.SetCharacterSprite(characterName, positionIndex, currentIndex, emotionKey, 0);
    }

    private void Select()
    {
        leftButton.onClick.RemoveAllListeners();
        rightButton.onClick.RemoveAllListeners();
        selectButton.onClick.RemoveAllListeners();
        buttonDown.onClick.RemoveAllListeners();
        buttonUp.onClick.RemoveAllListeners();
        StartCoroutine(SaveAndContinue());
    }

    private IEnumerator SaveAndContinue()
    {
        if (cloudData == null) cloudData = new JObject();
        cloudData[characterName] = currentIndex;
        yield return ThirdCharacterCloudUtils.SaveData(cloudData);

        yield return FadeOutCharacter(positionIndex, fadeDuration);

        Continue();
    }

    private IEnumerator FadeOutCharacter(int index, float duration)
    {
        if (manager == null) yield break;
        if (index < 0 || index >= manager.characterPositions.Length) yield break;

        RawImage baseLayer = manager.characterPositions[index];
        RawImage emotionLayer = manager.emotionLayers[index];
        RawImage dressLayer = manager.dressLayers[index];

        Color bc = baseLayer.color;
        Color ec = emotionLayer.color;
        Color dc = dressLayer.color;

        float t = 0f;
        while (t < duration)
        {
            float a = Mathf.Lerp(1f, 0f, t / duration);
            baseLayer.color = new Color(bc.r, bc.g, bc.b, a);
            emotionLayer.color = new Color(ec.r, ec.g, ec.b, a);
            dressLayer.color = new Color(dc.r, dc.g, dc.b, a);
            t += Time.deltaTime;
            yield return null;
        }

        baseLayer.color = new Color(bc.r, bc.g, bc.b, 0f);
        emotionLayer.color = new Color(ec.r, ec.g, ec.b, 0f);
        dressLayer.color = new Color(dc.r, dc.g, dc.b, 0f);
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

    private IEnumerator SlidePanel(Vector2 targetOffset, System.Action onComplete)
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
        return $"Type selection for third-type character '{characterName}'";
    }
}