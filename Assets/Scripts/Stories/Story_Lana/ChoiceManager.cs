using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using Fungus;


// Компонент для обработки затемнения
public class ButtonDarkenEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    private Image buttonImage;
    private Color originalColor;
    private float darkenAmount;

    public void Initialize(float darkAmount)
    {
        buttonImage = GetComponent<Image>();
        originalColor = buttonImage.color;
        darkenAmount = darkAmount;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        buttonImage.color = new Color(
            originalColor.r * (1f - darkenAmount),
            originalColor.g * (1f - darkenAmount),
            originalColor.b * (1f - darkenAmount),
            originalColor.a
        );
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        buttonImage.color = originalColor;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        buttonImage.color = new Color(
            originalColor.r * (1f - darkenAmount * 1.5f),
            originalColor.g * (1f - darkenAmount * 1.5f),
            originalColor.b * (1f - darkenAmount * 1.5f),
            originalColor.a
        );
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (RectTransformUtility.RectangleContainsScreenPoint(
            buttonImage.rectTransform,
            eventData.position,
            eventData.pressEventCamera))
        {
            OnPointerEnter(eventData);
        }
        else
        {
            buttonImage.color = originalColor;
        }
    }
}

[System.Serializable]
public class LocalizedText
{
    [TextArea(3, 10)]
    public string russian = "";
    [TextArea(3, 10)]
    public string english = "";

    public string GetLocalizedString()
    {
        // Получаем текущий язык из системы локализации
        string currentLanguage = Application.systemLanguage.ToString();
        return currentLanguage == "Russian" ? russian : english;
    }
}

[System.Serializable]
public class LocalizedButtonOption
{
    [Tooltip("Тексты кнопки на разных языках")]
    public LocalizedText buttonText;
    [Tooltip("Блок Fungus, который будет вызван при нажатии кнопки")]
    public Block targetBlock;
}

[System.Serializable]
public class ButtonVisualSettings
{
    public Sprite buttonSprite;
    public Color buttonColor = Color.white;
    public Vector2 buttonSize = new Vector2(200, 50);
    [Range(0f, 0.5f)] 
    public float darkenAmount = 0.1f;
}

[System.Serializable]
public class TextVisualSettings
{
    public TMP_FontAsset font;
    public float fontSize = 24f;
    public Color textColor = Color.black;
    public FontStyles fontStyle = FontStyles.Normal;
    public TextAlignmentOptions textAlignment = TextAlignmentOptions.Center;
    public float characterSpacing = 0f;
    public float lineSpacing = 0f;
}

public class ChoiceManager : MenuDialog, IPointerClickHandler, IPointerDownHandler
{
    [Header("Localized Options")]
    [Tooltip("Настройки локализованных кнопок")]
    [SerializeField] private LocalizedButtonOption[] buttonOptions;

    [Header("Core Components")]
    [SerializeField] private Button[] buttons;
    [SerializeField] private TMPro.TextMeshProUGUI[] buttonTexts;
    [SerializeField] private Slider timerSlider;
    [SerializeField] private bool autoSelectButton = true;

    [Header("Visual Customization")]
    [SerializeField] private ButtonVisualSettings buttonSettings;
    [SerializeField] private TextVisualSettings textSettings;

    [Header("Debug Settings")]
    [SerializeField] private bool debugMode = true;

    private int nextOptionIndex = 0;

    protected override void Awake()
    {
        base.Awake();
        cachedButtons = buttons;
        cachedSlider = timerSlider;

        if (debugMode) Debug.Log("ChoiceManager Awake called");
        
        CheckRequiredComponents();
        InitializeButtons();
        ApplyVisualSettings();
        UpdateButtonTexts();

        if (Application.isPlaying)
        {
            Clear();
        }

        CheckEventSystem();
    }

    private void CheckRequiredComponents()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("ChoiceManager: Canvas не найден в родительских объектах!");
            return;
        }

        GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
        if (raycaster == null)
        {
            Debug.LogError("ChoiceManager: GraphicRaycaster отсутствует на Canvas!");
            return;
        }

        if (!raycaster.enabled)
        {
            Debug.LogError("ChoiceManager: GraphicRaycaster отключен!");
        }

        foreach (var button in buttons)
        {
            if (button == null)
            {
                Debug.LogError("ChoiceManager: Одна из кнопок не назначена в инспекторе!");
                continue;
            }

            if (!button.GetComponent<Image>())
            {
                Debug.LogError($"ChoiceManager: На кнопке {button.name} отсутствует компонент Image!");
            }
        }
    }

    private void InitializeButtons()
    {
        if (buttons == null || buttons.Length == 0)
        {
            Debug.LogError("ChoiceManager: Массив кнопок пуст!");
            return;
        }

        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null)
            {
                Debug.LogError($"ChoiceManager: Кнопка с индексом {i} не назначена!");
                continue;
            }

            int index = i;
            buttons[i].onClick.RemoveAllListeners();
            
            buttons[i].transition = Selectable.Transition.None;

            var darkenEffect = buttons[i].gameObject.GetComponent<ButtonDarkenEffect>();
            if (darkenEffect == null)
            {
                darkenEffect = buttons[i].gameObject.AddComponent<ButtonDarkenEffect>();
            }
            darkenEffect.Initialize(buttonSettings.darkenAmount);

            buttons[i].onClick.AddListener(() => {
                if (debugMode) Debug.Log($"Клик по кнопке {index} зарегистрирован");
                HandleButtonClick(index);
            });

            AddButtonDebugger(buttons[i].gameObject);
        }
    }

    private void ApplyVisualSettings()
    {
        if (buttons == null || buttonTexts == null) return;

        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null)
            {
                Image buttonImage = buttons[i].GetComponent<Image>();
                if (buttonImage != null)
                {
                    if (buttonSettings.buttonSprite != null)
                        buttonImage.sprite = buttonSettings.buttonSprite;
                    buttonImage.color = buttonSettings.buttonColor;
                }

                RectTransform rectTransform = buttons[i].GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.sizeDelta = buttonSettings.buttonSize;
                }

                var darkenEffect = buttons[i].gameObject.GetComponent<ButtonDarkenEffect>();
                if (darkenEffect != null)
                {
                    darkenEffect.Initialize(buttonSettings.darkenAmount);
                }
            }

            if (buttonTexts[i] != null)
            {
                if (textSettings.font != null)
                    buttonTexts[i].font = textSettings.font;
                buttonTexts[i].fontSize = textSettings.fontSize;
                buttonTexts[i].color = textSettings.textColor;
                buttonTexts[i].fontStyle = textSettings.fontStyle;
                buttonTexts[i].alignment = textSettings.textAlignment;
                buttonTexts[i].characterSpacing = textSettings.characterSpacing;
                buttonTexts[i].lineSpacing = textSettings.lineSpacing;
            }
        }
    }

    private void UpdateButtonTexts()
    {
        if (buttonTexts == null || buttonOptions == null) return;

        for (int i = 0; i < buttonTexts.Length && i < buttonOptions.Length; i++)
        {
            if (buttonTexts[i] != null)
            {
                buttonTexts[i].text = buttonOptions[i].buttonText.GetLocalizedString();
            }
        }
    }

    public void OnLanguageChanged()
    {
        UpdateButtonTexts();
    }

    private void HandleButtonClick(int buttonIndex)
    {
        if (debugMode) Debug.Log($"HandleButtonClick вызван для кнопки {buttonIndex}");

        if (buttonIndex >= 0 && buttonIndex < buttonOptions.Length)
        {
            Block targetBlock = buttonOptions[buttonIndex].targetBlock;
            
            if (debugMode) Debug.Log($"Target block для кнопки {buttonIndex}: {targetBlock}");
            
            Clear();
            HideSayDialog();

            if (targetBlock != null)
            {
                var flowchart = targetBlock.GetFlowchart();
                if (flowchart != null)
                {
                    flowchart.StartCoroutine(CallBlock(targetBlock));
                }
                else
                {
                    Debug.LogError("Flowchart не найден для блока: " + targetBlock.name);
                }
            }
        }
        else
        {
            Debug.LogError($"Индекс кнопки {buttonIndex} выходит за пределы массива buttonOptions");
        }
    }

    private void AddButtonDebugger(GameObject buttonObject)
    {
        var debugger = buttonObject.GetComponent<ButtonDebugger>();
        if (debugger == null)
        {
            debugger = buttonObject.AddComponent<ButtonDebugger>();
        }
        debugger.debugMode = debugMode;
    }

    private void CheckEventSystem()
    {
        if (EventSystem.current == null)
        {
            Debug.LogError("ChoiceManager: EventSystem не найден в сцене!");
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (debugMode) Debug.Log("ChoiceManager получил OnPointerClick");
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (debugMode) Debug.Log("ChoiceManager получил OnPointerDown");
    }

    private void Update()
    {
        if (debugMode && Input.GetMouseButtonDown(0))
        {
            Vector2 mousePos = Input.mousePosition;
            Debug.Log($"Клик мыши в позиции: {mousePos}");
            
            PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
            eventDataCurrentPosition.position = mousePos;
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
            
            foreach (RaycastResult result in results)
            {
                Debug.Log($"Луч попал в объект: {result.gameObject.name}");
            }
        }
    }

    #if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            ApplyVisualSettings();
        }
    }
    #endif

    public void UpdateVisualSettings()
    {
        ApplyVisualSettings();
    }
}

// Вспомогательный класс для отладки кнопок
public class ButtonDebugger : MonoBehaviour, IPointerClickHandler, IPointerDownHandler
{
    public bool debugMode = true;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (debugMode) Debug.Log($"ButtonDebugger: Клик на кнопке {gameObject.name}");
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (debugMode) Debug.Log($"ButtonDebugger: Нажатие на кнопке {gameObject.name}");
    }
}