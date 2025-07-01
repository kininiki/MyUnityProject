using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fungus;
using System;
using System.Collections;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;


[Serializable]
public class DialogueBoxStyle
{
    public string name;
    
    [Header("Background (Addressable)")]
    public AssetReferenceT<Sprite> backgroundAddressable; // Addressable ссылка
    
    [NonSerialized] public Sprite loadedBackground; // Для временного хранения загруженного фона

    public TMP_FontAsset font;
    public int fontSize = 14;
    public Color textColor = Color.black;
    public FontStyles fontStyle = FontStyles.Normal;
    public TextAlignmentOptions alignment = TextAlignmentOptions.Center;
    public float typewriterSpeedMultiplier = 1f;
    public AudioClip typewriterSound;

    // Метод очистки загруженного ресурса
    public void ReleaseLoadedBackground()
    {
        if (loadedBackground != null && backgroundAddressable.IsValid())
        {
            Addressables.Release(loadedBackground);
            loadedBackground = null;
        }
    }
}

public class DialogueManager : MonoBehaviour
{
    private Canvas mainCanvas;

    [Header("Typewriter Effect")]
    public float baseTypingSpeed = 0.05f;
    public bool IsTyping { get; private set; }
    private Coroutine typewriterCoroutine;

    [Header("Typewriter Effect Settings")]
    public bool enableTypewriterEffect = true;

    [Serializable]
    public class DialogueBox
    {
        public string name;
        public GameObject dialogueBoxObject;
        public Image backgroundImage;
        public TextMeshProUGUI generalTextComponent;
        public TextMeshProUGUI nameTextComponent;
        public DialogueBoxStyle[] generalTextStyles;
        public DialogueBoxStyle[] nameTextStyles;

        [Header("Typewriter Effect Settings")]
        public bool enableTypewriterEffect = true; // Флаг для включения/отключения эффекта печати
        
        [Header("Animation")]
        public bool useAnimatedAppearance = false;
        public float appearanceDuration = 0.5f;
        public AnimationCurve appearanceCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    }

    public DialogueBox[] dialogueBoxes;
    private Coroutine autoHideCoroutine;
    private Flowchart currentFlowchart;
    private AudioSource audioSource;

    private void Start()
    {
        currentFlowchart = FindObjectOfType<Flowchart>();
        audioSource = gameObject.AddComponent<AudioSource>();
        
        mainCanvas = GetComponentInParent<Canvas>();
        if (mainCanvas == null)
        {
            mainCanvas = FindObjectOfType<Canvas>();
        }
        
        if (mainCanvas == null)
        {
            Debug.LogError("No Canvas found in the scene!");
            return;
        }

        // Инициализируем локализацию
        StartCoroutine(InitializeLocalization());
    }

    private IEnumerator InitializeLocalization()
    {
        yield return LocalizationSettings.InitializationOperation;
    }

    public void SetStyle(DialogueBox dialogueBox, int generalStyleIndex, int nameStyleIndex)
    {
        SetTextComponentStyle(dialogueBox.generalTextComponent, dialogueBox.generalTextStyles, generalStyleIndex, "general text");
        SetTextComponentStyle(dialogueBox.nameTextComponent, dialogueBox.nameTextStyles, nameStyleIndex, "name text");

        if (generalStyleIndex >= 0 && generalStyleIndex < dialogueBox.generalTextStyles.Length)
        {
            var style = dialogueBox.generalTextStyles[generalStyleIndex];

            // Загружаем фоновое изображение через Addressables
            if (style.backgroundAddressable != null && style.backgroundAddressable.RuntimeKeyIsValid())
            {
                StartCoroutine(LoadAndSetBackground(dialogueBox.backgroundImage, style));
            }
        }
    }

    // Корутина для загрузки и установки фона
    private IEnumerator LoadAndSetBackground(Image backgroundImage, DialogueBoxStyle style)
    {
        // Очистить предыдущий загруженный фон
        style.ReleaseLoadedBackground();

        AsyncOperationHandle<Sprite> handle = style.backgroundAddressable.LoadAssetAsync<Sprite>();
        yield return handle;

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            style.loadedBackground = handle.Result;
            backgroundImage.sprite = style.loadedBackground;
        }
        else
        {
            Debug.LogError($"Failed to load background from Addressable: {style.backgroundAddressable}");
        }
    }

    private void SetTextComponentStyle(TextMeshProUGUI textComponent, DialogueBoxStyle[] styles, int styleIndex, string componentName)
    {
        if (styleIndex < 0 || styleIndex >= styles.Length)
        {
            Debug.LogError($"Invalid style index for {componentName}");
            return;
        }

        var style = styles[styleIndex];
        textComponent.font = style.font;
        textComponent.fontSize = style.fontSize;
        textComponent.color = style.textColor;
        textComponent.fontStyle = style.fontStyle;
        textComponent.alignment = style.alignment;
        textComponent.richText = true;
    }

    public IEnumerator SetText(DialogueBox dialogueBox, string generalText, string nameText, bool useTypewriterEffect = true)
    {
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
        }

        dialogueBox.nameTextComponent.text = nameText;

        // Проверяем, включен ли эффект печати
        if (useTypewriterEffect)
        {
            typewriterCoroutine = StartCoroutine(TypewriterEffect(dialogueBox, generalText));
        }
        else
        {
            dialogueBox.generalTextComponent.text = generalText;
            dialogueBox.generalTextComponent.maxVisibleCharacters = generalText.Length;
            IsTyping = false;
        }

        // Ждем завершения печати, если он включен
        while (IsTyping)
        {
            yield return null;
        }
    }


    private IEnumerator TypewriterEffect(DialogueBox dialogueBox, string fullText)
    {
        IsTyping = true;
        var textComponent = dialogueBox.generalTextComponent;

        // Устанавливаем полный текст
        textComponent.text = fullText;

        // Если эффект печати отключен, просто показываем весь текст сразу
        if (!enableTypewriterEffect)
        {
            textComponent.maxVisibleCharacters = fullText.Length;
            IsTyping = false; // Завершаем печать сразу
            yield break; // Прерываем корутину
        }

        // Если эффект печати включен, начинаем анимацию по символам
        textComponent.maxVisibleCharacters = 0;  // Устанавливаем видимые символы на 0

        int visibleCount = 0;
        var currentStyle = dialogueBox.generalTextStyles[0];
        float currentTypingSpeed = baseTypingSpeed / currentStyle.typewriterSpeedMultiplier;

        // Основной цикл печати по одному символу
        while (visibleCount < fullText.Length)
        {
            textComponent.maxVisibleCharacters = visibleCount + 1;  // Увеличиваем количество видимых символов
            visibleCount++;

            // Проигрываем звук печати, если он установлен, и текст не внутри тега разметки
            if (currentStyle.typewriterSound != null && !IsInsideRichTextTag(fullText, visibleCount - 1))
            {
                audioSource.PlayOneShot(currentStyle.typewriterSound);
            }

            yield return new WaitForSeconds(currentTypingSpeed);
        }

        IsTyping = false;  // Сбрасываем флаг печати по завершении
    }



    private bool IsInsideRichTextTag(string text, int position)
    {
        int lastOpenBracket = text.LastIndexOf('<', position);
        if (lastOpenBracket == -1) return false;
        
        int lastCloseBracket = text.LastIndexOf('>', position);
        return lastOpenBracket > lastCloseBracket;
    }

    public void ShowDialogueBox(string dialogueBoxName, float duration = 0f, bool hideOnTap = false)
    {
        var dialogueBox = Array.Find(dialogueBoxes, box => box.name == dialogueBoxName);
        if (dialogueBox != null)
        {
            dialogueBox.dialogueBoxObject.SetActive(true);
            
            if (dialogueBox.useAnimatedAppearance)
            {
                StartCoroutine(AnimateDialogueBoxAppearance(dialogueBox));
            }
            
            if (dialogueBox.backgroundImage != null)
            {
                dialogueBox.backgroundImage.gameObject.SetActive(true);
            }

            if (autoHideCoroutine != null)
            {
                StopCoroutine(autoHideCoroutine);
            }

            if (duration > 0)
            {
                autoHideCoroutine = StartCoroutine(AutoHideDialogueBox(dialogueBox, duration));
            }

            if (hideOnTap)
            {
                StartCoroutine(WaitForTap(dialogueBox));
            }
        }
        else
        {
            Debug.LogError($"Dialogue box '{dialogueBoxName}' not found");
        }
    }

    private IEnumerator AnimateDialogueBoxAppearance(DialogueBox dialogueBox)
    {
        float elapsed = 0f;
        CanvasGroup canvasGroup = dialogueBox.dialogueBoxObject.GetComponent<CanvasGroup>();
        
        if (canvasGroup == null)
        {
            canvasGroup = dialogueBox.dialogueBoxObject.AddComponent<CanvasGroup>();
        }
        
        canvasGroup.alpha = 0f;
        
        while (elapsed < dialogueBox.appearanceDuration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = elapsed / dialogueBox.appearanceDuration;
            canvasGroup.alpha = dialogueBox.appearanceCurve.Evaluate(normalizedTime);
            yield return null;
        }
        
        canvasGroup.alpha = 1f;
    }

    private IEnumerator WaitForTap(DialogueBox dialogueBox)
    {
        while (true)
        {
            if ((Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)) 
                && !IsTyping)
            {
                HideDialogueBox(dialogueBox);
                yield break;
            }
            else if ((Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)) 
                     && IsTyping)
            {
                if (typewriterCoroutine != null)
                {
                    StopCoroutine(typewriterCoroutine);
                }
                var textComponent = dialogueBox.generalTextComponent;
                textComponent.maxVisibleCharacters = textComponent.text.Length;
                IsTyping = false;
            }
            yield return null;
        }
    }

    public void HideDialogueBox(DialogueBox dialogueBox)
    {
        if (dialogueBox != null && dialogueBox.dialogueBoxObject != null)
        {
            dialogueBox.dialogueBoxObject.SetActive(false);

            // Очистка загруженных фонов для всех стилей
            foreach (var style in dialogueBox.generalTextStyles)
            {
                style.ReleaseLoadedBackground();
            }

            if (autoHideCoroutine != null)
            {
                StopCoroutine(autoHideCoroutine);
                autoHideCoroutine = null;
            }
        }
    }


    public IEnumerator AutoHideDialogueBox(DialogueBox dialogueBox, float duration)
    {
        yield return new WaitForSeconds(duration);
        HideDialogueBox(dialogueBox);
    }
}

[Serializable]
public class LocalizedDialogueText
{
    public string tableReference = "DialogueTable"; // Название таблицы локализации по умолчанию
    public string entryReference; // Ключ для конкретной строки
}





[CommandInfo("Dialogue", "Show Dialogue Box", "Показывает диалоговую плашку с заданными параметрами")]
public class ShowDialogueBoxCommand : Command
{
    [SerializeField] protected DialogueManager dialogueManager;
    [SerializeField] protected string dialogueBoxName;
    [SerializeField] protected int generalTextStyleIndex;
    [SerializeField] protected int nameTextStyleIndex;
    
    [Header("Localized Text")]
    public LocalizedString generalText = new LocalizedString() { 
        TableReference = "DialogueTable", // Название таблицы локализации по умолчанию
        TableEntryReference = "default_text" // Ключ по умолчанию
    };
    public LocalizedString nameText = new LocalizedString() {
        TableReference = "DialogueTable", // Название таблицы локализации по умолчанию
        TableEntryReference = "default_name" // Ключ по умолчанию
    };
    
    [SerializeField] protected float duration = 0f;
    [SerializeField] protected bool hideOnTap = false;

    private bool isWaitingForCompletion = false;

    public override void OnEnter()
    {
        if (dialogueManager == null)
        {
            Debug.LogError("DialogueManager не назначен!");
            Continue();
            return;
        }

        var dialogueBox = Array.Find(dialogueManager.dialogueBoxes, box => box.name == dialogueBoxName);
        if (dialogueBox == null)
        {
            Debug.LogError($"Dialogue box '{dialogueBoxName}' not found");
            Continue();
            return;
        }

        dialogueManager.SetStyle(dialogueBox, generalTextStyleIndex, nameTextStyleIndex);
        StartCoroutine(ShowLocalizedDialogue(dialogueBox));
    }

    private IEnumerator ShowLocalizedDialogue(DialogueManager.DialogueBox dialogueBox)
    {
        isWaitingForCompletion = true;

        // Ждем завершения инициализации системы локализации
        yield return LocalizationSettings.InitializationOperation;

        string localizedGeneralText = "";
        string localizedNameText = "";

        // Проверяем, правильно ли настроены ссылки на локализацию
        if (string.IsNullOrEmpty(generalText.TableReference))
        {
            Debug.LogError("Table Reference не установлен для General Text");
            localizedGeneralText = "Error: No Table Reference";
        }
        else
        {
            // Подготовим операцию получения локализованного текста
            var generalTextOperation = generalText.GetLocalizedStringAsync();
            yield return generalTextOperation;

            // Проверяем успешность загрузки локализованной строки
            if (generalTextOperation.IsDone)
                localizedGeneralText = generalTextOperation.Result;
            else
                localizedGeneralText = "Error: Localization Failed";
        }

        if (string.IsNullOrEmpty(nameText.TableReference))
        {
            Debug.LogError("Table Reference не установлен для Name Text");
            localizedNameText = "Error: No Table Reference";
        }
        else
        {
            // Подготовим операцию получения локализованного имени
            var nameTextOperation = nameText.GetLocalizedStringAsync();
            yield return nameTextOperation;

            // Проверяем успешность загрузки локализованной строки
            if (nameTextOperation.IsDone)
                localizedNameText = nameTextOperation.Result;
            else
                localizedNameText = "Error: Localization Failed";
        }

        // Показываем диалоговое окно
        dialogueManager.ShowDialogueBox(dialogueBoxName, duration, hideOnTap);
        yield return StartCoroutine(dialogueManager.SetText(dialogueBox, localizedGeneralText, localizedNameText));

        if (duration > 0)
        {
            yield return new WaitForSeconds(duration);
            isWaitingForCompletion = false;
            Continue();
        }
        else if (hideOnTap)
        {
            while (isWaitingForCompletion)
            {
                if ((Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)))
                {
                    if (!dialogueManager.IsTyping)
                    {
                        isWaitingForCompletion = false;
                        yield return new WaitForSeconds(0.1f);
                        Continue();
                        break;
                    }
                }
                yield return null;
            }
        }
        else
        {
            yield return new WaitForSeconds(0.1f);
            isWaitingForCompletion = false;
            Continue();
        }
    }

    public override void OnExit()
    {
        base.OnExit();
        isWaitingForCompletion = false;
        if (dialogueManager != null)
        {
            var dialogueBox = Array.Find(dialogueManager.dialogueBoxes, box => box.name == dialogueBoxName);
            if (dialogueBox != null)
            {
                dialogueManager.HideDialogueBox(dialogueBox);
            }
        }
    }
}




[CommandInfo("Dialogue", "Show Notification", "Показывает уведомление на экране с заданными параметрами и не останавливает другие команды Fungus.")]
public class NotificationCommand : Command
{
    [SerializeField] protected DialogueManager dialogueManager;
    [SerializeField] protected string dialogueBoxName; // Имя плашки уведомления
    [SerializeField] protected int generalTextStyleIndex;
    [SerializeField] protected int nameTextStyleIndex;
    public static Action OnCameraMove; // Глобальное событие на движение камеры


    [Header("Localized Text")]
    public LocalizedString generalText = new LocalizedString()
    {
        TableReference = "DialogueTable",
        TableEntryReference = "default_text"
    };
    public LocalizedString nameText = new LocalizedString()
    {
        TableReference = "DialogueTable",
        TableEntryReference = "default_name"
    };

    [SerializeField] protected float duration = 3f; // Время отображения уведомления

    public override void OnEnter()
    {
        if (dialogueManager == null)
        {
            Debug.LogError("DialogueManager не назначен!");
            Continue();
            return;
        }

        var dialogueBox = Array.Find(dialogueManager.dialogueBoxes, box => box.name == dialogueBoxName);
        if (dialogueBox == null)
        {
            Debug.LogError($"Dialogue box '{dialogueBoxName}' not found");
            Continue();
            return;
        }

        dialogueManager.SetStyle(dialogueBox, generalTextStyleIndex, nameTextStyleIndex);
        StartCoroutine(ShowNotification(dialogueBox));
        
        // Сразу передаем управление следующим командам Fungus, не дожидаясь скрытия уведомления
        Continue();
    }

    private IEnumerator ShowNotification(DialogueManager.DialogueBox dialogueBox)
    {
        // Ждем инициализации системы локализации
        yield return LocalizationSettings.InitializationOperation;

        // Локализуем текст
        string localizedGeneralText = "";
        string localizedNameText = "";

        if (!string.IsNullOrEmpty(generalText.TableReference))
        {
            var generalTextOperation = generalText.GetLocalizedStringAsync();
            yield return generalTextOperation;
            localizedGeneralText = generalTextOperation.IsDone ? generalTextOperation.Result : "Error: Localization Failed";
        }

        if (!string.IsNullOrEmpty(nameText.TableReference))
        {
            var nameTextOperation = nameText.GetLocalizedStringAsync();
            yield return nameTextOperation;
            localizedNameText = nameTextOperation.IsDone ? nameTextOperation.Result : "Error: Localization Failed";
        }

        // Показываем уведомление
        dialogueManager.ShowDialogueBox(dialogueBoxName, duration, hideOnTap: false);
        StartCoroutine(dialogueManager.SetText(dialogueBox, localizedGeneralText, localizedNameText));
        
        // Ждём duration секунд и прячем уведомление
        yield return new WaitForSeconds(duration);
        dialogueManager.HideDialogueBox(dialogueBox);
    }

    public override string GetSummary()
    {
        return $"Show Notification '{dialogueBoxName}' for {duration} seconds";
    }

    public override Color GetButtonColor()
    {
        return new Color32(184, 230, 194, 255);
    }

    
}


[CommandInfo("Dialogue", "For Choices", "Показывает плашку для выбора с заданными параметрами. Плашка исчезает при нажатии на одну из кнопок.")]
public class ForChoicesCommand : Command
{
    [SerializeField] protected DialogueManager dialogueManager;
    [SerializeField] protected string dialogueBoxName; // Имя плашки для выбора
    [SerializeField] protected int generalTextStyleIndex;
    [SerializeField] protected int nameTextStyleIndex;

    [Header("Localized Text")]
    public LocalizedString generalText = new LocalizedString()
    {
        TableReference = "DialogueTable",
        TableEntryReference = "default_text"
    };
    public LocalizedString nameText = new LocalizedString()
    {
        TableReference = "DialogueTable",
        TableEntryReference = "default_name"
    };

    [SerializeField] protected Button[] choiceButtons; // Кнопки выбора, находящиеся на другом Canvas

    private bool isWaitingForChoice = true;

    public override void OnEnter()
    {
        if (dialogueManager == null)
        {
            Debug.LogError("DialogueManager не назначен!");
            Continue();
            return;
        }

        var dialogueBox = Array.Find(dialogueManager.dialogueBoxes, box => box.name == dialogueBoxName);
        if (dialogueBox == null)
        {
            Debug.LogError($"Dialogue box '{dialogueBoxName}' not found");
            Continue();
            return;
        }

        dialogueManager.SetStyle(dialogueBox, generalTextStyleIndex, nameTextStyleIndex);
        
        // Запускаем локализацию и показ плашки
        StartCoroutine(ShowForChoices(dialogueBox));

        // Продолжаем выполнение других команд, не дожидаясь завершения
        Continue();
    }


    private IEnumerator ShowForChoices(DialogueManager.DialogueBox dialogueBox)
    {
        // Ждем инициализации системы локализации
        yield return LocalizationSettings.InitializationOperation;

        // Локализуем текст
        string localizedGeneralText = "";
        string localizedNameText = "";

        // Загружаем локализованный текст для основного текста
        if (!string.IsNullOrEmpty(generalText.TableReference))
        {
            var generalTextOperation = generalText.GetLocalizedStringAsync();
            yield return generalTextOperation;
            localizedGeneralText = generalTextOperation.IsDone ? generalTextOperation.Result : "Error: Localization Failed";
        }

        // Загружаем локализованный текст для имени
        if (!string.IsNullOrEmpty(nameText.TableReference))
        {
            var nameTextOperation = nameText.GetLocalizedStringAsync();
            yield return nameTextOperation;
            localizedNameText = nameTextOperation.IsDone ? nameTextOperation.Result : "Error: Localization Failed";
        }

        // Показываем диалоговое окно
        dialogueManager.ShowDialogueBox(dialogueBoxName, 0, hideOnTap: false);

        // Применяем текст и отключаем эффект печати
        yield return StartCoroutine(dialogueManager.SetText(dialogueBox, localizedGeneralText, localizedNameText, useTypewriterEffect: false));

        // Подписываемся на событие нажатия кнопок
        foreach (var button in choiceButtons)
        {
            button.onClick.AddListener(OnChoiceMade);
        }

        // Ждем, пока не будет сделан выбор
        while (isWaitingForChoice)
        {
            yield return null;
        }

        // Отключаем плашку после выбора
        dialogueManager.HideDialogueBox(dialogueBox);

        // Отписываемся от событий кнопок
        foreach (var button in choiceButtons)
        {
            button.onClick.RemoveListener(OnChoiceMade);
        }
    }

    private void OnChoiceMade()
    {
        // Устанавливаем флаг, чтобы завершить ожидание выбора
        isWaitingForChoice = false;
    }

    public override string GetSummary()
    {
        return $"Show For Choices '{dialogueBoxName}' until a button is pressed.";
    }

    public override Color GetButtonColor()
    {
        return new Color32(240, 180, 120, 255);
    }
}